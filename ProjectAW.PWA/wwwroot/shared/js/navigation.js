// Barra inferior común para las pantallas autenticadas del operador.
const currentPath = window.location.pathname.toLocaleLowerCase("es");
const destinations = [
  {
    label: "Ajustes",
    href: "/modules/operaciones/ajustes/index.html",
    match: "/ajustes/",
    icon: "ti-settings"
  },
  {
    label: "Inicio",
    href: "/modules/operaciones/inicio/index.html",
    match: "/inicio/",
    icon: "ti-home"
  },
  {
    label: "Consultar",
    href: "/modules/operaciones/consulta/index.html",
    match: "/consulta/",
    icon: "ti-search"
  }
];

const nav = document.createElement("nav");
nav.className = "operator-bottom-nav";
nav.setAttribute("aria-label", "Navegación del operador");
for (const destination of destinations) {
  const link = document.createElement("a");
  link.className = "operator-bottom-nav__item";
  link.href = destination.href;
  link.setAttribute("aria-label", destination.label);
  if (currentPath.includes(destination.match)) link.setAttribute("aria-current", "page");
  const icon = document.createElement("i");
  icon.className = `ti ${destination.icon}`;
  icon.setAttribute("aria-hidden", "true");
  const label = document.createElement("span");
  label.textContent = destination.label;
  link.append(icon, label);
  nav.append(link);
}
document.body.append(nav);
