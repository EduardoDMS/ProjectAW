// Inicializa la consulta y las acciones disponibles para la vista de Roles.
document.addEventListener("DOMContentLoaded", () => {
	const api = "/api/RolApi",
		form = document.querySelector("#role-form"),
		table = document.querySelector("#roles-table"),
		modal = bootstrap.Modal.getOrCreateInstance(
			document.querySelector("#role-modal"),
		),
		alertBox = document.querySelector("#role-alert"),
		formAlert = document.querySelector("#role-form-alert"),
		feedback = document.querySelector("#role-feedback");
	let editingId = null;
	const safe = DataTable.render.text();
	// Centraliza las llamadas fetch al API de Roles y presenta sus errores.
	const request = async (url, options = {}) => {
		const r = await fetch(url, {
			credentials: "same-origin",
			headers: {
				Accept: "application/json, application/problem+json",
				...(options.body ? { "Content-Type": "application/json" } : {}),
			},
			...options,
		});
		if (!r.ok) {
			const p = await r.json().catch(() => ({}));
			throw new Error(window.getApiErrorMessage(r, p));
		}
		return r.status === 204 ? null : r.json();
	};
	const dt = new DataTable(table, {
		data: [],
		columns: [
			{ data: "idRol" },
			{ data: "nombre", render: safe },
			{
				data: null,
				orderable: false,
				searchable: false,
				render: (v, t, r) =>
					`<button class="btn btn-sm btn-outline-primary" data-edit="${Number(r.idRol)}">Editar</button>`,
			},
		],
		language: {
			emptyTable: "No hay roles registrados.",
			search: "Buscar:",
			lengthMenu: "Mostrar _MENU_",
			info: "Mostrando _START_ a _END_ de _TOTAL_",
			paginate: { previous: "Anterior", next: "Siguiente" },
		},
	});
	// Consulta GET /api/RolApi y muestra los roles en DataTables.
	async function load() {
		try {
			dt.clear()
				.rows.add(await request(api))
				.draw();
			alertBox.hidden = true;
		} catch (e) {
			window.showAppError(e.message || "No se pudieron cargar los roles.");
		}
	}
	// Limpia y abre el formulario para crear un rol.
	document.querySelector("#new-role").addEventListener("click", () => {
		editingId = null;
		form.reset();
		formAlert.hidden = true;
		document.querySelector("#role-modal-title").textContent = "Nuevo rol";
		modal.show();
	});
	// Busca el rol solicitado por la persona usuaria y carga sus datos para editar.
	table.addEventListener("click", async (e) => {
		const b = e.target.closest("[data-edit]");
		if (!b) return;
		try {
			const x = await request(`${api}/${b.dataset.edit}`);
			editingId = x.idRol;
			form.elements.nombre.value = x.nombre;
			document.querySelector("#role-modal-title").textContent =
				"Editar rol";
			modal.show();
		} catch (err) {
			window.showAppError(err.message || "No se pudo consultar el rol.");
		}
	});
	// Envía POST o PUT al API para crear o actualizar el rol.
	form.addEventListener("submit", async (e) => {
		e.preventDefault();
		try {
			const wasEditing = editingId !== null;
			await request(editingId ? `${api}/${editingId}` : api, {
				method: editingId ? "PUT" : "POST",
				body: JSON.stringify({
					nombre: form.elements.nombre.value.trim(),
				}),
			});
			modal.hide();
			feedback.textContent = "Rol guardado.";
			await load();
			await window.showAppSuccess(
				wasEditing
					? "El rol se actualizó correctamente."
					: "El rol se creó correctamente.",
				wasEditing ? "Rol actualizado" : "Rol creado",
			);
		} catch (err) {
			window.showAppError(err.message || "No se pudo guardar el rol.");
		}
	});
	load();
});
