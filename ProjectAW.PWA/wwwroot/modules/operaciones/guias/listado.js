import { logout } from "/shared/js/session.js";
import {
  formatScheduledAt,
  getGuides,
  getReceptionLookups,
  OPERATIONS,
  showApiError
} from "/modules/operaciones/guias.js";

const list = document.querySelector("#guides-list");
const count = document.querySelector("#guides-count");
const status = document.querySelector("#guides-status");
const message = document.querySelector("#guides-message");
const operationEntry = Object.entries(OPERATIONS).find(([, operation]) =>
  operation.id === Number(new URLSearchParams(window.location.search).get("tipo")));

document.querySelector("#current-year").textContent = String(new Date().getFullYear());
document.querySelector("#logout-button").addEventListener("click", event => {
  event.currentTarget.disabled = true;
  void logout(message);
});

if (!operationEntry) {
  window.location.replace("/modules/operaciones/inicio/index.html");
} else {
  const [key, operation] = operationEntry;
  document.title = `${operation.title} | Proyecto AW`;
  document.querySelector("#guides-title").textContent = operation.title;
  document.querySelector("#guides-subtitle").textContent = operation.subtitle;
  void loadGuides(key, operation);
}

function addDetail(container, label, value) {
  const row = document.createElement("div");
  const term = document.createElement("dt");
  term.textContent = label;
  const description = document.createElement("dd");
  description.textContent = value;
  row.append(term, description);
  container.append(row);
}

function createGuideItem(guide, lookups, operationKey) {
  const item = document.createElement("li");
  item.className = "guide-item";
  const number = document.createElement("strong");
  number.className = "guide-number";
  number.textContent = guide.numeroGuia || "Código no disponible";
  const scheduled = document.createElement("time");
  scheduled.className = "guide-schedule";
  scheduled.dateTime = guide.fechaProgramada;
  scheduled.textContent = formatScheduledAt(guide.fechaProgramada);
  item.append(number, scheduled);

  if (operationKey === "recepcion") {
    const details = document.createElement("dl");
    details.className = "guide-details";
    const provider = lookups.providers.get(Number(guide.idProveedor)) || "No disponible en el catálogo";
    const warehouse = lookups.warehouses.get(Number(guide.idAlmacenDestino)) || "No disponible en el catálogo";
    addDetail(details, "Proveedor", provider);
    addDetail(details, "Almacén destino", warehouse);
    item.append(details);
  }

  return item;
}

async function loadGuides(operationKey, operation) {
  list.setAttribute("aria-busy", "true");
  message.className = "alert d-none";
  status.textContent = "Cargando guías desde la API…";

  try {
    const [guides, lookups] = await Promise.all([
      getGuides(operation.id),
      operationKey === "recepcion" ? getReceptionLookups() : Promise.resolve(null)
    ]);

    count.textContent = `${guides.length} ${guides.length === 1 ? "guía pendiente" : "guías pendientes"}`;
    if (guides.length === 0) {
      const empty = document.createElement("li");
      empty.className = "empty-state";
      empty.textContent = "No hay guías pendientes para esta operación.";
      list.append(empty);
    } else {
      for (const guide of guides) list.append(createGuideItem(guide, lookups, operationKey));
    }
    status.textContent = `${guides.length} guías pendientes cargadas desde la API, ordenadas por hora programada.`;
  } catch (error) {
    if (error.status === 401) {
      window.location.replace("/");
      return;
    }
    message.textContent = showApiError(error);
    message.className = "alert alert-danger";
    count.textContent = "No disponible";
    status.textContent = "No se pudieron cargar las guías.";
  } finally {
    list.setAttribute("aria-busy", "false");
  }
}
