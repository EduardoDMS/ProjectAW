import { getJson } from "/shared/js/api-client.js";

export const OPERATIONS = Object.freeze({
  recepcion: { id: 1, title: "Recepción", subtitle: "Guías de entrada", icon: "↓", color: "reception" },
  despacho: { id: 2, title: "Despacho", subtitle: "Guías de salida", icon: "↑", color: "dispatch" },
  traslado: { id: 3, title: "Traslado", subtitle: "Movimientos entre almacenes", icon: "⇄", color: "transfer" }
});

export const PENDING_STATUS_ID = 1;

export async function getGuides(operationId, statusId = PENDING_STATUS_ID) {
  const params = new URLSearchParams({
    idTipoOperacionGuia: String(operationId),
    idEstadoGuia: String(statusId)
  });
  const result = await getJson(`api/guias?${params}`);
  if (!Array.isArray(result)) throw new Error("La API devolvió un formato de guías no esperado.");

  return result.slice().sort((first, second) =>
    new Date(first.fechaProgramada).getTime() - new Date(second.fechaProgramada).getTime());
}

export async function getReceptionLookups() {
  const [providers, warehouses] = await Promise.all([
    getJson("api/ProveedorApi"),
    getJson("api/AlmacenApi")
  ]);

  if (!Array.isArray(providers) || !Array.isArray(warehouses)) {
    throw new Error("La API devolvió un formato de catálogos no esperado.");
  }

  return {
    providers: new Map(providers.map(item => [Number(item.idProveedor), item.razonSocial])),
    warehouses: new Map(warehouses.map(item => [
      Number(item.idAlmacen),
      [item.codigo, item.descripcion].filter(Boolean).join(" · ")
    ]))
  };
}

export function isUnauthorized(error) {
  return error?.status === 401;
}

export function showApiError(error) {
  if (error?.status === 403) return "Tu usuario no tiene permiso para consultar estas guías.";
  if (error?.status === 401) return "Tu sesión venció. Inicia sesión nuevamente.";
  if (error?.status === null) return "No se pudo conectar con la API. Revisa la conexión e inténtalo de nuevo.";
  return error?.message || "No se pudieron cargar los datos desde la API.";
}

export function formatScheduledAt(value) {
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return "Fecha programada no disponible";
  return new Intl.DateTimeFormat("es-PE", {
    dateStyle: "medium",
    timeStyle: "short"
  }).format(date);
}
