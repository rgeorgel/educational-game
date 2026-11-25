import { t } from "../i18n.js";
// Figuras de frações em SVG: "barra" (retângulo dividido) e "circulo" (pizza).

const NS = "http://www.w3.org/2000/svg";

function svgEl(tag, attrs = {}) {
  const el = document.createElementNS(NS, tag);
  for (const [k, v] of Object.entries(attrs)) el.setAttribute(k, String(v));
  return el;
}

/** Caminho de uma fatia de pizza entre os ângulos a0 e a1 (radianos, 0 = topo, sentido horário). */
function slice(cx, cy, r, a0, a1) {
  const x0 = cx + r * Math.sin(a0);
  const y0 = cy - r * Math.cos(a0);
  const x1 = cx + r * Math.sin(a1);
  const y1 = cy - r * Math.cos(a1);
  const large = a1 - a0 > Math.PI ? 1 : 0;
  return `M ${cx} ${cy} L ${x0.toFixed(2)} ${y0.toFixed(2)} A ${r} ${r} 0 ${large} 1 ${x1.toFixed(2)} ${y1.toFixed(2)} Z`;
}

/**
 * Monta a figura. Partes interativas são focáveis e alternam com clique, Enter ou Espaço.
 * Devolve { node, update() } — update() recolore conforme isFilled(i).
 */
export function fractionFigure(figure, { interactive, isFilled, onToggle }) {
  const { shape, parts } = figure;
  const isCircle = shape === "circulo";
  const width = isCircle ? 220 : 320;
  const height = isCircle ? 220 : 90;
  const svg = svgEl("svg", {
    viewBox: `0 0 ${width} ${height}`,
    class: `fraction-figure ${isCircle ? "is-circle" : "is-bar"}${interactive ? " is-interactive" : ""}`,
    role: interactive ? "group" : "img",
    "aria-label": interactive ? t("Figura com {n} partes iguais", { n: parts }) : t("Figura com {n} partes, {p} pintadas", { n: parts, p: figure.filled }),
  });

  const pieces = [];
  for (let i = 0; i < parts; i++) {
    let piece;
    if (isCircle) {
      const step = (2 * Math.PI) / parts;
      piece = parts === 1 ? svgEl("circle", { cx: 110, cy: 110, r: 100 }) : svgEl("path", { d: slice(110, 110, 100, i * step, (i + 1) * step) });
    } else {
      const gap = 2;
      const w = (width - gap * (parts - 1)) / parts;
      piece = svgEl("rect", { x: (i * (w + gap)).toFixed(2), y: 5, width: w.toFixed(2), height: height - 10, rx: 4 });
    }
    piece.classList.add("fraction-part");
    if (interactive) {
      piece.setAttribute("tabindex", "0");
      piece.setAttribute("role", "checkbox");
      const toggle = () => onToggle?.(i);
      piece.addEventListener("click", toggle);
      piece.addEventListener("keydown", (e) => {
        if (e.key === "Enter" || e.key === " ") {
          e.preventDefault();
          e.stopPropagation();
          toggle();
        }
      });
    }
    pieces.push(piece);
    svg.append(piece);
  }

  function update() {
    pieces.forEach((p, i) => {
      const on = isFilled(i);
      p.classList.toggle("filled", on);
      if (interactive) {
        p.setAttribute("aria-checked", String(on));
        p.setAttribute("aria-label", `${t("Parte {n}", { n: i + 1 })}${on ? `, ${t("pintada")}` : ""}`);
      }
    });
  }
  update();

  return { node: svg, update };
}
