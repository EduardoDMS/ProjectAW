import "/shared/js/navigation.js";
import {
  formatScheduledAt,
  getMyGuideById,
  getGuideDetailLookups,
  OPERATIONS,
  showApiError
} from "/modules/operaciones/guias.js";

const page = document.querySelector(".guide-detail-page");
const message = document.querySelector("#guide-detail-message");
const guideId = Number(new URLSearchParams(window.location.search).get("id"));

document.querySelector("#current-year").textContent = String(new Date().getFullYear());
if (!Number.isSafeInteger(guideId) || guideId <= 0) {
  showError("El identificador de la guía no es válido.");
} else {
  void loadGuide();
}

async function loadGuide() {
  page.setAttribute("aria-busy", "true");
  try {
    const guide = await getMyGuideById(guideId);
    const lookups = await getGuideDetailLookups(guide);
    renderGuide(guide, lookups);
    if (guide.idEstadoGuia === 2) {
      const startButton = document.querySelector("#start-registration");
      startButton.hidden = false;
      startButton.addEventListener("click", () => {
        document.querySelector("#confirm-guide-code").textContent = guide.numeroGuia || `Guía #${guide.idGuia}`;
        document.querySelector("#confirm-guide-date").textContent = formatScheduledAt(guide.fechaProgramada);
        document.querySelector("#start-confirmation").showModal();
      });
      document.querySelector("#cancel-start").addEventListener("click", () => document.querySelector("#start-confirmation").close());
      document.querySelector("#confirm-start").addEventListener("click", () => {
        window.location.href = `/modules/operaciones/guias/registro.html?id=${encodeURIComponent(guide.idGuia)}`;
      });
    }
  } catch (error) {
    if (error.status === 401) {
      window.location.replace("/");
      return;
    }
    showError(showApiError(error));
  } finally {
    page.setAttribute("aria-busy", "false");
  }
}

function renderGuide(guide, lookups) {
  const operation = OPERATIONS[({ 1: "recepcion", 2: "despacho" })[guide.idTipoOperacionGuia]];
  const statusNames = { 1: "Pendiente", 2: "En proceso", 3: "Atendida", 4: "Cancelada" };
  document.title = `${guide.numeroGuia} | Proyecto AW`;
  document.querySelector("#guide-detail-title").textContent = guide.numeroGuia || `Guía #${guide.idGuia}`;
  document.querySelector("#guide-detail-operation").textContent = operation?.title || "Operación";
  const badge = document.querySelector("#guide-detail-status");
  badge.textContent = statusNames[guide.idEstadoGuia] || "Estado desconocido";
  badge.dataset.status = String(guide.idEstadoGuia);
  document.querySelector("#back-to-guides").href = "/modules/operaciones/guias/mis-guias.html";

  const warehouseName = id => lookups.warehouses.get(Number(id)) || `Almacén #${id}`;
  const counterpartyName = (id, kind) => lookups.counterparties.get(Number(id)) || `${kind} #${id}`;
  const facts = [
    ["Operación", operation?.title || `Tipo #${guide.idTipoOperacionGuia}`],
    ["Motivo", guide.nombreMotivo || "—"],
    ["Estado", statusNames[guide.idEstadoGuia] || "Desconocido"],
    ["Fecha programada", formatScheduledAt(guide.fechaProgramada)],
    ["Fecha de registro", formatScheduledAt(guide.fechaRegistro)]
  ];

  if (guide.tipoTercero && (guide.requiereProveedor || guide.requiereCliente)) {
    const counterpartyId = guide.requiereProveedor ? guide.idProveedor : guide.idCliente;
    facts.push([guide.tipoTercero, guide.nombreTercero || counterpartyName(counterpartyId, guide.tipoTercero)]);
  }
  if (guide.requiereAlmacenOrigen && guide.idAlmacenOrigen) facts.push(["Almacén de origen", guide.nombreAlmacenOrigen || warehouseName(guide.idAlmacenOrigen)]);
  if (guide.requiereAlmacenDestino && guide.idAlmacenDestino) facts.push(["Almacén de destino", guide.nombreAlmacenDestino || warehouseName(guide.idAlmacenDestino)]);
  if (guide.numeroGuiaRelacionada) facts.push(["Guía relacionada", guide.numeroGuiaRelacionada]);
  if (guide.tipoDocumentoReferencia || guide.numeroDocumentoReferencia) {
    facts.push(["Documento de referencia", [guide.tipoDocumentoReferencia, guide.numeroDocumentoReferencia].filter(Boolean).join(" · ")]);
  }
  if (guide.observacion) facts.push(["Observación", guide.observacion]);

  const summary = document.querySelector("#guide-detail-summary");
  summary.replaceChildren();
  for (const [label, value] of facts) {
    const row = document.createElement("div");
    const term = document.createElement("dt");
    term.textContent = label;
    const description = document.createElement("dd");
    description.textContent = value;
    row.append(term, description);
    summary.append(row);
  }

  const body = document.querySelector("#guide-detail-products");
  body.replaceChildren();
  for (const line of guide.detalles || []) {
    const row = document.createElement("tr");
    const product = document.createElement("td");
    product.textContent = lookups.products.get(Number(line.idProducto)) || `Producto #${line.idProducto}`;
    const quantity = document.createElement("td");
    quantity.className = "text-end";
    quantity.textContent = Number(line.cantidadEsperada).toLocaleString("es-PE", { maximumFractionDigits: 4 });
    row.append(product, quantity);
    body.append(row);
  }
  if (!body.children.length) {
    const row = document.createElement("tr");
    const cell = document.createElement("td");
    cell.colSpan = 2;
    cell.textContent = "La guía no tiene productos registrados.";
    row.append(cell);
    body.append(row);
  }
}

function showError(text) {
  message.textContent = text;
  message.className = "alert alert-danger";
  document.querySelector("#guide-detail-title").textContent = "No se pudo cargar la guía";
}
