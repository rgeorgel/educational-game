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
          h("p", { class: "subject-xp" }, `⚡ ${s.xp} XP`),
          h("span", { class: "btn btn-primary" }, "Jogar"),
        ),
      ),
    ),
    h(
      "div",
      { class: "totals" },
      h("div", { class: "tile" }, h("strong", {}, data.totals.problemsSolved), h("span", {}, "problemas resolvidos")),
      h("div", { class: "tile" }, h("strong", {}, data.totals.stars), h("span", {}, "estrelas")),
      h("a", { class: "tile tile-link", href: "#/progresso" }, h("strong", {}, "📈"), h("span", {}, "Ver meu progresso")),
    ),
  );
}
