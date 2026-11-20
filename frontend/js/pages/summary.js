import { api } from "../api.js";
import { h } from "../dom.js";
import { store } from "../store.js";
import { levelBadge, levelProgress, startAndPlay } from "./shared.js";

const TITLES = ["Continue praticando!", "Boa! Etapa concluída!", "Muito bem!", "Perfeito!"];

export async function summaryPage({ sessionId }) {
  const s = await api.summary(sessionId);
  const error = h("p", { class: "form-error", role: "alert", hidden: true });
  const profileId = store.profile.id;
  const mapLink = h("a", { href: `#/mapa/${s.subjectSlug}`, class: "btn btn-ghost" }, "Voltar ao mapa");

  let main;
  if (s.mode === "guardiao") main = guardianResult(s, profileId, error, mapLink);
  else if (s.mode === "nivelamento") main = placementResult(s, mapLink);
  else if (s.mode === "diario") main = dailyResult(s);
  else if (s.mode === "revisao") main = reviewResult(s, profileId, error, mapLink);
  else main = stageResult(s, profileId, error, mapLink);

  // Momento bom para lembrar o convidado de salvar: acabou de conquistar algo.
  const guestNudge =
    store.state.me?.isGuest && (s.stars > 0 || s.achievements.length > 0 || s.guardianPassed || s.resultLevel)
      ? h(
          "div",
          { class: "card guest-banner" },
          h("p", {}, h("strong", {}, "Não perca essa conquista! "), "Salve o progresso com uma conta para guardar tudo e jogar em outros aparelhos."),
          h("a", { href: "#/salvar", class: "btn btn-primary btn-small" }, "💾 Salvar progresso"),
        )
      : null;

  return h(
    "section",
    { class: "page-narrow summary" },
    main,
    guestNudge,
    s.achievements.length
      ? h(
          "section",
          { class: "card medals-earned" },
          h("h2", {}, s.achievements.length === 1 ? "Nova medalha!" : "Novas medalhas!"),
          h("ul", { class: "medal-grid" }, s.achievements.map(medal)),
        )
      : null,
    h("div", { class: "card" }, h("h2", {}, s.progress.name), levelProgress(s.progress)),
  );
}

function stageResult(s, profileId, error, mapLink) {
  const again = h("button", { type: "button", class: "btn" }, "🔁 Jogar de novo");
  again.addEventListener("click", () => startAndPlay(() => api.startSession(profileId, s.stage.id), again, error));

  let nextBtn = null;
  if (s.nextStage && s.nextStageUnlocked) {
    nextBtn = h("button", { type: "button", class: "btn btn-primary" }, `Próxima: ${s.nextStage.name} →`);
    nextBtn.addEventListener("click", () => startAndPlay(() => api.startSession(profileId, s.nextStage.id), nextBtn, error));
  }

  const message =
    s.stars === 0
      ? "Você precisa de pelo menos 60% de acertos para ganhar uma estrela e liberar a próxima etapa. Tente de novo — cada tentativa ajuda!"
      : s.stars < 3
        ? "Jogue de novo quando quiser para conseguir as 3 estrelas."
        : "Você acertou tudo!";

  return h(
    "div",
    { class: "card center result" },
    h("p", { class: "muted" }, s.stage.name),
    h("h1", {}, TITLES[s.stars]),
    h("star-rating", { stars: String(s.stars), class: "stars-big" }),
    h("p", { class: "summary-score" }, h("strong", {}, `${s.correct} de ${s.total}`), " certas"),
    h("p", { class: "xp" }, `+${s.xpEarned} XP`),
    h("p", { class: "muted" }, message),
    error,
    h("div", { class: "actions" }, nextBtn, again, mapLink),
  );
}

function guardianResult(s, profileId, error, mapLink) {
  const passed = s.guardianPassed;
  let retry = null;
  if (!passed && s.progress.guardianAvailable) {
    retry = h("button", { type: "button", class: "btn" }, "Tentar de novo");
    retry.addEventListener("click", () => startAndPlay(() => api.startGuardian(profileId, s.subjectSlug), retry, error));
  }
  return h(
    "div",
    { class: `card center result${passed ? " celebrate" : ""}` },
    h("p", { class: "result-icon", "aria-hidden": "true" }, passed ? "🏆" : "🛡️"),
    h("h1", {}, passed ? "Você venceu o Guardião!" : "O Guardião resistiu desta vez"),
    h("p", { class: "summary-score" }, h("strong", {}, `${s.correct} de ${s.total}`), " certas (precisava de 9)"),
    passed && s.resultLevel ? h("p", {}, "Novo nível: ", levelBadge(s.resultLevel)) : null,
    h(
      "p",
      { class: "muted" },
      passed
        ? `Agora as regiões do nível ${s.resultLevel?.name ?? ""} estão abertas no mapa. Bora explorar!`
        : "Cada tentativa mostra o que ainda falta treinar. Pratique as etapas do nível e volte quando quiser.",
    ),
    h("p", { class: "xp" }, `+${s.xpEarned} XP`),
    error,
    h("div", { class: "actions" }, passed ? h("a", { href: `#/mapa/${s.subjectSlug}`, class: "btn btn-primary" }, "Ver o mapa") : mapLink, retry),
  );
}

function placementResult(s, mapLink) {
  return h(
    "div",
    { class: "card center result" },
    h("p", { class: "result-icon", "aria-hidden": "true" }, "🧭"),
    h("h1", {}, "Teste concluído!"),
    h("p", {}, "Você vai começar no nível ", s.resultLevel ? levelBadge(s.resultLevel) : "Iniciante", "."),
    h("p", { class: "summary-score" }, h("strong", {}, `${s.correct} de ${s.total}`), " certas"),
    h("p", { class: "muted" }, "As habilidades dos níveis anteriores contam como dominadas, mas você pode jogar essas etapas quando quiser."),
    h("div", { class: "actions" }, h("a", { href: `#/mapa/${s.subjectSlug}`, class: "btn btn-primary" }, "Ir para o mapa")),
  );
}

function dailyResult(s) {
  const ok = s.correct > 0;
  return h(
    "div",
    { class: `card center result${ok ? " celebrate" : ""}` },
    h("p", { class: "result-icon", "aria-hidden": "true" }, ok ? "🌟" : "📆"),
    h("h1", {}, ok ? "Desafio do dia vencido!" : "Esse foi difícil!"),
    h("p", { class: "xp" }, `+${s.xpEarned} XP`),
    h("p", { class: "muted" }, ok ? "Você ganhou XP em dobro. Amanhã tem outro desafio!" : "Veja a explicação e tente o desafio de amanhã. Cada um ensina algo novo."),
    h("div", { class: "actions" }, h("a", { href: "#/", class: "btn btn-primary" }, "Voltar ao início")),
  );
}

function reviewResult(s, profileId, error, mapLink) {
  const again = h("button", { type: "button", class: "btn" }, "🧩 Revisar de novo");
  again.addEventListener("click", () => startAndPlay(() => api.startReview(profileId, s.subjectSlug), again, error));
  return h(
    "div",
    { class: "card center result" },
    h("p", { class: "result-icon", "aria-hidden": "true" }, "🧩"),
    h("h1", {}, "Revisão concluída!"),
    h("p", { class: "summary-score" }, h("strong", {}, `${s.correct} de ${s.total}`), " certas"),
    h("p", { class: "xp" }, `+${s.xpEarned} XP`),
    h("p", { class: "muted" }, "Revisar de vez em quando ajuda a não esquecer o que você já aprendeu."),
    error,
    h("div", { class: "actions" }, again, mapLink),
  );
}

export function medal(a) {
  const earned = Boolean(a.earnedAt);
  return h(
    "li",
    { class: `medal${earned ? " earned" : ""}`, title: a.description },
    h("span", { class: "medal-icon", "aria-hidden": "true" }, earned ? a.icon : "🔒"),
    h("strong", {}, a.name),
    h("span", { class: "muted small" }, a.description),
    h("span", { class: "visually-hidden" }, earned ? "Conquistada." : "Ainda não conquistada."),
  );
}
