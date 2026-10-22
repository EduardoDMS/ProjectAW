// Arranca la vista de existencias cuando el documento ya tiene su tabla y filtros.
document.addEventListener("DOMContentLoaded", async () => {
	const table = document.querySelector("#stock-table");
	const alertBox = document.querySelector("#stock-alert");
	const results = document.querySelector("#stock-results");
	const prompt = document.querySelector("#stock-select-prompt");
	let grid;

	// Llama al API con fetch y convierte Problem Details en un mensaje entendible.
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

	// Presenta errores de carga sin interrumpir la navegación por la aplicación.
	function showError(message) {
		alertBox.textContent = message;
		alertBox.hidden = !message;
	}

	// Formatea el instante de actualización según la zona horaria del navegador.
	function formatDate(value) {
		return value
			? new Date(value).toLocaleString("es-PE", { dateStyle: "medium", timeStyle: "short" })
			: "—";
	}

	// Carga los saldos desde GET /api/stocks y refresca el DataTable existente.
	async function loadStocks() {
		showError("");
		const idAlmacen = document.querySelector("#stock-warehouse").value;
		if (!idAlmacen) {
			if (grid) grid.clear().draw();
			results.hidden = true;
			prompt.hidden = false;
			return;
		}
		results.hidden = false;
		prompt.hidden = true;
		try {
			const stocks = await fetchJson(`/api/stocks?idAlmacen=${encodeURIComponent(idAlmacen)}`);
			const text = DataTable.render.text();
			const columns = [
				{ data: "codigoProducto", render: text },
				{ data: "producto", render: text },
				{ data: "ubicacion", render: text },
				{ data: "cantidadActual", className: "text-end", render: DataTable.render.number(".", ",", 0) },
				{ data: "unidadMedida", render: text },
				{ data: "fechaUltimoRegistro", render: (value, type) => type === "display" ? formatDate(value) : value },
			];

			if (!grid) {
				grid = new DataTable(table, {
					data: stocks,
					columns,
					order: [[0, "asc"], [2, "asc"]],
					pageLength: 10,
					lengthMenu: [10, 25, 50],
					autoWidth: false,
					language: {
						search: "Buscar:", lengthMenu: "Mostrar _MENU_", info: "Mostrando _START_–_END_ de _TOTAL_ saldos",
						infoEmpty: "Sin saldos", zeroRecords: "No hay coincidencias", emptyTable: "No hay existencias registradas",
						paginate: { first: "Primera", last: "Última", next: "Siguiente", previous: "Anterior" },
					},
				});
			} else {
				grid.clear().rows.add(stocks).draw();
			}
		} catch (error) {
			showError(error.message || "No fue posible consultar las existencias.");
		}
	}

	try {
		const warehouses = await fetchJson("/api/AlmacenApi");
		for (const item of warehouses.filter(x => x.activo)) document.querySelector("#stock-warehouse").add(new Option(`${item.codigo} · ${item.descripcion || ""}`, item.idAlmacen));
	} catch (error) { showError(error.message || "No fue posible cargar los almacenes activos."); }
	document.querySelector("#stock-warehouse").addEventListener("change", loadStocks);
	// Conecta la acción manual de actualización; requiere seleccionar almacén.
	document.querySelector("#stock-refresh").addEventListener("click", loadStocks);
});
