import { api } from "../api.js";
import { historyChart } from "../components/history-chart.js";
import { h } from "../dom.js";
import { store } from "../store.js";
import { levelProgress, statusChip } from "./shared.js";
import { medal } from "./summary.js";

export async function progressPage() {
  const id = store.profile.id;
  const [data, history, medals] = await Promise.all([api.progress(id), api.history(id, 14), api.achievements(id)]);
  const t = data.totals;
  const accuracy = t.problemsSolved ? Math.round((t.correct / t.problemsSolved) * 100) : 0;
  const earned = medals.filter((m) => m.earnedAt).length;

  return h(
    "section",
    { class: "page" },
    h("h1", {}, `Progresso de ${data.nickname} `, h("span", { "aria-hidden": "true" }, data.avatar)),
    h(
      "div",
      { class: "totals stagger" },
      h("div", { class: "tile" }, h("strong", {}, `🔥 ${history.streak}`), h("span", {}, history.streak === 1 ? "dia de prática" : "dias de prática")),
      h("div", { class: "tile" }, h("strong", {}, `⚡ ${t.xp}`), h("span", {}, "XP")),
      h("div", { class: "tile" }, h("strong", {}, t.problemsSolved), h("span", {}, "problemas")),
      h("div", { class: "tile" }, h("strong", {}, `${accuracy}%`), h("span", {}, "de acertos")),
      h("div", { class: "tile" }, h("strong", {}, `★ ${t.stars}`), h("span", {}, "estrelas")),
    ),
    h(
      "section",
      { class: "card" },
      h("h2", {}, "Problemas resolvidos nos últimos 14 dias"),
      h("p", { class: "muted small" }, `Melhor sequência: ${history.bestStreak} ${history.bestStreak === 1 ? "dia" : "dias"}. Pode faltar 1 dia sem perder a sequência.`),
      historyChart(history.days),
    ),
    h(
      "section",
      { class: "card" },
      h("h2", {}, `Medalhas (${earned} de ${medals.length})`),
      h("ul", { class: "medal-grid stagger" }, medals.map(medal)),
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
            // Avanço da habilidade dentro do nível atual (do início da faixa até o domínio).
            const span = k.targetRating - k.startRating;
            const pct = k.rating === null ? 0 : Math.max(0, Math.min(100, Math.round(((k.rating - k.startRating) / span) * 100)));
            const acc = k.attempts ? Math.round((k.correct / k.attempts) * 100) : null;
            return h(
              "li",
              { class: "skill-row" },
              h("div", { class: "skill-head" }, h("strong", {}, k.name), statusChip(k.status)),
              h("p", { class: "muted small" }, k.description),
              h("progress-bar", { value: String(pct), label: `${k.name}: ${pct}% do domínio`, level: s.level.slug }),
              h("p", { class: "muted small" }, k.attempts ? `${k.attempts} problemas · ${acc}% de acertos` : "Ainda não praticada"),
            );
          }),
        ),
      ),
    ),
  );
}
