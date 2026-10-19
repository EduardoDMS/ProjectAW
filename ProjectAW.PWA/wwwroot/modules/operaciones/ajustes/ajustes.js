import { logout } from "/shared/js/session.js";

const key = "projectaw.operator.capture-mode";
document.querySelector("#current-year").textContent = String(new Date().getFullYear());
document.querySelectorAll("input[name='capture-mode']").forEach(input => {
  input.checked = input.value === (localStorage.getItem(key) || "pda");
  input.addEventListener("change", () => {
    if (!input.checked) return;
    localStorage.setItem(key, input.value);
    document.querySelector("#settings-status").textContent = `Método guardado: ${input.value === "pda" ? "lector PDA" : "cámara"}.`;
  });
});
document.querySelector("#logout-button").addEventListener("click", event => {
  event.currentTarget.disabled = true;
  void logout(document.querySelector("#settings-status"));
});
