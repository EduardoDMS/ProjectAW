import { logout } from "/shared/js/session.js";
import { getJson } from "/shared/js/api-client.js";
import {
  finalizeGuideReadings, formatScheduledAt, getActiveLocations, getGuideDetailLookups,
  getGuideReadingHistory, getMyGuideById, registerGuideReading, showApiError
} from "/modules/operaciones/guias.js";

const page = document.querySelector(".registration-page");
const message = document.querySelector("#registration-message");
const guideId = Number(new URLSearchParams(location.search).get("id"));
const modeKey = "projectaw.operator.capture-mode";
const mode = localStorage.getItem(modeKey) || "pda";
const fields = {
  code: document.querySelector("#scan-code"), product: document.querySelector("#product-select"),
  quantity: document.querySelector("#read-quantity"), origin: document.querySelector("#origin-location"),
  destination: document.querySelector("#destination-location"), lot: document.querySelector("#read-lot"),
  serial: document.querySelector("#read-serial"), observation: document.querySelector("#read-observation")
};
let guide;
let products = [];
let history = [];
let lookupData;
let cameraStream;
let cameraTimer;
let detector;
let detecting = false;
let cameraPausedUntil = 0;
let lastCameraCode = null;
let cameraMissFrames = 0;
const locationLocked = { origin: true, destination: true };
const locations = { origin: [], destination: [] };

document.querySelector("#current-year").textContent = String(new Date().getFullYear());
document.querySelector("#logout-button").addEventListener("click", event => {
  event.currentTarget.disabled = true;
  void logout(message);
});
document.querySelector("#registration-back").href = Number.isSafeInteger(guideId) && guideId > 0
  ? `/modules/operaciones/guias/detalle.html?id=${encodeURIComponent(guideId)}`
  : "/modules/operaciones/guias/mis-guias.html";
document.querySelector("#clear-reading").addEventListener("click", clearFields);
document.querySelector("#submit-reading").addEventListener("click", () => void submitReading());
document.querySelector("#history-open").addEventListener("click", () => void openHistory());
document.querySelector("#history-close").addEventListener("click", () => document.querySelector("#history-dialog").close());
document.querySelector("#finalize-registration").addEventListener("click", openFinalizeConfirmation);
document.querySelector("#finalize-cancel").addEventListener("click", () => document.querySelector("#finalize-dialog").close());
document.querySelector("#finalize-confirm").addEventListener("click", () => void finalize());
document.querySelector("#guide-note-toggle").addEventListener("click", toggleNote);
document.querySelector("#guide-note-close").addEventListener("click", closeNote);
document.querySelector("#camera-start").addEventListener("click", () => void startCamera());
document.querySelector("#camera-stop").addEventListener("click", stopCamera);
document.querySelector("#origin-lock").addEventListener("click", () => toggleLocationLock("origin"));
document.querySelector("#destination-lock").addEventListener("click", () => toggleLocationLock("destination"));
fields.product.addEventListener("change", () => {
  const product = products.find(item => item.idProducto === Number(fields.product.value));
  if (product) {
    fields.code.value = product.codigo;
    focusNext("product");
  }
});
fields.code.addEventListener("keydown", event => {
  if (event.key === "Enter") { event.preventDefault(); processScannedCode(fields.code.value); }
});
fields.quantity.addEventListener("keydown", event => {
  if (event.key === "Enter") { event.preventDefault(); fields.lot.focus(); }
});
fields.lot.addEventListener("keydown", event => {
  if (event.key === "Enter") { event.preventDefault(); fields.serial.focus(); }
});
fields.serial.addEventListener("keydown", event => {
  if (event.key === "Enter") { event.preventDefault(); void submitReading(); }
});
fields.origin.addEventListener("keydown", event => {
  if (event.key === "Enter") { event.preventDefault(); fields.destination.closest("label").hidden ? fields.code.focus() : fields.destination.focus(); }
});
fields.destination.addEventListener("keydown", event => {
  if (event.key === "Enter") { event.preventDefault(); fields.code.focus(); }
});
document.addEventListener("visibilitychange", () => { if (document.hidden) stopCamera(); });
window.addEventListener("pagehide", stopCamera, { once: true });

if (!Number.isSafeInteger(guideId) || guideId < 1) showError("El identificador de la guía no es válido.");
else void loadRegistration();

async function loadRegistration() {
  page.setAttribute("aria-busy", "true");
  try {
    guide = await getMyGuideById(guideId);
    if (guide.idEstadoGuia !== 2) throw new Error("Esta guía ya no está disponible para registrar lecturas.");
    renderGuideHeader();
    products = await getJson("api/ProductoApi");
    if (!Array.isArray(products)) throw new Error("La API no devolvió el catálogo de productos correctamente.");
    renderProductOptions();
    lookupData = await getGuideDetailLookups(guide.idTipoOperacionGuia, products);
    renderGuide();
    await loadLocations();
    history = await getGuideReadingHistory(guideId);
    renderProgress();
    restoreLocationDefaults();
    configureCaptureMode();
    fields.code.focus({ preventScroll: true });
  } catch (error) {
    if (error.status === 401) { location.replace("/"); return; }
    showError(showApiError(error));
  } finally { page.setAttribute("aria-busy", "false"); }
}

function renderGuide() {
  renderGuideHeader();
  const details = [
    ["Código", guide.numeroGuia || `Guía #${guide.idGuia}`],
    ["Operación", ({ 1: "Recepción", 2: "Despacho", 3: "Traslado" })[guide.idTipoOperacionGuia] || "Operación"],
    ["Programada", formatScheduledAt(guide.fechaProgramada)],
    ["Registrada", formatScheduledAt(guide.fechaRegistro)],
    ["Proveedor", lookupData.counterparties.get(Number(guide.idProveedor))],
    ["Cliente", lookupData.counterparties.get(Number(guide.idCliente))],
    ["Almacén de origen", lookupData.warehouses.get(Number(guide.idAlmacenOrigen))],
    ["Almacén destino", lookupData.warehouses.get(Number(guide.idAlmacenDestino))],
    ["Documento", [guide.tipoDocumentoReferencia, guide.numeroDocumentoReferencia].filter(Boolean).join(" · ")],
    ["Observación", guide.observacion]
  ].filter(([, value]) => value);
  document.querySelector("#note-guide-code").textContent = guide.numeroGuia || `Guía #${guide.idGuia}`;
  const facts = document.querySelector("#note-guide-facts");
  facts.replaceChildren();
  for (const [label, value] of details) {
    const row = document.createElement("div"); const term = document.createElement("dt"); const description = document.createElement("dd");
    term.textContent = label; description.textContent = value; row.append(term, description); facts.append(row);
  }
  const noteProducts = document.querySelector("#note-guide-products");
  noteProducts.replaceChildren();
  for (const line of guide.detalles || []) {
    const item = products.find(p => Number(p.idProducto) === Number(line.idProducto));
    const row = document.createElement("tr"); const name = document.createElement("td"); const qty = document.createElement("td");
    name.textContent = item ? `${item.codigo} · ${item.descripcion}` : `Producto #${line.idProducto}`;
    qty.className = "text-end"; qty.textContent = Number(line.cantidadEsperada).toLocaleString("es-PE"); row.append(name, qty); noteProducts.append(row);
  }
  const isTransfer = guide.idTipoOperacionGuia === 3;
  const needsOrigin = guide.idTipoOperacionGuia === 2 || isTransfer;
  const needsDestination = guide.idTipoOperacionGuia === 1 || isTransfer;
  document.querySelector("#origin-field").hidden = !needsOrigin;
  document.querySelector("#destination-field").hidden = !needsDestination;
}

function renderGuideHeader() {
  document.title = `${guide.numeroGuia} · Registro | Proyecto AW`;
  document.querySelector("#registration-title").textContent = guide.numeroGuia || `Guía #${guide.idGuia}`;
  document.querySelector("#registration-subtitle").textContent = `${guide.detalles?.length || 0} productos · Fecha ${formatScheduledAt(guide.fechaProgramada)}`;
  document.querySelector("#registration-back").href = `/modules/operaciones/guias/detalle.html?id=${encodeURIComponent(guideId)}`;
}

function renderProductOptions() {
  const guideProductIds = new Set((guide.detalles || []).map(line => Number(line.idProducto)));
  const guideProducts = products.filter(product => guideProductIds.has(Number(product.idProducto)));
  for (const product of guideProducts) {
    const option = document.createElement("option");
    option.value = String(product.idProducto);
    option.textContent = `${product.codigo} · ${product.descripcion}`;
    fields.product.append(option);
  }
  if (guideProducts.length === 0) {
    fields.product.disabled = true;
    fields.product.replaceChildren(new Option("La guía no tiene productos disponibles", ""));
    feedback("No se encontraron productos asociados a esta guía.", true);
  } else {
    fields.product.disabled = false;
    feedback(`${guideProducts.length} productos disponibles para leer.`, false);
  }
}

async function loadLocations() {
  const requests = [];
  if (!document.querySelector("#origin-field").hidden) requests.push(getActiveLocations(guide.idAlmacenOrigen).then(items => fillLocations(fields.origin, items)));
  if (!document.querySelector("#destination-field").hidden) requests.push(getActiveLocations(guide.idAlmacenDestino).then(items => fillLocations(fields.destination, items)));
  await Promise.all(requests);
}

function fillLocations(select, items) {
  const key = select === fields.origin ? "origin" : "destination";
  const datalist = document.querySelector(`#${key}-options`);
  locations[key] = items;
  for (const location of items) {
    const option = document.createElement("option");
    option.value = location.codigo;
    option.label = [location.codigo, location.descripcion].filter(Boolean).join(" · ");
    datalist.append(option);
  }
}

function configureCaptureMode() {
  const hint = document.querySelector("#capture-mode-hint");
  const cameraButton = document.querySelector("#camera-start");
  if (mode === "camera") {
    hint.textContent = "Activa la cámara para leer códigos o escribe el valor manualmente.";
    cameraButton.hidden = false;
  } else {
    hint.textContent = "El lector PDA funciona como teclado: escanea y confirma con Enter.";
    fields.code.placeholder = "Escanea con PDA o escribe el código";
  }
}

function processScannedCode(rawCode) {
  const code = String(rawCode || "").trim();
  if (!code) { feedback("Escanea o escribe un código primero.", true); return false; }
  const product = products.find(item => (guide.detalles || []).some(line => Number(line.idProducto) === Number(item.idProducto)) && item.codigo.trim().toLowerCase() === code.toLowerCase());
  if (!product) { feedback("El código no pertenece a un producto de esta guía.", true); fields.code.select(); return false; }
  fields.product.value = String(product.idProducto);
  fields.code.value = product.codigo;
  feedback(`Producto reconocido: ${product.descripcion}`, false);
  fields.quantity.focus(); fields.quantity.select();
  cameraPausedUntil = Date.now() + 1800;
  return true;
}

async function submitReading() {
  const button = document.querySelector("#submit-reading");
  if (button.disabled) return;
  const product = products.find(item => item.idProducto === Number(fields.product.value));
  const quantity = Number(fields.quantity.value);
  if (!product) { feedback("Selecciona el producto o escanea su código.", true); fields.code.focus(); return; }
  if (!Number.isSafeInteger(quantity) || quantity < 0) { feedback("Ingresa una cantidad entera igual o mayor a cero.", true); fields.quantity.focus(); return; }
  const needsOrigin = !document.querySelector("#origin-field").hidden;
  const needsDestination = !document.querySelector("#destination-field").hidden;
  if (needsOrigin && !fields.origin.value) { feedback("Selecciona la ubicación de origen.", true); fields.origin.focus(); return; }
  if (needsDestination && !fields.destination.value) { feedback("Selecciona la ubicación destino.", true); fields.destination.focus(); return; }
  const originId = needsOrigin ? resolveLocationId("origin") : null;
  const destinationId = needsDestination ? resolveLocationId("destination") : null;
  if (needsOrigin && !originId) { feedback("La ubicación de origen no coincide con una ubicación activa del almacén.", true); fields.origin.focus(); return; }
  if (needsDestination && !destinationId) { feedback("La ubicación destino no coincide con una ubicación activa del almacén.", true); fields.destination.focus(); return; }
  button.disabled = true;
  try {
    const reading = await registerGuideReading(guideId, {
      idProducto: product.idProducto, cantidad: quantity, codigoLeido: fields.code.value.trim() || product.codigo,
      lote: fields.lot.value, serie: fields.serial.value, observacion: fields.observation.value,
      idUbicacionOrigen: originId,
      idUbicacionDestino: destinationId
    });
    history.unshift(reading);
    renderProgress();
    persistLocationDefaults();
    feedback(`Lectura guardada. Total registrado de ${product.codigo}: ${totalFor(product.idProducto)}.`, false);
    clearFields();
  } catch (error) {
    if (error.status === 401) { location.replace("/"); return; }
    feedback(showApiError(error), true);
  } finally { button.disabled = false; }
}

function renderProgress() {
  document.querySelector("#registration-progress-count").textContent = `${history.length} ${history.length === 1 ? "lectura guardada" : "lecturas guardadas"}`;
  const list = document.querySelector("#product-progress"); list.replaceChildren();
  let exact = true;
  for (const line of guide.detalles || []) {
    const product = products.find(item => Number(item.idProducto) === Number(line.idProducto));
    const count = totalFor(line.idProducto); const expected = Number(line.cantidadEsperada);
    exact &&= count === expected;
    const row = document.createElement("div"); row.className = "product-progress-row";
    const name = document.createElement("strong"); name.textContent = product ? `${product.codigo} · ${product.descripcion}` : `Producto #${line.idProducto}`;
    const totals = document.createElement("span"); totals.textContent = `${count} / ${expected} unidades`;
    const track = document.createElement("span"); track.className = "product-progress-track"; track.setAttribute("aria-hidden", "true");
    const fill = document.createElement("span"); fill.style.width = `${expected > 0 ? Math.min(count / expected * 100, 100) : count > 0 ? 100 : 0}%`; track.append(fill);
    row.append(name, totals, track); list.append(row);
  }
  document.querySelector("#registration-progress-detail").textContent = exact ? "Las cantidades coinciden con lo esperado." : "Puedes seguir leyendo y revisar diferencias al finalizar.";
}

function totalFor(productId) { return history.reduce((sum, item) => sum + (Number(item.idProducto) === Number(productId) ? Number(item.cantidad) : 0), 0); }

async function openHistory() {
  const dialog = document.querySelector("#history-dialog");
  const content = document.querySelector("#history-content");
  content.textContent = "Cargando historial…";
  dialog.showModal();
  try {
    history = await getGuideReadingHistory(guideId); renderProgress(); renderHistory(content);
  } catch (error) { content.textContent = showApiError(error); }
}

function renderHistory(content) {
  content.replaceChildren();
  if (!history.length) { content.textContent = "Todavía no hay lecturas guardadas."; return; }
  const list = document.createElement("ol"); list.className = "history-list";
  for (const item of history) {
    const row = document.createElement("li");
    const heading = document.createElement("strong"); heading.textContent = `${item.codigoProducto} · ${item.cantidad} unidades`;
    const detail = document.createElement("span"); detail.textContent = [item.codigoLeido, item.lote && `Lote ${item.lote}`, item.serie && `Serie ${item.serie}`, item.ubicacionOrigen && `Origen ${item.ubicacionOrigen}`, item.ubicacionDestino && `Destino ${item.ubicacionDestino}`, formatScheduledAt(item.fechaRegistroUtc)].filter(Boolean).join(" · ");
    row.append(heading, detail); list.append(row);
  }
  content.append(list);
}

function openFinalizeConfirmation() {
  const summary = document.querySelector("#finalize-summary");
  summary.replaceChildren();
  for (const line of guide.detalles || []) {
    const product = products.find(item => Number(item.idProducto) === Number(line.idProducto));
    const counted = totalFor(line.idProducto); const expected = Number(line.cantidadEsperada);
    const row = document.createElement("p");
    row.textContent = `${product?.codigo || `Producto #${line.idProducto}`}: esperado ${expected}, leído ${counted}, diferencia ${counted - expected}`;
    summary.append(row);
  }
  document.querySelector("#finalize-dialog").showModal();
}

async function finalize() {
  const button = document.querySelector("#finalize-confirm");
  button.disabled = true;
  try {
    await finalizeGuideReadings(guideId);
    location.replace(`/modules/operaciones/guias/detalle.html?id=${encodeURIComponent(guideId)}`);
  } catch (error) {
    if (error.status === 401) { location.replace("/"); return; }
    document.querySelector("#finalize-dialog").close(); feedback(showApiError(error), true);
  } finally { button.disabled = false; }
}

async function startCamera() {
  if (!("BarcodeDetector" in window) || !navigator.mediaDevices?.getUserMedia) {
    document.querySelector("#camera-status").textContent = "Este navegador no ofrece lectura de códigos con cámara. Puedes usar PDA o escribir el código.";
    return;
  }
  try {
    const supported = await BarcodeDetector.getSupportedFormats();
    const formats = ["qr_code", "code_128", "code_39", "ean_13", "ean_8", "upc_a", "upc_e", "itf", "codabar"].filter(value => supported.includes(value));
    if (!formats.length) throw new Error("El navegador no reconoce formatos de códigos compatibles.");
    detector = new BarcodeDetector({ formats });
    cameraStream = await navigator.mediaDevices.getUserMedia({ video: { facingMode: { ideal: "environment" }, width: { ideal: 640 }, height: { ideal: 480 } }, audio: false });
    const video = document.querySelector("#camera-preview"); video.srcObject = cameraStream; video.hidden = false;
    await video.play();
    document.querySelector("#camera-start").hidden = true;
    document.querySelector("#camera-stop").hidden = false;
    document.querySelector("#camera-status").textContent = "Cámara activa. Enfoca el código de un producto de la guía.";
    scanFrame();
  } catch (error) {
    stopCamera();
    document.querySelector("#camera-status").textContent = error.name === "NotAllowedError" ? "Permite el acceso a la cámara para escanear." : error.message || "No se pudo iniciar la cámara.";
  }
}

async function scanFrame() {
  const video = document.querySelector("#camera-preview");
  if (!cameraStream || document.hidden) return;
  try {
    if (!detecting && video.readyState >= HTMLMediaElement.HAVE_CURRENT_DATA && Date.now() >= cameraPausedUntil) {
      detecting = true;
      const matches = await detector.detect(video);
      if (matches[0]?.rawValue) {
        const value = matches[0].rawValue;
        cameraMissFrames = 0;
        if (value !== lastCameraCode) { lastCameraCode = value; processScannedCode(value); }
      } else if (++cameraMissFrames >= 5) lastCameraCode = null;
    }
  } catch { /* Un frame sin lectura no interrumpe el escaneo. */ }
  finally {
    detecting = false;
    if (cameraStream && !document.hidden) cameraTimer = window.setTimeout(scanFrame, 220);
  }
}

function stopCamera() {
  if (cameraTimer) window.clearTimeout(cameraTimer);
  cameraTimer = undefined;
  cameraStream?.getTracks().forEach(track => track.stop()); cameraStream = undefined;
  const video = document.querySelector("#camera-preview");
  if (video) { video.pause(); video.srcObject = null; video.hidden = true; }
  const start = document.querySelector("#camera-start"); const stop = document.querySelector("#camera-stop");
  if (start && mode === "camera") start.hidden = false;
  if (stop) stop.hidden = true;
}

function clearFields() {
  fields.code.value = ""; fields.product.value = ""; fields.quantity.value = "";
  fields.lot.value = ""; fields.serial.value = ""; fields.observation.value = "";
  feedback("Campos de lectura limpiados. Las ubicaciones bloqueadas se conservan.", false);
  fields.code.focus({ preventScroll: true });
}

function focusNext(current) { if (current === "product") fields.quantity.focus(); }
function feedback(text, isError) {
  const target = document.querySelector("#scan-feedback"); target.textContent = text; target.dataset.kind = isError ? "error" : "success";
}
function showError(text) { message.textContent = text; message.className = "alert alert-danger"; document.querySelector("#registration-title").textContent = "No se pudo cargar el registro"; }

function restoreLocationDefaults() {
  const last = history.find(item => item.idUbicacionOrigen || item.idUbicacionDestino);
  if (last?.ubicacionOrigen) fields.origin.value = last.ubicacionOrigen;
  if (last?.ubicacionDestino) fields.destination.value = last.ubicacionDestino;
  updateLocationLock("origin"); updateLocationLock("destination");
}
function persistLocationDefaults() {
  const current = history[0];
  if (current?.idUbicacionOrigen) fields.origin.value = locations.origin.find(item => item.idUbicacion === current.idUbicacionOrigen)?.codigo || current.ubicacionOrigen || "";
  if (current?.idUbicacionDestino) fields.destination.value = locations.destination.find(item => item.idUbicacion === current.idUbicacionDestino)?.codigo || current.ubicacionDestino || "";
  updateLocationLock("origin"); updateLocationLock("destination");
}
function toggleLocationLock(name) { locationLocked[name] = !locationLocked[name]; updateLocationLock(name); }
function updateLocationLock(name) {
  const field = fields[name]; const button = document.querySelector(`#${name}-lock`);
  const hasValue = Boolean(field.value);
  field.disabled = locationLocked[name] && hasValue;
  button.textContent = locationLocked[name] ? "🔒" : "🔓";
  button.setAttribute("aria-pressed", String(locationLocked[name]));
  button.title = locationLocked[name] ? "Desbloquear ubicación" : "Bloquear ubicación";
}
function resolveLocationId(name) {
  const value = fields[name].value.trim().toLowerCase();
  return locations[name].find(item => item.codigo.trim().toLowerCase() === value || String(item.idUbicacion) === value)?.idUbicacion || null;
}

function toggleNote() {
  const note = document.querySelector("#guide-note"); const opening = note.getAttribute("aria-hidden") !== "false";
  note.setAttribute("aria-hidden", String(!opening));
  document.querySelector("#guide-note-toggle").setAttribute("aria-expanded", String(opening));
}
function closeNote() {
  document.querySelector("#guide-note").setAttribute("aria-hidden", "true");
  document.querySelector("#guide-note-toggle").setAttribute("aria-expanded", "false");
}
