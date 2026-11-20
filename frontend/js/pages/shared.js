import { h } from "../dom.js";

/** Selo do nível (Iniciante, Médio…) com a cor do nível. */
export function levelBadge(level) {
  return h("span", { class: `level-badge level-${level.slug}` }, h("span", { "aria-hidden": "true" }, level.icon), ` ${level.name}`);
}

/** Barra "x% para o próximo nível". */
export function levelProgress(progress) {
  const label = progress.nextLevel
    ? progress.readyForNextLevel
      ? `Pronto para o Desafio do Guardião (${progress.nextLevel.name}) — em breve!`
      : `${progress.percent}% para ${progress.nextLevel.name}`
    : "Nível máximo!";
  return h(
    "div",
    { class: "level-progress" },
    h("div", { class: "level-progress-head" }, levelBadge(progress.level), h("span", { class: "muted small" }, label)),
    h("progress-bar", { value: String(progress.percent), label, level: progress.level.slug }),
    h("p", { class: "muted small" }, `${progress.masteredSkills} de ${progress.totalSkills} habilidades dominadas`),
  );
}

export const STATUS_LABEL = {
  notStarted: "Não iniciada",
  practicing: "Praticando",
  mastered: "Dominada",
  needsReview: "Precisa revisar",
};

export function statusChip(status) {
  return h("span", { class: `chip status-${status}` }, STATUS_LABEL[status] ?? status);
}

export function loading(text = "Carregando…") {
  return h("p", { class: "loading", role: "status" }, text);
}
