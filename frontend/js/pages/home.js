import { api } from "../api.js";
import { h } from "../dom.js";
import { store } from "../store.js";
import { avatarView, levelProgress, startAndPlay } from "./shared.js";

export async function homePage() {
  const profile = store.profile;
  const [data, daily] = await Promise.all([api.progress(profile.id), api.daily(profile.id)]);
  const error = h("p", { class: "form-error", role: "alert", hidden: true });

  const dailyCards = daily.map((d) => {
    if (d.done) {
      return h(
        "div",
        { class: "daily-item done" },
        h("span", { class: "daily-icon", "aria-hidden": "true" }, d.icon),
        h("span", {}, h("strong", {}, d.name), h("span", { class: "muted small" }, d.correct ? " · ✅ vencido hoje" : " · feito hoje")),
      );
    }
    const btn = h("button", { type: "button", class: "btn btn-primary btn-small" }, "Jogar");
    btn.addEventListener("click", () => startAndPlay(() => api.startDaily(profile.id, d.slug), btn, error));
    return h(
      "div",
      { class: "daily-item" },
      h("span", { class: "daily-icon", "aria-hidden": "true" }, d.icon),
      h("span", {}, h("strong", {}, d.name), h("span", { class: "muted small" }, " · 1 problema, XP em dobro")),
      btn,
    );
  });

  return h(
    "section",
    { class: "page" },
    h("div", { class: "greeting" }, h("a", { href: "#/avatar", title: "Personalizar avatar" }, avatarView(profile, "lg")), h("h1", {}, `Olá, ${profile.nickname}!`)),
    store.state.me?.isGuest
      ? h(
          "div",
          { class: "card guest-banner" },
          h("p", {}, h("strong", {}, "Você está jogando sem conta. "), "O progresso fica só neste aparelho."),
          h("a", { href: "#/salvar", class: "btn btn-primary btn-small" }, "💾 Salvar progresso"),
        )
      : null,
    h("section", { class: "card daily" }, h("h2", {}, "📆 Desafio do dia"), h("div", { class: "daily-list" }, dailyCards), error),
    h("p", { class: "muted" }, "Escolha uma trilha para continuar a expedição."),
    h(
      "div",
      { class: "subject-grid stagger" },
      data.subjects.map((s) =>
        h(
          "a",
          { class: "card subject-card", href: `#/mapa/${s.slug}`, dataset: { subject: s.slug } },
          h("div", { class: "subject-head" }, h("span", { class: "subject-icon", "aria-hidden": "true" }, s.icon), h("h2", {}, s.name)),
          levelProgress(s),
          s.guardianAvailable ? h("p", { class: "callout" }, "🛡️ O Desafio do Guardião está esperando por você!") : null,
          s.placementAvailable ? h("p", { class: "callout" }, "🧭 Faça o teste de nivelamento para começar no nível certo.") : null,
          s.needsReviewSkills > 0 ? h("p", { class: "callout" }, `🧩 ${s.needsReviewSkills} ${s.needsReviewSkills === 1 ? "habilidade pede" : "habilidades pedem"} revisão.`) : null,
          h("p", { class: "subject-xp" }, `⚡ ${s.xp} XP`),
          h("span", { class: "btn btn-primary" }, "Jogar"),
        ),
      ),
    ),
    h(
      "div",
      { class: "totals stagger" },
      h("div", { class: "tile" }, h("strong", {}, `🔥 ${data.totals.streak}`), h("span", {}, data.totals.streak === 1 ? "dia de prática" : "dias de prática")),
      h("div", { class: "tile" }, h("strong", {}, data.totals.problemsSolved), h("span", {}, "problemas resolvidos")),
      h("div", { class: "tile" }, h("strong", {}, `🏅 ${data.totals.achievements}`), h("span", {}, "medalhas")),
      h("a", { class: "tile tile-link", href: "#/progresso" }, h("strong", {}, "📈"), h("span", {}, "Ver meu progresso")),
    ),
  );
}
