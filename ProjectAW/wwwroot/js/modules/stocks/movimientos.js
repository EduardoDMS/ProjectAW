// Inicializa el historial de movimientos y enlaza los filtros con el endpoint del módulo.
document.addEventListener("DOMContentLoaded", async () => {
	const table = document.querySelector("#movement-table");
	const alertBox = document.querySelector("#movement-alert");
	let grid;

	// Consulta la API con fetch y normaliza sus respuestas de error.
	async function fetchJson(url) {
		const response = await fetch(url, {
			credentials: "same-origin",
			headers: { Accept: "application/json, application/problem+json" },
		});

		if (!response.ok) {
			const problem = await response.json().catch(() => ({}));
			throw new Error(window.getApiErrorMessage(response, problem));
		}

		return response.json();
	}

	// Muestra errores del servidor dentro de la página y permite volver a intentar.
	function showError(message) {
		alertBox.textContent = message;
		alertBox.hidden = !message;
	}

	// Convierte los identificadores estables de operación en etiquetas para el historial.
	function operationName(id) {
		return ({ 1: "Entrada", 2: "Salida", 3: "Traslado" })[id] || `Operación ${id}`;
	}

	// Representa fechas en formato local sin alterar el dato usado para ordenar.
	function formatDate(value) {
		return value
			? new Date(value).toLocaleString("es-PE", { dateStyle: "medium", timeStyle: "short" })
			: "—";
	}

	// Construye la URL de lectura solo con filtros ingresados por el usuario.
	function getApiUrl() {
		const query = new URLSearchParams();
		const guideId = document.querySelector("#movement-guide-filter").value.trim();
		const warehouseId = document.querySelector("#movement-warehouse-filter").value.trim();
		const operationId = document.querySelector("#movement-operation-filter").value.trim();
		if (guideId) query.set("idGuia", guideId);
		if (warehouseId) query.set("idAlmacen", warehouseId);
		if (operationId) query.set("idOperacion", operationId);
		return `/api/movimientos-stock${query.size ? `?${query}` : ""}`;
	}

	// Carga el historial de movimientos y configura o actualiza DataTables.
	async function loadMovements() {
		showError("");
		try {
			const records = await fetchJson(getApiUrl());
			const text = DataTable.render.text();
			const columns = [
				{ data: "numeroGuia", render: text },
				{ data: "operacion", render: text },
				{ data: "codigoProducto", render: text },
				{ data: "producto", render: text },
				{ data: "origen", render: text },
				{ data: "destino", render: text },
				{ data: "cantidadEsperada", className: "text-end", render: DataTable.render.number(".", ",", 0) },
				{ data: "cantidad", className: "text-end", render: DataTable.render.number(".", ",", 0) },
				{ data: "cantidadDiferenciada", className: "text-end", render: (value, type) => type === "display" && value > 0 ? `+${value}` : value },
				{ data: "operador", render: text },
				{ data: "observacion", defaultContent: "—", render: text },
				{ data: "fechaMovimiento", render: (value, type) => type === "display" ? formatDate(value) : value },
			];

			if (!grid) {
				grid = new DataTable(table, {
					data: records,
					columns,
					order: [[11, "desc"]],
					pageLength: 10,
					lengthMenu: [10, 25, 50],
					autoWidth: false,
					language: {
						search: "Buscar:", lengthMenu: "Mostrar _MENU_", info: "Mostrando _START_–_END_ de _TOTAL_ movimientos",
						infoEmpty: "Sin movimientos", zeroRecords: "No hay coincidencias", emptyTable: "Aún no se registran movimientos",
						paginate: { first: "Primera", last: "Última", next: "Siguiente", previous: "Anterior" },
					},
				});
			} else {
				grid.clear().rows.add(records).draw();
			}
		} catch (error) {
			showError(error.message || "No fue posible consultar los movimientos.");
		}
	}

	// Pobla los selectores con recursos activos y guías disponibles.
	try {
		const [warehouses, guides] = await Promise.all([fetchJson("/api/AlmacenApi"), fetchJson("/api/guias")]);
		for (const item of warehouses.filter(x => x.activo)) document.querySelector("#movement-warehouse-filter").add(new Option(`${item.codigo} · ${item.descripcion || ""}`, item.idAlmacen));
		for (const item of guides) document.querySelector("#movement-guide-filter").add(new Option(item.numeroGuia, item.idGuia));
	} catch (error) { showError(error.message || "No fue posible cargar los filtros."); }
	// Ejecuta la consulta filtrada cuando el usuario solicita aplicar los filtros.
	document.querySelector("#movement-filter").addEventListener("click", loadMovements);
	await loadMovements();
});
