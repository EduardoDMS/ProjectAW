import { ApiError } from "/shared/js/api-client.js";
import { logout, returnToLogin } from "/shared/js/session.js";
import { getGuides, isUnauthorized, OPERATIONS, showApiError } from "/modules/operaciones/guias.js";

const cardContainer = document.querySelector("#operation-cards");
const status = document.querySelector("#home-status");
const message = document.querySelector("#home-message");

document.querySelector("#current-year").textContent = String(new Date().getFullYear());
document.querySelector("#logout-button").addEventListener("click", event => {
  event.currentTarget.disabled = true;
  void logout(message);
});

function createCard(key, operation) {
  const article = document.createElement("article");
  article.className = `operation-card ${operation.color}`;
  const link = document.createElement("a");
  link.className = "operation-card-link";
  link.href = `/modules/operaciones/guias/index.html?tipo=${operation.id}`;
  link.setAttribute("aria-label", `Abrir ${operation.title}`);

  const icon = document.createElement("span");
  icon.className = "operation-icon";
  icon.setAttribute("aria-hidden", "true");
  icon.textContent = operation.icon;
  const heading = document.createElement("h2");
  heading.textContent = operation.title;
  const subtitle = document.createElement("p");
  subtitle.className = "operation-subtitle";
  subtitle.textContent = operation.subtitle;
  const count = document.createElement("p");
  count.className = "pending-count";
  count.dataset.countFor = key;
  count.textContent = "…";
  count.setAttribute("aria-label", "Consultando guías pendientes");
  const pendingLabel = document.createElement("span");
  pendingLabel.className = "pending-label";
  pendingLabel.textContent = "pendientes";
  const action = document.createElement("span");
  action.className = "operation-action";
  action.innerHTML = 'Ver guías <span aria-hidden="true">→</span>';
  link.append(icon, heading, subtitle, count, pendingLabel, action);
  article.append(link);
  return article;
}

for (const [key, operation] of Object.entries(OPERATIONS)) cardContainer.append(createCard(key, operation));

async function loadPendingCounts() {
  cardContainer.setAttribute("aria-busy", "true");
  message.className = "alert d-none";
  const entries = Object.entries(OPERATIONS);
  const results = await Promise.allSettled(entries.map(([, operation]) => getGuides(operation.id)));
  if (results.some(result => result.status === "rejected" && isUnauthorized(result.reason))) {
    status.textContent = "Tu sesión ya no está activa. Volviendo al acceso.";
    returnToLogin();
    return;
  }

  let hasErrors = false;
  results.forEach((result, index) => {
    const [key] = entries[index];
    const count = cardContainer.querySelector(`[data-count-for="${key}"]`);
    if (result.status === "fulfilled") {
      count.textContent = String(result.value.length);
      count.setAttribute("aria-label", `${result.value.length} guías pendientes`);
      return;
    }
    hasErrors = true;
    count.textContent = "—";
    count.setAttribute("aria-label", "No fue posible consultar las guías pendientes");
    if (result.reason instanceof ApiError && result.reason.status === 403) message.textContent = showApiError(result.reason);
  });

  status.textContent = hasErrors ? "No se pudieron consultar todas las guías pendientes." : "Pendientes actualizados desde la API.";
  if (hasErrors && !message.textContent) message.textContent = "No se pudieron consultar algunas guías. Comprueba la conexión e inténtalo de nuevo.";
  if (hasErrors) message.className = "alert alert-warning";
  cardContainer.setAttribute("aria-busy", "false");
}

void loadPendingCounts();
