import { api } from "../api.js";
import { historyChart } from "../components/history-chart.js";
import { h } from "../dom.js";
import { store } from "../store.js";
import { levelProgress, statusChip } from "./shared.js";
import { medal } from "./summary.js";
import { t, tn } from "../i18n.js";

export async function progressPage() {
  const id = store.profile.id;
  const [data, history, medals] = await Promise.all([api.progress(id), api.history(id, 14), api.achievements(id)]);
  const totals = data.totals;
  const accuracy = totals.problemsSolved ? Math.round((totals.correct / totals.problemsSolved) * 100) : 0;
  const earned = medals.filter((m) => m.earnedAt).length;

  return h(
    "section",
    { class: "page" },
    h("h1", {}, `${t("Progresso de {nome}", { nome: data.nickname })} `, h("span", { "aria-hidden": "true" }, data.avatar)),
    h(
      "div",
      { class: "totals stagger" },
      h("div", { class: "tile" }, h("strong", {}, `🔥 ${history.streak}`), h("span", {}, history.streak === 1 ? t("dia de prática") : t("dias de prática"))),
      h("div", { class: "tile" }, h("strong", {}, `⚡ ${totals.xp}`), h("span", {}, "XP")),
      h("div", { class: "tile" }, h("strong", {}, totals.problemsSolved), h("span", {}, t("problemas"))),
      h("div", { class: "tile" }, h("strong", {}, `${accuracy}%`), h("span", {}, t("de acertos"))),
      h("div", { class: "tile" }, h("strong", {}, `★ ${totals.stars}`), h("span", {}, t("estrelas"))),
    ),
    h(
      "section",
      { class: "card" },
      h("h2", {}, t("Problemas resolvidos nos últimos 14 dias")),
      h("p", { class: "muted small" }, tn(history.bestStreak, "Melhor sequência: {n} dia. Pode faltar 1 dia sem perder a sequência.", "Melhor sequência: {n} dias. Pode faltar 1 dia sem perder a sequência.")),
      historyChart(history.days),
    ),
    h(
      "section",
      { class: "card" },
      h("h2", {}, t("Medalhas ({a} de {b})", { a: earned, b: medals.length })),
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
              h("progress-bar", { value: String(pct), label: t("{nome}: {p}% do domínio", { nome: k.name, p: pct }), level: s.level.slug }),
              h("p", { class: "muted small" }, k.attempts ? t("{n} problemas · {p}% de acertos", { n: k.attempts, p: acc }) : t("Ainda não praticada")),
            );
          }),
        ),
      ),
    ),
  );
}
