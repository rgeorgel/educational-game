import { api } from "../api.js";
import { errorMessage, h } from "../dom.js";
import { navigate } from "../router.js";
import { store } from "../store.js";
import { levelProgress } from "./shared.js";

const TITLES = ["Continue praticando!", "Boa! Etapa concluída!", "Muito bem!", "Perfeito!"];

export async function summaryPage({ sessionId }) {
  const s = await api.summary(sessionId);
  const error = h("p", { class: "form-error", role: "alert", hidden: true });

  async function play(stageId, button) {
    button.disabled = true;
    try {
      const session = await api.startSession(store.profile.id, stageId);
      navigate(`/jogar/${session.sessionId}`);
    } catch (err) {
      error.textContent = errorMessage(err);
      error.hidden = false;
      button.disabled = false;
    }
  }

  const again = h("button", { type: "button", class: "btn" }, "🔁 Jogar de novo");
  again.addEventListener("click", () => play(s.stage.id, again));

  let nextBtn = null;
  if (s.nextStage && s.nextStageUnlocked) {
    nextBtn = h("button", { type: "button", class: "btn btn-primary" }, `Próxima: ${s.nextStage.name} →`);
    nextBtn.addEventListener("click", () => play(s.nextStage.id, nextBtn));
  }

  const message =
    s.stars === 0
      ? "Você precisa de pelo menos 60% de acertos para ganhar uma estrela e liberar a próxima etapa. Tente de novo — cada tentativa ajuda!"
      : s.stars < 3
        ? "Jogue de novo quando quiser para conseguir as 3 estrelas."
        : "Você acertou tudo!";

  return h(
    "section",
    { class: "page-narrow summary" },
    h(
      "div",
      { class: "card center" },
      h("p", { class: "muted" }, s.stage.name),
      h("h1", {}, TITLES[s.stars]),
      h("star-rating", { stars: String(s.stars), class: "stars-big" }),
      h("p", { class: "summary-score" }, h("strong", {}, `${s.correct} de ${s.total}`), " certas"),
      h("p", { class: "xp" }, `+${s.xpEarned} XP`),
      h("p", { class: "muted" }, message),
      error,
      h("div", { class: "actions" }, nextBtn, again, h("a", { href: `#/mapa/${s.subjectSlug}`, class: "btn btn-ghost" }, "Voltar ao mapa")),
    ),
    h("div", { class: "card" }, h("h2", {}, s.progress.name), levelProgress(s.progress)),
  );
}
