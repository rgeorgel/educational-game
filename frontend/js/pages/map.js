import { api } from "../api.js";
import { errorMessage, h } from "../dom.js";
import { navigate } from "../router.js";
import { store } from "../store.js";
import { levelProgress } from "./shared.js";

export async function mapPage({ subject }) {
  const profile = store.profile;
  const [subjects, map] = await Promise.all([api.subjects(), api.map(profile.id, subject)]);

  const error = h("p", { class: "form-error", role: "alert", hidden: true });

  async function start(stage, button) {
    button.disabled = true;
    try {
      const session = await api.startSession(profile.id, stage.id);
      navigate(`/jogar/${session.sessionId}`);
    } catch (err) {
      error.textContent = errorMessage(err);
      error.hidden = false;
      button.disabled = false;
    }
  }

  const tabs = h(
    "nav",
    { class: "subject-tabs", "aria-label": "Trilhas" },
    subjects.map((s) =>
      h("a", { href: `#/mapa/${s.slug}`, class: `tab${s.slug === subject ? " active" : ""}`, "aria-current": s.slug === subject ? "page" : null }, `${s.icon} ${s.name}`),
    ),
  );

  let stageNumber = 0;
  const regions = map.regions.map((region) =>
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
          button.addEventListener("click", () => start(stage, button));
          return h("li", {}, button);
        }),
      ),
    ),
  );

  return h(
    "section",
    { class: "page" },
    tabs,
    h("h1", {}, `${map.progress.icon} ${map.progress.name}`),
    h("div", { class: "card" }, levelProgress(map.progress)),
    error,
    regions,
    h("p", { class: "muted small center" }, "Ganhe pelo menos 1 estrela para liberar a próxima etapa. Novos níveis chegam em breve!"),
  );
}
