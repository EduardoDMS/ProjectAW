// Inicializa la vista de Proveedores y conecta sus datos con el API.
document.addEventListener("DOMContentLoaded", () => {
	const api = "/api/ProveedorApi",
		form = document.querySelector("#supplier-form"),
		table = document.querySelector("#suppliers-table"),
		modal = bootstrap.Modal.getOrCreateInstance(
			document.querySelector("#supplier-modal"),
		);
	let editingId = null;
	const alertBox = document.querySelector("#supplier-alert"),
		formAlert = document.querySelector("#supplier-form-alert"),
		feedback = document.querySelector("#supplier-feedback"),
		safe = DataTable.render.text();
	// Envía solicitudes fetch al API de Proveedores y convierte errores de respuesta.
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
			{ data: "nombreContacto", defaultContent: "—", render: safe },
			{ data: "correoContacto", defaultContent: "—", render: safe },
			{ data: "telefonoContacto", defaultContent: "—", render: safe },
			{ data: "activo", render: (v) => (v ? "Activo" : "Inactivo") },
			{
				data: null,
				orderable: false,
				searchable: false,
				render: (v, t, r) =>
					`<button class="btn btn-sm btn-outline-primary" data-edit="${Number(r.idProveedor)}">Editar</button> <button class="btn btn-sm btn-outline-secondary" data-toggle="${Number(r.idProveedor)}" data-active="${!r.activo}">${r.activo ? "Desactivar" : "Activar"}</button>`,
			},
		],
		language: {
			emptyTable: "No hay proveedores registrados.",
			search: "Buscar:",
			lengthMenu: "Mostrar _MENU_",
			info: "Mostrando _START_ a _END_ de _TOTAL_",
			paginate: { previous: "Anterior", next: "Siguiente" },
		},
	});
	// Filtra proveedores por estado usando la búsqueda de columna de DataTables.
	document.querySelector("#supplier-status").addEventListener("change", (e) =>
		dt
			.column(5)
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
	// Consulta GET /api/ProveedorApi y actualiza la tabla de proveedores.
	async function load() {
		try {
			dt.clear()
				.rows.add(await request(api))
				.draw();
			alertBox.hidden = true;
		} catch (e) {
			window.showAppError(e.message || "No se pudieron cargar los proveedores.");
		}
	}
	// Consulta tipos de documento y completa el selector del formulario de proveedores.
	async function loadTypes(selected) {
		const select = form.elements.idTipoDocumento;
		select.replaceChildren(new Option("Seleccione un tipo", ""));
		(await request("/api/TipoDocumentoApi")).forEach((x) =>
			select.add(new Option(x.documento, x.idTipoDoc)),
		);
		if (selected) select.value = selected;
	}
	document
		// Prepara y muestra el modal para registrar un proveedor.
		.querySelector("#new-supplier")
		.addEventListener("click", async () => {
			editingId = null;
			form.reset();
			form.elements.activo.checked = true;
			formAlert.hidden = true;
			document.querySelector("#supplier-modal-title").textContent =
				"Nuevo proveedor";
			try {
				await loadTypes();
				modal.show();
			} catch (error) {
				window.showAppError(
					error.message || "No se pudieron cargar los tipos de documento.",
				);
			}
		});
	// Procesa las acciones de edición y de cambio de estado de una fila.
	table.addEventListener("click", async (e) => {
		const edit = e.target.closest("[data-edit]"),
			toggle = e.target.closest("[data-toggle]");
		try {
			if (edit) {
				const x = await request(`${api}/${edit.dataset.edit}`);
				editingId = x.idProveedor;
				await loadTypes(x.idTipoDocumento);
				for (const k of [
					"razonSocial",
					"numeroDocumento",
					"direccion",
					"nombreContacto",
					"correoContacto",
					"telefonoContacto",
				])
					form.elements[k].value = x[k] || "";
				form.elements.activo.checked = x.activo;
				document.querySelector("#supplier-modal-title").textContent =
					"Editar proveedor";
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
			window.showAppError(err.message || "No se pudo completar la acción del proveedor.");
		}
	});
	// Envía POST o PUT al API con los datos validados del proveedor.
	form.addEventListener("submit", async (e) => {
		e.preventDefault();
		const payload = {
			razonSocial: form.elements.razonSocial.value.trim(),
			idTipoDocumento: Number(form.elements.idTipoDocumento.value),
			numeroDocumento: form.elements.numeroDocumento.value.trim(),
			direccion: form.elements.direccion.value.trim() || null,
			nombreContacto: form.elements.nombreContacto.value.trim() || null,
			correoContacto: form.elements.correoContacto.value.trim() || null,
			telefonoContacto:
				form.elements.telefonoContacto.value.trim() || null,
			activo: form.elements.activo.checked,
		};
		try {
			const wasEditing = editingId !== null;
			await request(editingId ? `${api}/${editingId}` : api, {
				method: editingId ? "PUT" : "POST",
				body: JSON.stringify(payload),
			});
			modal.hide();
			feedback.textContent = "Proveedor guardado.";
			await load();
			await window.showAppSuccess(
				wasEditing
					? "El proveedor se actualizó correctamente."
					: "El proveedor se creó correctamente.",
				wasEditing ? "Proveedor actualizado" : "Proveedor creado",
			);
		} catch (err) {
			window.showAppError(err.message || "No se pudo guardar el proveedor.");
		}
	});
	load();
});
