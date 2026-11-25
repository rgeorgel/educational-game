// Relatório da semana de um perfil (plano Família) e criação de treinos de reforço.
import { api } from "../api.js";
import { historyChart } from "../components/history-chart.js";
import { errorMessage, h } from "../dom.js";
import { navigate } from "../router.js";
import { lockedCard } from "./plans.js";
import { avatarView } from "./shared.js";
import { locale, t, tn } from "../i18n.js";

const pct = (c, total) => (total ? `${Math.round((c / total) * 100)}%` : "—");
const STATUS = { Mastered: t("dominada"), Practicing: t("praticando"), NeedsReview: t("pede revisão"), NotStarted: t("não iniciada") };

function compare(now, before, unit = "") {
  if (!before && !now) return h("span", { class: "muted small" }, "—");
  if (!before) return h("span", { class: "delta up small" }, t("novo nesta semana"));
  const diff = now - before;
  const cls = diff > 0 ? "up" : diff < 0 ? "down" : "flat";
  const arrow = diff > 0 ? "▲" : diff < 0 ? "▼" : "■";
  return h("span", { class: `delta ${cls} small` }, `${arrow} ${diff > 0 ? "+" : ""}${diff}${unit} ${t("vs. semana anterior")}`);
}

function stat(label, value, delta) {
  return h("div", { class: "tile" }, h("strong", {}, value), h("span", {}, label), delta);
}

function goalButton(profileId, skill, existingGoals, onError) {
  if (existingGoals.some((g) => g.skill.id === skill.id && !g.completedAt)) return h("span", { class: "muted small" }, t("🎯 treino criado"));
  const btn = h("button", { type: "button", class: "btn btn-small" }, t("🎯 Criar treino"));
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
    return h("section", { class: "page-narrow" }, h("p", {}, h("a", { href: "#/responsavel" }, t("← Área do responsável"))), h("h1", {}, t("Relatório da semana")), locked);
  }
  const error = h("p", { class: "form-error", role: "alert", hidden: true });
  const showError = (err) => {
    error.replaceChildren();
    const locked = lockedCard(err);
    if (locked) error.append(locked);
    else error.textContent = errorMessage(err);
    error.hidden = false;
  };
  const week = r.thisWeek;
  const l = r.lastWeek;
  const dateFmt = new Intl.DateTimeFormat(locale(), { day: "2-digit", month: "2-digit", timeZone: "UTC" });
  const fmt = (d) => dateFmt.format(new Date(`${d}T00:00:00Z`));

  const removeGoal = (g) => {
    const btn = h("button", { type: "button", class: "btn btn-ghost btn-small" }, t("Apagar"));
    btn.addEventListener("click", async () => {
      await api.deleteGoal(profileId, g.id);
      navigate(`/semana/${profileId}`);
    });
    return btn;
  };

  return h(
    "section",
    { class: "page" },
    h("p", {}, h("a", { href: "#/responsavel" }, t("← Área do responsável"))),
    h("div", { class: "greeting" }, avatarView({ avatar: r.avatar, nickname: r.nickname }, "lg"), h("div", {}, h("h1", {}, t("Semana de {nome}", { nome: r.nickname })), h("p", { class: "muted small" }, t("{de} a {ate}", { de: fmt(r.from), ate: fmt(r.to) })))),
    r.tips.length ? h("section", { class: "card tips" }, h("h2", {}, t("💡 Resumo")), h("ul", {}, r.tips.map((tip) => h("li", {}, tip)))) : null,
    h(
      "div",
      { class: "totals" },
      stat(t("dias com prática"), t("{a} de {b}", { a: week.activeDays, b: 7 }), compare(week.activeDays, l.activeDays)),
      stat(t("problemas"), String(week.problems), compare(week.problems, l.problems)),
      stat(t("de acertos"), pct(week.correct, week.problems), null),
      stat(t("minutos jogando"), String(week.minutes), compare(week.minutes, l.minutes, " min")),
      stat("XP", `⚡ ${week.xp}`, null),
    ),
    h("section", { class: "card" }, h("h2", {}, t("Problemas por dia")), historyChart(r.days)),
    h(
      "section",
      { class: "card stack" },
      h("h2", {}, t("🎯 Treinos de reforço")),
      h("p", { class: "muted small" }, t("Escolha uma habilidade para treinar. Ela aparece na tela inicial da criança com o botão \"Jogar\" e conta como concluída com 2 estrelas.")),
      r.goals.length
        ? h(
            "ul",
            { class: "goal-list" },
            r.goals.map((g) =>
              h(
                "li",
                {},
                h("span", {}, h("strong", {}, `${g.skill.subjectIcon} ${g.skill.name}`), h("span", { class: "muted small" }, g.completedAt ? ` · ✅ ${t("concluído")}` : ` · ${t("em aberto")}${g.attempts ? ` · ${tn(g.attempts, "{n} tentativa", "{n} tentativas")}` : ""}`)),
                g.completedAt ? null : removeGoal(g),
              ),
            ),
          )
        : h("p", { class: "muted small" }, t("Nenhum treino ainda.")),
      error,
    ),
    h(
      "section",
      { class: "card" },
      h("h2", {}, t("Pede atenção")),
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
        : h("p", { class: "muted small" }, t("Nada pedindo atenção agora. 👏")),
    ),
    h(
      "section",
      { class: "card" },
      h("h2", {}, t("O que praticou na semana")),
      r.skills.length
        ? h(
            "div",
            { class: "table-scroll" },
            h(
              "table",
              { class: "class-skills" },
              h("thead", {}, h("tr", {}, h("th", {}, t("Habilidade")), h("th", { class: "n" }, t("Respostas")), h("th", { class: "n" }, t("Acertos")), h("th", { class: "n" }, t("Evolução")), h("th", {}, ""))),
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
        : h("p", { class: "muted small" }, t("Ainda não jogou nesta semana.")),
    ),
    r.medals.length ? h("section", { class: "card" }, h("h2", {}, t("🏅 Medalhas da semana")), h("p", {}, r.medals.map((m) => `${m.icon} ${m.name}`).join(" · "))) : null,
    h("p", { class: "muted small" }, t("🖨️ Quer praticar no papel? "), h("a", { href: "#/fichas" }, t("Gere uma ficha para imprimir")), "."),
  );
}
