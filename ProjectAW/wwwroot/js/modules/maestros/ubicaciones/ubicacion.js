// Inicializa la tabla de Ubicaciones y sus controles cuando la vista está lista.
document.addEventListener("DOMContentLoaded", () => {
	const api = "/api/UbicacionApi",
		form = document.querySelector("#location-form"),
		table = document.querySelector("#locations-table");
	const modal = bootstrap.Modal.getOrCreateInstance(
		document.querySelector("#location-modal"),
	);
	const alertBox = document.querySelector("#location-alert"),
		formAlert = document.querySelector("#location-form-alert"),
		feedback = document.querySelector("#location-feedback");
	let rows = [],
		editingId = null;
	const safe = DataTable.render.text();
	// Envía solicitudes fetch a Ubicaciones y devuelve JSON o un error legible.
	const request = async (url, options = {}) => {
		const response = await fetch(url, {
			credentials: "same-origin",
			headers: {
				Accept: "application/json, application/problem+json",
				...(options.body ? { "Content-Type": "application/json" } : {}),
			},
			...options,
		});
		if (!response.ok) {
			const p = await response.json().catch(() => ({}));
			throw new Error(window.getApiErrorMessage(response, p));
		}
		return response.status === 204 ? null : response.json();
	};
	const dt = new DataTable(table, {
		data: [],
		columns: [
			{ data: "codigo", render: safe },
			{ data: "descripcion", defaultContent: "—", render: safe },
			{
				data: "almacen",
				render: (v, t, r) =>
					safe.display(r.almacen?.codigo ?? r.idAlmacen ?? "—"),
			},
			{ data: "activo", render: (v) => (v ? "Activa" : "Inactiva") },
			{
				data: null,
				orderable: false,
				searchable: false,
				render: (v, t, r) =>
					`<button class="btn btn-sm btn-outline-primary" data-edit="${Number(r.idUbicacion)}">Editar</button> <button class="btn btn-sm btn-outline-secondary" data-toggle="${Number(r.idUbicacion)}" data-active="${!r.activo}">${r.activo ? "Desactivar" : "Activar"}</button>`,
			},
		],
		language: {
			emptyTable: "No hay ubicaciones registradas.",
			search: "Buscar:",
			lengthMenu: "Mostrar _MENU_",
			info: "Mostrando _START_ a _END_ de _TOTAL_",
			paginate: { previous: "Anterior", next: "Siguiente" },
		},
	});
	// Filtra la tabla por el estado seleccionado sin volver a consultar el API.
	document.querySelector("#location-status").addEventListener("change", (e) =>
		dt
			.column(3)
			.search(
				e.target.value === ""
					? ""
					: e.target.value === "true"
						? "^Activa$"
						: "^Inactiva$",
				true,
				false,
			)
			.draw(),
	);
	// Consulta las ubicaciones y actualiza la tabla DataTables.
	async function load() {
		try {
			rows = await request(api);
			dt.clear().rows.add(rows).draw();
			alertBox.hidden = true;
		} catch (e) {
			window.showAppError(e.message || "No se pudieron cargar las ubicaciones.");
		}
	}
	// Consulta las ubicaciones y actualiza la tabla DataTables.
	// Consulta almacenes del API y llena el selector de ubicación.
	async function loadWarehouses(selected) {
		const list = await request("/api/AlmacenApi"),
			select = form.elements.idAlmacen;
		select.replaceChildren(new Option("Seleccione un almacén", ""));
		list.forEach((x) => select.add(new Option(x.codigo, x.idAlmacen)));
		if (selected) select.value = selected;
	}
	document
		// Prepara el modal y carga almacenes para crear una ubicación.
		.querySelector("#new-location")
		.addEventListener("click", async () => {
			editingId = null;
			form.reset();
			form.elements.activo.checked = true;
			formAlert.hidden = true;
			document.querySelector("#location-modal-title").textContent =
				"Nueva ubicación";
			try {
				await loadWarehouses();
				modal.show();
			} catch (error) {
				window.showAppError(
					error.message || "No se pudieron cargar los almacenes.",
				);
			}
		});
	// Resuelve las acciones de editar ubicación o cambiar su estado.
	table.addEventListener("click", async (e) => {
		const edit = e.target.closest("[data-edit]"),
			toggle = e.target.closest("[data-toggle]");
		try {
			if (edit) {
				const x = await request(`${api}/${edit.dataset.edit}`);
				editingId = x.idUbicacion;
				form.elements.codigo.value = x.codigo;
				form.elements.descripcion.value = x.descripcion || "";
				form.elements.activo.checked = x.activo;
				await loadWarehouses(x.idAlmacen);
				document.querySelector("#location-modal-title").textContent =
					"Editar ubicación";
				modal.show();
			}
			if (toggle) {
				await request(
					`${api}/${toggle.dataset.toggle}/estado?activo=${toggle.dataset.active}`,
					{ method: "PATCH" },
				);
				await load();
			}
		} catch (err) {
			window.showAppError(err.message || "No se pudo completar la acción de la ubicación.");
		}
	});
	// Envía los datos de ubicación por POST o PUT al endpoint del módulo.
	form.addEventListener("submit", async (e) => {
		e.preventDefault();
		const payload = {
			codigo: form.elements.codigo.value.trim(),
			descripcion: form.elements.descripcion.value.trim() || null,
			idAlmacen: Number(form.elements.idAlmacen.value),
			activo: form.elements.activo.checked,
		};
		try {
			const wasEditing = editingId !== null;
			await request(editingId ? `${api}/${editingId}` : api, {
				method: editingId ? "PUT" : "POST",
				body: JSON.stringify(payload),
			});
			modal.hide();
			feedback.textContent = "Ubicación guardada.";
			await load();
			await window.showAppSuccess(
				wasEditing
					? "La ubicación se actualizó correctamente."
					: "La ubicación se creó correctamente.",
				wasEditing ? "Ubicación actualizada" : "Ubicación creada",
			);
		} catch (err) {
			window.showAppError(err.message || "No se pudo guardar la ubicación.");
		}
	});
	load();
});
