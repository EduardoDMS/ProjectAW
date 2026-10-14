// Inicializa la vista y la tabla de Clientes al completar la carga del HTML.
document.addEventListener("DOMContentLoaded", () => {
	const api = "/api/ClienteApi",
		form = document.querySelector("#client-form"),
		table = document.querySelector("#clients-table"),
		modal = bootstrap.Modal.getOrCreateInstance(
			document.querySelector("#client-modal"),
		);
	let editingId = null;
	const alertBox = document.querySelector("#client-alert"),
		formAlert = document.querySelector("#client-form-alert"),
		feedback = document.querySelector("#client-feedback"),
		safe = DataTable.render.text();
	// Envía solicitudes fetch al API de Clientes y extrae los mensajes de error.
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
			{ data: "razonSocial", render: safe },
			{
				data: null,
				render: (v, t, r) =>
					safe.display(
						`${r.tipoDocumento?.documento ?? r.idTipoDocumento} · ${r.numeroDocumento}`,
					),
			},
			{ data: "correo", defaultContent: "—", render: safe },
			{ data: "telefono", defaultContent: "—", render: safe },
			{ data: "activo", render: (v) => (v ? "Activo" : "Inactivo") },
			{
				data: null,
				orderable: false,
				searchable: false,
				render: (v, t, r) =>
					`<button class="btn btn-sm btn-outline-primary" data-edit="${Number(r.idCliente)}">Editar</button> <button class="btn btn-sm btn-outline-secondary" data-toggle="${Number(r.idCliente)}" data-active="${!r.activo}">${r.activo ? "Desactivar" : "Activar"}</button>`,
			},
		],
		language: {
			emptyTable: "No hay clientes registrados.",
			search: "Buscar:",
			lengthMenu: "Mostrar _MENU_",
			info: "Mostrando _START_ a _END_ de _TOTAL_",
			paginate: { previous: "Anterior", next: "Siguiente" },
		},
	});
	// Filtra en DataTables los clientes activos o inactivos.
	document.querySelector("#client-status").addEventListener("change", (e) =>
		dt
			.column(4)
			.search(
				e.target.value === ""
					? ""
					: e.target.value === "true"
						? "^Activo$"
						: "^Inactivo$",
				true,
				false,
			)
			.draw(),
	);
	// Consulta GET /api/ClienteApi y actualiza la tabla de clientes.
	async function load() {
		try {
			dt.clear()
				.rows.add(await request(api))
				.draw();
			alertBox.hidden = true;
		} catch (e) {
			window.showAppError(e.message || "No se pudieron cargar los clientes.");
		}
	}
	// Consulta tipos de documento y completa el selector del formulario de clientes.
	async function loadTypes(selected) {
		const select = form.elements.idTipoDocumento;
		select.replaceChildren(new Option("Seleccione un tipo", ""));
		(await request("/api/TipoDocumentoApi")).forEach((x) =>
			select.add(new Option(x.documento, x.idTipoDoc)),
		);
		if (selected) select.value = selected;
	}
	document
		// Prepara los campos y abre el modal para registrar un cliente.
		.querySelector("#new-client")
		.addEventListener("click", async () => {
			editingId = null;
			form.reset();
			form.elements.activo.checked = true;
			formAlert.hidden = true;
			document.querySelector("#client-modal-title").textContent =
				"Nuevo cliente";
			try {
				await loadTypes();
				modal.show();
			} catch (error) {
				window.showAppError(
					error.message || "No se pudieron cargar los tipos de documento.",
				);
			}
		});
	// Resuelve las acciones de editar cliente o cambiar su estado.
	table.addEventListener("click", async (e) => {
		const edit = e.target.closest("[data-edit]"),
			toggle = e.target.closest("[data-toggle]");
		try {
			if (edit) {
				const x = await request(`${api}/${edit.dataset.edit}`);
				editingId = x.idCliente;
				await loadTypes(x.idTipoDocumento);
				for (const k of [
					"razonSocial",
					"numeroDocumento",
					"correo",
					"telefono",
					"direccion",
				])
					form.elements[k].value = x[k] || "";
				form.elements.activo.checked = x.activo;
				document.querySelector("#client-modal-title").textContent =
					"Editar cliente";
				modal.show();
			}
			if (toggle) {
				await request(`${api}/${toggle.dataset.toggle}/estado`, {
					method: "PATCH",
					body: JSON.stringify(toggle.dataset.active === "true"),
				});
				await load();
			}
		} catch (err) {
			window.showAppError(err.message || "No se pudo completar la acción del cliente.");
		}
	});
	// Valida y envía los datos del cliente al API mediante POST o PUT.
	form.addEventListener("submit", async (e) => {
		e.preventDefault();
		const payload = {
			razonSocial: form.elements.razonSocial.value.trim(),
			idTipoDocumento: Number(form.elements.idTipoDocumento.value),
			numeroDocumento: form.elements.numeroDocumento.value.trim(),
			direccion: form.elements.direccion.value.trim(),
			correo: form.elements.correo.value.trim() || null,
			telefono: form.elements.telefono.value.trim() || null,
			activo: form.elements.activo.checked,
		};
		try {
			const wasEditing = editingId !== null;
			await request(editingId ? `${api}/${editingId}` : api, {
				method: editingId ? "PUT" : "POST",
				body: JSON.stringify(payload),
			});
			modal.hide();
			feedback.textContent = "Cliente guardado.";
			await load();
			await window.showAppSuccess(
				wasEditing
					? "El cliente se actualizó correctamente."
					: "El cliente se creó correctamente.",
				wasEditing ? "Cliente actualizado" : "Cliente creado",
			);
		} catch (err) {
			window.showAppError(err.message || "No se pudo guardar el cliente.");
		}
	});
	load();
});
