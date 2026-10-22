// Administra el catálogo de motivos y presenta una tabla filtrable del mismo estilo que Maestros.
document.addEventListener("DOMContentLoaded", () => {
	const app = document.querySelector("#motives-app");
	if (!app) return;
	// El formulario modal vive fuera de #motives-app; los selectores deben buscar en todo el documento.
	const $ = (selector) => document.querySelector(selector);
	const rows = $("#motives-rows");
	const alertBox = $("#motives-alert");
	const formAlert = $("#motive-form-alert");
	const modalElement = document.querySelector("#motive-editor");
	const modal = window.bootstrap?.Modal.getOrCreateInstance(modalElement);
	const searchInput = $("#motive-search");
	const operationFilter = $("#motive-operation-filter");
	const statusFilter = $("#motive-status-filter");
	const countLabel = $("#motives-count");
	const fields = ["provider", "client", "origin", "destination"];
	const fieldLabels = ["Proveedor", "Cliente", "Almacén origen", "Almacén destino"];
	let motives = [];
	let behaviors = [];
	let editingId = null;

	async function request(url, options = {}) {
		const headers = new Headers(options.headers || {});
		headers.set("Accept", "application/json, application/problem+json");
		if (options.body != null) headers.set("Content-Type", "application/json");
		const controller = new AbortController();
		const timeoutId = window.setTimeout(() => controller.abort(), 15000);
		try {
			const response = await fetch(url, { credentials: "same-origin", ...options, signal: controller.signal, headers });
			if (!response.ok) {
				const problem = await response.json().catch(() => ({}));
				throw new Error(window.getApiErrorMessage(response, problem));
			}
			if (response.status === 204) return null;
			if (!response.headers.get("content-type")?.includes("json"))
				throw new Error("La API respondió con contenido inesperado. Comprueba que la sesión de administrador siga activa y que la API esté disponible.");
			return await response.json();
		} catch (error) {
			if (error.name === "AbortError") {
				const endpoint = new URL(url, window.location.href).pathname;
				throw new Error(`La solicitud ${endpoint} tardó más de 15 segundos. Comprueba que el servidor y la base de datos estén respondiendo.`);
			}
			throw error;
		} finally {
			window.clearTimeout(timeoutId);
		}
	}

	function showError(target, message) {
		target.textContent = message;
		target.hidden = false;
	}

	function iconButton(icon, label, className, onClick) {
		const button = document.createElement("button");
		button.type = "button";
		button.className = `table-action ${className || ""}`.trim();
		button.title = label;
		button.setAttribute("aria-label", label);
		const glyph = document.createElement("i");
		glyph.className = `ti ${icon}`;
		glyph.setAttribute("aria-hidden", "true");
		button.append(glyph);
		button.addEventListener("click", onClick);
		return button;
	}

	function appendCell(row, label, content, className = "") {
		const cell = document.createElement("td");
		cell.dataset.label = label;
		if (className) cell.className = className;
		if (content instanceof Node) cell.append(content);
		else cell.textContent = content;
		row.append(cell);
		return cell;
	}

	function requestedFields(item) {
		return [item.requiereProveedor, item.requiereCliente, item.requiereAlmacenOrigen, item.requiereAlmacenDestino]
			.map((enabled, index) => enabled ? fieldLabels[index] : null)
			.filter(Boolean);
	}

	function filteredMotives() {
		const query = searchInput.value.trim().toLocaleLowerCase("es");
		return motives.filter((item) => {
			const matchesQuery = !query || `${item.nombreMotivo} ${item.nombreComportamiento} ${item.descripcionComportamiento}`.toLocaleLowerCase("es").includes(query);
			const matchesOperation = !operationFilter.value || Number(item.idTipoOperacionGuia) === Number(operationFilter.value);
			const matchesStatus = !statusFilter.value || String(Boolean(item.activo)) === statusFilter.value;
			return matchesQuery && matchesOperation && matchesStatus;
		});
	}

	function renderRows() {
		const visible = filteredMotives();
		rows.replaceChildren();
		countLabel.replaceChildren();		const count = document.createElement("strong"); count.textContent = String(visible.length); countLabel.append(count, document.createTextNode(` de ${motives.length} motivos`));
		for (const item of visible) {
			const row = document.createElement("tr");
			const name = document.createElement("span"); name.className = "motives-name"; name.textContent = item.nombreMotivo;
			appendCell(row, "Motivo", name);
			const operation = document.createElement("span"); operation.className = "motives-kind";
			const operationIcon = document.createElement("i"); operationIcon.className = `ti ${Number(item.idTipoOperacionGuia) === 1 ? "ti-arrow-down-left" : "ti-arrow-up-right"}`; operationIcon.setAttribute("aria-hidden", "true");
			operation.append(operationIcon, document.createTextNode(item.tipoOperacion));
			appendCell(row, "Operación", operation);
			const behavior = document.createElement("span"); behavior.className = "motives-behavior"; behavior.textContent = item.nombreComportamiento;
			appendCell(row, "Comportamiento", behavior);
			const chips = document.createElement("div"); chips.className = "motives-fields";
			for (const label of requestedFields(item)) { const chip = document.createElement("span"); chip.textContent = label; chips.append(chip); }
			if (!chips.children.length) { const chip = document.createElement("span"); chip.textContent = "Sin datos adicionales"; chips.append(chip); }
			appendCell(row, "Datos solicitados", chips);
			const status = document.createElement("span"); status.className = `record-status ${item.activo ? "is-active" : "is-inactive"}`; status.textContent = item.activo ? "Activo" : "Inactivo";
			appendCell(row, "Estado", status);
			const actions = document.createElement("div"); actions.className = "record-actions motives-actions";
			actions.append(iconButton("ti-pencil", "Editar motivo", "", () => openEditor(item)));
			actions.append(iconButton(item.activo ? "ti-toggle-right" : "ti-toggle-left", item.activo ? "Desactivar" : "Activar", "table-action--muted", () => changeStatus(item)));
			actions.append(iconButton("ti-trash", "Eliminar motivo", "table-action--danger", () => remove(item)));
			appendCell(row, "Acciones", actions, "record-actions");
			rows.append(row);
		}
		if (!visible.length) {
			const row = document.createElement("tr");
			const cell = document.createElement("td"); cell.colSpan = 6; cell.className = "table-empty";
			cell.textContent = motives.length ? "No hay motivos que coincidan con los filtros." : "Todavía no hay motivos registrados.";
			row.append(cell); rows.append(row);
		}
	}

	async function load() {
		alertBox.hidden = true;
		if (motives.length === 0) {
			rows.replaceChildren();
			const loadingRow = document.createElement("tr");
			const loadingCell = document.createElement("td");
			loadingCell.colSpan = 6;
			loadingCell.className = "table-loading";
			loadingCell.textContent = "Cargando motivos…";
			loadingRow.append(loadingCell);
			rows.append(loadingRow);
			countLabel.textContent = "Consultando la API…";
		}
		try {
			[motives, behaviors] = await Promise.all([request("/api/guias/motivos"), request("/api/guias/motivos/comportamientos")]);
			const behaviorSelect = $("#motive-behavior");
			behaviorSelect.replaceChildren(...behaviors.map((item) => new Option(item.nombreComportamiento, String(item.idTipoComportamiento))));
			alertBox.hidden = true;
			renderRows();
		} catch (error) {
			const failedRow = document.createElement("tr");
			const failedCell = document.createElement("td");
			failedCell.colSpan = 6;
			failedCell.className = "table-empty motives-load-error";
			failedCell.textContent = error.message || "No se pudieron cargar los motivos.";
			const retry = document.createElement("button");
			retry.type = "button";
			retry.className = "btn btn-sm btn-outline-primary ms-2";
			retry.textContent = "Reintentar";
			retry.addEventListener("click", () => void load());
			failedCell.append(retry);
			failedRow.append(failedCell);
			if (motives.length > 0) {
				renderRows();
				countLabel.textContent = `${motives.length} motivos · se conserva la última consulta válida`;
			} else {
				rows.replaceChildren(failedRow);
				countLabel.textContent = "No se pudo cargar el listado";
			}
			showError(alertBox, error.message || "No se pudieron cargar los motivos.");
		}
	}

	function syncFields() {
		const behavior = behaviors.find((item) => Number(item.idTipoComportamiento) === Number($("#motive-behavior").value));
		$("#motive-help").textContent = behavior?.descripcionComportamiento || "";
		const internal = behavior?.nombreComportamiento === "TRASLADO_INTERNO";
		$("#motive-operation").disabled = internal;
		if (internal) {
			$("#motive-operation").value = "2";
			$("#motive-origin").checked = true;
			$("#motive-destination").checked = true;
			$("#motive-client").checked = false;
			$("#motive-provider").checked = false;
		} else if ($("#motive-operation").value === "1") {
			$("#motive-origin").checked = false;
			$("#motive-destination").checked = true;
		}
	}

	function openEditor(item = null) {
		editingId = item?.idMotivoGuia ?? null;
		$("#motive-title").textContent = editingId ? `Editar ${item.nombreMotivo}` : "Nuevo motivo";
		$("#motive-name").value = item?.nombreMotivo ?? "";
		$("#motive-operation").value = String(item?.idTipoOperacionGuia ?? 1);
		$("#motive-behavior").value = String(item?.idTipoComportamiento ?? 1);
		fields.forEach((key, index) => {
			const property = ["requiereProveedor", "requiereCliente", "requiereAlmacenOrigen", "requiereAlmacenDestino"][index];
			$(`#motive-${key}`).checked = item?.[property] ?? false;
		});
		$("#motive-active").checked = item?.activo ?? true;
		formAlert.hidden = true;
		syncFields();
		modal?.show();
	}

	async function changeStatus(item) {
		try {
			await request(`/api/guias/motivos/${item.idMotivoGuia}/estado?activo=${!item.activo}`, { method: "PATCH" });
			await load();
		} catch (error) { showError(alertBox, error.message || "No se pudo cambiar el estado."); }
	}

	async function remove(item) {
		if (!window.confirm(`¿Eliminar el motivo «${item.nombreMotivo}»?`)) return;
		try {
			await request(`/api/guias/motivos/${item.idMotivoGuia}`, { method: "DELETE" });
			await load();
		} catch (error) { showError(alertBox, error.message || "No se pudo eliminar el motivo."); }
	}

	$("#new-motive").addEventListener("click", () => openEditor());
	$("#motive-behavior").addEventListener("change", syncFields);
	$("#motive-operation").addEventListener("change", syncFields);
	searchInput.addEventListener("input", renderRows);
	operationFilter.addEventListener("change", renderRows);
	statusFilter.addEventListener("change", renderRows);
	$("#motive-form").addEventListener("submit", async (event) => {
		event.preventDefault();
		const button = $("#save-motive");
		const payload = {
			nombreMotivo: $("#motive-name").value.trim(),
			idTipoOperacionGuia: Number($("#motive-operation").value),
			idTipoComportamiento: Number($("#motive-behavior").value),
			requiereProveedor: $("#motive-provider").checked,
			requiereCliente: $("#motive-client").checked,
			requiereAlmacenOrigen: $("#motive-origin").checked,
			requiereAlmacenDestino: $("#motive-destination").checked,
			activo: $("#motive-active").checked,
		};
		button.disabled = true;
		try {
			await request(editingId ? `/api/guias/motivos/${editingId}` : "/api/guias/motivos", { method: editingId ? "PUT" : "POST", body: JSON.stringify(payload) });
			modal?.hide();
			await load();
		} catch (error) { showError(formAlert, error.message || "No se pudo guardar el motivo."); }
		finally { button.disabled = false; }
	});
	load();
});
