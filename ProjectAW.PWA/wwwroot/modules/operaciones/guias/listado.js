import { logout } from "/shared/js/session.js";
import {
  formatScheduledAt,
  getGuideById,
  getGuides,
  getReceptionLookups,
  OPERATIONS,
  showApiError,
  takeGuide
} from "/modules/operaciones/guias.js";

const list = document.querySelector("#guides-list");
const count = document.querySelector("#guides-count");
const status = document.querySelector("#guides-status");
const message = document.querySelector("#guides-message");
let selectionInProgress = false;
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

function createGuideItem(guide, lookups, operationKey, operation) {
  const item = document.createElement("li");
  const card = document.createElement("button");
  card.type = "button";
  card.className = "guide-item";
  card.setAttribute("aria-label", `Seleccionar guía ${guide.numeroGuia || "sin código"}`);
  const number = document.createElement("strong");
  number.className = "guide-number";
  number.textContent = guide.numeroGuia || "Código no disponible";
  const scheduled = document.createElement("time");
  scheduled.className = "guide-schedule";
  scheduled.dateTime = guide.fechaProgramada;
  scheduled.textContent = formatScheduledAt(guide.fechaProgramada);
  card.append(number, scheduled);

  if (operationKey === "recepcion") {
    const details = document.createElement("dl");
    details.className = "guide-details";
    const provider = lookups.providers.get(Number(guide.idProveedor)) || "No disponible en el catálogo";
    const warehouse = lookups.warehouses.get(Number(guide.idAlmacenDestino)) || "No disponible en el catálogo";
    addDetail(details, "Proveedor", provider);
    addDetail(details, "Almacén destino", warehouse);
    card.append(details);
  }

  card.addEventListener("click", () => void selectGuide(guide.idGuia, operationKey, operation));
  item.append(card);
  return item;
}

async function selectGuide(id, operationKey, operation) {
  if (selectionInProgress) return;
  selectionInProgress = true;
  const message = document.querySelector("#guides-message");
  try {
    // Confirma el estado y obtiene el código y fecha actuales antes de preguntar.
    const guide = await getGuideById(id);
    if (guide.idEstadoGuia !== 1) {
      throw new Error("Esta guía ya no está pendiente. Actualiza la lista e inténtalo de nuevo.");
    }

    document.querySelector("#confirm-guide-code").textContent = guide.numeroGuia || "Código no disponible";
    document.querySelector("#confirm-guide-date").textContent = formatScheduledAt(guide.fechaProgramada);
    const dialog = document.querySelector("#take-guide-confirmation");
    const confirmed = await new Promise(resolve => {
      dialog.addEventListener("close", () => resolve(dialog.returnValue === "confirm"), { once: true });
      dialog.showModal();
    });
    if (!confirmed) return;

    const confirmButton = document.querySelector("#confirm-take-guide");
    confirmButton.disabled = true;
    try {
      await takeGuide(guide.idGuia);
      window.location.assign(`/modules/operaciones/guias/detalle.html?id=${encodeURIComponent(guide.idGuia)}`);
    } finally {
      confirmButton.disabled = false;
    }
  } catch (error) {
    if (error.status === 401) {
      window.location.replace("/");
      return;
    }
    message.textContent = showApiError(error);
    message.className = "alert alert-danger";
  } finally {
    selectionInProgress = false;
  }
}

async function loadGuides(operationKey, operation) {
  list.replaceChildren();
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
      for (const guide of guides) list.append(createGuideItem(guide, lookups, operationKey, operation));
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
