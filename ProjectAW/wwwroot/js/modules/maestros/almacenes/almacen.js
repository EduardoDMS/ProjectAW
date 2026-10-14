// Inicializa el módulo de la vista, conecta sus controles y carga el catálogo del API.
document.addEventListener("DOMContentLoaded", async () => {
	const API = "/api/AlmacenApi";
	// Busca elementos del HTML para enlazar la vista con las operaciones de almacén.
	const select = (selector) => document.querySelector(selector);
	const tableElement = select("#warehouses-table");
	const form = select("#warehouse-form");
	const modal = window.bootstrap.Modal.getOrCreateInstance(
		select("#warehouse-modal"),
	);
	let warehouses = [];
	let editingId = null;
	let grid;

	// Envía la petición HTTP al endpoint de Almacenes y convierte errores de API en mensajes.
	async function fetchJson(url, options = {}) {
		const headers = new Headers(options.headers || {});
		headers.set("Accept", "application/json, application/problem+json");

		if (options.body && !headers.has("Content-Type")) {
			headers.set("Content-Type", "application/json");
		}

		const response = await fetch(url, {
			credentials: "same-origin",
			...options,
			headers,
		});

		if (!response.ok) {
			const problem = await response.json().catch(() => ({}));
			throw new Error(window.getApiErrorMessage(response, problem));
		}

		return response.status === 204 ? null : response.json();
	}

	// Muestra u oculta mensajes en la vista, ya sean errores de API o confirmaciones.
	function showAlert(message, target = select("#warehouse-alert")) {
		target.textContent = message;
		target.hidden = !message;
	}

	// Aplica en memoria el filtro de activos/inactivos seleccionado por la persona usuaria.
	function getVisibleWarehouses() {
		// Redibuja la tabla cuando cambia el filtro de estado.
		const status = select("#warehouse-status").value;
		return status
			? warehouses.filter(
					(warehouse) => String(warehouse.activo) === status,
				)
			: warehouses;
	}

	// Entrega los almacenes a DataTables y define cómo representar sus campos y acciones.
	function drawTable() {
		const text = DataTable.render.text();
		const visibleWarehouses = getVisibleWarehouses();

		if (!grid) {
			grid = new DataTable(tableElement, {
				data: visibleWarehouses,
				columns: [
					{ data: "codigo", render: text },
					{ data: "descripcion", defaultContent: "—", render: text },
					{
						data: "fchRegistro",
						render: (value, type) => {
							if (type !== "display") return value;
							return value
								? new Date(value).toLocaleDateString("es-PE")
								: "—";
						},
					},
					{
						data: "activo",
						render: (value, type) => {
							if (type !== "display")
								return value ? "Activo" : "Inactivo";
							const statusClass = value
								? "is-active"
								: "is-inactive";
							const label = value ? "Activo" : "Inactivo";
							return `<span class="record-status ${statusClass}">${label}</span>`;
						},
					},
					{
						data: null,
						orderable: false,
						searchable: false,
						className: "text-end",
						render: (_value, _type, warehouse) => `
							<button class="table-action" data-action="edit" data-id="${Number(warehouse.idAlmacen)}" type="button">Editar</button>
							<button class="table-action table-action--muted" data-action="status" data-id="${Number(warehouse.idAlmacen)}" type="button">${warehouse.activo ? "Desactivar" : "Activar"}</button>
						`,
					},
				],
				order: [[0, "asc"]],
				pageLength: 10,
				lengthMenu: [10, 25, 50],
				autoWidth: false,
				language: {
					search: "Buscar:",
					lengthMenu: "Mostrar _MENU_",
					info: "Mostrando _START_–_END_ de _TOTAL_ almacenes",
					infoEmpty: "Sin almacenes",
					zeroRecords: "No hay coincidencias",
					emptyTable: "No hay almacenes registrados",
					paginate: {
						first: "Primera",
						last: "Última",
						next: "Siguiente",
						previous: "Anterior",
					},
				},
			});
			return;
		}

		grid.clear().rows.add(visibleWarehouses).draw();
	}

	// Consulta GET /api/AlmacenApi y actualiza la tabla y el contador de resultados.
	async function loadWarehouses() {
		showAlert("");

		try {
			warehouses = await fetchJson(API);
			drawTable();
			select("#warehouse-feedback").textContent =
				`${warehouses.length} almacenes`;
		} catch (error) {
			window.showAppError(error.message || "No se pudieron cargar los almacenes.");
		}
	}

	// Prepara el formulario del modal para crear un almacén nuevo.
	function resetForm() {
		editingId = null;
		form.reset();
		select("#warehouse-active").checked = true;
		select("#warehouse-modal-title").textContent = "Nuevo almacén";
		select("#save-warehouse").textContent = "Guardar almacén";
		showAlert("", select("#warehouse-form-alert"));
	}

	// Consulta GET /api/AlmacenApi/{id} y carga el registro elegido en el modal.
	async function editWarehouse(id) {
		try {
			const warehouse = await fetchJson(`${API}/${id}`);
			editingId = id;
			select("#warehouse-code").value = warehouse.codigo;
			select("#warehouse-description").value =
				warehouse.descripcion || "";
			select("#warehouse-active").checked = warehouse.activo;
			select("#warehouse-modal-title").textContent = "Editar almacén";
			select("#save-warehouse").textContent = "Guardar cambios";
			showAlert("", select("#warehouse-form-alert"));
			modal.show();
		} catch (error) {
			window.showAppError(error.message || "No se pudo consultar el almacén.");
		}
	}

	// Envía PATCH /api/AlmacenApi/{id} para activar o desactivar el almacén.
	async function toggleWarehouseStatus(id) {
		const warehouse = warehouses.find((item) => item.idAlmacen === id);
		if (!warehouse) return;

		try {
			await fetchJson(`${API}/${id}`, {
				method: "PATCH",
				body: JSON.stringify(!warehouse.activo),
			});
			await loadWarehouses();
		} catch (error) {
			window.showAppError(
				error.message || "No se pudo cambiar el estado del almacén.",
			);
		}
	}

	// Valida el formulario y envía POST o PUT al API según se cree o edite el almacén.
	form.addEventListener("submit", async (event) => {
		event.preventDefault();
		if (!form.reportValidity()) return;

		const payload = {
			codigo: select("#warehouse-code").value.trim(),
			descripcion: select("#warehouse-description").value.trim(),
			activo: select("#warehouse-active").checked,
		};
		const button = select("#save-warehouse");
		button.disabled = true;

		try {
			const wasEditing = editingId !== null;
			const url = editingId ? `${API}/${editingId}` : API;
			const method = editingId ? "PUT" : "POST";
			await fetchJson(url, { method, body: JSON.stringify(payload) });
			modal.hide();
			await loadWarehouses();
			select("#warehouse-feedback").textContent = "Cambios guardados";
			await window.showAppSuccess(
				wasEditing
					? "El almacén se actualizó correctamente."
					: "El almacén se creó correctamente.",
				wasEditing ? "Almacén actualizado" : "Almacén creado",
			);
		} catch (error) {
			window.showAppError(
				error.message || "No se pudo guardar el almacén.",
			);
		} finally {
			button.disabled = false;
		}
	});

	// Conecta los botones de cada fila con la edición o el cambio de estado del almacén.
	tableElement.addEventListener("click", (event) => {
		const button = event.target.closest("[data-action]");
		if (!button) return;

		const id = Number(button.dataset.id);
		if (button.dataset.action === "edit") {
			editWarehouse(id);
			return;
		}
		toggleWarehouseStatus(id);
	});

	// Abre el formulario de creación al pulsar el botón Nueva almacén.
	select("#new-warehouse").addEventListener("click", () => {
		resetForm();
		modal.show();
	});
	// Redibuja la tabla cuando cambia el filtro de estado.
	select("#warehouse-status").addEventListener("change", drawTable);
	await loadWarehouses();
});
