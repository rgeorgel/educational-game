import { api } from "../api.js";
import { errorMessage, h } from "../dom.js";
import { navigate } from "../router.js";
import { store } from "../store.js";
import { levelProgress, startAndPlay } from "./shared.js";

export async function mapPage({ subject }) {
  const profile = store.profile;
  const [subjects, map] = await Promise.all([api.subjects(), api.map(profile.id, subject)]);
  const progress = map.progress;

  const error = h("p", { class: "form-error", role: "alert", hidden: true });

  const tabs = h(
    "nav",
    { class: "subject-tabs", "aria-label": "Trilhas" },
    subjects.map((s) =>
      h("a", { href: `#/mapa/${s.slug}`, class: `tab${s.slug === subject ? " active" : ""}`, "aria-current": s.slug === subject ? "page" : null }, `${s.icon} ${s.name}`),
    ),
  );

  // ---------- Teste de nivelamento (só antes de começar a trilha) ----------
  let placement = null;
  if (progress.placementAvailable) {
    const startBtn = h("button", { type: "button", class: "btn btn-primary" }, "Fazer o teste (10 perguntas)");
    startBtn.addEventListener("click", () => startAndPlay(() => api.startPlacement(profile.id, subject), startBtn, error));
    const skipBtn = h("button", { type: "button", class: "btn btn-ghost" }, "Começar do início");
    skipBtn.addEventListener("click", async () => {
      skipBtn.disabled = true;
      try {
        await api.skipPlacement(profile.id, subject);
        navigate(`/mapa/${subject}`);
      } catch (err) {
        error.textContent = errorMessage(err);
        error.hidden = false;
        skipBtn.disabled = false;
      }
    });
    placement = h(
      "section",
      { class: "card banner banner-placement" },
      h("div", { class: "banner-icon", "aria-hidden": "true" }, "🧭"),
      h(
        "div",
        { class: "banner-body" },
        h("h2", {}, "Já sabe um pouco de " + progress.name + "?"),
        h("p", { class: "muted" }, "Faça um teste rápido para começar no nível certo. Não há dicas nem nota — é só para descobrir por onde começar."),
        h("div", { class: "actions-left" }, startBtn, skipBtn),
      ),
    );
  }

  // ---------- Desafio do Guardião ----------
  let guardian = null;
  if (progress.guardianAvailable) {
    const btn = h("button", { type: "button", class: "btn btn-primary" }, "Enfrentar o Guardião");
    btn.addEventListener("click", () => startAndPlay(() => api.startGuardian(profile.id, subject), btn, error));
    guardian = h(
      "section",
      { class: "card banner banner-guardian" },
      h("div", { class: "banner-icon", "aria-hidden": "true" }, "🛡️"),
      h(
        "div",
        { class: "banner-body" },
        h("h2", {}, `Desafio do Guardião: rumo ao ${progress.nextLevel.name}!`),
        h("p", { class: "muted" }, "12 problemas de todas as habilidades do nível. Acerte pelo menos 9 para subir de nível. Se não der, pratique as habilidades que faltam e tente de novo quando quiser."),
        h("div", { class: "actions-left" }, btn),
      ),
    );
  }

  // ---------- Revisão inteligente ----------
  let review = null;
  if (progress.reviewAvailable) {
    const btn = h("button", { type: "button", class: "btn" }, "🧩 Revisar");
    btn.addEventListener("click", () => startAndPlay(() => api.startReview(profile.id, subject), btn, error));
    review = h(
      "div",
      { class: "card review-bar" },
      h(
        "p",
        {},
        progress.needsReviewSkills > 0
          ? `${progress.needsReviewSkills} ${progress.needsReviewSkills === 1 ? "habilidade pede" : "habilidades pedem"} revisão. `
          : "Treine o que você mais precisa: ",
        h("span", { class: "muted small" }, "8 problemas das habilidades em que você está mais fraco ou que não pratica há tempo."),
      ),
      btn,
    );
  }

  let stageNumber = 0;
  const levels = map.levels.map((section) => {
    const levelStages = section.regions.flatMap((r) => r.stages);
    const stars = levelStages.reduce((sum, s) => sum + s.stars, 0);
    const header = h(
      "header",
      { class: "level-head" },
      h("span", { class: `level-badge level-${section.level.slug}` }, h("span", { "aria-hidden": "true" }, section.level.icon), ` ${section.level.name}`),
      section.unlocked
        ? h("span", { class: "muted small" }, `★ ${stars} de ${levelStages.length * 3}${section.current ? " · nível atual" : ""}`)
        : h("span", { class: "muted small" }, "🔒 Passe no Desafio do Guardião para abrir"),
    );

    if (!section.unlocked) {
      stageNumber += levelStages.length;
      return h(
        "section",
        { class: "level-section locked" },
        header,
        h("ul", { class: "locked-regions" }, section.regions.map((r) => h("li", {}, h("span", { "aria-hidden": "true" }, r.icon), ` ${r.name}`))),
      );
    }

    const regions = section.regions.map((region) =>
        h(
          "section",
          { class: "card region" },
          h("header", { class: "region-head" }, h("span", { class: "region-icon", "aria-hidden": "true" }, region.icon), h("div", {}, h("h2", {}, region.name), h("p", { class: "muted small" }, region.description))),
          h(
            "ol",
            { class: "stage-path" },
            region.stages.map((stage) => {
              stageNumber++;
              const button = h(
                "button",
                {
                  type: "button",
                  class: `stage${stage.unlocked ? "" : " locked"}${stage.stars > 0 ? " done" : ""}`,
                  disabled: !stage.unlocked,
                  "aria-label": stage.unlocked
                    ? `Etapa ${stageNumber}: ${stage.name}. ${stage.stars} de 3 estrelas. ${stage.problemCount} problemas.`
                    : `Etapa ${stageNumber}: ${stage.name}. Bloqueada.`,
                },
                h("span", { class: "stage-node", "aria-hidden": "true" }, stage.unlocked ? String(stageNumber) : "🔒"),
                h(
                  "span",
                  { class: "stage-info" },
                  h("span", { class: "stage-name" }, stage.name),
                  h("span", { class: "muted small" }, `${stage.problemCount} problemas · ${stage.skills.join(", ")}`),
                ),
                h("star-rating", { stars: String(stage.stars) }),
              );
              button.addEventListener("click", () => startAndPlay(() => api.startSession(profile.id, stage.id), button, error));
              return h("li", {}, button);
            }),
          ),
        ),
    );

    // Níveis já superados ficam recolhidos; o nível atual aparece aberto.
    if (!section.current) {
      return h("details", { class: "level-section past" }, h("summary", {}, header), regions);
    }
    return h("section", { class: "level-section current" }, header, regions);
  });

  return h(
    "section",
    { class: "page" },
    tabs,
    h("h1", {}, `${progress.icon} ${progress.name}`),
    h("div", { class: "card" }, levelProgress(progress)),
    error,
    placement,
    guardian,
    review,
    levels,
    h("p", { class: "muted small center" }, `Ganhe pelo menos 1 estrela para liberar a próxima etapa. Quando a barra do nível chegar a ${progress.guardianAt}% (a marca na barra), o Desafio do Guardião abre.`),
  );
}
