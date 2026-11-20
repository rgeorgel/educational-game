import { api } from "../api.js";
import { h } from "../dom.js";
import { store } from "../store.js";
import { levelProgress } from "./shared.js";

export async function homePage() {
  const profile = store.profile;
  const data = await api.progress(profile.id);

  return h(
    "section",
    { class: "page" },
    h("h1", {}, `Olá, ${profile.nickname}! `, h("span", { "aria-hidden": "true" }, profile.avatar)),
    h("p", { class: "muted" }, "Escolha uma trilha para continuar a expedição."),
    h(
      "div",
      { class: "subject-grid" },
      data.subjects.map((s) =>
        h(
          "a",
          { class: "card subject-card", href: `#/mapa/${s.slug}` },
          h("div", { class: "subject-head" }, h("span", { class: "subject-icon", "aria-hidden": "true" }, s.icon), h("h2", {}, s.name)),
          levelProgress(s),
          s.guardianAvailable ? h("p", { class: "callout" }, "🛡️ O Desafio do Guardião está esperando por você!") : null,
          s.placementAvailable ? h("p", { class: "callout" }, "🧭 Faça o teste de nivelamento para começar no nível certo.") : null,
          h("p", { class: "subject-xp" }, `⚡ ${s.xp} XP`),
          h("span", { class: "btn btn-primary" }, "Jogar"),
        ),
      ),
    ),
    h(
      "div",
      { class: "totals" },
      h("div", { class: "tile" }, h("strong", {}, `🔥 ${data.totals.streak}`), h("span", {}, data.totals.streak === 1 ? "dia de prática" : "dias de prática")),
      h("div", { class: "tile" }, h("strong", {}, data.totals.problemsSolved), h("span", {}, "problemas resolvidos")),
      h("div", { class: "tile" }, h("strong", {}, `🏅 ${data.totals.achievements}`), h("span", {}, "medalhas")),
      h("a", { class: "tile tile-link", href: "#/progresso" }, h("strong", {}, "📈"), h("span", {}, "Ver meu progresso")),
    ),
  );
}
