import { getJson } from "/shared/js/api-client.js";
import "/shared/js/navigation.js";
import {
  deleteGuideReading, finalizeGuideReadings, formatScheduledAt, getActiveLocations, getGuideDetailLookups,
  getGuideReadingHistory, getMyGuideById, registerGuideReading, showApiError
} from "/modules/operaciones/guias.js";

const page = document.querySelector(".registration-page");
const message = document.querySelector("#registration-message");
const guideId = Number(new URLSearchParams(location.search).get("id"));
const mode = localStorage.getItem("projectaw.operator.capture-mode") || "pda";
const additionalInfo = {
  lot: localStorage.getItem("projectaw.operator.lot-enabled") === "true",
  serial: localStorage.getItem("projectaw.operator.serial-enabled") === "true",
  expiry: localStorage.getItem("projectaw.operator.expiry-enabled") === "true"
};
const byId = id => document.querySelector(`#${id}`);
const fields = {
  productCode: byId("scan-code"), productName: byId("product-name"), productQuantity: byId("read-quantity"),
  productLot: byId("product-lot"), productSerial: byId("product-serial"), productExpiry: byId("product-expiry"),
  allocationCode: byId("allocation-code"), allocationProduct: byId("allocation-product"), allocationQuantity: byId("allocation-quantity"),
  origin: byId("origin-location"), destination: byId("destination-location"), outboundSource: byId("outbound-source-location"),
  observation: byId("read-observation")
};
byId("product-lot-field").hidden = !additionalInfo.lot;
byId("product-serial-field").hidden = !additionalInfo.serial;
byId("product-expiry-field").hidden = !additionalInfo.expiry;
let guide;
let products = [];
let history = [];
let stockRows = [];
let lookupData;
let flow = { step: 1, captured: [] };
let hasDispatchLocations = false;
let cameraStream;
let cameraTimer;
let detector;
let detecting = false;
let activeCodeField = fields.productCode;
let cameraPausedUntil = 0;
let lastCameraCode = null;
let cameraMissFrames = 0;
const locations = { origin: [], destination: [] };
const flowKey = `projectaw.operator.guide-flow.${guideId}`;

byId("current-year").textContent = String(new Date().getFullYear());
byId("registration-back").href = Number.isSafeInteger(guideId) && guideId > 0
  ? `/modules/operaciones/guias/detalle.html?id=${encodeURIComponent(guideId)}`
  : "/modules/operaciones/guias/mis-guias.html";
byId("clear-product").addEventListener("click", clearProductFields);
byId("capture-product").addEventListener("click", captureProduct);
byId("save-allocation").addEventListener("click", () => void saveAllocation());
byId("clear-allocation").addEventListener("click", clearAllocationFields);
byId("to-locations").addEventListener("click", () => void continueFromProducts());
byId("back-products").addEventListener("click", () => goToStep(1));
byId("to-consolidated").addEventListener("click", () => goToStep(3));
byId("back-locations").addEventListener("click", () => goToStep(isDispatch() && !hasDispatchLocations ? 1 : 2));
byId("refresh-consolidated").addEventListener("click", () => void refreshHistory());
byId("consolidated-readings").addEventListener("click", event => void removeAllocation(event));
byId("finalize-registration").addEventListener("click", openFinalizeConfirmation);
byId("finalize-cancel").addEventListener("click", () => byId("finalize-dialog").close());
byId("finalize-confirm").addEventListener("click", () => void finalize());
byId("api-error-close").addEventListener("click", () => byId("api-error-dialog").close());
byId("guide-note-toggle").addEventListener("click", toggleNote);
byId("guide-note-close").addEventListener("click", closeNote);
byId("camera-start").addEventListener("click", () => void startCamera());
byId("camera-stop").addEventListener("click", stopCamera);
document.querySelectorAll("[data-step-go]").forEach(button => button.addEventListener("click", () => {
  const step = Number(button.dataset.stepGo);
  if (isDispatch() && !hasDispatchLocations && step === 2) return;
  if (isDispatch() && !hasDispatchLocations && step === 3 && flow.step === 1) { void continueFromProducts(); return; }
  goToStep(step);
}));

for (const input of [fields.productCode, fields.allocationCode]) {
  input.addEventListener("focus", () => { activeCodeField = input; });
  input.addEventListener("input", () => resolveProductInput(input));
  input.addEventListener("keydown", event => {
    if (event.key === "Enter") {
      event.preventDefault();
      const product = resolveProductInput(input);
      if (product) (input === fields.productCode ? fields.productQuantity : fields.allocationQuantity).focus();
    }
  });
}
for (const input of [fields.outboundSource, fields.destination]) input.addEventListener("focus", () => { activeCodeField = input; });
fields.productQuantity.addEventListener("keydown", event => { if (event.key === "Enter") { event.preventDefault(); captureProduct(); } });
fields.allocationQuantity.addEventListener("keydown", event => { if (event.key === "Enter") { event.preventDefault(); if (isDispatch()) fields.destination.focus(); else (byId("origin-field").hidden ? fields.destination : fields.origin).focus(); } });
fields.origin.addEventListener("keydown", event => { if (event.key === "Enter") { event.preventDefault(); byId("destination-field").hidden ? focusAfterLocations() : fields.destination.focus(); } });
fields.destination.addEventListener("keydown", event => { if (event.key === "Enter") { event.preventDefault(); isDispatch() ? fields.allocationCode.focus() : focusAfterLocations(); } });
fields.outboundSource.addEventListener("keydown", event => { if (event.key === "Enter") { event.preventDefault(); fields.productCode.focus(); } });
fields.observation.addEventListener("keydown", event => { if (event.key === "Enter") { event.preventDefault(); void saveAllocation(); } });
document.addEventListener("visibilitychange", () => { if (document.hidden) stopCamera(); });
window.addEventListener("pagehide", stopCamera, { once: true });

if (!Number.isSafeInteger(guideId) || guideId < 1) showError("El identificador de la guía no es válido.");
else void loadRegistration();

async function loadRegistration() {
  page.setAttribute("aria-busy", "true");
  try {
    guide = await getMyGuideById(guideId);
    if (![1, 2].includes(Number(guide.idTipoOperacionGuia))) {
      throw new Error("El operador solo puede registrar guías de recepción o despacho.");
    }
    if (guide.idEstadoGuia !== 2) throw new Error("Esta guía ya no está disponible para registrar.");
    products = await getJson("api/ProductoApi");
    if (!Array.isArray(products)) throw new Error("La API no devolvió el catálogo de productos correctamente.");
    lookupData = await getGuideDetailLookups(guide, products);
    await loadLocations();
    if (isDispatch()) stockRows = await getJson(`api/stocks?idAlmacen=${encodeURIComponent(guide.idAlmacenOrigen)}`);
    history = await getGuideReadingHistory(guideId);
    restoreFlow();
    renderGuide();
    if (isDispatch() && !hasDispatchLocations && flow.step === 2) flow.step = 3;
    renderAll();
    configureCaptureMode();
    goToStep(flow.step, false);
  } catch (error) {
    showApiFailure(showApiError(error));
  } finally { page.setAttribute("aria-busy", "false"); }
}

function restoreFlow() {
  try {
    const saved = JSON.parse(localStorage.getItem(flowKey) || "null");
    if (saved && Array.isArray(saved.captured)) flow = { step: Number(saved.step) || 1, captured: saved.captured };
  } catch { flow = { step: 1, captured: [] }; }

  const merged = new Map();
  for (const item of flow.captured) {
    const id = Number(item.idProducto);
    const key = isDispatch() ? `${id}|${item.idUbicacionOrigen || ""}` : `${id}|${item.lote || ""}|${item.serie || ""}`;
    const current = merged.get(key);
    if (current) current.cantidad = Number(current.cantidad) + Number(item.cantidad);
    else merged.set(key, { ...item, idProducto: id, cantidad: Number(item.cantidad) });
  }
  flow.captured = [...merged.values()];

  // Si hay lecturas guardadas de una sesión anterior, estas también cuentan como productos capturados.
  const totals = new Map();
  for (const item of history) totals.set(Number(item.idProducto), (totals.get(Number(item.idProducto)) || 0) + Number(item.cantidad));
  if (!isDispatch()) {
    for (const [idProducto, cantidad] of totals) {
      const captured = flow.captured.filter(item => Number(item.idProducto) === idProducto).reduce((sum, item) => sum + Number(item.cantidad), 0);
      if (captured < cantidad) {
        const product = products.find(item => Number(item.idProducto) === idProducto);
        flow.captured.push({ idProducto, codigo: product?.codigo || "", descripcion: product?.descripcion || "", cantidad: cantidad - captured, recuperado: true });
      }
    }
  }
  if (isDispatch()) {
    const persistedBySource = new Map();
    for (const item of history) {
      const key = `${Number(item.idProducto)}|${Number(item.idUbicacionOrigen || 0)}`;
      persistedBySource.set(key, (persistedBySource.get(key) || 0) + Number(item.cantidad));
    }
    for (const [key, cantidad] of persistedBySource) {
      const [idProducto, idUbicacionOrigen] = key.split("|").map(Number);
      const captured = flow.captured.filter(item => Number(item.idProducto) === idProducto && Number(item.idUbicacionOrigen) === idUbicacionOrigen).reduce((sum, item) => sum + Number(item.cantidad), 0);
      if (captured < cantidad) {
        const stock = stockRows.find(item => Number(item.idProducto) === idProducto && Number(item.idUbicacion || 0) === idUbicacionOrigen);
        const locationItem = locations.origin.find(item => Number(item.idUbicacion) === idUbicacionOrigen);
        const product = productById(idProducto);
        flow.captured.push({ idProducto, codigo: product?.codigo || "", descripcion: product?.descripcion || "", cantidad: cantidad - captured, idUbicacionOrigen, ubicacionOrigen: locationItem?.codigo || "", recuperado: true, cantidadStock: Number(stock?.cantidadActual || 0) });
      }
    }
  }
  if (!flow.captured.length && flow.step > 1) flow.step = 1;
  persistFlow();
}

function persistFlow() { localStorage.setItem(flowKey, JSON.stringify(flow)); }

function renderGuide() {
  const operationNames = { 1: "Recepción", 2: "Despacho" };
  const operationName = operationNames[guide.idTipoOperacionGuia] || "Registro";
  document.title = `${operationName} · ${guide.numeroGuia} | Proyecto AW`;
  byId("registration-title").textContent = operationName;
  byId("registration-subtitle").textContent = `${guide.numeroGuia || `Guía #${guide.idGuia}`} · ${formatScheduledAt(guide.fechaProgramada)}`;
  byId("products-step-title").textContent = guide.idTipoOperacionGuia === 1 ? "Recepción de productos" : "Productos a despachar";
  byId("registration-back").href = `/modules/operaciones/guias/detalle.html?id=${encodeURIComponent(guideId)}`;
  const needsOrigin = guide.idTipoOperacionGuia === 2;
  const needsDestination = guide.idTipoOperacionGuia === 1;
  const dispatch = isDispatch();
  byId("origin-field").hidden = dispatch || !needsOrigin;
  byId("destination-field").hidden = dispatch ? false : !needsDestination;
  byId("product-lot-field").hidden = dispatch || !additionalInfo.lot;
  byId("product-serial-field").hidden = dispatch || !additionalInfo.serial;
  byId("product-expiry-field").hidden = dispatch || !additionalInfo.expiry;
  byId("outbound-source-field").hidden = !dispatch;
  byId("product-code-field").firstChild.textContent = dispatch ? "Código del producto a extraer" : "Código del producto";
  byId("product-quantity-field").firstChild.textContent = dispatch ? "Cantidad extraída" : "Cantidad contada";
  byId("products-step-title").textContent = dispatch ? "Extracción de productos" : "Recepción de productos";
  byId("locations-step-title").textContent = dispatch ? "Ubicación de despacho" : "Asignación de ubicaciones";
  byId("allocation-progress-title").textContent = dispatch ? "Cantidades depositadas" : "Distribución capturada";
  byId("destination-field").firstChild.textContent = dispatch ? "Ubicación de despacho" : "Ubicación destino";
  byId("save-allocation").textContent = dispatch ? "Registrar despacho" : "Registrar ubicación";
  byId("to-locations").textContent = dispatch
    ? (hasDispatchLocations ? "Continuar a despacho" : "Continuar al Consolidado")
    : "Continuar a Ubicaciones";
  document.querySelector('[data-step-go="2"]').hidden = dispatch && !hasDispatchLocations;
  byId("back-locations").innerHTML = dispatch && !hasDispatchLocations
    ? '<i class="ti ti-arrow-left" aria-hidden="true"></i> Productos'
    : '<i class="ti ti-arrow-left" aria-hidden="true"></i> Ubicaciones';
  byId("allocation-code").closest("label").firstChild.textContent = dispatch ? "Código del producto a despachar" : "Código del producto";
  byId("allocation-quantity").closest("label").firstChild.textContent = dispatch ? "Cantidad despachada" : "Cantidad a ubicar";
  byId("outbound-stock-suggestions").hidden = !dispatch;
  if (guide.idTipoOperacionGuia === 1) byId("destination-field").firstChild.textContent = "Ubicación destino";
  if (guide.idTipoOperacionGuia === 2) byId("origin-field").firstChild.textContent = "Ubicación de origen";

  const facts = [
    ["Código", guide.numeroGuia || `Guía #${guide.idGuia}`],
    ["Operación", operationName], ["Motivo", guide.nombreMotivo],
    ["Programada", formatScheduledAt(guide.fechaProgramada)],
    ["Registrada", formatScheduledAt(guide.fechaRegistro)],
    [guide.tipoTercero, guide.nombreTercero || lookupData.counterparties.get(Number(guide.requiereProveedor ? guide.idProveedor : guide.idCliente))],
    [guide.requiereAlmacenOrigen ? "Almacén de origen" : null, guide.nombreAlmacenOrigen || lookupData.warehouses.get(Number(guide.idAlmacenOrigen))],
    [guide.requiereAlmacenDestino ? "Almacén destino" : null, guide.nombreAlmacenDestino || lookupData.warehouses.get(Number(guide.idAlmacenDestino))],
    ["Documento", [guide.tipoDocumentoReferencia, guide.numeroDocumentoReferencia].filter(Boolean).join(" · ")],
    ["Observación", guide.observacion]
  ].filter(([, value]) => value);
  byId("note-guide-code").textContent = guide.numeroGuia || `Guía #${guide.idGuia}`;
  const factsList = byId("note-guide-facts"); factsList.replaceChildren();
  for (const [label, value] of facts) {
    const row = document.createElement("div"); const term = document.createElement("dt"); const description = document.createElement("dd");
    term.textContent = label; description.textContent = value; row.append(term, description); factsList.append(row);
  }
  const noteProducts = byId("note-guide-products"); noteProducts.replaceChildren();
  for (const line of guide.detalles || []) {
    const item = products.find(product => Number(product.idProducto) === Number(line.idProducto));
    const row = document.createElement("tr"); const name = document.createElement("td"); const qty = document.createElement("td");
    name.textContent = item ? `${item.codigo} · ${item.descripcion}` : `Producto #${line.idProducto}`;
    qty.className = "text-end"; qty.textContent = Number(line.cantidadEsperada).toLocaleString("es-PE"); row.append(name, qty); noteProducts.append(row);
  }
  if (dispatch) renderOutboundStockSuggestions();
}

function isDispatch() { return Number(guide?.idTipoOperacionGuia) === 2; }
function isInternalDispatch() { return isDispatch() && Number(guide?.idTipoComportamiento) === 2; }

function renderOutboundStockSuggestions() {
  const host = byId("outbound-stock-suggestions").querySelector(".table-responsive");
  host.replaceChildren();
  byId("outbound-stock-suggestions").querySelector("h3").textContent = `Stock de ${lookupData.warehouses.get(Number(guide.idAlmacenOrigen)) || "almacén de origen"} sugerido por producto`;
  const table = createTable(["Código", "Producto", "Cantidad solicitada", "Ubicaciones sugeridas", "Stock sugerido"]);
  for (const line of guide.detalles || []) {
    const product = productById(line.idProducto);
    const expected = Number(line.cantidadEsperada);
    const rows = stockRows.filter(row => Number(row.idProducto) === Number(line.idProducto) && Number(row.cantidadActual) > 0)
      .sort((a, b) => Number(b.cantidadActual) - Number(a.cantidadActual));
    const single = rows.find(row => Number(row.cantidadActual) >= expected);
    const selected = single ? [single] : [];
    if (!single) {
      let sum = 0;
      for (const row of rows) { selected.push(row); sum += Number(row.cantidadActual); if (sum >= expected) break; }
    }
    const row = table.body.insertRow();
    row.insertCell().textContent = product?.codigo || `#${line.idProducto}`;
    row.insertCell().textContent = product?.descripcion || "";
    row.insertCell().textContent = String(expected);
    row.insertCell().textContent = selected.map(item => `${item.ubicacion} (${item.cantidadActual})`).join(" · ") || "Sin existencias";
    row.insertCell().textContent = String(selected.reduce((sum, item) => sum + Number(item.cantidadActual), 0));
  }
  host.append(table.element);
}

async function loadLocations() {
  const requests = [];
  if (guide.idAlmacenOrigen) requests.push(getActiveLocations(guide.idAlmacenOrigen).then(items => {
    fillLocations("origin", items);
    if (isDispatch()) {
      const dispatchLocations = items.filter(item => item.esUbicacionDespacho === true);
      fillLocations("destination", dispatchLocations);
      hasDispatchLocations = dispatchLocations.length > 0;
    }
  }));
  if (guide.idAlmacenDestino && !isDispatch()) requests.push(getActiveLocations(guide.idAlmacenDestino).then(items => fillLocations("destination", items)));
  await Promise.all(requests);
}

function fillLocations(key, items) {
  locations[key] = items;
  const datalist = byId(`${key}-options`); datalist.replaceChildren();
  for (const item of items) {
    const option = document.createElement("option"); option.value = item.codigo;
    option.label = [item.codigo, item.descripcion].filter(Boolean).join(" · "); datalist.append(option);
  }
}

function configureCaptureMode() {
  const hint = byId("camera-status");
  if (mode === "camera") {
    hint.textContent = "Activa la cámara para escanear; el código irá al paso activo.";
    byId("camera-start").hidden = false;
  } else hint.textContent = "PDA lista: escanea el código y confirma con Enter.";
}

function resolveProductInput(input) {
  const raw = input.value.trim().toLocaleLowerCase("es");
  const inGuide = products.filter(product => (guide.detalles || []).some(line => Number(line.idProducto) === Number(product.idProducto)));
  const product = inGuide.find(item => String(item.codigo || "").trim().toLocaleLowerCase("es") === raw) ||
    inGuide.find(item => [item.nombre, item.descripcion].some(value => String(value || "").trim().toLocaleLowerCase("es") === raw));
  const output = input === fields.productCode ? fields.productName : fields.allocationProduct;
  output.value = product ? (product.descripcion || product.nombre || "Producto reconocido") : "";
  if (product && input === fields.allocationCode && !capturedTotal(product.idProducto))
    showFeedback("allocation-feedback", "Primero registra este producto en el paso Productos.", true);
  return product || null;
}

function processScannedCode(raw) {
  const input = activeCodeField;
  if (isDispatch() && input === fields.outboundSource) {
    input.value = String(raw || "").trim(); fields.productCode.focus(); cameraPausedUntil = Date.now() + 800; return true;
  }
  if (isDispatch() && input === fields.destination) {
    input.value = String(raw || "").trim(); fields.allocationCode.focus(); cameraPausedUntil = Date.now() + 800; return true;
  }
  input.value = String(raw || "").trim();
  const product = resolveProductInput(input);
  if (!product) {
    showFeedback(input === fields.productCode ? "scan-feedback" : "allocation-feedback", "El código no coincide exactamente con un producto de esta guía.", true);
    input.select(); return false;
  }
  const output = input === fields.productCode ? fields.productName : fields.allocationProduct;
  output.value = product.descripcion || product.nombre || "Producto reconocido";
  showFeedback(input === fields.productCode ? "scan-feedback" : "allocation-feedback", `Producto reconocido: ${product.descripcion}`, false);
  (input === fields.productCode ? fields.productQuantity : fields.allocationQuantity).focus();
  cameraPausedUntil = Date.now() + 1200;
  return true;
}

function captureProduct() {
  if (isDispatch()) { captureOutboundProduct(); return; }
  const product = resolveProductInput(fields.productCode);
  const quantity = Number(fields.productQuantity.value);
  if (!product) { showFeedback("scan-feedback", "Escanea o escribe el código exacto de un producto de la guía.", true); fields.productCode.focus(); return; }
  if (!Number.isSafeInteger(quantity) || quantity <= 0) { showFeedback("scan-feedback", "Ingresa una cantidad entera mayor que cero.", true); fields.productQuantity.focus(); return; }
  const lote = additionalInfo.lot ? fields.productLot.value.trim() : "";
  const serie = additionalInfo.serial ? fields.productSerial.value.trim() : "";
  const existing = flow.captured.find(item => Number(item.idProducto) === Number(product.idProducto) &&
    (item.lote || "") === lote && (item.serie || "") === serie);
  if (existing) existing.cantidad = Number(existing.cantidad) + quantity;
  else flow.captured.push({ idProducto: Number(product.idProducto), codigo: product.codigo, descripcion: product.descripcion, cantidad: quantity, lote, serie });
  persistFlow(); renderAll(); clearProductFields();
  showFeedback("scan-feedback", `Captura guardada temporalmente: ${product.codigo} · ${quantity} unidades.`, false);
}

function captureOutboundProduct() {
  const product = resolveProductInput(fields.productCode);
  const quantity = Number(fields.productQuantity.value);
  const locationId = resolveLocationId("origin", fields.outboundSource.value);
  if (!locationId) { showFeedback("scan-feedback", "Escanea o escribe una ubicación activa del almacén de origen.", true); fields.outboundSource.focus(); return; }
  if (!product) { showFeedback("scan-feedback", "Escanea o escribe el código exacto de un producto de la guía.", true); fields.productCode.focus(); return; }
  if (!Number.isSafeInteger(quantity) || quantity <= 0) { showFeedback("scan-feedback", "Ingresa una cantidad entera mayor que cero.", true); fields.productQuantity.focus(); return; }
  const stock = stockRows.find(item => Number(item.idProducto) === Number(product.idProducto) &&
    String(item.ubicacion || "").trim().toLocaleLowerCase("es") === fields.outboundSource.value.trim().toLocaleLowerCase("es"));
  const alreadyCaptured = flow.captured.filter(item => Number(item.idProducto) === Number(product.idProducto) && Number(item.idUbicacionOrigen) === locationId)
    .reduce((sum, item) => sum + Number(item.cantidad), 0);
  const alreadySaved = history.filter(item => Number(item.idProducto) === Number(product.idProducto) && Number(item.idUbicacionOrigen) === locationId)
    .reduce((sum, item) => sum + Number(item.cantidad), 0);
  const available = Number(stock?.cantidadActual || 0) - Math.max(alreadyCaptured, alreadySaved);
  if (!stock || quantity > available) {
    showFeedback("scan-feedback", `La ubicación ${fields.outboundSource.value} tiene ${Math.max(available, 0)} unidades disponibles para extraer.`, true);
    fields.productQuantity.focus(); return;
  }
  const existing = flow.captured.find(item => Number(item.idProducto) === Number(product.idProducto) && Number(item.idUbicacionOrigen) === locationId);
  if (existing) existing.cantidad = Number(existing.cantidad) + quantity;
  else flow.captured.push({ idProducto: Number(product.idProducto), codigo: product.codigo, descripcion: product.descripcion, cantidad: quantity,
    idUbicacionOrigen: locationId, ubicacionOrigen: fields.outboundSource.value.trim(), cantidadStock: Number(stock.cantidadActual) });
  persistFlow(); renderAll(); fields.productCode.value = ""; fields.productName.value = ""; fields.productQuantity.value = "";
  fields.productCode.focus({ preventScroll: true });
  showFeedback("scan-feedback", `Extracción temporal guardada: ${product.codigo} · ${quantity} unidades de ${fields.outboundSource.value}.`, false);
}

async function saveAllocation() {
  if (isDispatch()) { await saveOutboundPlacement(); return; }
  const button = byId("save-allocation");
  if (button.disabled) return;
  const product = resolveProductInput(fields.allocationCode);
  const quantity = Number(fields.allocationQuantity.value);
  if (!product) { showFeedback("allocation-feedback", "Escanea un producto capturado en el paso anterior.", true); fields.allocationCode.focus(); return; }
  if (!Number.isSafeInteger(quantity) || quantity <= 0) { showFeedback("allocation-feedback", "Ingresa una cantidad entera mayor que cero.", true); fields.allocationQuantity.focus(); return; }
  const remaining = capturedTotal(product.idProducto) - totalFor(product.idProducto);
  if (quantity > remaining) { showFeedback("allocation-feedback", `Solo quedan ${Math.max(remaining, 0)} unidades de ${product.codigo} por asignar.`, true); fields.allocationQuantity.focus(); return; }
  const capture = nextPendingCapture(product.idProducto);
  const batchRemaining = capture ? remainingForCapture(capture) : 0;
  if (!capture || quantity > batchRemaining) {
    showFeedback("allocation-feedback", `El lote/serie activo dispone de ${Math.max(batchRemaining, 0)} unidades. Registra esa cantidad para continuar con el siguiente registro adicional.`, true);
    fields.allocationQuantity.focus(); return;
  }
  const needsOrigin = !byId("origin-field").hidden;
  const needsDestination = !byId("destination-field").hidden;
  const originId = needsOrigin ? resolveLocationId("origin") : null;
  const destinationId = needsDestination ? resolveLocationId("destination") : null;
  if (needsOrigin && !originId) { showFeedback("allocation-feedback", "La ubicación de origen no coincide con una ubicación activa del almacén.", true); fields.origin.focus(); return; }
  if (needsDestination && !destinationId) { showFeedback("allocation-feedback", "La ubicación destino no coincide con una ubicación activa del almacén.", true); fields.destination.focus(); return; }

  button.disabled = true;
  try {
    const reading = await registerGuideReading(guideId, {
      idProducto: product.idProducto, cantidad: quantity, codigoLeido: product.codigo,
      lote: capture.lote || null,
      serie: capture.serie || null,
      observacion: fields.observation.value.trim() || null,
      idUbicacionOrigen: originId, idUbicacionDestino: destinationId
    });
    history.unshift(reading); renderAll(); clearAllocationFields();
    showFeedback("allocation-feedback", `Distribución guardada para ${product.codigo}. Quedan ${capturedTotal(product.idProducto) - totalFor(product.idProducto)} unidades por ubicar.`, false);
  } catch (error) {
    showApiFailure(showApiError(error), "allocation-feedback");
  } finally { button.disabled = false; }
}

async function saveOutboundPlacement() {
  const button = byId("save-allocation");
  if (button.disabled) return;
  const product = resolveProductInput(fields.allocationCode);
  const quantity = Number(fields.allocationQuantity.value);
  const destinationId = resolveLocationId("destination");
  if (!hasDispatchLocations) { showApiFailure("Este almacén no tiene ubicaciones habilitadas para despacho. Continúa directamente al consolidado.", "allocation-feedback"); return; }
  if (!destinationId) { showApiFailure("La ubicación ingresada no pertenece a las ubicaciones de despacho habilitadas de este almacén.", "allocation-feedback"); fields.destination.focus(); return; }
  if (!product) { showFeedback("allocation-feedback", "Escanea un producto extraído en el paso anterior.", true); fields.allocationCode.focus(); return; }
  if (!Number.isSafeInteger(quantity) || quantity <= 0) { showFeedback("allocation-feedback", "Ingresa una cantidad entera mayor que cero.", true); fields.allocationQuantity.focus(); return; }
  const pending = flow.captured.filter(item => Number(item.idProducto) === Number(product.idProducto))
    .map(item => ({ item, remaining: Number(item.cantidad) - history.filter(reading => Number(reading.idProducto) === Number(item.idProducto) && Number(reading.idUbicacionOrigen) === Number(item.idUbicacionOrigen)).reduce((sum, reading) => sum + Number(reading.cantidad), 0) }))
    .filter(entry => entry.remaining > 0);
  const available = pending.reduce((sum, entry) => sum + entry.remaining, 0);
  if (quantity > available) { showFeedback("allocation-feedback", `Quedan ${available} unidades extraídas de ${product.codigo} por depositar.`, true); fields.allocationQuantity.focus(); return; }

  button.disabled = true;
  let remaining = quantity;
  try {
    for (const entry of pending) {
      if (remaining <= 0) break;
      const part = Math.min(remaining, entry.remaining);
      const reading = await registerGuideReading(guideId, {
        idProducto: product.idProducto, cantidad: part, codigoLeido: product.codigo,
        observacion: fields.observation.value.trim() || null,
        idUbicacionOrigen: Number(entry.item.idUbicacionOrigen), idUbicacionDestino: destinationId
      });
      history.unshift(reading);
      remaining -= part;
    }
    renderAll(); clearAllocationFields();
    showFeedback("allocation-feedback", `Despacho guardado para ${product.codigo}.`, false);
  } catch (error) {
    try { history = await getGuideReadingHistory(guideId); renderAll(); } catch { /* Conserva el error inicial y permite reintentar. */ }
    showApiFailure(showApiError(error), "allocation-feedback");
  } finally { button.disabled = false; }
}

async function continueFromProducts() {
  if (!isDispatch() || hasDispatchLocations) { goToStep(2); return; }
  if (!flow.captured.length) { showFeedback("scan-feedback", "Registra al menos un producto antes de continuar.", true); return; }
  const incomplete = (guide.detalles || []).find(line => capturedTotal(line.idProducto) !== Number(line.cantidadEsperada));
  if (incomplete) {
    const product = productById(incomplete.idProducto);
    showApiFailure(`Completa la cantidad esperada de ${product?.codigo || `el producto #${incomplete.idProducto}`} antes de ir al consolidado.`, "scan-feedback");
    return;
  }
  const button = byId("to-locations");
  button.disabled = true;
  try {
    for (const item of flow.captured) {
      const saved = history.filter(reading => Number(reading.idProducto) === Number(item.idProducto) && Number(reading.idUbicacionOrigen) === Number(item.idUbicacionOrigen))
        .reduce((sum, reading) => sum + Number(reading.cantidad), 0);
      const remaining = Number(item.cantidad) - saved;
      if (remaining <= 0) continue;
      await registerGuideReading(guideId, {
        idProducto: Number(item.idProducto), cantidad: remaining, codigoLeido: item.codigo,
        idUbicacionOrigen: Number(item.idUbicacionOrigen), idUbicacionDestino: null
      });
    }
    history = await getGuideReadingHistory(guideId);
    renderAll();
    goToStep(3);
  } catch (error) {
    try { history = await getGuideReadingHistory(guideId); renderAll(); } catch { /* Mantén el error inicial si la consulta también falla. */ }
    showApiFailure(showApiError(error), "scan-feedback");
  } finally { button.disabled = false; }
}

function goToStep(step, persist = true) {
  if (isDispatch() && !hasDispatchLocations && step === 2) step = 3;
  if (step > 1 && !flow.captured.length) { showFeedback("scan-feedback", "Registra al menos un producto antes de continuar.", true); return; }
  if (step > 2 && !history.length) { showFeedback(isDispatch() && !hasDispatchLocations ? "scan-feedback" : "allocation-feedback", isDispatch() && !hasDispatchLocations ? "Registra las cantidades extraídas antes de continuar." : "Registra al menos una ubicación antes de continuar.", true); return; }
  if (isDispatch() && step > 2 && (guide.detalles || []).some(line => capturedTotal(line.idProducto) <= 0 || capturedTotal(line.idProducto) !== totalFor(line.idProducto))) {
    showApiFailure("Para continuar, extrae cada producto de la guía y deposita exactamente la misma cantidad en ubicaciones de despacho.", "allocation-feedback"); return;
  }
  flow.step = Math.max(1, Math.min(3, step));
  if (persist) persistFlow();
  for (let index = 1; index <= 3; index++) {
    byId(["step-products", "step-locations", "step-consolidated"][index - 1]).hidden = index !== flow.step;
    const button = document.querySelector(`[data-step-go="${index}"]`);
    button.classList.toggle("is-current", index === flow.step);
    button.classList.toggle("is-complete", index < flow.step);
    button.setAttribute("aria-current", index === flow.step ? "step" : "false");
  }
  if (flow.step === 1) { activeCodeField = fields.productCode; (isDispatch() ? fields.outboundSource : fields.productCode).focus({ preventScroll: true }); }
  if (flow.step === 2) { activeCodeField = fields.allocationCode; fields.allocationCode.focus({ preventScroll: true }); }
  if (flow.step === 3) { renderConsolidated(); }
  lastCameraCode = null; cameraMissFrames = 0;
  stopCamera();
}

function renderAll() {
  renderCapturedProducts(); renderAllocationProgress(); renderConsolidated(); renderProgress();
}

function renderCapturedProducts() {
  const host = byId("captured-products"); host.replaceChildren();
  if (!flow.captured.length) { host.textContent = "Todavía no se capturan productos."; return; }
  if (isDispatch()) {
    const table = createTable(["Código", "Producto", "Ubicación de origen", "Cantidad extraída", "Stock en ubicación", "Acción"]);
    flow.captured.forEach((item, index) => {
      const row = table.body.insertRow();
      row.insertCell().textContent = item.codigo;
      row.insertCell().textContent = item.descripcion;
      row.insertCell().textContent = item.ubicacionOrigen || locations.origin.find(location => Number(location.idUbicacion) === Number(item.idUbicacionOrigen))?.codigo || "—";
      row.insertCell().textContent = String(item.cantidad);
      row.insertCell().textContent = String(item.cantidadStock ?? "—");
      const action = row.insertCell();
      const remove = document.createElement("button"); remove.type = "button"; remove.className = "btn btn-sm btn-outline-danger"; remove.textContent = "Quitar";
      remove.disabled = history.some(reading => Number(reading.idProducto) === Number(item.idProducto) && Number(reading.idUbicacionOrigen) === Number(item.idUbicacionOrigen));
      remove.title = remove.disabled ? "Primero elimina las cantidades depositadas" : "Quitar extracción temporal";
      remove.addEventListener("click", () => { flow.captured.splice(index, 1); persistFlow(); renderAll(); }); action.append(remove);
    });
    host.append(table.element); return;
  }
  const metadataColumns = [additionalInfo.lot && "Lote", additionalInfo.serial && "Serie"].filter(Boolean);
  const table = createTable(["Producto", "Cantidad capturada", ...metadataColumns, "Acción"]);
  flow.captured.forEach((item, index) => {
    const row = table.body.insertRow();
    row.insertCell().textContent = `${item.codigo} · ${item.descripcion}`;
    row.insertCell().textContent = String(item.cantidad);
    if (additionalInfo.lot) row.insertCell().textContent = item.lote || "—";
    if (additionalInfo.serial) row.insertCell().textContent = item.serie || "—";
    const action = row.insertCell();
    const decrement = document.createElement("button"); decrement.type = "button"; decrement.className = "btn btn-sm btn-outline-secondary me-1"; decrement.textContent = "−";
    const allocated = allocatedForCapture(item);
    decrement.disabled = Number(item.cantidad) - 1 < allocated;
    decrement.title = "Restar una unidad de la captura";
    decrement.addEventListener("click", () => { item.cantidad--; if (item.cantidad <= 0) flow.captured.splice(index, 1); persistFlow(); renderAll(); });
    const remove = document.createElement("button"); remove.type = "button"; remove.className = "btn btn-sm btn-outline-secondary"; remove.textContent = "Quitar";
    remove.disabled = allocated > 0;
    remove.title = remove.disabled ? "Primero elimina o corrige las unidades distribuidas" : "Quitar captura temporal";
    remove.addEventListener("click", () => { flow.captured.splice(index, 1); persistFlow(); renderAll(); }); action.append(decrement, remove);
  });
  host.append(table.element);
}

function renderAllocationProgress() {
  const host = byId("allocation-progress"); host.replaceChildren();
  const ids = new Set([...(guide.detalles || []).map(line => Number(line.idProducto)), ...flow.captured.map(item => Number(item.idProducto))]);
  const table = createTable(isDispatch() ? ["Producto", "Extraído", "Despachado", "Pendiente"] : ["Producto", "Capturado", "Ubicado", "Pendiente"]);
  for (const id of ids) {
    const product = productById(id); const captured = capturedTotal(id); const allocated = totalFor(id); const row = table.body.insertRow();
    row.insertCell().textContent = product ? `${product.codigo} · ${product.descripcion}` : `Producto #${id}`;
    row.insertCell().textContent = String(captured); row.insertCell().textContent = String(allocated); row.insertCell().textContent = String(Math.max(captured - allocated, 0));
  }
  host.append(table.element);
}

function renderConsolidated() {
  if (!guide) return;
  const summary = byId("consolidated-summary"); summary.replaceChildren();
  const summaryTable = createTable(["Producto", "Esperado", "Capturado", "Distribuido", "Diferencia"]);
  for (const line of guide.detalles || []) {
    const product = productById(line.idProducto); const captured = capturedTotal(line.idProducto); const placed = totalFor(line.idProducto); const expected = Number(line.cantidadEsperada); const row = summaryTable.body.insertRow();
    row.insertCell().textContent = product ? `${product.codigo} · ${product.descripcion}` : `Producto #${line.idProducto}`;
    row.insertCell().textContent = String(expected); row.insertCell().textContent = String(captured); row.insertCell().textContent = String(placed); row.insertCell().textContent = String(placed - expected);
  }
  summary.append(summaryTable.element);

  const readings = byId("consolidated-readings"); readings.replaceChildren();
  if (!history.length) { readings.textContent = "Todavía no hay distribuciones registradas."; return; }
  if (isDispatch()) {
    const detailTable = createTable(["CodigoProd", "Nombre", "UbicaciónOrigen", "Almacén Origen", "Cantidad destinada", "Ubicación de despacho", "Acción"]);
    for (const item of [...history].reverse()) {
      const row = detailTable.body.insertRow();
      row.insertCell().textContent = item.codigoProducto;
      row.insertCell().textContent = item.descripcionProducto || "";
      row.insertCell().textContent = item.ubicacionOrigen || "—";
      row.insertCell().textContent = lookupData.warehouses.get(Number(guide.idAlmacenOrigen)) || "—";
      row.insertCell().textContent = String(item.cantidad);
      row.insertCell().textContent = item.ubicacionDestino || "—";
      const action = row.insertCell();
      const remove = document.createElement("button"); remove.type = "button"; remove.className = "btn btn-sm btn-outline-danger"; remove.textContent = "Quitar";
      remove.dataset.removeReading = String(item.idLecturaGuia); remove.setAttribute("aria-label", `Quitar despacho de ${item.codigoProducto}`); action.append(remove);
    }
    readings.append(detailTable.element); return;
  }
  const detailTable = createTable(["Producto", "Cantidad", "Ubicación de origen", "Ubicación destino", "Lote", "Serie", "Acción"]);
  for (const item of [...history].reverse()) {
    const row = detailTable.body.insertRow();
    row.insertCell().textContent = `${item.codigoProducto} · ${item.descripcionProducto || ""}`;
    row.insertCell().textContent = String(item.cantidad);
    row.insertCell().textContent = item.ubicacionOrigen || "—";
    row.insertCell().textContent = item.ubicacionDestino || "—";
    row.insertCell().textContent = item.lote || "—"; row.insertCell().textContent = item.serie || "—";
    const action = row.insertCell();
    const remove = document.createElement("button"); remove.type = "button"; remove.className = "btn btn-sm btn-outline-danger"; remove.textContent = "Quitar";
    remove.dataset.removeReading = String(item.idLecturaGuia); remove.setAttribute("aria-label", `Quitar lectura de ${item.codigoProducto}`); action.append(remove);
  }
  readings.append(detailTable.element);
}

function renderProgress() {
  const captured = flow.captured.reduce((sum, item) => sum + Number(item.cantidad), 0);
  const allocated = history.reduce((sum, item) => sum + Number(item.cantidad), 0);
  byId("registration-progress-count").textContent = `${flow.captured.length} capturas · ${allocated} ubicadas`;
  const steps = isDispatch() && !hasDispatchLocations ? 2 : 3;
  byId("registration-progress-detail").textContent = `${captured} unidades capturadas · Paso ${flow.step === 3 && steps === 2 ? 2 : flow.step} de ${steps}`;
}

function createTable(headers) {
  const element = document.createElement("table"); element.className = "table table-sm align-middle guide-flow-table";
  const head = element.createTHead().insertRow();
  for (const label of headers) { const cell = document.createElement("th"); cell.scope = "col"; cell.textContent = label; head.append(cell); }
  return { element, body: element.createTBody() };
}

function capturedTotal(productId) { return flow.captured.reduce((sum, item) => sum + (Number(item.idProducto) === Number(productId) ? Number(item.cantidad) : 0), 0); }
function totalFor(productId) { return history.reduce((sum, item) => sum + (Number(item.idProducto) === Number(productId) ? Number(item.cantidad) : 0), 0); }
function allocatedForCapture(capture) {
  if (isDispatch()) return history.reduce((sum, item) => Number(item.idProducto) === Number(capture.idProducto) &&
    Number(item.idUbicacionOrigen) === Number(capture.idUbicacionOrigen) ? sum + Number(item.cantidad) : sum, 0);
  return history.reduce((sum, item) => Number(item.idProducto) === Number(capture.idProducto) &&
    (item.lote || "") === (capture.lote || "") && (item.serie || "") === (capture.serie || "")
    ? sum + Number(item.cantidad) : sum, 0);
}
function remainingForCapture(capture) { return Math.max(Number(capture.cantidad) - allocatedForCapture(capture), 0); }
function nextPendingCapture(productId) { return flow.captured.find(item => Number(item.idProducto) === Number(productId) && remainingForCapture(item) > 0); }
function productById(productId) { return products.find(item => Number(item.idProducto) === Number(productId)); }

async function refreshHistory() {
  try { history = await getGuideReadingHistory(guideId); renderAll(); showFeedback("consolidated-feedback", "Consolidado actualizado.", false); }
  catch (error) { showApiFailure(showApiError(error), "consolidated-feedback"); }
}

async function removeAllocation(event) {
  const button = event.target.closest("[data-remove-reading]");
  if (!button) return;
  const readingId = Number(button.dataset.removeReading);
  const reading = history.find(item => Number(item.idLecturaGuia) === readingId);
  if (!reading || !window.confirm(`¿Quitar la distribución de ${reading.codigoProducto}, ${reading.cantidad} unidades?`)) return;
  button.disabled = true;
  try {
    await deleteGuideReading(guideId, readingId);
    history = await getGuideReadingHistory(guideId);
    renderAll();
    showFeedback("consolidated-feedback", "Distribución quitada. Puedes volver a capturarla con los datos correctos.", false);
  } catch (error) {
    showApiFailure(showApiError(error), "consolidated-feedback");
  } finally { button.disabled = false; }
}

function clearProductFields() {
  fields.productCode.value = ""; fields.productName.value = ""; fields.productQuantity.value = "";
  fields.productLot.value = ""; fields.productSerial.value = ""; fields.productExpiry.value = "";
  fields.productCode.focus({ preventScroll: true });
}
function clearAllocationFields() {
  fields.allocationCode.value = ""; fields.allocationProduct.value = ""; fields.allocationQuantity.value = "";
  fields.observation.value = "";
  fields.allocationCode.focus({ preventScroll: true });
}
function focusAfterLocations() {
  fields.observation.focus();
}
function resolveLocationId(name, rawValue = fields[name].value) {
  const value = rawValue.trim().toLocaleLowerCase("es");
  return locations[name].find(item => item.codigo.trim().toLocaleLowerCase("es") === value || String(item.idUbicacion) === value)?.idUbicacion || null;
}
function showFeedback(id, text, isError) { const target = byId(id); target.textContent = text; target.dataset.kind = isError ? "error" : "success"; }
function showError(text) { message.textContent = text; message.className = "alert alert-danger"; byId("registration-title").textContent = "No se pudo cargar la guía"; showApiFailure(text); }
function showApiFailure(text, feedbackId = null) {
  if (feedbackId) showFeedback(feedbackId, text, true);
  if (window.Swal?.fire) {
    void window.Swal.fire({ toast: true, position: "top-end", icon: "error", title: "No se pudo completar", text, showConfirmButton: false, showCloseButton: true, timer: 6500, timerProgressBar: true });
    return;
  }
  byId("api-error-text").textContent = text;
  const dialog = byId("api-error-dialog");
  if (!dialog.open) dialog.show();
  window.setTimeout(() => { if (dialog.open) dialog.close(); }, 6500);
}

function openFinalizeConfirmation() {
  const summary = byId("finalize-summary"); summary.replaceChildren();
  const missing = (guide.detalles || []).filter(line => totalFor(line.idProducto) === 0);
  if (missing.length) { showFeedback("consolidated-feedback", "Registra una distribución para cada producto de la guía antes de finalizar.", true); return; }
  if (isDispatch() && (guide.detalles || []).some(line => capturedTotal(line.idProducto) !== totalFor(line.idProducto))) {
    showApiFailure(hasDispatchLocations
      ? "La cantidad depositada debe coincidir exactamente con la cantidad extraída para cada producto."
      : "La cantidad registrada debe coincidir exactamente con la cantidad esperada de cada producto.", "consolidated-feedback"); return;
  }
  const unassigned = flow.captured.filter(item => capturedTotal(item.idProducto) !== totalFor(item.idProducto));
  if (unassigned.length) { showFeedback("consolidated-feedback", "Distribuye toda la cantidad capturada entre las ubicaciones antes de finalizar.", true); return; }
  for (const line of guide.detalles || []) {
    const product = productById(line.idProducto); const placed = totalFor(line.idProducto); const expected = Number(line.cantidadEsperada);
    const row = document.createElement("p"); row.textContent = `${product?.codigo || `Producto #${line.idProducto}`}: esperado ${expected}, distribuido ${placed}, diferencia ${placed - expected}`; summary.append(row);
  }
  byId("finalize-dialog").showModal();
}

async function finalize() {
  const button = byId("finalize-confirm"); button.disabled = true;
  try {
    await finalizeGuideReadings(guideId);
    localStorage.removeItem(flowKey);
    location.replace(`/modules/operaciones/guias/detalle.html?id=${encodeURIComponent(guideId)}`);
  } catch (error) {
    byId("finalize-dialog").close(); showApiFailure(showApiError(error), "consolidated-feedback");
  } finally { button.disabled = false; }
}

async function startCamera() {
  if (!("BarcodeDetector" in window) || !navigator.mediaDevices?.getUserMedia) {
    byId("camera-status").textContent = "Este navegador no ofrece lectura con cámara. Puedes usar PDA o escribir el código."; return;
  }
  try {
    lastCameraCode = null; cameraMissFrames = 0;
    const supported = await BarcodeDetector.getSupportedFormats();
    const formats = ["qr_code", "code_128", "code_39", "ean_13", "ean_8", "upc_a", "upc_e", "itf", "codabar"].filter(value => supported.includes(value));
    if (!formats.length) throw new Error("El navegador no reconoce formatos de códigos compatibles.");
    detector = new BarcodeDetector({ formats });
    cameraStream = await navigator.mediaDevices.getUserMedia({ video: { facingMode: { ideal: "environment" }, width: { ideal: 640 }, height: { ideal: 480 } }, audio: false });
    const video = byId("camera-preview"); video.srcObject = cameraStream; video.hidden = false; await video.play();
    byId("camera-start").hidden = true; byId("camera-stop").hidden = false;
    byId("camera-status").textContent = "Cámara activa. Enfoca el código que quieras registrar."; scanFrame();
  } catch (error) { stopCamera(); byId("camera-status").textContent = error.name === "NotAllowedError" ? "Permite el acceso a la cámara para escanear." : error.message || "No se pudo iniciar la cámara."; }
}

async function scanFrame() {
  const video = byId("camera-preview"); if (!cameraStream || document.hidden) return;
  try {
    if (!detecting && video.readyState >= HTMLMediaElement.HAVE_CURRENT_DATA && Date.now() >= cameraPausedUntil) {
      detecting = true; const matches = await detector.detect(video);
      if (matches[0]?.rawValue) { const value = matches[0].rawValue; cameraMissFrames = 0; if (value !== lastCameraCode) { lastCameraCode = value; processScannedCode(value); } }
      else if (++cameraMissFrames >= 5) lastCameraCode = null;
    }
  } catch { /* Un frame sin lectura no interrumpe el escaneo. */ }
  finally { detecting = false; if (cameraStream && !document.hidden) cameraTimer = window.setTimeout(scanFrame, 250); }
}

function stopCamera() {
  if (cameraTimer) window.clearTimeout(cameraTimer); cameraTimer = undefined;
  cameraStream?.getTracks().forEach(track => track.stop()); cameraStream = undefined;
  const video = byId("camera-preview"); if (video) { video.pause(); video.srcObject = null; video.hidden = true; }
  byId("camera-start").hidden = mode !== "camera"; byId("camera-stop").hidden = true;
}

function toggleNote() {
  const note = byId("guide-note"); const opening = note.getAttribute("aria-hidden") !== "false";
  note.setAttribute("aria-hidden", String(!opening)); byId("guide-note-toggle").setAttribute("aria-expanded", String(opening));
}
function closeNote() { byId("guide-note").setAttribute("aria-hidden", "true"); byId("guide-note-toggle").setAttribute("aria-expanded", "false"); }
