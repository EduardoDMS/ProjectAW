import { logout } from "/shared/js/session.js";
import { formatScheduledAt, getMyGuides, OPERATIONS, showApiError } from "/modules/operaciones/guias.js";

const section = document.querySelector(".my-guides-page");
const list = document.querySelector("#my-guides-list");
const status = document.querySelector("#my-guides-status");
const message = document.querySelector("#my-guides-message");
const tabs = [...document.querySelectorAll("[role='tab']")];
let selectedStatus = 2;

document.querySelector("#current-year").textContent = String(new Date().getFullYear());
document.querySelector("#logout-button").addEventListener("click", event => {
  event.currentTarget.disabled = true;
  void logout(message);
});

for (const tab of tabs) {
  tab.addEventListener("click", () => {
    selectedStatus = Number(tab.dataset.status);
    for (const item of tabs) item.setAttribute("aria-selected", String(item === tab));
    void loadGuides();
  });
}

async function loadGuides() {
  section.setAttribute("aria-busy", "true");
  list.setAttribute("aria-busy", "true");
  list.replaceChildren();
  message.className = "alert d-none";
  status.textContent = "Cargando tus guías…";
  try {
    const guides = await getMyGuides(selectedStatus);
    if (!Array.isArray(guides)) throw new Error("La API devolvió un formato de guías no esperado.");
    if (!guides.length) {
      const empty = document.createElement("li");
      empty.className = "empty-state";
      empty.textContent = selectedStatus === 2
        ? "No tienes guías en proceso. Cuando tomes una guía, aparecerá aquí."
        : "Aún no tienes guías terminadas.";
      list.append(empty);
    } else {
      for (const guide of guides) list.append(createGuideCard(guide));
    }
    status.textContent = `${guides.length} ${selectedStatus === 2 ? "guías en proceso" : "guías terminadas"}.`;
  } catch (error) {
    if (error.status === 401) {
      window.location.replace("/");
      return;
    }
    message.textContent = showApiError(error);
    message.className = "alert alert-danger";
    status.textContent = "No se pudieron cargar tus guías.";
  } finally {
    section.setAttribute("aria-busy", "false");
    list.setAttribute("aria-busy", "false");
  }
}

function createGuideCard(guide) {
  const item = document.createElement("li");
  const link = document.createElement("a");
  link.className = "guide-item a-guide-item";
  link.href = `/modules/operaciones/guias/detalle.html?id=${encodeURIComponent(guide.idGuia)}`;
  const number = document.createElement("strong");
  number.className = "guide-number";
  number.textContent = guide.numeroGuia || `Guía #${guide.idGuia}`;
  const date = document.createElement("time");
  date.className = "guide-schedule";
  date.dateTime = guide.fechaProgramada;
  date.textContent = formatScheduledAt(guide.fechaProgramada);
  const operation = document.createElement("span");
  operation.className = "guide-operation-label";
  operation.textContent = Object.values(OPERATIONS).find(item => item.id === guide.idTipoOperacionGuia)?.title || "Operación";
  const state = document.createElement("span");
  state.className = "my-guide-state";
  state.dataset.status = String(guide.idEstadoGuia);
  state.textContent = guide.idEstadoGuia === 2 ? "En proceso" : "Terminada";
  link.append(number, date, operation, state);
  item.append(link);
  return item;
}

void loadGuides();
