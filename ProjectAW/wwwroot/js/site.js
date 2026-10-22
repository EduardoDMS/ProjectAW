// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

// Convierte respuestas de error de API en un mensaje entendible para SweetAlert2.
window.getApiErrorMessage = function (response, problem = {}) {
	const validationMessages = problem.errors
		? Object.values(problem.errors).flat(Infinity)
		: [];
	const apiMessage =
		problem.mensaje ||
		problem.detail ||
		problem.message ||
		validationMessages.find((message) => typeof message === "string") ||
		problem.title;

	if (apiMessage) return apiMessage;

	const statusMessages = {
		400: "Revisa los datos ingresados e inténtalo nuevamente.",
		401: "Tu sesión expiró. Inicia sesión e inténtalo nuevamente.",
		403: "No tienes permiso para realizar esta operación.",
		404: "No se encontró el registro solicitado.",
		409: "La operación entra en conflicto con un registro existente.",
		500: "Ocurrió un error en el servidor. Inténtalo más tarde.",
	};

	return (
		statusMessages[response.status] ||
		`No se pudo completar la solicitud (código ${response.status}).`
	);
};

// Presenta errores de API o validación sin bloquear la pantalla ni pedir confirmación.
window.showAppError = function (message, title = "No se pudo completar") {
	const text =
		typeof message === "string" && message.trim()
			? message
			: "Ocurrió un error inesperado. Inténtalo nuevamente.";

	if (window.Swal?.fire) {
		return window.Swal.fire({
			toast: true,
			position: "top-end",
			icon: "error",
			title,
			text,
			showConfirmButton: false,
			timer: 7000,
			timerProgressBar: true,
			showCloseButton: true,
			customClass: { popup: "app-error-toast" },
		});
	}

	const previous = document.querySelector("[data-app-error-toast]");
	previous?.remove();
	const toast = document.createElement("section");
	toast.dataset.appErrorToast = "true";
	toast.setAttribute("role", "alert");
	toast.setAttribute("aria-live", "assertive");
	Object.assign(toast.style, {
		position: "fixed", top: "1rem", right: "1rem", zIndex: "2000",
		width: "min(26rem, calc(100vw - 2rem))", padding: "1rem 2.75rem 1rem 1rem",
		background: "#fff", color: "#25364a", borderLeft: "4px solid #dc3545",
		borderRadius: ".5rem", boxShadow: "0 8px 28px rgba(20, 40, 65, .2)",
	});
	const heading = document.createElement("strong");
	heading.textContent = title;
	const description = document.createElement("div");
	description.textContent = text;
	description.style.marginTop = ".25rem";
	const close = document.createElement("button");
	close.type = "button";
	close.textContent = "×";
	close.setAttribute("aria-label", "Cerrar aviso");
	Object.assign(close.style, { position: "absolute", top: ".4rem", right: ".65rem", border: "0", background: "transparent", fontSize: "1.4rem", cursor: "pointer" });
	close.addEventListener("click", () => toast.remove());
	toast.append(heading, description, close);
	document.body.append(toast);
	window.setTimeout(() => toast.remove(), 8000);
};

// Confirma los registros y ediciones correctos con un mensaje breve y consistente.
window.showAppSuccess = function (message, title = "Cambios guardados") {
	if (window.Swal?.fire) {
		return window.Swal.fire({
			icon: "success",
			title,
			text: message,
			confirmButtonText: "Continuar",
			confirmButtonColor: "#004596",
			timer: 2200,
			timerProgressBar: true,
		});
	}

	window.alert(`${title}\n\n${message}`);
};
