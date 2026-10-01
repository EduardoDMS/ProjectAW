document.addEventListener('DOMContentLoaded', () => {
    cargarProductos();
});

async function cargarProductos() {
    try {
        const response = await fetch('/api/ProductoApi');

        if (!response.ok) {
            throw new Error("No se pudieron obtener los productos.");
        }

        const productos = await response.json();

        console.log(productos);
    }
    catch (error) {
        console.error("Error:", error);
    }
}