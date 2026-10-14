(() => {
	const body = document.body;
	const toggle = document.querySelector("[data-sidebar-toggle]");
	const sidebar = document.querySelector("#appSidebar");
	const closeButton = document.querySelector("[data-sidebar-close]");
	const groups = document.querySelectorAll("[data-sidebar-group]");
	const mobileQuery = window.matchMedia("(max-width: 991.98px)");
	const storageKey = "aw-sidebar-collapsed";

	if (!toggle || !sidebar) return;

	// Actualiza la navegación lateral en móvil y sus atributos de accesibilidad.
	function setMobileOpen(open) {
		body.classList.toggle("sidebar-mobile-open", open);
		toggle.setAttribute("aria-expanded", String(open));
		toggle.setAttribute(
			"aria-label",
			open ? "Cerrar menú lateral" : "Abrir menú lateral",
		);
	}

	// Contrae o expande la navegación y conserva la preferencia localmente.
	function setCollapsed(collapsed, persist = true) {
		body.classList.toggle("sidebar-collapsed", collapsed);
		if (persist) {
			try {
				localStorage.setItem(storageKey, String(collapsed));
			} catch {
				/* almacenamiento opcional */
			}
		}
		if (!mobileQuery.matches) {
			toggle.setAttribute("aria-expanded", String(!collapsed));
			toggle.setAttribute(
				"aria-label",
				collapsed ? "Expandir menú lateral" : "Contraer menú lateral",
			);
		}
	}

	try {
		setCollapsed(localStorage.getItem(storageKey) === "true", false);
	} catch {
		setCollapsed(false, false);
	}
	if (mobileQuery.matches) setMobileOpen(false);

	// Alterna el menú lateral: contraído en escritorio o desplegado en móvil.
	toggle.addEventListener("click", () => {
		if (mobileQuery.matches)
			setMobileOpen(!body.classList.contains("sidebar-mobile-open"));
		else setCollapsed(!body.classList.contains("sidebar-collapsed"));
	});

	// Cierra el menú móvil al usar el botón de cierre.
	closeButton?.addEventListener("click", () => setMobileOpen(false));
	// Permite cerrar el menú móvil con la tecla Escape.
	document.addEventListener("keydown", (event) => {
		if (event.key === "Escape") setMobileOpen(false);
	});
	sidebar.querySelectorAll("a").forEach((link) =>
		link.addEventListener("click", () => {
			if (mobileQuery.matches) setMobileOpen(false);
		}),
	);

	for (const group of groups) {
		const submenu = document.getElementById(
			group.getAttribute("aria-controls"),
		);
		if (!submenu) continue;

		// Expande o contrae el submenú seleccionado.
		group.addEventListener("click", () => {
			if (
				body.classList.contains("sidebar-collapsed") &&
				!mobileQuery.matches
			) {
				setCollapsed(false);
				group.setAttribute("aria-expanded", "true");
				submenu.classList.add("is-open");
				return;
			}
			const open = group.getAttribute("aria-expanded") !== "true";
			group.setAttribute("aria-expanded", String(open));
			submenu.classList.toggle("is-open", open);
		});
	}

	// Sincroniza el estado del menú al cambiar entre tamaños de pantalla.
	mobileQuery.addEventListener("change", () => {
		setMobileOpen(false);
		if (!mobileQuery.matches) {
			toggle.setAttribute(
				"aria-expanded",
				String(!body.classList.contains("sidebar-collapsed")),
			);
			toggle.setAttribute(
				"aria-label",
				body.classList.contains("sidebar-collapsed")
					? "Expandir menú lateral"
					: "Contraer menú lateral",
			);
		}
	});
})();
