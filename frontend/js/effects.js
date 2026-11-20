// Efeitos visuais leves (sem bibliotecas): confete, texto flutuante e contagem animada.
// Todos respeitam "Animações reduzidas" (preferência do aparelho ou ⚙️ Configurações).
import { getPrefs } from "./prefs.js";

const COLORS = ["#4361ee", "#8b5cf6", "#10b981", "#ffc233", "#ff8a3d", "#e5484d", "#0ea5c6"];

export function reducedMotion() {
  return getPrefs().motion === "reduzido" || window.matchMedia("(prefers-reduced-motion: reduce)").matches;
}

/** Estoura confete a partir de um ponto (padrão: centro-alto da tela). */
export function confetti({ x = window.innerWidth / 2, y = window.innerHeight / 3, count = 70, spread = 1 } = {}) {
  if (reducedMotion()) return;
  const layer = document.createElement("div");
  layer.className = "confetti-layer";
  layer.setAttribute("aria-hidden", "true");
  for (let i = 0; i < count; i++) {
    const piece = document.createElement("span");
    piece.className = "confetti-piece";
    const angle = Math.random() * Math.PI * 2;
    const distance = (120 + Math.random() * 260) * spread;
    piece.style.setProperty("left", `${x}px`);
    piece.style.setProperty("top", `${y}px`);
    piece.style.setProperty("background", COLORS[i % COLORS.length]);
    piece.style.setProperty("--dx", `${Math.cos(angle) * distance}px`);
    piece.style.setProperty("--dy", `${Math.sin(angle) * distance + 220}px`); // a gravidade puxa para baixo
    piece.style.setProperty("--rot", `${Math.round(Math.random() * 720 - 360)}deg`);
    piece.style.setProperty("--delay", `${Math.random() * 0.12}s`);
    if (i % 3 === 0) piece.style.setProperty("border-radius", "50%");
    layer.append(piece);
  }
  document.body.append(layer);
  setTimeout(() => layer.remove(), 1800);
}

/** Confete saindo de um elemento (ex.: o botão ou o card da resposta). */
export function confettiFrom(element, options = {}) {
  const rect = element.getBoundingClientRect();
  confetti({ x: rect.left + rect.width / 2, y: rect.top + rect.height / 2, ...options });
}

/** Texto que sobe e some a partir de um elemento (ex.: "+18 XP"). */
export function floatText(element, text) {
  if (reducedMotion()) return;
  const rect = element.getBoundingClientRect();
  const span = document.createElement("span");
  span.className = "float-text";
  span.textContent = text;
  span.setAttribute("aria-hidden", "true");
  span.style.setProperty("left", `${rect.left + rect.width / 2}px`);
  span.style.setProperty("top", `${rect.top + rect.height / 2}px`);
  document.body.append(span);
  setTimeout(() => span.remove(), 1200);
}

/** Conta de 0 até o valor (ex.: XP ganho). O texto final é sempre o valor exato. */
export function countUp(element, to, { prefix = "", suffix = "", duration = 900 } = {}) {
  if (reducedMotion() || to <= 0) {
    element.textContent = `${prefix}${to}${suffix}`;
    return;
  }
  const start = performance.now();
  const step = (now) => {
    const t = Math.min(1, (now - start) / duration);
    const eased = 1 - Math.pow(1 - t, 3);
    element.textContent = `${prefix}${Math.round(to * eased)}${suffix}`;
    if (t < 1) requestAnimationFrame(step);
  };
  requestAnimationFrame(step);
}
