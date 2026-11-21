// Relatório da semana de um perfil (plano Família) e criação de treinos de reforço.
import { api } from "../api.js";
import { historyChart } from "../components/history-chart.js";
import { errorMessage, h } from "../dom.js";
import { navigate } from "../router.js";
import { lockedCard } from "./plans.js";
import { avatarView } from "./shared.js";

const pct = (c, t) => (t ? `${Math.round((c / t) * 100)}%` : "—");
const STATUS = { Mastered: "dominada", Practicing: "praticando", NeedsReview: "pede revisão", NotStarted: "não iniciada" };

function compare(now, before, unit = "") {
  if (!before && !now) return h("span", { class: "muted small" }, "—");
  if (!before) return h("span", { class: "delta up small" }, "novo nesta semana");
  const diff = now - before;
  const cls = diff > 0 ? "up" : diff < 0 ? "down" : "flat";
  const arrow = diff > 0 ? "▲" : diff < 0 ? "▼" : "■";
  return h("span", { class: `delta ${cls} small` }, `${arrow} ${diff > 0 ? "+" : ""}${diff}${unit} vs. semana anterior`);
}

function stat(label, value, delta) {
  return h("div", { class: "tile" }, h("strong", {}, value), h("span", {}, label), delta);
}

function goalButton(profileId, skill, existingGoals, onError) {
  if (existingGoals.some((g) => g.skill.id === skill.id && !g.completedAt)) return h("span", { class: "muted small" }, "🎯 treino criado");
  const btn = h("button", { type: "button", class: "btn btn-small" }, "🎯 Criar treino");
  btn.addEventListener("click", async () => {
    btn.disabled = true;
    try {
      await api.createGoal(profileId, skill.slug, skill.subjectSlug);
      navigate(`/semana/${profileId}`);
    } catch (err) {
      onError(err);
      btn.disabled = false;
    }
  });
  return btn;
}

export async function weeklyPage({ profileId }) {
  let r;
  try {
    r = await api.weeklyReport(profileId);
  } catch (err) {
    const locked = lockedCard(err);
    if (!locked) throw err;
    return h("section", { class: "page-narrow" }, h("p", {}, h("a", { href: "#/responsavel" }, "← Área do responsável")), h("h1", {}, "Relatório da semana"), locked);
  }
  const error = h("p", { class: "form-error", role: "alert", hidden: true });
  const showError = (err) => {
    error.replaceChildren();
    const locked = lockedCard(err);
    if (locked) error.append(locked);
    else error.textContent = errorMessage(err);
    error.hidden = false;
  };
  const t = r.thisWeek;
  const l = r.lastWeek;
  const dateFmt = new Intl.DateTimeFormat("pt-BR", { day: "2-digit", month: "2-digit", timeZone: "UTC" });
  const fmt = (d) => dateFmt.format(new Date(`${d}T00:00:00Z`));

  const removeGoal = (g) => {
    const btn = h("button", { type: "button", class: "btn btn-ghost btn-small" }, "Apagar");
    btn.addEventListener("click", async () => {
      await api.deleteGoal(profileId, g.id);
      navigate(`/semana/${profileId}`);
    });
    return btn;
  };

  return h(
    "section",
    { class: "page" },
    h("p", {}, h("a", { href: "#/responsavel" }, "← Área do responsável")),
    h("div", { class: "greeting" }, avatarView({ avatar: r.avatar, nickname: r.nickname }, "lg"), h("div", {}, h("h1", {}, `Semana de ${r.nickname}`), h("p", { class: "muted small" }, `${fmt(r.from)} a ${fmt(r.to)}`))),
    r.tips.length ? h("section", { class: "card tips" }, h("h2", {}, "💡 Resumo"), h("ul", {}, r.tips.map((tip) => h("li", {}, tip)))) : null,
    h(
      "div",
      { class: "totals" },
      stat("dias com prática", `${t.activeDays} de 7`, compare(t.activeDays, l.activeDays)),
      stat("problemas", String(t.problems), compare(t.problems, l.problems)),
      stat("de acertos", pct(t.correct, t.problems), null),
      stat("minutos jogando", String(t.minutes), compare(t.minutes, l.minutes, " min")),
      stat("XP", `⚡ ${t.xp}`, null),
    ),
    h("section", { class: "card" }, h("h2", {}, "Problemas por dia"), historyChart(r.days)),
    h(
      "section",
      { class: "card stack" },
      h("h2", {}, "🎯 Treinos de reforço"),
      h("p", { class: "muted small" }, "Escolha uma habilidade para treinar. Ela aparece na tela inicial da criança com o botão \"Jogar\" e conta como concluída com 2 estrelas."),
      r.goals.length
        ? h(
            "ul",
            { class: "goal-list" },
            r.goals.map((g) =>
              h(
                "li",
                {},
                h("span", {}, h("strong", {}, `${g.skill.subjectIcon} ${g.skill.name}`), h("span", { class: "muted small" }, g.completedAt ? " · ✅ concluído" : ` · em aberto${g.attempts ? ` · ${g.attempts} ${g.attempts === 1 ? "tentativa" : "tentativas"}` : ""}`)),
                g.completedAt ? null : removeGoal(g),
              ),
            ),
          )
        : h("p", { class: "muted small" }, "Nenhum treino ainda."),
      error,
    ),
    h(
      "section",
      { class: "card" },
      h("h2", {}, "Pede atenção"),
      r.attention.length
        ? h(
            "ul",
            { class: "goal-list" },
            r.attention.map((s) =>
              h(
                "li",
                {},
                h("span", {}, h("strong", {}, `${s.subjectIcon} ${s.name}`), h("span", { class: "muted small" }, ` · ${s.levelName}${s.bncc.length ? ` · BNCC ${s.bncc.map((b) => b.code).join(", ")}` : ""}`)),
                goalButton(profileId, s, r.goals, showError),
              ),
            ),
          )
        : h("p", { class: "muted small" }, "Nada pedindo atenção agora. 👏"),
    ),
    h(
      "section",
      { class: "card" },
      h("h2", {}, "O que praticou na semana"),
      r.skills.length
        ? h(
            "div",
            { class: "table-scroll" },
            h(
              "table",
              { class: "class-skills" },
              h("thead", {}, h("tr", {}, h("th", {}, "Habilidade"), h("th", { class: "n" }, "Respostas"), h("th", { class: "n" }, "Acertos"), h("th", { class: "n" }, "Evolução"), h("th", {}, ""))),
              h(
                "tbody",
                {},
                r.skills.map((s) =>
                  h(
                    "tr",
                    {},
                    h("td", {}, h("strong", {}, `${s.skill.subjectIcon} ${s.skill.name}`), h("div", { class: "muted small" }, `${s.skill.levelName} · ${STATUS[s.status] ?? s.status}`)),
                    h("td", { class: "n" }, s.attempts),
                    h("td", { class: "n" }, pct(s.correct, s.attempts)),
                    h("td", { class: "n" }, s.ratingChange > 0 ? `▲ +${s.ratingChange}` : s.ratingChange < 0 ? `▼ ${s.ratingChange}` : "■ 0"),
                    h("td", {}, goalButton(profileId, s.skill, r.goals, showError)),
                  ),
                ),
              ),
            ),
          )
        : h("p", { class: "muted small" }, "Ainda não jogou nesta semana."),
    ),
    r.medals.length ? h("section", { class: "card" }, h("h2", {}, "🏅 Medalhas da semana"), h("p", {}, r.medals.map((m) => `${m.icon} ${m.name}`).join(" · "))) : null,
    h("p", { class: "muted small" }, "🖨️ Quer praticar no papel? ", h("a", { href: "#/fichas" }, "Gere uma ficha para imprimir"), "."),
  );
}
