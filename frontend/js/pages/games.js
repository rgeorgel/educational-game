// Menu de jogos: cada habilidade do currículo é um jogo que dá para escolher e jogar a qualquer hora
// (ex.: "hoje só quero Sudoku"), fora da trilha do mapa. Filtros por tópico, A–Z e busca.
import { api } from "../api.js";
import { errorMessage, h } from "../dom.js";
import { getPrefs, setPrefs } from "../prefs.js";
import { store } from "../store.js";
import { levelBadge, statusChip } from "./shared.js";
import { t } from "../i18n.js";

/** Ícone de cada jogo (pelo slug da habilidade). Sem ícone, usa o do tópico. */
const GAME_ICONS = {
  // Matemática
  "contagem-comparacao": "🐥",
  "dezenas-unidades": "🧮",
  "adicao-ate-100": "➕",
  "subtracao-ate-100": "➖",
  "multiplicacao-grupos": "🍎",
  "dinheiro-e-horas": "🕰️",
  "tabuada-completa": "✖️",
  divisao: "➗",
  expressoes: "🧾",
  "fracoes-basicas": "🍕",
  "decimais-porcentagem": "💯",
  "perimetro-area": "📐",
  "numeros-inteiros": "🌡️",
  "potencias-raizes": "🚀",
  "mmc-mdc": "🔗",
  "fracoes-operacoes": "🥧",
  "proporcao-porcentagem": "🛒",
  "equacoes-1-grau": "❓",
  "sistemas-equacoes": "🔐",
  "equacoes-2-grau": "🏹",
  "juros-porcentagem": "💰",
  "geometria-pitagoras": "📏",
  combinatoria: "🎲",
  // Lógica
  "padroes-visuais": "🔷",
  "sequencias-simples": "🪜",
  "qual-e-diferente": "🔍",
  "maior-menor-igual": "🐊",
  "sequencias-compostas": "🌀",
  "grades-numericas": "🔢",
  "verdadeiro-ou-falso": "✅",
  balancas: "⚖️",
  "tabelas-logicas": "🗂️",
  "sudoku-6x6": "🧱",
  proposicoes: "💬",
  conjuntos: "⭕",
  "enigmas-simbolos": "🔣",
  "quem-e-quem": "🕵️",
  "tabelas-verdade": "📋",
  "cavaleiros-mentirosos": "🛡️",
  "pensamento-computacional": "💻",
  "desafios-olimpicos": "🏅",
};

export function gameIcon(slug, fallback = "🎮") {
  return GAME_ICONS[slug] ?? fallback;
}

/** Para a busca: minúsculas e sem acentos ("Lógica" acha "logica"). */
const fold = (text) => text.normalize("NFD").replace(/\p{Diacritic}/gu, "").toLowerCase();

const RECENT_MAX = 10;

export async function gamesPage() {
  const profile = store.profile;
  const data = await api.games(profile.id);
  const subjects = new Map(data.subjects.map((s) => [s.slug, s]));
  const error = h("p", { class: "form-error", role: "alert", hidden: true });

  let filter = "todos"; // "todos" | slug do tópico | "az"
  let query = "";

  const play = async (game, card) => {
    card.disabled = true;
    card.classList.add("is-loading");
    error.hidden = true;
    try {
      const session = await api.startGame(profile.id, game.subjectSlug, game.slug);
      location.hash = `#/jogar/${session.sessionId}`;
    } catch (err) {
      error.textContent = errorMessage(err);
      error.hidden = false;
      error.scrollIntoView({ behavior: "smooth", block: "center" });
      card.disabled = false;
      card.classList.remove("is-loading");
    }
  };

  const card = (game) => {
    const subject = subjects.get(game.subjectSlug);
    const stats = getPrefs().gameStats;
    const label = [
      game.name,
      subject?.name,
      game.level.name,
      stats && game.attempts > 0 ? t("{p}% dominado", { p: game.percent }) : null,
    ]
      .filter(Boolean)
      .join(". ");
    const el = h(
      "button",
      { type: "button", class: "game-card", dataset: { subject: game.subjectSlug }, "aria-label": label, title: game.description },
      h("span", { class: "game-art", "aria-hidden": "true" }, h("span", { class: "game-icon" }, gameIcon(game.slug, subject?.icon))),
      h(
        "span",
        { class: "game-info" },
        h("strong", { class: "game-name" }, game.name),
        h("span", { class: "game-desc" }, game.description),
        h("span", { class: "game-meta" }, levelBadge(game.level), stats && game.attempts > 0 ? statusChip(game.status) : null),
        stats
          ? h(
              "span",
              { class: "game-stats" },
              h("progress-bar", { value: String(game.percent), level: game.level.slug, label: t("{p}% dominado", { p: game.percent }) }),
              h(
                "span",
                { class: "muted small" },
                game.attempts > 0 ? t("{a} de {b} certas", { a: game.correct, b: game.attempts }) : t("Ainda não jogado"),
              ),
            )
          : null,
      ),
    );
    el.addEventListener("click", () => play(game, el));
    return el;
  };

  /** Fileira com rolagem lateral (como numa loja de jogos). */
  const row = (title, games, { id } = {}) =>
    games.length
      ? h(
          "section",
          { class: "game-row", "aria-labelledby": id },
          h("h2", { id, class: "game-row-title" }, title),
          h("div", { class: "game-scroller" }, games.map(card)),
        )
      : null;

  const grid = (games) => h("div", { class: "game-grid" }, games.map(card));

  const recent = () =>
    data.games
      .filter((g) => g.lastPlayedAt && (filter === "todos" || filter === g.subjectSlug))
      .sort((a, b) => b.lastPlayedAt.localeCompare(a.lastPlayedAt))
      .slice(0, RECENT_MAX);

  /** Uma fileira por nível de cada tópico, na ordem do currículo. */
  const byLevel = (subjectSlug, withSubjectName) => {
    const games = data.games.filter((g) => g.subjectSlug === subjectSlug);
    const levels = [...new Map(games.map((g) => [g.level.slug, g.level])).values()];
    const subject = subjects.get(subjectSlug);
    return levels.map((level) =>
      row(
        withSubjectName ? `${subject.icon} ${subject.name} · ${level.icon} ${level.name}` : `${level.icon} ${level.name}`,
        games.filter((g) => g.level.slug === level.slug),
        { id: `jogos-${subjectSlug}-${level.slug}` },
      ),
    );
  };

  const content = h("div", { class: "games-content" });

  const render = () => {
    const q = fold(query.trim());
    if (q) {
      const found = data.games.filter((g) => fold(`${g.name} ${g.description}`).includes(q));
      content.replaceChildren(
        found.length
          ? h("section", { class: "game-row" }, h("h2", { class: "game-row-title" }, t("Resultados da busca")), grid(found))
          : h("p", { class: "muted center games-empty" }, t("Nenhum jogo encontrado. Tente outra palavra.")),
      );
      return;
    }
    if (filter === "az") {
      const sorted = [...data.games].sort((a, b) => a.name.localeCompare(b.name));
      content.replaceChildren(grid(sorted));
      return;
    }
    if (filter === "todos") {
      content.replaceChildren(
        ...[
          row(t("🔁 Jogue de novo"), recent(), { id: "jogos-recentes" }),
          row(t("⭐ No seu nível"), data.games.filter((g) => g.currentLevel), { id: "jogos-nivel" }),
          ...data.subjects.flatMap((s) => byLevel(s.slug, true)),
        ].filter(Boolean),
      );
      return;
    }
    content.replaceChildren(...[row(t("🔁 Jogue de novo"), recent(), { id: "jogos-recentes" }), ...byLevel(filter, false)].filter(Boolean));
  };

  // ---------- Filtros ----------
  const chipDefs = [{ id: "todos", label: t("Todos") }, ...data.subjects.map((s) => ({ id: s.slug, label: `${s.icon} ${s.name}` })), { id: "az", label: t("A–Z") }];
  const chips = chipDefs.map((c) => {
    const chip = h("button", { type: "button", class: "filter-chip", "aria-pressed": String(c.id === filter) }, c.label);
    chip.addEventListener("click", () => {
      filter = c.id;
      chips.forEach((other, i) => other.setAttribute("aria-pressed", String(chipDefs[i].id === filter)));
      render();
    });
    return chip;
  });

  const search = h("input", {
    type: "search",
    class: "games-search",
    placeholder: t("Buscar um jogo (ex.: sudoku, frações)"),
    "aria-label": t("Buscar um jogo"),
    autocomplete: "off",
    enterkeyhint: "search",
  });
  search.addEventListener("input", () => {
    query = search.value;
    render();
  });

  const statsToggle = h(
    "label",
    { class: "games-stats-toggle" },
    h("span", {}, t("Mostrar estatísticas")),
    h("input", {
      type: "checkbox",
      role: "switch",
      class: "switch",
      checked: getPrefs().gameStats,
      onChange: (e) => {
        setPrefs({ gameStats: e.target.checked });
        render();
      },
    }),
  );

  render();

  return h(
    "section",
    { class: "page games-page" },
    h(
      "header",
      { class: "games-head" },
      h("h1", {}, t("🎮 Jogos")),
      h("p", { class: "muted" }, t("Escolha um jogo e jogue quanto quiser. Cada partida tem {n} desafios, no seu ritmo.", { n: data.problemsPerGame })),
    ),
    search,
    h("div", { class: "games-toolbar" }, h("div", { class: "filter-chips", role: "group", "aria-label": t("Filtrar jogos") }, chips), statsToggle),
    error,
    content,
  );
}
