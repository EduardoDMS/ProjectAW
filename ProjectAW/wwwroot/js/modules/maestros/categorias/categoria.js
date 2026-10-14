// Inicializa la tabla y los controles de la vista de Categorías.
document.addEventListener("DOMContentLoaded", () => {
	const api = "/api/CategoriaApi",
		form = document.querySelector("#category-form"),
		table = document.querySelector("#categories-table"),
		modal = bootstrap.Modal.getOrCreateInstance(
			document.querySelector("#category-modal"),
		);
	let editingId = null;
	const alertBox = document.querySelector("#category-alert"),
		formAlert = document.querySelector("#category-form-alert"),
		feedback = document.querySelector("#category-feedback"),
		safe = DataTable.render.text();
	// Centraliza fetch al API de Categorías y convierte respuestas fallidas en errores visibles.
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
			{ data: "idCategoria" },
			{ data: "nombre", render: safe },
			{ data: "descripcion", defaultContent: "—", render: safe },
			{ data: "activo", render: (v) => (v ? "Activa" : "Inactiva") },
			{
				data: null,
				orderable: false,
				searchable: false,
				render: (v, t, r) =>
					`<button class="btn btn-sm btn-outline-primary" data-edit="${Number(r.idCategoria)}">Editar</button> <button class="btn btn-sm btn-outline-secondary" data-toggle="${Number(r.idCategoria)}" data-active="${!r.activo}">${r.activo ? "Desactivar" : "Activar"}</button>`,
			},
		],
		language: {
			emptyTable: "No hay categorías registradas.",
			search: "Buscar:",
			lengthMenu: "Mostrar _MENU_",
			info: "Mostrando _START_ a _END_ de _TOTAL_",
			paginate: { previous: "Anterior", next: "Siguiente" },
		},
	});
	// Filtra las filas de DataTables según el estado seleccionado.
	document.querySelector("#category-status").addEventListener("change", (e) =>
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
	// Consulta GET /api/CategoriaApi y actualiza las filas de la tabla.
	async function load() {
		try {
			dt.clear()
				.rows.add(await request(api))
				.draw();
			alertBox.hidden = true;
		} catch (e) {
			window.showAppError(e.message || "No se pudieron cargar las categorías.");
		}
	}
	// Abre el formulario vacío para dar de alta una categoría.
	document.querySelector("#new-category").addEventListener("click", () => {
		editingId = null;
		form.reset();
		form.elements.activo.checked = true;
		formAlert.hidden = true;
		document.querySelector("#category-modal-title").textContent =
			"Nueva categoría";
		modal.show();
	});
	// Atiende acciones de edición y de activación enviadas desde las filas de la tabla.
	table.addEventListener("click", async (e) => {
		const edit = e.target.closest("[data-edit]"),
			toggle = e.target.closest("[data-toggle]");
		try {
			if (edit) {
				const x = await request(`${api}/${edit.dataset.edit}`);
				editingId = x.idCategoria;
				form.elements.nombre.value = x.nombre;
				form.elements.descripcion.value = x.descripcion;
				form.elements.activo.checked = x.activo;
				document.querySelector("#category-modal-title").textContent =
					"Editar categoría";
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
			window.showAppError(err.message || "No se pudo completar la acción de la categoría.");
		}
	});
	// Envía POST o PUT al API según se cree o actualice la categoría.
	form.addEventListener("submit", async (e) => {
		e.preventDefault();
		const payload = {
			nombre: form.elements.nombre.value.trim(),
			descripcion: form.elements.descripcion.value.trim(),
			activo: form.elements.activo.checked,
		};
		try {
			const wasEditing = editingId !== null;
			await request(editingId ? `${api}/${editingId}` : api, {
				method: editingId ? "PUT" : "POST",
				body: JSON.stringify(payload),
			});
			modal.hide();
			feedback.textContent = "Categoría guardada.";
			await load();
			await window.showAppSuccess(
				wasEditing
					? "La categoría se actualizó correctamente."
					: "La categoría se creó correctamente.",
				wasEditing ? "Categoría actualizada" : "Categoría creada",
			);
		} catch (err) {
			window.showAppError(err.message || "No se pudo guardar la categoría.");
		}
	});
	load();
});
