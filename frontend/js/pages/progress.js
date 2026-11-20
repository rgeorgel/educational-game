import { api } from "../api.js";
import { h } from "../dom.js";
import { store } from "../store.js";
import { levelProgress, statusChip } from "./shared.js";

export async function progressPage() {
  const data = await api.progress(store.profile.id);
  const t = data.totals;
  const accuracy = t.problemsSolved ? Math.round((t.correct / t.problemsSolved) * 100) : 0;

  return h(
    "section",
    { class: "page" },
    h("h1", {}, `Progresso de ${data.nickname} `, h("span", { "aria-hidden": "true" }, data.avatar)),
    h(
      "div",
      { class: "totals" },
      h("div", { class: "tile" }, h("strong", {}, `⚡ ${t.xp}`), h("span", {}, "XP")),
      h("div", { class: "tile" }, h("strong", {}, t.problemsSolved), h("span", {}, "problemas")),
      h("div", { class: "tile" }, h("strong", {}, `${accuracy}%`), h("span", {}, "de acertos")),
      h("div", { class: "tile" }, h("strong", {}, `★ ${t.stars}`), h("span", {}, "estrelas")),
    ),
    data.subjects.map((s) =>
      h(
        "section",
        { class: "card subject-progress" },
        h("h2", {}, `${s.icon} ${s.name}`),
        levelProgress(s),
        h(
          "ul",
          { class: "skill-list" },
          s.skills.map((k) => {
            // Avanço da habilidade rumo ao domínio (rating 300 → 1000 no nível Iniciante).
            const pct = k.rating === null ? 0 : Math.max(0, Math.min(100, Math.round(((k.rating - 300) / 700) * 100)));
            const acc = k.attempts ? Math.round((k.correct / k.attempts) * 100) : null;
            return h(
              "li",
              { class: "skill-row" },
              h("div", { class: "skill-head" }, h("strong", {}, k.name), statusChip(k.status)),
              h("p", { class: "muted small" }, k.description),
              h("progress-bar", { value: String(pct), label: `${k.name}: ${pct}% do domínio` }),
              h("p", { class: "muted small" }, k.attempts ? `${k.attempts} problemas · ${acc}% de acertos` : "Ainda não praticada"),
            );
          }),
        ),
      ),
    ),
  );
}
