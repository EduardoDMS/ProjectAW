// Administra usuarios desde las APIs existentes de Seguridad y Roles.
document.addEventListener("DOMContentLoaded", () => {
	const api = "/api/UsuarioApi";
	const form = document.querySelector("#user-form");
	const table = document.querySelector("#users-table");
	const modal = bootstrap.Modal.getOrCreateInstance(document.querySelector("#user-modal"));
	const alertBox = document.querySelector("#user-alert");
	const formAlert = document.querySelector("#user-form-alert");
	const feedback = document.querySelector("#user-feedback");
	const roleSelect = document.querySelector("#user-role");
	const safeText = DataTable.render.text();
	let users = [];
	let roles = [];
	let editingId = null;

	async function request(url, options = {}) {
		const headers = new Headers(options.headers || {});
		headers.set("Accept", "application/json, application/problem+json");
		if (options.body != null && !headers.has("Content-Type"))
			headers.set("Content-Type", "application/json");
		const response = await fetch(url, { credentials: "same-origin", ...options, headers });
		if (!response.ok) {
			const problem = await response.json().catch(() => ({}));
			throw new Error(window.getApiErrorMessage(response, problem));
		}
		return response.status === 204 ? null : response.json();
	}

	const statusMarkup = (active) =>
		`<span class="record-status ${active ? "is-active" : "is-inactive"}">${active ? "Activo" : "Inactivo"}</span>`;
	const actionMarkup = (user) =>
		`<button class="btn btn-sm btn-outline-primary" type="button" data-edit="${Number(user.idUsuario)}">Ver / editar</button> ` +
		`<button class="btn btn-sm btn-outline-secondary" type="button" data-toggle="${Number(user.idUsuario)}" data-active="${!user.activo}">${user.activo ? "Desactivar" : "Activar"}</button>`;

	const grid = new DataTable(table, {
		data: [],
		columns: [
			{ data: "username", render: safeText },
			{ data: null, render: (_value, _type, user) => safeText.display(`${user.nombre} ${user.apellido}`) },
			{ data: "nombreRol", render: safeText },
			{ data: "activo", render: (active, type) => type === "display" ? statusMarkup(active) : active ? "Activo" : "Inactivo" },
			{ data: null, orderable: false, searchable: false, render: (_value, _type, user) => actionMarkup(user) },
		],
		language: {
			emptyTable: "No hay usuarios registrados.",
			search: "Buscar:",
			lengthMenu: "Mostrar _MENU_",
			info: "Mostrando _START_ a _END_ de _TOTAL_",
			paginate: { previous: "Anterior", next: "Siguiente" },
		},
	});

	function showError(message, target = alertBox) {
		target.textContent = message;
		target.hidden = false;
	}

	async function loadUsers() {
		try {
			users = await request(api);
			drawUsers();
			alertBox.hidden = true;
		} catch (error) {
			showError(error.message || "No se pudieron cargar los usuarios.");
		}
	}

	function drawUsers() {
		const status = document.querySelector("#user-status").value;
		const filtered = status === "" ? users : users.filter((user) => String(user.activo) === status);
		grid.clear().rows.add(filtered).draw();
	}

	async function loadRoles() {
		const allRoles = await request("/api/RolApi");
		roles = allRoles.filter((role) => /^(admin|administrador|administrador inicial|operador)$/i.test(role.nombre.trim()));
		roleSelect.replaceChildren(new Option("Selecciona Administrador u Operador", ""));
		for (const role of roles)
			roleSelect.append(new Option(role.nombre, String(role.idRol)));
		if (!roles.length) throw new Error("No se encontraron roles Administrador u Operador. Revisa el catálogo de roles.");
	}

	function prepareForm(user = null) {
		editingId = user?.idUsuario ?? null;
		form.reset();
		formAlert.hidden = true;
		document.querySelector("#user-modal-title").textContent = user ? "Editar usuario" : "Nuevo usuario";
		document.querySelector("#save-user").textContent = user ? "Guardar cambios" : "Crear usuario";
		const password = document.querySelector("#user-password");
		password.required = !user;
		password.value = "";
		document.querySelector("#password-required-mark").hidden = Boolean(user);
		document.querySelector("#password-hint").textContent = user
			? "Déjala vacía para conservar la contraseña actual."
			: "Define la contraseña inicial para esta cuenta.";
		if (user) {
			form.elements.nombre.value = user.nombre;
			form.elements.apellido.value = user.apellido;
			form.elements.username.value = user.username;
			roleSelect.value = String(user.idRol);
		}
		modal.show();
	}

	document.querySelector("#new-user").addEventListener("click", () => prepareForm());
	document.querySelector("#user-status").addEventListener("change", drawUsers);

	table.addEventListener("click", async (event) => {
		const editButton = event.target.closest("[data-edit]");
		const toggleButton = event.target.closest("[data-toggle]");
		try {
			if (editButton) {
				const user = await request(`${api}/${Number(editButton.dataset.edit)}`);
				prepareForm(user);
				return;
			}
			if (toggleButton) {
				const user = users.find((item) => item.idUsuario === Number(toggleButton.dataset.toggle));
				if (!user) return;
				const nextState = toggleButton.dataset.active === "true";
				const result = await Swal.fire({
					title: nextState ? "¿Activar usuario?" : "¿Desactivar usuario?",
					text: `${user.username} ${nextState ? "podrá volver a iniciar sesión" : "ya no podrá iniciar sesión"}.`,
					icon: "question",
					showCancelButton: true,
					confirmButtonText: nextState ? "Activar" : "Desactivar",
					cancelButtonText: "Cancelar",
				});
				if (!result.isConfirmed) return;
				await request(`${api}/${user.idUsuario}/estado`, { method: "PATCH", body: JSON.stringify(nextState) });
				feedback.textContent = `Estado actualizado para ${user.username}.`;
				await loadUsers();
			}
		} catch (error) {
			window.showAppError(error.message || "No se pudo completar la acción del usuario.");
		}
	});

	form.addEventListener("submit", async (event) => {
		event.preventDefault();
		if (!form.reportValidity()) return;
		const payload = {
			nombre: form.elements.nombre.value.trim(),
			apellido: form.elements.apellido.value.trim(),
			username: form.elements.username.value.trim(),
			password: form.elements.password.value,
			idRol: Number(form.elements.idRol.value),
		};
		const saveButton = document.querySelector("#save-user");
		saveButton.disabled = true;
		try {
			const wasEditing = editingId !== null;
			await request(wasEditing ? `${api}/${editingId}` : api, {
				method: wasEditing ? "PUT" : "POST",
				body: JSON.stringify(payload),
			});
			modal.hide();
			await loadUsers();
			await window.showAppSuccess(
				wasEditing ? "Los datos del usuario se actualizaron." : "El usuario se creó correctamente.",
				wasEditing ? "Usuario actualizado" : "Usuario creado",
			);
		} catch (error) {
			showError(error.message || "No se pudo guardar el usuario.", formAlert);
		} finally {
			saveButton.disabled = false;
		}
	});

	(async () => {
		try {
			await loadRoles();
			await loadUsers();
		} catch (error) {
			showError(error.message || "No se pudieron cargar los roles.");
			document.querySelector("#new-user").disabled = true;
		}
	})();
});
