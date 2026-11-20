// Gráfico de colunas em SVG (uma série, ou empilhado com até 2 séries), sem bibliotecas.
// Regras: colunas finas (<= 24px) com ponta arredondada de 4px e base reta, grade em linha fina,
// dica ao passar o mouse/teclado em cada coluna e a mesma informação disponível como tabela.
import { append, h, num } from "./dom.js";

const PAD = { top: 12, right: 8, bottom: 26, left: 40 };

function niceMax(value) {
  if (value <= 4) return 4;
  const pow = 10 ** Math.floor(Math.log10(value));
  for (const step of [1, 2, 2.5, 5, 10]) {
    const max = Math.ceil(value / (step * pow)) * step * pow;
    if (max / (step * pow) <= 5) return max;
  }
  return value;
}

/** Caminho de uma coluna com o topo arredondado (raio 4) e a base reta. */
function columnPath(x, y, w, hgt, r = 4) {
  if (hgt <= 0) return "";
  const rr = Math.min(r, w / 2, hgt);
  return `M${x},${y + hgt}V${y + rr}Q${x},${y} ${x + rr},${y}H${x + w - rr}Q${x + w},${y} ${x + w},${y + rr}V${y + hgt}Z`;
}

/**
 * @param {object} opts
 * @param {Array<{label: string, values: number[], tip?: string[]}>} opts.data  uma entrada por coluna
 * @param {Array<{name: string, cls: string}>} opts.series  séries (cls = classe CSS da cor)
 * @param {(d) => string} opts.xLabel  rótulo curto do eixo X
 * @param {number} [opts.labelEvery]  mostra 1 rótulo a cada N colunas (padrão automático)
 * @param {string} opts.title  nome do gráfico (acessibilidade e cabeçalho da tabela)
 * @param {string} [opts.unit]  unidade no rodapé da dica (ex.: "respostas")
 */
export function columnChart({ data, series, xLabel, title, labelEvery, height = 220 }) {
  const tip = h("div", { class: "chart-tip", role: "status", hidden: true });
  const figure = h("div", { class: "chart-plot" });
  // Uma série: o título do card já diz o que é (sem legenda). Duas ou mais: legenda sempre.
  const wrap = h("div", { class: "chart" }, series.length > 1 ? legend(series) : null, figure, tip, tableView({ data, series, title }));

  const total = (d) => d.values.reduce((a, b) => a + b, 0);
  const max = niceMax(Math.max(0, ...data.map(total)));

  const draw = () => {
    const width = Math.max(280, figure.clientWidth || 600);
    const plotW = width - PAD.left - PAD.right;
    const plotH = height - PAD.top - PAD.bottom;
    const band = plotW / data.length;
    const barW = Math.max(2, Math.min(24, band - 2, band * 0.72));
    const y = (v) => PAD.top + plotH - (v / max) * plotH;
    const every = labelEvery ?? Math.max(1, Math.ceil(data.length / Math.floor(plotW / 56)));

    const svg = h("svg", { viewBox: `0 0 ${width} ${height}`, width, height, role: "group", "aria-label": title, class: "chart-svg" });
    const grid = h("g", { class: "grid" });
    for (let i = 0; i <= 4; i++) {
      const v = (max / 4) * i;
      const yy = Math.round(y(v)) + 0.5;
      grid.append(
        h("line", { x1: PAD.left, x2: width - PAD.right, y1: yy, y2: yy, class: i === 0 ? "baseline" : "gridline" }),
        h("text", { x: PAD.left - 6, y: yy + 4, class: "tick", "text-anchor": "end" }, num(Math.round(v))),
      );
    }
    svg.append(grid);

    const marks = h("g", { class: "marks" });
    data.forEach((d, i) => {
      const cx = PAD.left + band * i + band / 2;
      const x = cx - barW / 2;
      const col = h("g", { class: "col", tabindex: 0, role: "listitem", "aria-label": `${d.label}: ${describe(d, series)}` });
      // Empilha de baixo para cima, com 2px de "ar" entre os segmentos.
      let base = 0;
      const nonZero = d.values.map((v, k) => [v, k]).filter(([v]) => v > 0);
      nonZero.forEach(([v, k], idx) => {
        const top = base + v;
        const isTop = idx === nonZero.length - 1;
        const y0 = y(base) - (idx > 0 ? 2 : 0);
        const y1 = y(top);
        const hgt = Math.max(isTop ? 1 : 0, y0 - y1);
        col.append(
          isTop
            ? h("path", { d: columnPath(x, y0 - hgt, barW, hgt), class: `bar ${series[k].cls}` })
            : h("rect", { x, y: y0 - hgt, width: barW, height: hgt, class: `bar ${series[k].cls}` }),
        );
        base = top;
      });
      // Área de toque: a faixa inteira da coluna (maior que a marca).
      col.append(h("rect", { x: PAD.left + band * i, y: PAD.top, width: band, height: plotH, class: "hit" }));
      const show = () => showTip(d, col, cx);
      col.addEventListener("pointerenter", show);
      col.addEventListener("focus", show);
      col.addEventListener("pointerleave", hideTip);
      col.addEventListener("blur", hideTip);
      marks.append(col);
      if (i % every === 0) {
        svg.append(h("text", { x: cx, y: height - 8, class: "tick", "text-anchor": "middle" }, xLabel(d)));
      }
    });
    marks.setAttribute("role", "list");
    svg.append(marks);
    figure.replaceChildren(svg);
  };

  function showTip(d, col, cx) {
    wrap.querySelectorAll(".col.active").forEach((c) => c.classList.remove("active"));
    col.classList.add("active");
    wrap.classList.add("hovering");
    tip.replaceChildren();
    append(tip, [
      h("strong", {}, d.label),
      series.map((s, k) => h("div", { class: "tip-row" }, series.length > 1 ? h("span", { class: `swatch ${s.cls}` }) : null, h("span", {}, s.name), h("b", {}, num(d.values[k])))),
      (d.tip ?? []).map((line) => h("div", { class: "tip-extra" }, line)),
    ]);
    tip.hidden = false;
    const left = Math.min(Math.max(cx, 80), figure.clientWidth - 80);
    tip.style.setProperty("left", `${left}px`);
  }
  function hideTip() {
    tip.hidden = true;
    wrap.classList.remove("hovering");
    wrap.querySelectorAll(".col.active").forEach((c) => c.classList.remove("active"));
  }

  new ResizeObserver(() => draw()).observe(figure);
  requestAnimationFrame(draw);
  return wrap;
}

function describe(d, series) {
  return series.map((s, k) => `${s.name} ${num(d.values[k])}`).join(", ") + (d.tip?.length ? `. ${d.tip.join(". ")}` : "");
}

function legend(series) {
  return h("ul", { class: "legend" }, series.map((s) => h("li", {}, h("span", { class: `swatch ${s.cls}` }), s.name)));
}

function tableView({ data, series, title }) {
  return h(
    "details",
    { class: "table-view" },
    h("summary", {}, "Ver como tabela"),
    h(
      "div",
      { class: "table-scroll" },
      h(
        "table",
        {},
        h("caption", { class: "sr-only" }, title),
        h("thead", {}, h("tr", {}, h("th", {}, ""), series.map((s) => h("th", { class: "n" }, s.name)), data.some((d) => d.tip?.length) ? h("th", {}, "Detalhe") : null)),
        h(
          "tbody",
          {},
          data.map((d) =>
            h("tr", {}, h("th", { scope: "row" }, d.label), d.values.map((v) => h("td", { class: "n" }, num(v))), d.tip?.length ? h("td", {}, d.tip.join(" · ")) : data.some((x) => x.tip?.length) ? h("td", {}, "") : null),
          ),
        ),
      ),
    ),
  );
}

/** Barra horizontal fina para comparar valores numa tabela (ex.: % de acerto). */
export function meter(value, max = 100, label = "") {
  const m = h("span", { class: "meter", role: "img", "aria-label": label || `${Math.round(value)} de ${max}` }, h("span", { class: "meter-fill" }));
  m.firstChild.style.setProperty("width", `${Math.max(0, Math.min(100, (value / max) * 100))}%`);
  return m;
}
