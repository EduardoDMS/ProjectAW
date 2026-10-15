import { ApiError, checkApiReachability, postJson } from "/shared/js/api-client.js";

const form = document.querySelector("#login-form");
const username = document.querySelector("#username");
const password = document.querySelector("#password");
const togglePassword = document.querySelector("#toggle-password");
const submitButton = document.querySelector("#login-submit");
const submitLabel = submitButton.querySelector(".submit-label");
const spinner = submitButton.querySelector(".spinner-border");
const message = document.querySelector("#login-message");
const apiStatus = document.querySelector("#api-status");
const apiStatusMessage = apiStatus.querySelector(".status-message");

let submitting = false;
let checkingApi = false;

document.querySelector("#current-year").textContent = String(new Date().getFullYear());

async function updateApiStatus() {
  if (checkingApi) return;
  checkingApi = true;
  apiStatus.className = "api-status is-checking";
  apiStatusMessage.textContent = "Verificando disponibilidad del servidor…";

  const reachable = await checkApiReachability();
  apiStatus.className = `api-status ${reachable ? "is-connected" : "is-disconnected"}`;
  apiStatusMessage.textContent = reachable
    ? "Servidor API alcanzable"
    : "No se pudo contactar con el servidor API";
  checkingApi = false;
}

apiStatus.title = "Esta comprobación verifica conectividad HTTPS; no valida sesión ni el estado de endpoints específicos.";
void updateApiStatus();
window.addEventListener("online", updateApiStatus);
window.addEventListener("offline", () => {
  apiStatus.className = "api-status is-disconnected";
  apiStatusMessage.textContent = "Sin conexión de red";
});
window.setInterval(updateApiStatus, 30000);

function showMessage(text, type) {
  message.textContent = text;
  message.className = `alert alert-${type}`;
  message.focus();
}

function clearMessage() {
  message.textContent = "";
  message.className = "alert d-none";
}

function setSubmitting(value) {
  submitting = value;
  submitButton.disabled = value;
  submitButton.setAttribute("aria-busy", String(value));
  submitButton.classList.toggle("is-loading", value);
  spinner.classList.toggle("d-none", !value);
  submitLabel.textContent = value ? "Validando acceso…" : "Iniciar sesión";
}

username.addEventListener("input", () => {
  username.setCustomValidity(username.value.trim() ? "" : "Ingresa tu usuario.");
});

togglePassword.addEventListener("click", () => {
  const reveal = password.type === "password";
  password.type = reveal ? "text" : "password";
  togglePassword.setAttribute("aria-label", reveal ? "Ocultar contraseña" : "Mostrar contraseña");
  togglePassword.setAttribute("aria-pressed", String(reveal));
});

form.addEventListener("submit", async (event) => {
  event.preventDefault();
  if (submitting) return;

  clearMessage();
  username.setCustomValidity(username.value.trim() ? "" : "Ingresa tu usuario.");
  form.classList.add("was-validated");

  if (!form.checkValidity()) {
    form.querySelector(":invalid")?.focus();
    return;
  }

  setSubmitting(true);
  let authenticated = false;

  try {
    await postJson("api/auth/login", {
      username: username.value.trim(),
      password: password.value
    });

    authenticated = true;
    password.value = "";
    window.location.assign("/modules/operaciones/inicio/index.html");
  } catch (error) {
    if (error instanceof ApiError && error.status === 401) {
      showMessage("Usuario o contraseña incorrectos.", "danger");
    } else if (error instanceof ApiError && error.status === 403) {
      showMessage("Tu usuario no tiene permiso para acceder.", "warning");
    } else if (error instanceof ApiError && error.status === 400) {
      showMessage("Revisa los datos ingresados e inténtalo nuevamente.", "warning");
    } else if (error instanceof ApiError && error.status === null) {
      showMessage(error.message || "No fue posible conectar con la API.", "danger");
    } else {
      showMessage("No fue posible iniciar sesión. Inténtalo de nuevo más tarde.", "danger");
    }
  } finally {
    if (!authenticated) setSubmitting(false);
  }
});
