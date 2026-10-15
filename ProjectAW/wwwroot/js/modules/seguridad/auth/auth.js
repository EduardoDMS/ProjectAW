(() => {
	const loginPage = document.querySelector("#login-page");
	const loginForm = document.querySelector("#login-form");

	// Muestra mensajes de inicio o cierre de sesión en la vista de autenticación.
	const showAlert = (element, message) => {
		if (!element) return;
		element.textContent = message;
		element.hidden = false;
	};

	// Bloquea o habilita un botón mientras fetch espera respuesta del API.
	const setLoading = (button, isLoading) => {
		if (!button) return;
		button.disabled = isLoading;
		button.classList.toggle("is-loading", isLoading);
		button.setAttribute("aria-busy", String(isLoading));
	};

	if (loginForm && loginPage) {
		const alertBox = document.querySelector("#login-error");
		const submitButton = document.querySelector("#login-submit");
		const usernameInput = document.querySelector("#username");
		const passwordInput = document.querySelector("#password");
		const credentialInputs = [usernameInput, passwordInput];
		const passwordToggle = document.querySelector("#toggle-password");

		// Aplica los estados visuales de Bootstrap y actualiza el mensaje del campo.
		const validateInput = (input) => {
			const isFilled =
				input === usernameInput
					? Boolean(input.value.trim())
					: Boolean(input.value);
			const fieldLabel = input === usernameInput ? "usuario" : "contraseña";
			input.setCustomValidity(
				isFilled ? "" : `Ingresa tu ${fieldLabel}.`,
			);

			const isValid = input.checkValidity();
			const feedback = document.querySelector(`#${input.id}-feedback`);
			input.classList.toggle("is-valid", isValid);
			input.classList.toggle("is-invalid", !isValid);

			if (feedback && !isValid) {
				feedback.textContent = input.validationMessage;
			}

			return isValid;
		};

		credentialInputs.forEach((input) => {
			input.addEventListener("input", () => {
				if (input.dataset.touched === "true") {
					validateInput(input);
				}
			});
		});

		// Envía las credenciales por fetch al servidor y redirige al iniciar sesión.
		loginForm.addEventListener("submit", async (event) => {
			event.preventDefault();
			alertBox.hidden = true;

			const invalidInputs = credentialInputs.filter((input) => {
				input.dataset.touched = "true";
				return !validateInput(input);
			});

			if (invalidInputs.length > 0) {
				invalidInputs[0].focus();
				return;
			}

			setLoading(submitButton, true);

			try {
				const response = await fetch(loginPage.dataset.loginUrl, {
					method: "POST",
					credentials: "same-origin",
					headers: {
						"Content-Type": "application/json",
						Accept: "application/json, application/problem+json",
					},
					body: JSON.stringify({
						username: usernameInput.value.trim(),
						password: passwordInput.value,
					}),
				});

				if (!response.ok) {
					const problem = await response.json().catch(() => ({}));
					showAlert(
						alertBox,
						problem.mensaje ||
							problem.detail ||
							problem.title ||
							"No se pudo iniciar sesión. Verifica tus credenciales.",
					);
					passwordInput.focus();
					return;
				}

				window.location.assign(loginPage.dataset.returnUrl || "/");
			} catch {
				showAlert(
					alertBox,
					"No fue posible conectar con el servidor. Comprueba tu conexión e inténtalo de nuevo.",
				);
			} finally {
				setLoading(submitButton, false);
			}
		});

		// Alterna la visibilidad de la contraseña sin modificar los datos enviados.
		passwordToggle?.addEventListener("click", () => {
			const isVisible = passwordInput.type === "text";
			passwordInput.type = isVisible ? "password" : "text";
			passwordToggle.setAttribute(
				"aria-label",
				isVisible ? "Mostrar contraseña" : "Ocultar contraseña",
			);
			passwordToggle.setAttribute("aria-pressed", String(!isVisible));
			passwordToggle.querySelector(".ti")?.classList.toggle("ti-eye", isVisible);
			passwordToggle.querySelector(".ti")?.classList.toggle("ti-eye-off", !isVisible);
		});
	}

	// Registra el cierre de sesión para cada botón de salida de la vista.
	document.querySelectorAll("[data-auth-logout]").forEach((button) => {
		button.addEventListener("click", async () => {
			button.disabled = true;
			button.setAttribute("aria-busy", "true");
			const feedback = document.querySelector("#logout-feedback");
			if (feedback) feedback.hidden = true;

			try {
				const response = await fetch("/api/auth/logout", {
					method: "POST",
					credentials: "same-origin",
					headers: { Accept: "application/problem+json" },
				});

				if (response.ok || response.status === 401) {
					window.location.assign("/Login");
					return;
				}

				const problem = await response.json().catch(() => ({}));
				throw new Error(
					problem.mensaje ||
						problem.detail ||
						"No se pudo cerrar la sesión.",
				);
			} catch (error) {
				button.disabled = false;
				button.removeAttribute("aria-busy");
				if (feedback) {
					feedback.textContent =
						error instanceof Error
							? error.message
							: "No fue posible conectar con el servidor.";
					feedback.hidden = false;
				}
			}
		});
	});
})();
