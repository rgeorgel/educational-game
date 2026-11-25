import { api } from "../api.js";
import { h } from "../dom.js";
import { store } from "../store.js";
import { confetti, countUp } from "../effects.js";
import { levelBadge, levelProgress, startAndPlay } from "./shared.js";
import { canShare, shareBox } from "../share.js";
import { t } from "../i18n.js";

const TITLES = [t("Continue praticando!"), t("Boa! Etapa concluída!"), t("Muito bem!"), t("Perfeito!")];

export async function summaryPage({ sessionId }) {
  const s = await api.summary(sessionId);
  const error = h("p", { class: "form-error", role: "alert", hidden: true });
  const profileId = store.profile.id;
  const mapLink = h("a", { href: `#/mapa/${s.subjectSlug}`, class: "btn btn-ghost" }, t("Voltar ao mapa"));

  let main;
  if (s.mode === "guardiao") main = guardianResult(s, profileId, error, mapLink);
  else if (s.mode === "nivelamento") main = placementResult(s, mapLink);
  else if (s.mode === "diario") main = dailyResult(s);
  else if (s.mode === "revisao") main = reviewResult(s, profileId, error, mapLink);
  else if (s.mode === "reforco") main = reinforcementResult(s);
  else main = stageResult(s, profileId, error, mapLink);

  // Momento bom para lembrar o convidado de salvar: acabou de conquistar algo.
  const guestNudge =
    store.state.me?.isGuest && (s.stars > 0 || s.achievements.length > 0 || s.guardianPassed || s.resultLevel)
      ? h(
          "div",
          { class: "card guest-banner" },
          h("p", {}, h("strong", {}, `${t("Não perca essa conquista!")} `), t("Salve o progresso com uma conta para guardar tudo e jogar em outros aparelhos.")),
          h("a", { href: "#/salvar", class: "btn btn-primary btn-small" }, t("💾 Salvar progresso")),
        )
      : null;

  // Comemoração: XP contando e confete quando a conquista merece.
  const celebrate = s.stars === 3 || s.guardianPassed || (s.mode === "diario" && s.correct > 0) || (s.mode === "nivelamento" && s.resultLevel) || s.achievements.length > 0;
  setTimeout(() => {
    if (!main.isConnected) return;
    const xp = main.querySelector(".xp");
    if (xp) countUp(xp, s.xpEarned, { prefix: "+", suffix: " XP", duration: 1100 });
    if (celebrate) confetti({ count: 120, spread: 1.4 });
  }, 450);

  return h(
    "section",
    { class: "page-narrow summary" },
    main,
    canShare(s) ? h("section", { class: "card" }, h("h2", {}, t("Desafie os amigos")), shareBox(s)) : null,
    guestNudge,
    s.achievements.length
      ? h(
          "section",
          { class: "card medals-earned" },
          h("h2", {}, s.achievements.length === 1 ? t("Nova medalha!") : t("Novas medalhas!")),
          h("ul", { class: "medal-grid stagger" }, s.achievements.map(medal)),
        )
      : null,
    h("div", { class: "card" }, h("h2", {}, s.progress.name), levelProgress(s.progress)),
  );
}

function stageResult(s, profileId, error, mapLink) {
  const again = h("button", { type: "button", class: "btn" }, t("🔁 Jogar de novo"));
  again.addEventListener("click", () => startAndPlay(() => api.startSession(profileId, s.stage.id), again, error));

  let nextBtn = null;
  if (s.nextStage && s.nextStageUnlocked) {
    nextBtn = h("button", { type: "button", class: "btn btn-primary" }, t("Próxima: {nome} →", { nome: s.nextStage.name }));
    nextBtn.addEventListener("click", () => startAndPlay(() => api.startSession(profileId, s.nextStage.id), nextBtn, error));
  }

  const message =
    s.stars === 0
      ? t("Você precisa de pelo menos 60% de acertos para ganhar uma estrela e liberar a próxima etapa. Tente de novo — cada tentativa ajuda!")
      : s.stars < 3
        ? t("Jogue de novo quando quiser para conseguir as 3 estrelas.")
        : t("Você acertou tudo!");

  return h(
    "div",
    { class: "card center result" },
    h("p", { class: "muted" }, s.stage.name),
    h("h1", {}, TITLES[s.stars]),
    h("star-rating", { stars: String(s.stars), class: "stars-big" }),
    h("p", { class: "summary-score" }, h("strong", {}, t("{a} de {b}", { a: s.correct, b: s.total })), ` ${t("certas")}`),
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
    retry = h("button", { type: "button", class: "btn" }, t("Tentar de novo"));
    retry.addEventListener("click", () => startAndPlay(() => api.startGuardian(profileId, s.subjectSlug), retry, error));
  }
  return h(
    "div",
    { class: `card center result${passed ? " celebrate" : ""}` },
    h("p", { class: "result-icon", "aria-hidden": "true" }, passed ? "🏆" : "🛡️"),
    h("h1", {}, passed ? t("Você venceu o Guardião!") : t("O Guardião resistiu desta vez")),
    h("p", { class: "summary-score" }, h("strong", {}, t("{a} de {b}", { a: s.correct, b: s.total })), ` ${t("certas (precisava de 9)")}`),
    passed && s.resultLevel ? h("p", {}, t("Novo nível: "), levelBadge(s.resultLevel)) : null,
    h(
      "p",
      { class: "muted" },
      passed
        ? t("Agora as regiões do nível {nivel} estão abertas no mapa. Bora explorar!", { nivel: s.resultLevel?.name ?? "" })
        : t("Cada tentativa mostra o que ainda falta treinar. Pratique as etapas do nível e volte quando quiser."),
    ),
    h("p", { class: "xp" }, `+${s.xpEarned} XP`),
    error,
    h("div", { class: "actions" }, passed ? h("a", { href: `#/mapa/${s.subjectSlug}`, class: "btn btn-primary" }, t("Ver o mapa")) : mapLink, retry),
  );
}

function placementResult(s, mapLink) {
  return h(
    "div",
    { class: "card center result" },
    h("p", { class: "result-icon", "aria-hidden": "true" }, "🧭"),
    h("h1", {}, t("Teste concluído!")),
    h("p", {}, t("Você vai começar no nível "), s.resultLevel ? levelBadge(s.resultLevel) : t("Iniciante"), "."),
    h("p", { class: "summary-score" }, h("strong", {}, t("{a} de {b}", { a: s.correct, b: s.total })), ` ${t("certas")}`),
    h("p", { class: "muted" }, t("As habilidades dos níveis anteriores contam como dominadas, mas você pode jogar essas etapas quando quiser.")),
    h("div", { class: "actions" }, h("a", { href: `#/mapa/${s.subjectSlug}`, class: "btn btn-primary" }, t("Ir para o mapa"))),
  );
}

function dailyResult(s) {
  const ok = s.correct > 0;
  return h(
    "div",
    { class: `card center result${ok ? " celebrate" : ""}` },
    h("p", { class: "result-icon", "aria-hidden": "true" }, ok ? "🌟" : "📆"),
    h("h1", {}, ok ? t("Desafio do dia vencido!") : t("Esse foi difícil!")),
    h("p", { class: "xp" }, `+${s.xpEarned} XP`),
    h("p", { class: "muted" }, ok ? t("Você ganhou XP em dobro. Amanhã tem outro desafio!") : t("Veja a explicação e tente o desafio de amanhã. Cada um ensina algo novo.")),
    h("div", { class: "actions" }, h("a", { href: "#/", class: "btn btn-primary" }, t("Voltar ao início"))),
  );
}

function reviewResult(s, profileId, error, mapLink) {
  const again = h("button", { type: "button", class: "btn" }, t("🧩 Revisar de novo"));
  again.addEventListener("click", () => startAndPlay(() => api.startReview(profileId, s.subjectSlug), again, error));
  return h(
    "div",
    { class: "card center result" },
    h("p", { class: "result-icon", "aria-hidden": "true" }, "🧩"),
    h("h1", {}, t("Revisão concluída!")),
    h("p", { class: "summary-score" }, h("strong", {}, t("{a} de {b}", { a: s.correct, b: s.total })), ` ${t("certas")}`),
    h("p", { class: "xp" }, `+${s.xpEarned} XP`),
    h("p", { class: "muted" }, t("Revisar de vez em quando ajuda a não esquecer o que você já aprendeu.")),
    error,
    h("div", { class: "actions" }, again, mapLink),
  );
}

function reinforcementResult(s) {
  const done = s.stars >= 2;
  return h(
    "div",
    { class: `card center result${done ? " celebrate" : ""}` },
    h("p", { class: "result-icon", "aria-hidden": "true" }, done ? "🎯" : "💪"),
    h("h1", {}, done ? t("Treino concluído!") : t("Bom treino!")),
    h("star-rating", { stars: String(s.stars), class: "stars-big" }),
    h("p", { class: "summary-score" }, h("strong", {}, t("{a} de {b}", { a: s.correct, b: s.total })), ` ${t("certas")}`),
    h("p", { class: "xp" }, `+${s.xpEarned} XP`),
    h("p", { class: "muted" }, done ? t("Seu responsável vai ver que você concluiu este treino.") : t("Faltou pouco: com 2 estrelas o treino fica concluído. Tente de novo quando quiser!")),
    h("div", { class: "actions" }, h("a", { href: "#/", class: "btn btn-primary" }, t("Voltar ao início"))),
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
    h("span", { class: "visually-hidden" }, earned ? t("Conquistada.") : t("Ainda não conquistada.")),
  );
}
