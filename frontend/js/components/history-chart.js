// Gráfico de colunas (SVG) com os problemas resolvidos por dia.
// Uma série só: o título nomeia o que está plotado (sem legenda). Tooltip por coluna
// no hover e no foco do teclado; a tabela ao lado traz os mesmos números.
import { h } from "../dom.js";
import { locale, t } from "../i18n.js";

const NS = "http://www.w3.org/2000/svg";
const svg = (tag, attrs = {}) => {
  const el = document.createElementNS(NS, tag);
  for (const [k, v] of Object.entries(attrs)) el.setAttribute(k, String(v));
  return el;
};

const weekday = new Intl.DateTimeFormat(locale(), { weekday: "short" });
const dayMonth = new Intl.DateTimeFormat(locale(), { day: "numeric", month: "short" });
const parse = (iso) => new Date(`${iso}T12:00:00`);

/** Topo "redondo" do eixo: 4, 5, 10, 20, 25, 50… */
function niceMax(max) {
  if (max <= 4) return 4;
  const pow = 10 ** Math.floor(Math.log10(max));
  for (const step of [1, 2, 2.5, 5, 10]) if (step * pow >= max) return step * pow;
  return 10 * pow;
}

/** Caminho de coluna com cantos de 4px só no topo (base reta). */
function columnPath(x, y, w, hgt, r = 4) {
  const rr = Math.min(r, w / 2, hgt);
  return `M ${x} ${y + hgt} V ${y + rr} Q ${x} ${y} ${x + rr} ${y} H ${x + w - rr} Q ${x + w} ${y} ${x + w} ${y + rr} V ${y + hgt} Z`;
}

export function historyChart(days) {
  const W = 640;
  const H = 220;
  const pad = { top: 20, right: 8, bottom: 30, left: 32 };
  const innerW = W - pad.left - pad.right;
  const innerH = H - pad.top - pad.bottom;
  const max = niceMax(Math.max(...days.map((d) => d.problems), 1));
  const slot = innerW / days.length;
  const barW = Math.min(24, slot - 6);
  const y = (v) => pad.top + innerH - (v / max) * innerH;

  const root = svg("svg", { viewBox: `0 0 ${W} ${H}`, class: "history-chart", role: "img", "aria-label": t("Problemas resolvidos por dia") });

  // Grade recessiva: 3 linhas finas + base.
  for (const t of [0, max / 2, max]) {
    root.append(svg("line", { x1: pad.left, x2: W - pad.right, y1: y(t), y2: y(t), class: t === 0 ? "axis" : "grid" }));
    const label = svg("text", { x: pad.left - 6, y: y(t) + 4, "text-anchor": "end", class: "tick" });
    label.textContent = String(Math.round(t));
    root.append(label);
  }

  const tooltip = h("div", { class: "chart-tooltip", role: "status", hidden: true });
  const wrap = h("div", { class: "chart-wrap" }, root, tooltip);
  const best = days.reduce((a, d) => (d.problems > a.problems ? d : a), days[0]);

  days.forEach((d, i) => {
    const cx = pad.left + slot * i + slot / 2;
    const date = parse(d.date);
    const isToday = i === days.length - 1;

    if (d.problems > 0) {
      const top = y(d.problems);
      root.append(svg("path", { d: columnPath(cx - barW / 2, top, barW, pad.top + innerH - top), class: "bar" }));
    }

    // Rótulos seletivos: só a coluna de hoje e o melhor dia.
    if (d.problems > 0 && (isToday || d === best)) {
      const v = svg("text", { x: cx, y: y(d.problems) - 6, "text-anchor": "middle", class: "value" });
      v.textContent = String(d.problems);
      root.append(v);
    }

    // Eixo x: dia da semana, a cada 2 dias para não amontoar.
    if (i % 2 === (days.length - 1) % 2) {
      const t = svg("text", { x: cx, y: H - 10, "text-anchor": "middle", class: "tick" });
      t.textContent = isToday ? "hoje" : weekday.format(date).replace(".", "");
      root.append(t);
    }

    // Alvo de hover/foco maior que a coluna.
    const hit = svg("rect", { x: pad.left + slot * i, y: pad.top, width: slot, height: innerH, class: "hit", tabindex: 0 });
    const text = t("{dia}: {p} problemas, {c} certos, {xp} XP", { dia: isToday ? t("Hoje") : dayMonth.format(date), p: d.problems, c: d.correct, xp: d.xp });
    hit.setAttribute("aria-label", text);
    const show = () => {
      tooltip.replaceChildren(
        h("strong", {}, t("{p} problemas", { p: d.problems })),
        h("span", {}, t("{c} certos · {xp} XP", { c: d.correct, xp: d.xp })),
        h("span", { class: "muted" }, isToday ? t("Hoje") : `${weekday.format(date)}, ${dayMonth.format(date)}`),
      );
      tooltip.hidden = false;
      const pct = (cx / W) * 100;
      tooltip.style.setProperty("left", `${pct}%`);
      // Nas pontas, alinha o tooltip para dentro do gráfico em vez de centralizar.
      tooltip.classList.toggle("align-end", pct > 80);
      tooltip.classList.toggle("align-start", pct < 20);
      root.querySelectorAll(".hit.active").forEach((n) => n.classList.remove("active"));
      hit.classList.add("active");
    };
    const hide = () => {
      tooltip.hidden = true;
      hit.classList.remove("active");
    };
    hit.addEventListener("pointerenter", show);
    hit.addEventListener("pointerleave", hide);
    hit.addEventListener("focus", show);
    hit.addEventListener("blur", hide);
    root.append(hit);
  });

  const table = h(
    "details",
    { class: "chart-table" },
    h("summary", {}, t("Ver como tabela")),
    h(
      "table",
      {},
      h("thead", {}, h("tr", {}, h("th", { scope: "col" }, t("Dia")), h("th", { scope: "col" }, t("Problemas")), h("th", { scope: "col" }, t("Certos")), h("th", { scope: "col" }, "XP"))),
      h(
        "tbody",
        {},
        days.map((d) => h("tr", {}, h("th", { scope: "row" }, dayMonth.format(parse(d.date))), h("td", {}, d.problems), h("td", {}, d.correct), h("td", {}, d.xp))),
      ),
    ),
  );

  return h("div", { class: "history" }, wrap, table);
}
