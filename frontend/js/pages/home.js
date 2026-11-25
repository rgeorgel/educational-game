import { api } from "../api.js";
import { h } from "../dom.js";
import { store } from "../store.js";
import { avatarView, levelProgress, startAndPlay } from "./shared.js";
import { dueText, starsText } from "./assignments.js";
import { t, tn } from "../i18n.js";

export async function homePage() {
  const profile = store.profile;
  const [data, daily, tasks, goals] = await Promise.all([
    api.progress(profile.id),
    api.daily(profile.id),
    store.state.me?.isGuest ? [] : api.profileAssignments(profile.id).catch(() => []),
    api.goals(profile.id).catch(() => []),
  ]);
  const error = h("p", { class: "form-error", role: "alert", hidden: true });

  const dailyCards = daily.map((d) => {
    if (d.done) {
      return h(
        "div",
        { class: "daily-item done" },
        h("span", { class: "daily-icon", "aria-hidden": "true" }, d.icon),
        h("span", {}, h("strong", {}, d.name), h("span", { class: "muted small" }, ` · ${d.correct ? t("✅ vencido hoje") : t("feito hoje")}`)),
      );
    }
    const btn = h("button", { type: "button", class: "btn btn-primary btn-small" }, t("Jogar"));
    btn.addEventListener("click", () => startAndPlay(() => api.startDaily(profile.id, d.slug), btn, error));
    return h(
      "div",
      { class: "daily-item" },
      h("span", { class: "daily-icon", "aria-hidden": "true" }, d.icon),
      h("span", {}, h("strong", {}, d.name), h("span", { class: "muted small" }, ` · ${t("1 problema, XP em dobro")}`)),
      btn,
    );
  });

  return h(
    "section",
    { class: "page" },
    h("div", { class: "greeting" }, h("a", { href: "#/avatar", title: t("Personalizar avatar") }, avatarView(profile, "lg")), h("h1", {}, t("Olá, {nome}!", { nome: profile.nickname }))),
    store.state.me?.isGuest
      ? h(
          "div",
          { class: "card guest-banner" },
          h("p", {}, h("strong", {}, `${t("Você está jogando sem conta.")} `), t("O progresso fica só neste aparelho.")),
          h("a", { href: "#/salvar", class: "btn btn-primary btn-small" }, t("💾 Salvar progresso")),
        )
      : null,
    goals.some((g) => !g.completedAt) ? goalsCard(goals.filter((g) => !g.completedAt), profile.id, error) : null,
    tasks.length ? tasksCard(tasks, profile.id, error) : null,
    h("section", { class: "card daily" }, h("h2", {}, t("📆 Desafio do dia")), h("div", { class: "daily-list" }, dailyCards), error),
    h("p", { class: "muted" }, t("Escolha uma trilha para continuar a expedição.")),
    h(
      "div",
      { class: "subject-grid stagger" },
      data.subjects.map((s) =>
        h(
          "a",
          { class: "card subject-card", href: `#/mapa/${s.slug}`, dataset: { subject: s.slug } },
          h("div", { class: "subject-head" }, h("span", { class: "subject-icon", "aria-hidden": "true" }, s.icon), h("h2", {}, s.name)),
          levelProgress(s),
          s.guardianAvailable ? h("p", { class: "callout" }, t("🛡️ O Desafio do Guardião está esperando por você!")) : null,
          s.placementAvailable ? h("p", { class: "callout" }, t("🧭 Faça o teste de nivelamento para começar no nível certo.")) : null,
          s.needsReviewSkills > 0 ? h("p", { class: "callout" }, `🧩 ${tn(s.needsReviewSkills, "{n} habilidade pede revisão.", "{n} habilidades pedem revisão.")}`) : null,
          h("p", { class: "subject-xp" }, `⚡ ${s.xp} XP`),
          h("span", { class: "btn btn-primary" }, t("Jogar")),
        ),
      ),
    ),
    h(
      "div",
      { class: "totals stagger" },
      h("div", { class: "tile" }, h("strong", {}, `🔥 ${data.totals.streak}`), h("span", {}, data.totals.streak === 1 ? t("dia de prática") : t("dias de prática"))),
      h("div", { class: "tile" }, h("strong", {}, data.totals.problemsSolved), h("span", {}, t("problemas resolvidos"))),
      h("div", { class: "tile" }, h("strong", {}, `🏅 ${data.totals.achievements}`), h("span", {}, t("medalhas"))),
      h("a", { class: "tile tile-link", href: "#/progresso" }, h("strong", {}, "📈"), h("span", {}, t("Ver meu progresso"))),
    ),
  );
}

/** Tarefas que o professor passou para a turma do aluno. */
function tasksCard(tasks, profileId, error) {
  const pending = tasks.filter((task) => !task.done).length;
  return h(
    "section",
    { class: "card daily tasks" },
    h("h2", {}, t("📚 Tarefas da turma"), pending ? h("span", { class: "badge-count" }, String(pending)) : null),
    h(
      "div",
      { class: "daily-list" },
      tasks.map((task) => {
        const info = `${task.classroomName} · ${starsText(task.minStars)} · ${dueText(task.dueDate)}${task.overdue ? ` · ${t("atrasada")}` : ""}`;
        if (task.done) {
          return h("div", { class: "daily-item done" }, h("span", { class: "daily-icon", "aria-hidden": "true" }, "✅"), h("span", {}, h("strong", {}, task.stage.name), h("span", { class: "muted small" }, ` · ${info}`)));
        }
        const btn = h("button", { type: "button", class: "btn btn-primary btn-small" }, t("Jogar"));
        btn.addEventListener("click", () => startAndPlay(() => api.startSession(profileId, task.stage.id), btn, error));
        return h(
          "div",
          { class: "daily-item" },
          h("span", { class: "daily-icon", "aria-hidden": "true" }, task.stage.subjectIcon),
          h("span", {}, h("strong", {}, task.stage.name), h("span", { class: "muted small" }, ` · ${info}`), task.note ? h("span", { class: "small task-note" }, `“${task.note}”`) : null),
          btn,
        );
      }),
    ),
  );
}

/** Treinos de reforço que o responsável escolheu (plano Família). */
function goalsCard(goals, profileId, error) {
  return h(
    "section",
    { class: "card daily tasks" },
    h("h2", {}, t("🎯 Treino de reforço")),
    h("p", { class: "muted small" }, t("Seu responsável escolheu para você treinar. Ganhe 2 estrelas para concluir!")),
    h(
      "div",
      { class: "daily-list" },
      goals.map((g) => {
        const btn = h("button", { type: "button", class: "btn btn-primary btn-small" }, t("Jogar"));
        btn.addEventListener("click", () => startAndPlay(() => api.startGoal(profileId, g.id), btn, error));
        return h(
          "div",
          { class: "daily-item" },
          h("span", { class: "daily-icon", "aria-hidden": "true" }, g.skill.subjectIcon),
          h("span", {}, h("strong", {}, g.skill.name), h("span", { class: "muted small" }, ` · ${g.skill.levelName} · ${t("8 problemas")}`)),
          btn,
        );
      }),
    ),
  );
}
