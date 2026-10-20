import { logout } from "/shared/js/session.js";
import "/shared/js/navigation.js";

const key = "projectaw.operator.capture-mode";
const additionalInfoKeys = {
  lot: "projectaw.operator.lot-enabled",
  serial: "projectaw.operator.serial-enabled",
  expiry: "projectaw.operator.expiry-enabled"
};
document.querySelector("#current-year").textContent = String(new Date().getFullYear());
const legacyAdditionalInfo = localStorage.getItem("projectaw.operator.additional-info-enabled") === "true";
const settingsStatus = document.querySelector("#settings-status");
for (const [name, storageKey] of Object.entries(additionalInfoKeys)) {
  const input = document.querySelector(`#${name}-enabled`);
  const stored = localStorage.getItem(storageKey);
  input.checked = stored === null ? legacyAdditionalInfo : stored === "true";
  input.addEventListener("change", () => {
    localStorage.setItem(storageKey, String(input.checked));
    const label = name === "expiry" ? "Fecha de vencimiento" : name === "lot" ? "Lote" : "Serie";
    settingsStatus.textContent = `${label} ${input.checked ? "activado" : "desactivado"}.`;
  });
}
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
