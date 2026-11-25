// Fichas para imprimir (planos Família e Professor Pro): problemas gerados na hora + gabarito.
import { api } from "../api.js";
import { errorMessage, h } from "../dom.js";
import { lockedCard } from "./plans.js";
import { t } from "../i18n.js";

function problemBody(p) {
  const parts = [h("p", { class: "ws-prompt" }, p.prompt)];
  if (p.visual) parts.push(h("p", { class: "ws-visual" }, p.visual));
  if (p.code) parts.push(h("pre", { class: "ws-code" }, p.code));
  if (p.sequence) parts.push(h("p", { class: "ws-sequence" }, p.sequence.map((x) => x ?? "____").join("  ,  ")));
  if (p.grid) {
    const size = p.grid.size;
    const rows = [];
    for (let r = 0; r < size; r++) rows.push(h("tr", {}, p.grid.cells.slice(r * size, r * size + size).map((c) => h("td", {}, c ?? ""))));
    parts.push(h("table", { class: `ws-grid ws-grid-${size}` }, h("tbody", {}, rows)));
    if (p.grid.target) parts.push(h("p", { class: "small" }, t("Cada linha, coluna e diagonal soma {n}.", { n: p.grid.target })));
  }
  if (p.type === "ordenar" && p.choices) parts.push(h("p", { class: "ws-choices" }, t("Itens: {lista}", { lista: p.choices.join("  ·  ") })), h("p", { class: "ws-line" }, t("Ordem: ________________________________")));
  else if (p.choices) parts.push(h("ul", { class: "ws-options" }, p.choices.map((c) => h("li", {}, `◯ ${c}`))));
  else if (p.type !== "grade") parts.push(h("p", { class: "ws-line" }, t("Resposta: ______________")));
  return parts;
}

function sheet(ws) {
  return h(
    "div",
    { class: "print-area worksheet" },
    h(
      "header",
      { class: "ws-head" },
      h("div", {}, h("strong", {}, "🧭 Educa Game"), h("span", {}, ` · ${ws.skill.subjectName} · ${ws.skill.levelName}`)),
      h("p", { class: "ws-fields" }, t("Nome: ______________________________   Data: ____/____/______")),
      h("h2", {}, ws.skill.name),
      h("p", { class: "small" }, ws.description, ws.skill.bncc.length ? ` · BNCC: ${ws.skill.bncc.map((b) => b.code).join(", ")}` : ""),
    ),
    h("ol", { class: "ws-problems" }, ws.problems.map((p) => h("li", {}, problemBody(p)))),
    h(
      "section",
      { class: "ws-answers" },
      h("h2", {}, t("Gabarito — {nome}", { nome: ws.skill.name })),
      h("ol", {}, ws.answers.map((a) => h("li", {}, h("strong", {}, a.answer), h("span", { class: "small" }, ` — ${a.explanation}`)))),
      h("p", { class: "small muted" }, t("Ficha nº {n} (gere de novo para outros problemas)", { n: ws.seed })),
    ),
  );
}

export async function worksheetsPage() {
  const subjects = await api.curriculum();
  const output = h("div", {});
  const error = h("div", { role: "alert" });

  const skillSelect = h(
    "select",
    { name: "skill", required: true, "aria-label": t("Habilidade") },
    subjects.map((s) => s.levels.map((l) => h("optgroup", { label: `${s.icon} ${s.name} · ${l.icon} ${l.name}` }, l.skills.map((k) => h("option", { value: `${s.slug}|${k.slug}` }, k.name))))),
  );
  const count = h("select", { name: "count", "aria-label": t("Quantidade") }, [6, 10, 16, 20].map((n) => h("option", { value: String(n), selected: n === 10 }, t("{p} problemas", { p: n }))));
  const difficulty = h("select", { name: "difficulty", "aria-label": t("Dificuldade") }, [["facil", t("Fácil")], ["medio", t("Média")], ["dificil", t("Difícil")]].map(([v, l]) => h("option", { value: v, selected: v === "medio" }, l)));
  const generate = h("button", { type: "submit", class: "btn btn-primary" }, t("Gerar ficha"));
  const print = h("button", { type: "button", class: "btn", hidden: true }, t("🖨️ Imprimir"));
  print.addEventListener("click", () => window.print());

  const form = h(
    "form",
    {
      class: "card stack no-print",
      onSubmit: async (e) => {
        e.preventDefault();
        generate.disabled = true;
        error.replaceChildren();
        try {
          const [subject, skill] = skillSelect.value.split("|");
          const ws = await api.worksheet({ subject, skill, count: count.value, difficulty: difficulty.value });
          output.replaceChildren(sheet(ws));
          print.hidden = false;
        } catch (err) {
          error.append(lockedCard(err) ?? h("p", { class: "form-error" }, errorMessage(err)));
        } finally {
          generate.disabled = false;
        }
      },
    },
    h("label", { class: "stack" }, h("span", { class: "small muted" }, t("Habilidade")), skillSelect),
    h("div", { class: "assignment-form-row" }, h("label", { class: "stack" }, h("span", { class: "small muted" }, t("Quantidade")), count), h("label", { class: "stack" }, h("span", { class: "small muted" }, t("Dificuldade")), difficulty)),
    h("div", { class: "actions-left" }, generate, print),
  );

  return h(
    "section",
    { class: "page" },
    h("div", { class: "no-print" }, h("h1", {}, t("🖨️ Fichas para imprimir")), h("p", { class: "muted" }, t("Problemas novos a cada ficha, com gabarito na última página. Ótimo para treinar no papel, na sala ou em casa."))),
    form,
    error,
    output,
  );
}
