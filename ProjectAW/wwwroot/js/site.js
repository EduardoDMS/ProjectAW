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

// Presenta errores de API o de validación sin interrumpir el flujo de la vista.
window.showAppError = function (message, title = "No se pudo completar") {
	const text =
		typeof message === "string" && message.trim()
			? message
			: "Ocurrió un error inesperado. Inténtalo nuevamente.";

	if (window.Swal?.fire) {
		return window.Swal.fire({
			icon: "error",
			title,
			text,
			confirmButtonText: "Entendido",
			confirmButtonColor: "#004596",
		});
	}

	window.alert(`${title}\n\n${text}`);
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
