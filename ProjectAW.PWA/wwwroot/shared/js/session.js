import { ApiError, post } from "/shared/js/api-client.js";

export async function logout(messageElement = null) {
  try {
    await post("api/auth/logout");
  } catch (error) {
    if (!(error instanceof ApiError) || error.status !== 401) {
      if (messageElement) {
        messageElement.textContent = "No se pudo cerrar la sesión. Revisa la conexión e inténtalo otra vez.";
        messageElement.className = "alert alert-danger";
        return false;
      }
    }
  }

  window.location.assign("/");
  return true;
}

export function returnToLogin() {
  window.location.assign("/");
}
