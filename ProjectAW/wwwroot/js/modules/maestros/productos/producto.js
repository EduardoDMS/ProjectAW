// Inicializa la vista de Productos y carga categorías y productos desde sus APIs.
document.addEventListener("DOMContentLoaded", async () => {
	const API = "/api/ProductoApi";
	// Busca elementos de la vista por selector CSS para enlazar eventos y campos.
	const select = (selector) => document.querySelector(selector);
	const alertBox = select("#product-alert");
	const formAlert = select("#product-form-alert");
	const form = select("#product-form");
	const modal = window.bootstrap.Modal.getOrCreateInstance(
		select("#product-modal"),
	);
	const tableElement = select("#products-table");
	let records = [];
	let categories = [];
	let units = [];
	let selectedId = null;
	let grid;

	// Envía una petición fetch al API de Productos y normaliza las respuestas de error.
	async function fetchJson(url, options = {}) {
		const headers = new Headers(options.headers || {});
		headers.set("Accept", "application/json, application/problem+json");
		if (options.body && !headers.has("Content-Type"))
			headers.set("Content-Type", "application/json");
		const response = await fetch(url, {
			credentials: "same-origin",
			...options,
			headers,
		});
		if (!response.ok) {
			const error = await response.json().catch(() => ({}));
			throw new Error(window.getApiErrorMessage(response, error));
		}
		return response.status === 204 ? null : response.json();
	}

	// Presenta en la vista mensajes de validación, error o resultado de una operación.
	function notify(message, target = alertBox) {
		target.textContent = message;
		target.hidden = !message;
	}
	// Busca el nombre de categoría para mostrarlo en la tabla junto al producto.
	function categoryName(id) {
		return (
			categories.find((item) => item.idCategoria === id)?.nombre ||
			`Categoría #${id}`
		);
	}
	// Filtra localmente los productos según el estado elegido en la vista.
	function activeRows() {
		// Aplica el filtro local de estado y vuelve a dibujar la tabla.
		const filter = select("#product-status").value;
		return filter === ""
			? records
			: records.filter((item) => String(item.activo) === filter);
	}
	// Configura DataTables o refresca sus filas con el catálogo recibido del API.
	function drawRows() {
		const filtered = activeRows();
		if (!grid) {
			const text = DataTable.render.text();
			grid = new DataTable(tableElement, {
				data: filtered,
				columns: [
					{ data: "codigo", render: text },
					{ data: "descripcion", render: text },
					{
						data: "idCategoria",
						render: (value, type) =>
							type === "display"
								? text.display(categoryName(value))
								: categoryName(value),
					},
					{ data: "lote", render: text },
					{ data: "serie", render: text },
					{
						data: "activo",
						render: (value, type) =>
							type === "display"
								? `<span class="record-status ${value ? "is-active" : "is-inactive"}">${value ? "Activo" : "Inactivo"}</span>`
								: value
									? "Activo"
									: "Inactivo",
					},
					{
						data: null,
						orderable: false,
						searchable: false,
						className: "text-end",
						render: (_value, _type, row) =>
							`<button type="button" class="btn table-action" data-action="edit" data-id="${row.idProducto}">Editar</button><button type="button" class="btn table-action table-action--muted" data-action="status" data-id="${row.idProducto}">${row.activo ? "Desactivar" : "Activar"}</button>`,
					},
				],
				order: [[0, "asc"]],
				pageLength: 10,
				lengthMenu: [10, 25, 50],
				autoWidth: false,
				language: {
					search: "Buscar:",
					lengthMenu: "Mostrar _MENU_",
					info: "Mostrando _START_–_END_ de _TOTAL_ productos",
					infoEmpty: "Sin productos",
					zeroRecords: "No hay coincidencias",
					emptyTable: "No hay productos registrados",
					paginate: {
						first: "Primera",
						last: "Última",
						next: "Siguiente",
						previous: "Anterior",
					},
				},
			});
		} else grid.clear().rows.add(filtered).draw();
	}
	// Consulta GET /api/ProductoApi y actualiza la tabla de productos.
	async function loadRecords() {
		notify("");
		try {
			records = await fetchJson(API);
			drawRows();
			select("#product-feedback").textContent =
				`${records.length} productos`;
		} catch (error) {
			window.showAppError(error.message || "No se pudieron cargar los productos.");
		}
	}
	// Consulta el catálogo de categorías y llena el selector del formulario.
	async function loadCategories() {
		[categories, units] = await Promise.all([fetchJson("/api/CategoriaApi"), fetchJson("/api/unidades-medida")]);
		categories = categories.filter(x => x.activo);
		units = units.filter(x => x.activo);
		const categorySelect = select("#product-category");
		categorySelect.replaceChildren(new Option("Selecciona categoría", ""));
		for (const category of categories)
			categorySelect.append(
				new Option(category.nombre, String(category.idCategoria)),
			);
		const unitSelect = select("#product-unit");
		unitSelect.replaceChildren(new Option("Selecciona unidad", ""));
		for (const unit of units) unitSelect.add(new Option(`${unit.nombre} (${unit.codigo})`, String(unit.idUnidadMedida)));
	}
	// Limpia los campos y restaura los valores iniciales del modal de productos.
	function resetForm() {
		selectedId = null;
		form.reset();
		select("#product-active").checked = true;
		select("#product-modal-title").textContent = "Nuevo producto";
		select("#save-product").textContent = "Guardar producto";
		notify("", formAlert);
	}
	// Abre el modal preparado para registrar un producto nuevo.
	function openCreate() {
		resetForm();
		modal.show();
	}
	// Consulta GET /api/ProductoApi/{id} y carga los datos del producto en el modal.
	async function openEdit(id) {
		try {
			const item = await fetchJson(`${API}/${id}`);
			selectedId = id;
			select("#product-code").value = item.codigo;
			select("#product-name").value = item.descripcion || "";
			select("#product-category").value = String(item.idCategoria);
			select("#product-unit").value = String(item.idUnidadMedida);
			select("#product-serial").value = item.serie;
			select("#product-lot").value = item.lote || "";
			select("#product-active").checked = item.activo;
			select("#product-modal-title").textContent = "Editar producto";
			select("#save-product").textContent = "Guardar cambios";
			notify("", formAlert);
			modal.show();
		} catch (error) {
			window.showAppError(error.message || "No se pudo consultar el producto.");
		}
	}
	// Envía PATCH al API para cambiar el estado y luego vuelve a consultar el catálogo.
	async function changeState(item) {
		try {
			await fetchJson(`${API}/${item.idProducto}/estado`, {
				method: "PATCH",
				body: JSON.stringify(!item.activo),
			});
			await loadRecords();
		} catch (error) {
			window.showAppError(error.message || "No se pudo cambiar el estado del producto.");
		}
	}
	// Valida los datos y envía POST o PUT al API para crear o actualizar el producto.
	form.addEventListener("submit", async (event) => {
		event.preventDefault();
		if (!form.reportValidity()) return;
		const payload = {
			codigo: select("#product-code").value.trim(),
			descripcion: select("#product-name").value.trim(),
			idCategoria: Number(select("#product-category").value),
			idUnidadMedida: Number(select("#product-unit").value),
			serie: select("#product-serial").value.trim(),
			lote: select("#product-lot").value.trim() || null,
			activo: select("#product-active").checked,
		};
		const button = select("#save-product");
		button.disabled = true;
		try {
			const wasEditing = selectedId !== null;
			await fetchJson(selectedId ? `${API}/${selectedId}` : API, {
				method: selectedId ? "PUT" : "POST",
				body: JSON.stringify(payload),
			});
			modal.hide();
			await loadRecords();
			select("#product-feedback").textContent = "Cambios guardados";
			await window.showAppSuccess(
				wasEditing
					? "El producto se actualizó correctamente."
					: "El producto se creó correctamente.",
				wasEditing ? "Producto actualizado" : "Producto creado",
			);
		} catch (error) {
			window.showAppError(error.message || "No se pudo guardar el producto.");
		} finally {
			button.disabled = false;
		}
	});
	// Interpreta los botones de las filas y llama a editar o cambiar estado.
	tableElement.addEventListener("click", (event) => {
		const button = event.target.closest("button[data-action]");
		if (!button) return;
		const item = records.find(
			(record) => record.idProducto === Number(button.dataset.id),
		);
		if (!item) return;
		if (button.dataset.action === "edit") openEdit(item.idProducto);
		else changeState(item);
	});
	// Abre el modal vacío para crear un producto.
	select("#new-product").addEventListener("click", openCreate);
	// Aplica el filtro local de estado y vuelve a dibujar la tabla.
	select("#product-status").addEventListener("change", drawRows);
	try {
		await loadCategories();
		await loadRecords();
	} catch (error) {
		window.showAppError(error.message || "No se pudieron cargar las categorías.");
	}
});
