import { api } from "../api.js";
import { errorMessage, h } from "../dom.js";
import { navigate } from "../router.js";
import { store } from "../store.js";
import { levelProgress, startAndPlay } from "./shared.js";
import { t, tn } from "../i18n.js";

export async function mapPage({ subject }) {
  const profile = store.profile;
  const [subjects, map] = await Promise.all([api.subjects(), api.map(profile.id, subject)]);
  const progress = map.progress;

  const error = h("p", { class: "form-error", role: "alert", hidden: true });

  const tabs = h(
    "nav",
    { class: "subject-tabs", "aria-label": t("Trilhas") },
    subjects.map((s) =>
      h("a", { href: `#/mapa/${s.slug}`, class: `tab${s.slug === subject ? " active" : ""}`, "aria-current": s.slug === subject ? "page" : null }, `${s.icon} ${s.name}`),
    ),
  );

  /** Muda o nível na trilha (escolha inicial ou ir para um nível mais fácil) e recarrega o mapa. */
  const changeLevel = async (level, button) => {
    button.disabled = true;
    error.hidden = true;
    try {
      await api.changeLevel(profile.id, subject, level.slug);
      navigate(`/mapa/${subject}`);
    } catch (err) {
      error.textContent = errorMessage(err);
      error.hidden = false;
      button.disabled = false;
    }
  };

  // ---------- Ir para um nível mais fácil ----------
  // Discreto no cartão do nível; vira um aviso quando o jogador está acertando pouco no nível atual.
  let easierToggle = null;
  let easierBanner = null;
  const currentIndex = map.levels.findIndex((l) => l.current);
  if (currentIndex > 0) {
    const lower = map.levels.slice(0, currentIndex);
    const picker = levelPicker(
      lower,
      {
        id: "easier-picker",
        selected: lower.at(-1).level.slug,
        confirmLabel: (level) => t("Ir para o {nivel}", { nivel: level.name }),
        note: t("Suas estrelas e o que você já praticou ficam guardados. Para subir de novo, passe no Desafio do Guardião."),
      },
      changeLevel,
    );
    if (progress.suggestEasierLevel) {
      const toggle = pickerToggle(h("button", { type: "button", class: "btn" }, t("Escolher um nível mais fácil")), picker);
      easierBanner = h(
        "section",
        { class: "card banner banner-easier" },
        h("div", { class: "banner-icon", "aria-hidden": "true" }, "🧗"),
        h(
          "div",
          { class: "banner-body" },
          h("h2", {}, t("O nível {nivel} está difícil?", { nivel: progress.level.name })),
          h("p", { class: "muted" }, t("Você está acertando poucas perguntas por aqui. Voltar um nível e treinar mais um pouco ajuda a chegar mais forte. Você decide!")),
          h("div", { class: "actions-left" }, toggle),
          picker,
        ),
      );
    } else {
      easierToggle = h(
        "div",
        { class: "easier-level" },
        pickerToggle(h("button", { type: "button", class: "btn btn-ghost btn-small" }, t("Está difícil? Ir para um nível mais fácil")), picker),
        picker,
      );
    }
  }

  // ---------- Teste de nivelamento (só antes de começar a trilha) ----------
  let placement = null;
  if (progress.placementAvailable) {
    const startBtn = h("button", { type: "button", class: "btn btn-primary" }, t("Fazer o teste (10 perguntas)"));
    startBtn.addEventListener("click", () => startAndPlay(() => api.startPlacement(profile.id, subject), startBtn, error));
    const skipBtn = h("button", { type: "button", class: "btn btn-ghost" }, t("Começar do início"));
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
    const picker = levelPicker(
      map.levels,
      {
        id: "level-picker",
        selected: progress.suggestedLevel,
        suggested: progress.suggestedLevel,
        confirmLabel: (level) => t("Começar no {nivel}", { nivel: level.name }),
        note: t("As etapas dos níveis anteriores ficam abertas e contam como dominadas. Para subir mais, passe no Desafio do Guardião."),
      },
      changeLevel,
    );
    const chooseBtn = pickerToggle(h("button", { type: "button", class: "btn" }, t("Escolher meu nível")), picker);
    placement = h(
      "section",
      { class: "card banner banner-placement" },
      h("div", { class: "banner-icon", "aria-hidden": "true" }, "🧭"),
      h(
        "div",
        { class: "banner-body" },
        h("h2", {}, t("Já sabe um pouco de {nome}?", { nome: progress.name })),
        h("p", { class: "muted" }, t("Faça um teste rápido para começar no nível certo, ou escolha você mesmo o nível. Não há dicas nem nota — é só para descobrir por onde começar.")),
        h("div", { class: "actions-left" }, startBtn, chooseBtn, skipBtn),
        picker,
      ),
    );
  }

  // ---------- Desafio do Guardião ----------
  let guardian = null;
  if (progress.guardianAvailable) {
    const btn = h("button", { type: "button", class: "btn btn-primary" }, t("Enfrentar o Guardião"));
    btn.addEventListener("click", () => startAndPlay(() => api.startGuardian(profile.id, subject), btn, error));
    guardian = h(
      "section",
      { class: "card banner banner-guardian" },
      h("div", { class: "banner-icon", "aria-hidden": "true" }, "🛡️"),
      h(
        "div",
        { class: "banner-body" },
        h("h2", {}, t("Desafio do Guardião: rumo ao {nivel}!", { nivel: progress.nextLevel.name })),
        h("p", { class: "muted" }, t("12 problemas de todas as habilidades do nível. Acerte pelo menos 9 para subir de nível. Se não der, pratique as habilidades que faltam e tente de novo quando quiser.")),
        h("div", { class: "actions-left" }, btn),
      ),
    );
  }

  // ---------- Revisão inteligente ----------
  let review = null;
  if (progress.reviewAvailable) {
    const btn = h("button", { type: "button", class: "btn" }, t("🧩 Revisar"));
    btn.addEventListener("click", () => startAndPlay(() => api.startReview(profile.id, subject), btn, error));
    review = h(
      "div",
      { class: "card review-bar" },
      h(
        "p",
        {},
        progress.needsReviewSkills > 0
          ? `${tn(progress.needsReviewSkills, "{n} habilidade pede revisão.", "{n} habilidades pedem revisão.")} `
          : t("Treine o que você mais precisa: "),
        h("span", { class: "muted small" }, t("8 problemas das habilidades em que você está mais fraco ou que não pratica há tempo.")),
      ),
      btn,
    );
  }

  // Próxima etapa sugerida: a primeira aberta e ainda sem estrelas no nível atual.
  const currentStages = map.levels.filter((l) => l.current).flatMap((l) => l.regions.flatMap((r) => r.stages));
  const nextStageId = currentStages.find((st) => st.unlocked && st.stars === 0)?.id ?? null;

  let stageNumber = 0;
  const levels = map.levels.map((section) => {
    const levelStages = section.regions.flatMap((r) => r.stages);
    const stars = levelStages.reduce((sum, s) => sum + s.stars, 0);
    const header = h(
      "header",
      { class: "level-head" },
      h("span", { class: `level-badge level-${section.level.slug}` }, h("span", { "aria-hidden": "true" }, section.level.icon), ` ${section.level.name}`),
      section.unlocked
        ? h("span", { class: "muted small" }, `★ ${t("{a} de {b}", { a: stars, b: levelStages.length * 3 })}${section.current ? ` · ${t("nível atual")}` : ""}`)
        : h("span", { class: "muted small" }, t("🔒 Passe no Desafio do Guardião para abrir")),
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
        trail(
          region.stages.map((stage) => ({ stage, number: ++stageNumber })),
          nextStageId,
          (stage, button) => startAndPlay(() => api.startSession(profile.id, stage.id), button, error),
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
    h("div", { class: "map-hero", dataset: { subject } }, h("h1", {}, `${progress.icon} ${progress.name}`), h("div", { class: "card" }, levelProgress(progress), easierToggle)),
    error,
    easierBanner,
    placement,
    guardian,
    review,
    levels,
    h("p", { class: "muted small center" }, t("Ganhe pelo menos 1 estrela para liberar a próxima etapa. Quando a barra do nível chegar a {g}% (a marca na barra), o Desafio do Guardião abre.", { g: progress.guardianAt })),
  );
}

// ---------- Escolha do nível inicial ----------

/**
 * Lista níveis para o jogador escolher (onde começar a trilha, ou para qual nível mais fácil ir).
 * Começa com `selected` marcado; `suggested` ganha o selo "Sugerido".
 */
function levelPicker(sections, { id, selected: selectedSlug, suggested, confirmLabel, note }, onConfirm) {
  let selected = sections.find((s) => s.level.slug === selectedSlug)?.level ?? sections[0].level;

  const confirmBtn = h("button", { type: "button", class: "btn btn-primary" });
  const options = sections.map(({ level, regions }) => {
    const option = h(
      "button",
      { type: "button", role: "radio", class: "level-option", dataset: { slug: level.slug } },
      h("span", { class: `level-badge level-${level.slug}` }, h("span", { "aria-hidden": "true" }, level.icon), ` ${level.name}`),
      level.slug === suggested ? h("span", { class: "chip" }, t("Sugerido")) : null,
      h("span", { class: "muted small level-option-topics" }, regions.map((r) => r.name).join(" · ")),
    );
    option.addEventListener("click", () => select(level));
    return option;
  });

  const select = (level) => {
    selected = level;
    options.forEach((o) => {
      const on = o.dataset.slug === level.slug;
      o.setAttribute("aria-checked", String(on));
      o.tabIndex = on ? 0 : -1;
    });
    confirmBtn.textContent = confirmLabel(level);
  };
  select(selected);

  // Setas mudam a escolha, como num grupo de rádios.
  const group = h("div", { role: "radiogroup", class: "level-options", "aria-label": t("Níveis") }, options);
  group.addEventListener("keydown", (e) => {
    const step = { ArrowDown: 1, ArrowRight: 1, ArrowUp: -1, ArrowLeft: -1 }[e.key];
    if (!step) return;
    e.preventDefault();
    const index = sections.findIndex((s) => s.level.slug === selected.slug);
    const next = sections[(index + step + sections.length) % sections.length].level;
    select(next);
    options.find((o) => o.dataset.slug === next.slug).focus();
  });
  confirmBtn.addEventListener("click", () => onConfirm(selected, confirmBtn));

  return h(
    "div",
    { id, class: "level-picker", hidden: true },
    group,
    h("p", { class: "muted small" }, note),
    h("div", { class: "actions-left" }, confirmBtn),
  );
}

/** Faz o botão abrir e fechar a lista de níveis. */
function pickerToggle(button, picker) {
  button.setAttribute("aria-expanded", "false");
  button.setAttribute("aria-controls", picker.id);
  button.addEventListener("click", () => {
    picker.hidden = !picker.hidden;
    button.setAttribute("aria-expanded", String(!picker.hidden));
    if (!picker.hidden) picker.querySelector("[aria-checked='true']")?.focus();
  });
  return button;
}

// ---------- Trilha em ziguezague ----------

const SVG_NS = "http://www.w3.org/2000/svg";
// Deslocamento horizontal de cada etapa (-1 a 1), formando uma curva suave.
const WAVE = [0, 0.7, 1, 0.7, 0, -0.7, -1, -0.7];

/** Curva suave passando pelos centros dos nós. */
function curve(points) {
  if (points.length < 2) return "";
  let d = `M ${points[0][0]} ${points[0][1]}`;
  for (let i = 1; i < points.length; i++) {
    const [x0, y0] = points[i - 1];
    const [x1, y1] = points[i];
    const dy = (y1 - y0) / 2;
    d += ` C ${x0} ${y0 + dy}, ${x1} ${y1 - dy}, ${x1} ${y1}`;
  }
  return d;
}

function trail(items, nextStageId, onPlay) {
  const svg = document.createElementNS(SVG_NS, "svg");
  svg.classList.add("trail-path");
  svg.setAttribute("aria-hidden", "true");
  const basePath = document.createElementNS(SVG_NS, "path");
  const donePath = document.createElementNS(SVG_NS, "path");
  donePath.classList.add("done");
  svg.append(basePath, donePath);

  const steps = items.map(({ stage, number }, index) => {
    const challenge = /desafio/i.test(stage.slug);
    const done = stage.stars > 0;
    const isNext = stage.id === nextStageId;
    const label = stage.unlocked
      ? `${t("Etapa {n}: {nome}. {e} de 3 estrelas. {p} problemas.", { n: number, nome: stage.name, e: stage.stars, p: stage.problemCount })}${isNext ? ` ${t("Próxima etapa.")}` : ""}`
      : t("Etapa {n}: {nome}. Bloqueada.", { n: number, nome: stage.name });
    const node = h(
      "button",
      { type: "button", class: "trail-node", disabled: !stage.unlocked, "aria-label": label, title: stage.skills.join(", ") },
      h("span", { "aria-hidden": "true" }, !stage.unlocked ? "🔒" : challenge ? "🏆" : String(number)),
    );
    node.addEventListener("click", () => onPlay(stage, node));

    const step = h(
      "li",
      { class: ["trail-step", done ? "done" : "", stage.unlocked ? "" : "locked", isNext ? "next" : "", challenge ? "challenge" : ""].filter(Boolean).join(" ") },
      isNext ? h("span", { class: "trail-bubble", "aria-hidden": "true" }, t("Jogar!")) : null,
      node,
      h(
        "span",
        { class: "trail-label" },
        h("strong", {}, stage.name),
        h("star-rating", { stars: String(stage.stars) }),
        h("span", { class: "muted small" }, t("{p} problemas", { p: stage.problemCount })),
      ),
    );
    step.style.setProperty("--wave", String(WAVE[index % WAVE.length]));
    return step;
  });

  const list = h("ol", { class: "trail" }, svg, steps);
  const doneCount = items.findIndex(({ stage }) => stage.stars === 0);
  const completed = doneCount === -1 ? items.length : doneCount;

  // Redesenha o caminho sempre que o tamanho muda (tela girada, detalhes abertos…).
  const draw = () => {
    const box = list.getBoundingClientRect();
    if (box.width === 0) return;
    const points = [...list.querySelectorAll(".trail-node")].map((n) => {
      const r = n.getBoundingClientRect();
      return [Math.round(r.left + r.width / 2 - box.left), Math.round(r.top + r.height / 2 - box.top)];
    });
    svg.setAttribute("viewBox", `0 0 ${Math.round(box.width)} ${Math.round(box.height)}`);
    basePath.setAttribute("d", curve(points));
    donePath.setAttribute("d", completed > 0 ? curve(points.slice(0, Math.min(completed + 1, points.length))) : "");
  };
  new ResizeObserver(draw).observe(list);
  return list;
}
