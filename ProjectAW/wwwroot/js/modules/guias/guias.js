// Inicializa la pantalla de Guías, sus eventos y los datos que consume de las APIs.
document.addEventListener("DOMContentLoaded", () => {
	const app = document.querySelector("#guides-app");
	if (!app) return;

	const API = "/api/guias";
	const OPERATIONS = [
		{ id: 1, name: "Entrada", code: "ENT" },
		{ id: 2, name: "Salida", code: "SAL" },
		{ id: 3, name: "Traslado", code: "TRA" },
	];
	const STATUSES = [
		{ id: 1, name: "Pendiente", css: "pending" },
		{ id: 2, name: "En proceso", css: "progress" },
		{ id: 3, name: "Atendida", css: "done" },
		{ id: 4, name: "Cancelada", css: "cancelled" },
	];

	// Busca un elemento del HTML por selector para conectar la vista con este módulo.
	const select = (selector) => document.querySelector(selector);
	const rows = select("#guide-rows");
	const alertBox = select("#guide-alert");
	const formAlert = select("#guide-form-alert");
	const search = select("#guide-search");
	const operationFilter = select("#guide-operation-filter");
	const statusFilter = select("#guide-status-filter");
	const form = select("#guide-form");
	const dateInput = select("#guide-date");
	const editorElement = select("#guide-editor");
	const detailElement = select("#guide-detail-modal");
	const editor = window.bootstrap?.Modal.getOrCreateInstance(editorElement);
	const detailModal =
		window.bootstrap?.Modal.getOrCreateInstance(detailElement);
	let guides = [];
	let products = [];
	let productsWithOriginStock = null;
	let stockLookupVersion = 0;
	let warehouses = [];
	let clients = [];
	let suppliers = [];
	let lookupsReady = false;
	let lookupWarning = null;
	let editingId = null;
	let loadingGuides = false;

	// Envía fetch a los endpoints de Guías y convierte respuestas de error en mensajes.
	async function fetchApi(url, options = {}) {
		const headers = new Headers(options.headers || {});
		headers.set("Accept", "application/json, application/problem+json");
		if (options.body != null && !headers.has("Content-Type"))
			headers.set("Content-Type", "application/json");
		const response = await fetch(url, {
			credentials: "same-origin",
			...options,
			headers,
		});
		if (!response.ok) {
			const problem = await response.json().catch(() => ({}));
			throw new Error(window.getApiErrorMessage(response, problem));
		}
		if (response.status === 204) return null;
		return response.json();
	}

	// Muestra un mensaje de error o estado en la zona de alertas de la vista.
	function showAlert(message, target = alertBox) {
		target.textContent = message;
		target.hidden = false;
	}

	// Limpia y oculta una alerta anterior del listado o del formulario.
	function clearAlert(target = alertBox) {
		target.textContent = "";
		target.hidden = true;
	}

	// Convierte el identificador de operación de la API en un nombre para la interfaz.
	function operationName(id) {
		return (
			OPERATIONS.find((item) => item.id === Number(id))?.name ||
			`Operación ${id}`
		);
	}

	// Obtiene etiqueta y estilo del estado de guía recibido desde el API.
	function statusInfo(id) {
		return (
			STATUSES.find((item) => item.id === Number(id)) || {
				name: `Estado ${id}`,
				css: "unknown",
			}
		);
	}

	// Busca un registro de catálogo por identificador para presentar sus datos relacionados.
	function byId(items, id, key) {
		return items.find((item) => Number(item[key]) === Number(id));
	}

	// Resuelve el almacén de origen o destino a partir del catálogo cargado.
	function warehouseName(id) {
		const item = byId(warehouses, id, "idAlmacen");
		return item
			? `${item.codigo}${item.descripcion ? ` · ${item.descripcion}` : ""}`
			: id
				? `Almacén #${id}`
				: "—";
	}

	// Arma la referencia comercial o el traslado usando los catálogos de la vista.
	function counterparty(guide) {
		if (guide.idTipoOperacionGuia === 1)
			return (
				byId(suppliers, guide.idProveedor, "idProveedor")
					?.razonSocial ||
				(guide.idProveedor ? `Proveedor #${guide.idProveedor}` : "—")
			);
		if (guide.idTipoOperacionGuia === 2)
			return (
				byId(clients, guide.idCliente, "idCliente")?.razonSocial ||
				(guide.idCliente ? `Cliente #${guide.idCliente}` : "—")
			);
		return `${warehouseName(guide.idAlmacenOrigen)} → ${warehouseName(guide.idAlmacenDestino)}`;
	}

	// Formatea fechas de la API para mostrarlas de forma localizada en la pantalla.
	function formatDate(value, withTime = true) {
		if (!value) return "—";
		const date = new Date(value);
		if (Number.isNaN(date.getTime())) return "—";
		return new Intl.DateTimeFormat(
			"es-PE",
			withTime
				? { dateStyle: "medium", timeStyle: "short" }
				: { dateStyle: "medium" },
		).format(date);
	}

	// Configura el atributo min del selector para bloquear fechas anteriores a hoy.
	function setMinimumProgramDate() {
		const now = new Date();
		const localDate = new Date(
			now.getTime() - now.getTimezoneOffset() * 60000,
		)
			.toISOString()
			.slice(0, 10);
		dateInput.min = `${localDate}T00:00`;
	}

	// Normaliza valores para comparar texto sin distinguir mayúsculas al filtrar guías.
	function escapeForSearch(value) {
		return String(value ?? "").toLocaleLowerCase("es");
	}

	// Llena los filtros de operación y estado con las opciones reconocidas por la vista.
	function setupFilters() {
		for (const operation of OPERATIONS) {
			operationFilter.append(
				new Option(operation.name, String(operation.id)),
			);
			// Actualiza los campos requeridos al cambiar el tipo de operación.
			select("#guide-operation").append(
				new Option(operation.name, String(operation.id)),
			);
		}
		for (const status of STATUSES)
			statusFilter.append(new Option(status.name, String(status.id)));
	}

	// Construye las opciones de un selector a partir de una lista recibida del API.
	function fillSelect(select, items, valueKey, labelFor) {
		select.replaceChildren(new Option("Selecciona una opción", ""));
		for (const item of items)
			select.append(new Option(labelFor(item), String(item[valueKey])));
	}

	// Consulta productos, almacenes, clientes y proveedores requeridos por el formulario.
	async function loadLookups() {
		const endpoints = [
			["Productos", "/api/ProductoApi"],
			["Almacenes", "/api/AlmacenApi"],
			["Clientes", "/api/ClienteApi"],
			["Proveedores", "/api/ProveedorApi"],
		];
		const results = await Promise.allSettled(
			endpoints.map(([, url]) => fetchApi(url)),
		);
		const failures = [];
		results.forEach((result, index) => {
			if (result.status === "rejected")
				failures.push(endpoints[index][0]);
			else {
				if (index === 0) products = result.value;
				if (index === 1) warehouses = result.value;
				if (index === 2) clients = result.value;
				if (index === 3) suppliers = result.value;
			}
		});

		// Añade una marca visible a los registros inactivos ofrecidos por el API.
		const inactiveSuffix = (item) =>
			item.activo === false ? " (inactivo)" : "";
		fillSelect(
			select("#guide-provider"),
			suppliers,
			"idProveedor",
			(item) => `${item.razonSocial}${inactiveSuffix(item)}`,
		);
		fillSelect(
			select("#guide-client"),
			clients,
			"idCliente",
			(item) => `${item.razonSocial}${inactiveSuffix(item)}`,
		);
		fillSelect(
			select("#guide-origin"),
			warehouses,
			"idAlmacen",
			(item) =>
				`${item.codigo} · ${item.descripcion || "Almacén"}${inactiveSuffix(item)}`,
		);
		fillSelect(
			select("#guide-destination"),
			warehouses,
			"idAlmacen",
			(item) =>
				`${item.codigo} · ${item.descripcion || "Almacén"}${inactiveSuffix(item)}`,
		);

		lookupsReady = failures.length === 0;
		// Abre el formulario de creación de una guía.
		select("#new-guide").disabled = !lookupsReady;
		if (!lookupsReady)
			lookupWarning = `No se pudieron cargar los catálogos de ${failures.join(", ")}. Puedes consultar las guías; el registro y la edición quedan deshabilitados.`;
	}

	// Actualiza los contadores de guías usando los resultados cargados desde el API.
	function updateSummary() {
		select("#guide-total").textContent = String(guides.length);
		select("#guide-pending").textContent = String(
			guides.filter((guide) => guide.idEstadoGuia === 1).length,
		);
		select("#guide-progress").textContent = String(
			guides.filter((guide) => guide.idEstadoGuia === 2).length,
		);
		select("#guide-cancelled").textContent = String(
			guides.filter((guide) => guide.idEstadoGuia === 4).length,
		);
	}

	// Crea una celda segura usando textContent para mostrar valores de la respuesta.
	function createCell(row, value, className = "") {
		const cell = document.createElement("td");
		if (className) cell.className = className;
		cell.textContent = value ?? "—";
		row.append(cell);
		return cell;
	}

	// Filtra localmente las guías según el texto de búsqueda de la vista.
	function visibleGuides() {
		const query = escapeForSearch(search.value.trim());
		return guides.filter((guide) => {
			const text = [
				guide.numeroGuia,
				operationName(guide.idTipoOperacionGuia),
				counterparty(guide),
				guide.tipoDocumentoReferencia,
				guide.numeroDocumentoReferencia,
			]
				.map(escapeForSearch)
				.join(" ");
			return !query || text.includes(query);
		});
	}

	// Dibuja las guías recibidas del API, sus estados y acciones disponibles.
	function renderGuides() {
		const items = visibleGuides();
		rows.replaceChildren();
		if (items.length === 0) {
			const row = document.createElement("tr");
			const cell = document.createElement("td");
			cell.colSpan = 6;
			cell.className = "guide-empty";
			cell.textContent = guides.length
				? "No hay guías que coincidan con la búsqueda."
				: "Todavía no hay guías para mostrar.";
			row.append(cell);
			rows.append(row);
			select("#guide-count").textContent = `0 de ${guides.length} guías`;
			return;
		}

		for (const guide of items) {
			const row = document.createElement("tr");
			const numberCell = createCell(row, "", "guide-number-cell");
			const number = document.createElement("strong");
			number.textContent = guide.numeroGuia;
			const id = document.createElement("small");
			id.textContent = `Registro #${guide.idGuia}`;
			numberCell.append(number, id);

			const operationCell = createCell(row, "", "guide-operation-cell");
			const operation = document.createElement("strong");
			operation.textContent = operationName(guide.idTipoOperacionGuia);
			const reference = document.createElement("small");
			reference.textContent = counterparty(guide);
			operationCell.append(operation, reference);

			createCell(
				row,
				formatDate(guide.fechaProgramada),
				"guide-date-cell",
			);
			const productCell = createCell(row, "", "guide-product-cell");
			const quantity = guide.detalles?.length || 0;
			productCell.textContent = `${quantity} ${quantity === 1 ? "producto" : "productos"}`;

			const statusCell = createCell(row, "");
			const info = statusInfo(guide.idEstadoGuia);
			const badge = document.createElement("span");
			badge.className = `guide-status guide-status--${info.css}`;
			badge.textContent = info.name;
			statusCell.append(badge);

			const actions = document.createElement("td");
			actions.className = "guide-actions";
			actions.append(
				actionButton("Ver", "guide-action", () =>
					openDetail(guide.idGuia),
				),
			);
			if (guide.idEstadoGuia === 1) {
				const edit = actionButton("Editar", "guide-action", () =>
					openEdit(guide.idGuia),
				);
				edit.disabled = !lookupsReady;
				actions.append(
					edit,
					actionButton(
						"Cancelar",
						"guide-action guide-action--danger",
						() => cancelGuide(guide),
					),
				);
			}
			row.append(actions);
			rows.append(row);
		}
		select("#guide-count").textContent =
			`${items.length} de ${guides.length} ${guides.length === 1 ? "guía" : "guías"}`;
	}

	// Crea un botón de acción y conecta su comportamiento sin insertar HTML de datos.
	function actionButton(label, className, handler) {
		const button = document.createElement("button");
		button.type = "button";
		button.className = className;
		button.textContent = label;
		button.addEventListener("click", handler);
		return button;
	}

	// Consulta GET /api/guias con los filtros elegidos y actualiza listado y resumen.
	async function loadGuides(silent = false) {
		if (loadingGuides) return;
		loadingGuides = true;
		clearAlert();
		if (!silent) rows.innerHTML =
			'<tr><td class="guide-empty" colspan="6">Consultando guías...</td></tr>';
		const params = new URLSearchParams();
		if (operationFilter.value)
			params.set("idTipoOperacionGuia", operationFilter.value);
		if (statusFilter.value) params.set("idEstadoGuia", statusFilter.value);
		const url = params.size ? `${API}?${params}` : API;
		try {
			guides = await fetchApi(url);
			updateSummary();
			renderGuides();
			if (lookupWarning)
				window.showAppError(
					lookupWarning,
					"No se cargaron todos los catálogos",
				);
		} catch (error) {
			rows.replaceChildren();
			updateSummary();
			select("#guide-count").textContent = "";
			window.showAppError(error.message || "No se pudieron consultar las guías.");
		} finally {
			loadingGuides = false;
		}
	}

	// Muestra y exige únicamente los campos que corresponden a la operación seleccionada.
	function setOperationFields(operationId) {
		const operation = Number(operationId);
		const fields = [
			["#guide-provider-wrap", "#guide-provider", operation === 1],
			["#guide-client-wrap", "#guide-client", operation === 2],
			[
				"#guide-origin-wrap",
				"#guide-origin",
				operation === 2 || operation === 3,
			],
			[
				"#guide-destination-wrap",
				"#guide-destination",
				operation === 1 || operation === 3,
			],
		];
		for (const [wrapperSelector, inputSelector, visible] of fields) {
			const wrapper = select(wrapperSelector);
			const input = select(inputSelector);
			wrapper.hidden = !visible;
			input.disabled = !visible;
			input.required = visible;
			if (!visible) input.value = "";
		}
	}

	// En salidas y traslados limita el catálogo al stock disponible en el origen.
	async function refreshProductChoices() {
		const operation = Number(select("#guide-operation").value);
		const originId = Number(select("#guide-origin").value);
		const requiresOriginStock = operation === 2 || operation === 3;
		const version = ++stockLookupVersion;

		if (!requiresOriginStock) {
			productsWithOriginStock = null;
			updateProductSelects();
			return;
		}

		productsWithOriginStock = new Set();
		updateProductSelects();
		if (!originId) return;

		try {
			const stock = await fetchApi(`/api/stocks?idAlmacen=${encodeURIComponent(originId)}`);
			if (version !== stockLookupVersion) return;
			productsWithOriginStock = new Set(
				stock.filter((item) => Number(item.cantidadActual) > 0)
					.map((item) => Number(item.idProducto)),
			);
			updateProductSelects();
		} catch (error) {
			if (version !== stockLookupVersion) return;
			productsWithOriginStock = new Set();
			updateProductSelects();
			window.showAppError(error.message || "No se pudo consultar el stock del almacén de origen.", "Stock no disponible");
		}
	}

	// Actualiza las líneas preservando la selección si el producto sigue disponible.
	function updateProductSelects() {
		const choices = productsWithOriginStock === null
			? products
			: products.filter((item) => productsWithOriginStock.has(Number(item.idProducto)));
		for (const product of document.querySelectorAll(".guide-line__product")) {
			const selectedId = product.value;
			product.replaceChildren(new Option(
				productsWithOriginStock === null || !select("#guide-origin").value
					? "Selecciona un producto"
					: choices.length ? "Selecciona un producto con stock" : "Sin productos con stock en el origen",
				"",
			));
			for (const item of choices) {
				product.append(new Option(
					`${item.codigo} · ${item.descripcion}${item.activo === false ? " (inactivo)" : ""}`,
					String(item.idProducto),
				));
			}
			if (choices.some((item) => Number(item.idProducto) === Number(selectedId)))
				product.value = selectedId;
		}
	}

	// Agrega una línea de producto y cantidad al formulario, evitando productos repetidos.
	function addLine(productId = "", quantity = "") {
		const line = document.createElement("div");
		line.className = "guide-line";
		const product = document.createElement("select");
		product.className = "form-select guide-line__product";
		product.required = true;
		product.append(new Option("Selecciona un producto", ""));
		const choices = productsWithOriginStock === null
			? products
			: products.filter((item) => productsWithOriginStock.has(Number(item.idProducto)));
		for (const item of choices)
			product.append(
				new Option(
					`${item.codigo} · ${item.descripcion}${item.activo === false ? " (inactivo)" : ""}`,
					String(item.idProducto),
				),
			);
		if (
			productId &&
			!choices.some(
				(item) => Number(item.idProducto) === Number(productId),
			)
		) {
			product.append(
				new Option(
					`Producto #${productId} (inactivo)`,
					String(productId),
				),
			);
		}
		product.value = productId ? String(productId) : "";
		// Comprueba inmediatamente que el producto elegido no esté ya en otra línea.
		product.addEventListener("change", () => {
			if (!product.value) return;

			const duplicate = [
				...document.querySelectorAll(".guide-line__product"),
			].some((item) => item !== product && item.value === product.value);

			if (duplicate) {
				product.value = "";
				window.showAppError(
					"Ese producto ya está agregado. Cada producto solo puede aparecer una vez.",
					"Revisa los productos",
				);
				product.focus();
				return;
			}

			clearAlert(formAlert);
		});

		const amount = document.createElement("input");
		amount.className = "form-control guide-line__quantity";
		amount.type = "number";
		amount.min = "0.0001";
		amount.step = "0.0001";
		amount.placeholder = "0.0000";
		amount.required = true;
		amount.value = quantity;

		const remove = document.createElement("button");
		remove.type = "button";
		remove.className = "guide-remove-line";
		remove.setAttribute("aria-label", "Quitar producto");
		remove.title = "Quitar producto";
		remove.textContent = "×";
		// Quita la línea seleccionada y actualiza el contador de productos.
		remove.addEventListener("click", () => {
			line.remove();
			updateGuideLineCount();
		});
		line.append(product, amount, remove);
		select("#guide-lines").append(line);
		updateGuideLineCount();
	}

	// Actualiza el indicador visual con el número de líneas de producto.
	function updateGuideLineCount() {
		select("#guide-line-count").textContent = String(
			select("#guide-lines").children.length,
		);
	}

	// Desplaza el modal hasta la sección elegida y marca su pestaña como activa.
	function jumpToSection(button) {
		const container = editorElement.querySelector(".modal-body");
		const target = document.getElementById(button.dataset.guideJump);
		if (!container || !target) return;
		const top =
			target.getBoundingClientRect().top -
			container.getBoundingClientRect().top +
			container.scrollTop;
		container.scrollTo({ top: Math.max(0, top - 60), behavior: "smooth" });
		editorElement.querySelectorAll("[data-guide-jump]").forEach((item) => {
			const active = item === button;
			item.classList.toggle("is-active", active);
			if (active) item.setAttribute("aria-current", "step");
			else item.removeAttribute("aria-current");
		});
	}

	// Limpia el modal y prepara una guía nueva con fecha de hoy y una línea inicial.
	function resetForm() {
		editingId = null;
		productsWithOriginStock = null;
		stockLookupVersion++;
		form.reset();
		select("#guide-editor-title").textContent = "Registrar guía";
		select("#guide-editor-hint").textContent =
			"Completa los datos de la operación y agrega sus productos.";
		select("#save-guide-label").textContent = "Guardar guía";
		// Actualiza los campos requeridos al cambiar el tipo de operación.
		select("#guide-operation").disabled = false;
		select("#guide-lines").replaceChildren();
		updateGuideLineCount();
		select("#guide-reference-type").value = "";
		select("#guide-reference-number").value = "";
		select("#guide-observation").value = "";
		// Actualiza los campos requeridos al cambiar el tipo de operación.
		const operation = select("#guide-operation");
		operation.value = "1";
		setOperationFields(operation.value);
		editorElement.querySelector(".modal-body").scrollTop = 0;
		setMinimumProgramDate();
		const now = new Date();
		now.setMinutes(now.getMinutes() - now.getTimezoneOffset());
		dateInput.value = now.toISOString().slice(0, 16);
		addLine();
		clearAlert(formAlert);
	}

	// Abre el modal de creación después de confirmar que los catálogos están listos.
	function openCreate() {
		if (!lookupsReady) return;
		resetForm();
		editor.show();
	}

	// Convierte la fecha recibida por API al formato local de input datetime-local.
	function toDateInput(value) {
		if (!value) return "";
		const date = new Date(value);
		if (Number.isNaN(date.getTime())) return "";
		date.setMinutes(date.getMinutes() - date.getTimezoneOffset());
		return date.toISOString().slice(0, 16);
	}

	// Consulta GET /api/guias/{id} y rellena el modal con la guía seleccionada.
	async function openEdit(id) {
		if (!lookupsReady) return;
		clearAlert(formAlert);
		try {
			const guide = await fetchApi(`${API}/${id}`);
			if (guide.idEstadoGuia !== 1)
				throw new Error("Solo se pueden editar guías pendientes.");
			resetForm();
			editingId = id;
			select("#guide-editor-title").textContent =
				`Editar ${guide.numeroGuia}`;
			select("#guide-editor-hint").textContent =
				"La operación queda fija durante la edición. Puedes ajustar referencias, fecha y productos.";
			select("#save-guide-label").textContent = "Guardar cambios";
			// Actualiza los campos requeridos al cambiar el tipo de operación.
			select("#guide-operation").value = String(
				guide.idTipoOperacionGuia,
			);
			// Actualiza los campos requeridos al cambiar el tipo de operación.
			select("#guide-operation").disabled = true;
			setOperationFields(guide.idTipoOperacionGuia);
			select("#guide-provider").value = guide.idProveedor
				? String(guide.idProveedor)
				: "";
			select("#guide-client").value = guide.idCliente
				? String(guide.idCliente)
				: "";
			select("#guide-origin").value = guide.idAlmacenOrigen
				? String(guide.idAlmacenOrigen)
				: "";
			select("#guide-destination").value = guide.idAlmacenDestino
				? String(guide.idAlmacenDestino)
				: "";
			select("#guide-date").value = toDateInput(guide.fechaProgramada);
			select("#guide-reference-type").value =
				guide.tipoDocumentoReferencia || "";
			select("#guide-reference-number").value =
				guide.numeroDocumentoReferencia || "";
			select("#guide-observation").value = guide.observacion || "";
			select("#guide-lines").replaceChildren();
			await refreshProductChoices();
			for (const line of guide.detalles || [])
				addLine(line.idProducto, line.cantidadEsperada);
			editorElement.querySelector(".modal-body").scrollTop = 0;
			editor.show();
		} catch (error) {
			window.showAppError(
				error.message || "No se pudo cargar la guía para editar.",
			);
		}
	}

	// Construye el objeto JSON que la API espera para crear o actualizar una guía.
	function buildPayload() {
		// Actualiza los campos requeridos al cambiar el tipo de operación.
		const operationId = Number(select("#guide-operation").value);
		// Lee el identificador del selector activo o devuelve null si el campo no aplica.
		const getId = (selector) =>
			select(selector).disabled || !select(selector).value
				? null
				: Number(select(selector).value);
		const details = [...document.querySelectorAll(".guide-line")].map(
			(line) => ({
				idProducto: Number(
					line.querySelector(".guide-line__product").value,
				),
				cantidadEsperada: Number(
					line.querySelector(".guide-line__quantity").value,
				),
			}),
		);
		return {
			...(editingId === null ? { idTipoOperacionGuia: operationId } : {}),
			idProveedor: getId("#guide-provider"),
			idCliente: getId("#guide-client"),
			idAlmacenOrigen: getId("#guide-origin"),
			idAlmacenDestino: getId("#guide-destination"),
			fechaProgramada: select("#guide-date").value,
			tipODocumentoReferencia:
				select("#guide-reference-type").value.trim() || null,
			numeroDocumentoReferencia:
				select("#guide-reference-number").value.trim() || null,
			observacion: select("#guide-observation").value.trim() || null,
			detalles: details,
		};
	}

	// Valida reglas de la interfaz antes de enviar datos al endpoint de Guías.
	function validateForm(payload) {
		setMinimumProgramDate();
		if (dateInput.value.slice(0, 10) < dateInput.min.slice(0, 10)) {
			window.showAppError(
				"La fecha programada no puede ser anterior a hoy.",
				"Revisa la fecha",
			);
			dateInput.focus();
			return false;
		}
		if (!form.reportValidity()) return false;
		if (!lookupsReady) {
			window.showAppError(
				"Los catálogos no están disponibles. Recarga la página e inténtalo de nuevo.",
				"Datos necesarios no disponibles",
			);
			return false;
		}
		if (!payload.detalles.length) {
			window.showAppError(
				"No se permite enviar una guía con 0 productos. Agrega al menos un producto.",
				"Agrega productos a la guía",
			);
			return false;
		}
		const productIds = payload.detalles.map((line) => line.idProducto);
		if (productIds.some((id) => !id)) {
			window.showAppError(
				"Selecciona un producto válido en cada línea.",
				"Revisa los productos",
			);
			return false;
		}
		if (new Set(productIds).size !== productIds.length) {
			window.showAppError(
				"Un producto no puede repetirse en la misma guía.",
				"Revisa los productos",
			);
			return false;
		}
		if (
			payload.detalles.some(
				(line) =>
					!Number.isFinite(line.cantidadEsperada) ||
					line.cantidadEsperada <= 0,
			)
		) {
			window.showAppError(
				"Todas las cantidades deben ser mayores a cero.",
				"Revisa las cantidades",
			);
			return false;
		}
		if (
			operationIdForPayload(payload) === 3 &&
			payload.idAlmacenOrigen === payload.idAlmacenDestino
		) {
			window.showAppError(
				"El almacén de origen y el de destino deben ser distintos.",
				"Revisa los almacenes",
			);
			return false;
		}
		return true;
	}

	// Obtiene la operación enviada en creación o la operación fija durante la edición.
	function operationIdForPayload(payload) {
		return editingId === null
			? Number(payload.idTipoOperacionGuia)
			: // Actualiza los campos requeridos al cambiar el tipo de operación.
				Number(select("#guide-operation").value);
	}

	// Envía POST /api/guias o PUT /api/guias/{id} y refresca el listado.
	async function saveGuide(event) {
		event.preventDefault();
		clearAlert(formAlert);
		const payload = buildPayload();
		if (!validateForm(payload)) return;
		const button = select("#save-guide");
		button.disabled = true;
		button.classList.add("is-saving");
		try {
			const wasEditing = editingId !== null;
			const url = wasEditing ? `${API}/${editingId}` : API;
			await fetchApi(url, {
				method: wasEditing ? "PUT" : "POST",
				body: JSON.stringify(payload),
			});
			editor.hide();
			await loadGuides();
			await window.showAppSuccess(
				wasEditing
					? "La guía se actualizó correctamente."
					: "La guía se registró correctamente.",
				wasEditing ? "Guía actualizada" : "Guía registrada",
			);
		} catch (error) {
			window.showAppError(
				error.message || "No se pudo guardar la guía.",
			);
		} finally {
			button.disabled = false;
			button.classList.remove("is-saving");
		}
	}

	// Envía PUT /api/guias/{id}/cancelar tras confirmar la acción con la persona usuaria.
	async function cancelGuide(guide) {
		if (
			!window.confirm(
				`¿Cancelar la guía ${guide.numeroGuia}? Esta operación no se puede deshacer desde esta pantalla.`,
			)
		)
			return;
		try {
			await fetchApi(`${API}/${guide.idGuia}/cancelar`, {
				method: "PUT",
			});
			await loadGuides();
			showAlert(`La guía ${guide.numeroGuia} fue cancelada.`);
		} catch (error) {
			window.showAppError(error.message || "No se pudo cancelar la guía.");
		}
	}

	// Consulta GET /api/guias/{id} y abre la vista de detalle de la guía.
	async function openDetail(id) {
		try {
			const guide = await fetchApi(`${API}/${id}`);
			renderDetail(guide);
			detailModal.show();
		} catch (error) {
			window.showAppError(
				error.message || "No se pudo consultar el detalle de la guía.",
			);
		}
	}

	// Presenta datos y productos de la respuesta de detalle sin confiar en HTML externo.
	function renderDetail(guide) {
		select("#guide-detail-title").textContent = guide.numeroGuia;
		select("#guide-detail-subtitle").textContent =
			`${operationName(guide.idTipoOperacionGuia)} · ${statusInfo(guide.idEstadoGuia).name}`;
		const summary = select("#guide-detail-summary");
		summary.replaceChildren();
		const details = [
			["Programada", formatDate(guide.fechaProgramada)],
			["Registrada", formatDate(guide.fechaRegistro)],
			[
				"Referencia",
				[guide.tipoDocumentoReferencia, guide.numeroDocumentoReferencia]
					.filter(Boolean)
					.join(" · ") || "—",
			],
			[
				"Tercero",
				guide.idTipoOperacionGuia === 3 ? "—" : counterparty(guide),
			],
			[
				"Almacenes",
				guide.idTipoOperacionGuia === 1
					? `Destino: ${warehouseName(guide.idAlmacenDestino)}`
					: guide.idTipoOperacionGuia === 2
						? `Origen: ${warehouseName(guide.idAlmacenOrigen)}`
						: `Origen: ${warehouseName(guide.idAlmacenOrigen)} → Destino: ${warehouseName(guide.idAlmacenDestino)}`,
			],
			["Observación", guide.observacion || "—"],
		];
		for (const [label, value] of details) {
			const item = document.createElement("div");
			const caption = document.createElement("small");
			caption.textContent = label;
			const content = document.createElement("strong");
			content.textContent = value;
			item.append(caption, content);
			summary.append(item);
		}

		const body = select("#guide-detail-lines");
		body.replaceChildren();
		for (const line of guide.detalles || []) {
			const row = document.createElement("tr");
			const productCell = document.createElement("td");
			const product = byId(products, line.idProducto, "idProducto");
			productCell.textContent = product
				? `${product.codigo} · ${product.descripcion}`
				: `Producto #${line.idProducto}`;
			const amountCell = document.createElement("td");
			amountCell.className = "text-end";
			amountCell.textContent = Number(
				line.cantidadEsperada,
			).toLocaleString("es-PE", {
				minimumFractionDigits: 0,
				maximumFractionDigits: 4,
			});
			row.append(productCell, amountCell);
			body.append(row);
		}
		if (!body.children.length) {
			const row = document.createElement("tr");
			const cell = document.createElement("td");
			cell.colSpan = 2;
			cell.textContent = "La guía no contiene productos.";
			row.append(cell);
			body.append(row);
		}
	}

	setMinimumProgramDate();
	setupFilters();
	// Actualiza los campos requeridos al cambiar el tipo de operación.
	select("#guide-operation").addEventListener("change", (event) => {
		setOperationFields(event.target.value);
		void refreshProductChoices();
	});
	select("#guide-origin").addEventListener("change", () => {
		void refreshProductChoices();
	});
	// Abre el formulario de creación de una guía.
	select("#new-guide").addEventListener("click", openCreate);
	// Agrega una línea vacía para seleccionar producto y cantidad.
	select("#add-guide-line").addEventListener("click", () => addLine());
	editorElement.querySelectorAll("[data-guide-jump]").forEach((button) => {
		button.addEventListener("click", () => jumpToSection(button));
	});
	// Conecta el envío del formulario con la validación y el guardado por API.
	form.addEventListener("submit", saveGuide);
	// Vuelve a consultar el endpoint de listado al filtrar por tipo de operación.
	operationFilter.addEventListener("change", loadGuides);
	// Vuelve a consultar el endpoint de listado al filtrar por estado.
	statusFilter.addEventListener("change", loadGuides);
	// Actualiza en pantalla el listado según el texto escrito.
	search.addEventListener("input", renderGuides);

	(async () => {
		await loadLookups();
		await loadGuides();
		if (app.dataset.openCreate === "true" && lookupsReady) openCreate();
	})();
	window.setInterval(() => {
		if (document.hidden || editorElement.classList.contains("show") || detailElement.classList.contains("show")) return;
		void loadGuides(true);
	}, 15000);
});
