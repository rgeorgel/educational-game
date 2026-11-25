import { errorMessage, h } from "../dom.js";
import { t } from "../i18n.js";

/** Selo do nível (Iniciante, Médio…) com a cor do nível. */
export function levelBadge(level) {
  return h("span", { class: `level-badge level-${level.slug}` }, h("span", { "aria-hidden": "true" }, level.icon), ` ${level.name}`);
}

/**
 * Barra do nível: mostra o avanço real (média das habilidades rumo ao domínio),
 * a marca em que o Guardião abre e as habilidades que mais faltam.
 */
export function levelProgress(progress) {
  const next = progress.nextLevel;
  let label;
  if (!next) label = progress.percent >= 100 ? t("Nível máximo dominado!") : t("{p}% do nível máximo", { p: progress.percent });
  else if (progress.guardianAvailable) label = t("{p}% · Guardião liberado!", { p: progress.percent });
  else if (progress.readyForNextLevel) label = t("{p}% · {nivel} em breve", { p: progress.percent, nivel: next.name });
  else label = t("{p}% · o Guardião abre em {g}%", { p: progress.percent, g: progress.guardianAt });

  // Habilidades mais longe do domínio (as que mais ajudam a barra a subir).
  const missing = (progress.skills ?? [])
    .map((k) => ({ name: k.name, pct: k.rating === null ? 0 : Math.max(0, Math.min(100, Math.floor(((k.rating - k.startRating) / (k.targetRating - k.startRating)) * 100))) }))
    .filter((k) => k.pct < 100)
    .sort((a, b) => a.pct - b.pct)
    .slice(0, 3);

  return h(
    "div",
    { class: "level-progress" },
    h("div", { class: "level-progress-head" }, levelBadge(progress.level), h("span", { class: "muted small" }, label)),
    h("progress-bar", {
      value: String(progress.percent),
      label: next ? t("{p}% do nível {nivel}; o Guardião abre em {g}%", { p: progress.percent, nivel: progress.level.name, g: progress.guardianAt }) : label,
      level: progress.level.slug,
      mark: next ? String(progress.guardianAt) : null,
    }),
    h(
      "p",
      { class: "muted small" },
      t("{a} de {b} habilidades dominadas", { a: progress.masteredSkills, b: progress.totalSkills }),
      missing.length && !progress.guardianAvailable ? ` · ${t("Para avançar, pratique: {lista}", { lista: missing.map((m) => `${m.name} (${m.pct}%)`).join(", ") })}` : "",
    ),
  );
}

export const STATUS_LABEL = {
  notStarted: t("Não iniciada"),
  practicing: t("Praticando"),
  mastered: t("Dominada"),
  needsReview: t("Precisa revisar"),
};

export function statusChip(status) {
  return h("span", { class: `chip status-${status}` }, STATUS_LABEL[status] ?? status);
}

/** Inicia uma sessão (etapa, Guardião ou nivelamento) e vai para a tela de jogo. */
export async function startAndPlay(start, button, errorBox) {
  button.disabled = true;
  try {
    const session = await start();
    location.hash = `#/jogar/${session.sessionId}`;
  } catch (err) {
    errorBox.textContent = errorMessage(err);
    errorBox.hidden = false;
    button.disabled = false;
  }
}

export function loading(text = t("Carregando…")) {
  return h("p", { class: "loading", role: "status" }, text);
}

/** Avatar do perfil: emoji, acessório no canto e cor de fundo desbloqueada. */
export function avatarView(profile, size = "md") {
  return h(
    "span",
    { class: `avatar-badge size-${size} color-${profile.color || "padrao"}`, "aria-hidden": "true" },
    h("span", { class: "avatar-emoji" }, profile.avatar),
    profile.accessoryIcon ? h("span", { class: `avatar-accessory acc-${accessorySlot(profile.accessoryIcon)}` }, profile.accessoryIcon) : null,
  );
}

/** Onde o acessório fica no avatar: na cabeça, no rosto ou no peito. */
function accessorySlot(icon) {
  if (icon === "🕶️") return "rosto";
  if (icon === "🏅") return "peito";
  return "cabeca";
}
