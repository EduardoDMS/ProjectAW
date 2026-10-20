export class ApiError extends Error {
  constructor(message, status = null, details = null) {
    super(message);
    this.name = "ApiError";
    this.status = status;
    this.details = details;
  }
}

export async function checkApiReachability(timeoutMs = 5000) {
  const config = window.PROJECTAW_API_CONFIG;
  if (!config?.apiBaseUrl) return false;

  const controller = new AbortController();
  const timeout = window.setTimeout(() => controller.abort(), timeoutMs);

  try {
    // A no-cors probe can confirm a network/TLS response without exposing API data.
    // Its opaque response does not prove that a particular API route is healthy.
    await fetch(`${config.apiBaseUrl}/`, {
      method: "GET",
      mode: "no-cors",
      credentials: "omit",
      cache: "no-store",
      signal: controller.signal
    });
    return true;
  } catch {
    return false;
  } finally {
    window.clearTimeout(timeout);
  }
}

async function requestJson(path, options = {}) {
  const config = window.PROJECTAW_API_CONFIG;
  if (!config?.apiBaseUrl) {
    throw new ApiError("Configura la URL de la API para este entorno.");
  }

  const url = `${config.apiBaseUrl}/${String(path).replace(/^\//, "")}`;

  try {
    const response = await fetch(url, {
      ...options,
      credentials: config.credentials,
      headers: {
        Accept: "application/json, application/problem+json",
        ...(options.body === undefined ? {} : { "Content-Type": "application/json" }),
        ...options.headers
      },
    });

    const contentType = response.headers.get("content-type") || "";
    const data = contentType.includes("json")
      ? await response.json()
      : await response.text();

    if (!response.ok) {
      throw new ApiError(
        data?.mensaje || data?.detail || data?.title || `La API respondió con HTTP ${response.status}.`,
        response.status,
        data
      );
    }

    return data;
  } catch (error) {
    if (error instanceof ApiError) throw error;
    throw new ApiError("No fue posible conectar con la API. Revisa su disponibilidad y la configuración CORS.");
  }
}

export function getJson(path) {
  return requestJson(path, { method: "GET" });
}

export function postJson(path, body) {
  return requestJson(path, { method: "POST", body: JSON.stringify(body) });
}

export function post(path) {
  return requestJson(path, { method: "POST" });
}

export function deleteJson(path) {
  return requestJson(path, { method: "DELETE" });
}
