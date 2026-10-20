import { getJson } from "/shared/js/api-client.js";
import "/shared/js/navigation.js";

const form = document.querySelector("#stock-consult-form");
const codeInput = document.querySelector("#stock-consult-code");
const label = document.querySelector("#stock-consult-label");
const status = document.querySelector("#stock-consult-status");
const results = document.querySelector("#stock-consult-results");
let mode = "product";
let productCatalog = null;

document.querySelector("#current-year").textContent = String(new Date().getFullYear());
document.querySelectorAll("[data-mode]").forEach(button => button.addEventListener("click", () => {
  mode = button.dataset.mode;
  document.querySelectorAll("[data-mode]").forEach(item => item.setAttribute("aria-pressed", String(item === button)));
  label.textContent = mode === "product" ? "Código del producto" : "Código de ubicación";
  codeInput.placeholder = mode === "product" ? "Escanea o escribe el código del producto" : "Escanea o escribe la ubicación";
  codeInput.value = "";
  status.textContent = "";
  results.replaceChildren();
  codeInput.focus();
}));

function addText(parent, tag, className, text) {
  const element = document.createElement(tag);
  if (className) element.className = className;
  element.textContent = text;
  parent.append(element);
  return element;
}

function renderProductResults(stock, product) {
  results.replaceChildren();
  if (!stock.length) { status.textContent = "El producto no tiene existencias registradas en ubicaciones activas."; return; }
  status.textContent = `${product.codigo} · ${product.descripcion}: ${stock.length} ubicaciones con existencia.`;
  const warehouses = new Map();
  for (const item of stock) {
    const key = Number(item.idAlmacen);
    if (!warehouses.has(key)) warehouses.set(key, { codigo: item.codigoAlmacen, nombre: item.almacen, locations: [] });
    warehouses.get(key).locations.push(item);
  }
  for (const warehouse of warehouses.values()) {
    const card = document.createElement("article"); card.className = "stock-consult-card";
    addText(card, "h2", "", `${warehouse.codigo} · ${warehouse.nombre || "Almacén"}`);
    const list = document.createElement("ul"); list.className = "stock-consult-list";
    for (const item of warehouse.locations) {
      const row = document.createElement("li");
      addText(row, "strong", "", item.ubicacion || "Ubicación sin código");
      addText(row, "span", "", `${item.cantidadActual} ${item.unidadMedida || "unidades"}`);
      list.append(row);
    }
    card.append(list); results.append(card);
  }
}

function renderLocationResults(stock, code) {
  results.replaceChildren();
  if (!stock.length) { status.textContent = "No hay productos con stock en esa ubicación, o el código no existe."; return; }
  status.textContent = `${stock.length} productos encontrados en la ubicación ${code}.`;
  const locations = new Map();
  for (const item of stock) {
    const key = `${item.idAlmacen}:${item.ubicacion}`;
    if (!locations.has(key)) locations.set(key, { ...item, products: [] });
    locations.get(key).products.push(item);
  }
  for (const location of locations.values()) {
    const card = document.createElement("article"); card.className = "stock-consult-card";
    addText(card, "h2", "", `${location.ubicacion} · ${location.codigoAlmacen} ${location.almacen || ""}`.trim());
    const list = document.createElement("ul"); list.className = "stock-consult-list";
    for (const item of location.products) {
      const row = document.createElement("li");
      addText(row, "strong", "", `${item.codigoProducto} · ${item.producto}`);
      addText(row, "span", "", `${item.cantidadActual} ${item.unidadMedida || "unidades"}`);
      list.append(row);
    }
    card.append(list); results.append(card);
  }
}

form.addEventListener("submit", async event => {
  event.preventDefault();
  const code = codeInput.value.trim();
  if (!code) return;
  const submit = form.querySelector("button[type=submit]");
  submit.disabled = true;
  results.replaceChildren(); status.textContent = "Consultando…";
  try {
    if (mode === "product") {
      if (!productCatalog) productCatalog = await getJson("api/ProductoApi");
      const product = productCatalog.find(item => String(item.codigo || "").trim().toLocaleLowerCase("es") === code.toLocaleLowerCase("es"));
      if (!product) { status.textContent = "No se encontró un producto con ese código."; return; }
      renderProductResults(await getJson(`api/stocks?idProducto=${encodeURIComponent(product.idProducto)}`), product);
    } else {
      const stock = await getJson(`api/stocks?codigoUbicacion=${encodeURIComponent(code)}`);
      renderLocationResults(stock, code);
    }
  } catch (error) {
    if (error.status === 401) { location.replace("/"); return; }
    status.textContent = error.message || "No se pudo consultar el inventario.";
  } finally { submit.disabled = false; }
});
