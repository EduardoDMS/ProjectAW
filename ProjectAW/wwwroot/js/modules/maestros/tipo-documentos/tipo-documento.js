// Al cargar la vista, consulta tipos de documento y los presenta en una tabla de solo lectura.
// Carga los tipos de documento con fetch y completa la tabla de consulta.
document.addEventListener("DOMContentLoaded", () => {
	const table = document.querySelector("#document-types-table"),
		alertBox = document.querySelector("#document-alert"),
		feedback = document.querySelector("#document-feedback"),
		safe = DataTable.render.text();
	const dt = new DataTable(table, {
		data: [],
		columns: [
			{ data: "idTipoDoc", render: (v) => String(v).padStart(2, "0") },
			{ data: "documento", render: safe },
			{ data: null, render: () => "Clientes y proveedores" },
		],
		language: {
			emptyTable: "No hay tipos de documento registrados.",
			search: "Buscar:",
			lengthMenu: "Mostrar _MENU_",
			info: "Mostrando _START_ a _END_ de _TOTAL_",
			paginate: { previous: "Anterior", next: "Siguiente" },
		},
	});
	fetch("/api/TipoDocumentoApi", {
		credentials: "same-origin",
		headers: { Accept: "application/json, application/problem+json" },
	})
		.then(async (r) => {
			if (!r.ok) {
				const p = await r.json().catch(() => ({}));
				throw new Error(window.getApiErrorMessage(r, p));
			}
			return r.json();
		})
		.then((data) => {
			dt.clear().rows.add(data).draw();
			feedback.textContent = `${data.length} tipos disponibles`;
		})
		.catch((e) => {
			window.showAppError(e.message || "No se pudieron cargar los tipos de documento.");
		});
});
