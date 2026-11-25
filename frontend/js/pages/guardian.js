import { api } from "../api.js";
import { errorMessage, h } from "../dom.js";
import { store } from "../store.js";
import { avatarView, levelBadge } from "./shared.js";
import { planPromo } from "./plans.js";
import { locale, t } from "../i18n.js";

const dateFmt = new Intl.DateTimeFormat(locale(), { day: "numeric", month: "long" });

function lastActiveText(date) {
  if (!date) return t("Ainda não jogou");
  const d = new Date(`${date}T12:00:00`);
  const today = new Date();
  const diff = Math.round((new Date(today.toDateString()) - new Date(d.toDateString())) / 86400000);
  if (diff === 0) return t("Jogou hoje");
  if (diff === 1) return t("Jogou ontem");
  return t("Jogou pela última vez em {data}", { data: dateFmt.format(d) });
}

/** Cartão com o resumo de um perfil (usado pelo responsável e pelo professor). */
export function profileReportCard(r, extra = null) {
  const accuracy = r.lastSevenDays.problems ? Math.round((r.lastSevenDays.correct / r.lastSevenDays.problems) * 100) : null;
  return h(
    "article",
    { class: "card report-card" },
    h(
      "header",
      { class: "report-head" },
      avatarView(r, "lg"),
      h("div", {}, h("h2", {}, r.nickname), h("p", { class: "muted small" }, lastActiveText(r.lastActive))),
    ),
    h(
      "div",
      { class: "report-stats" },
      h("div", {}, h("strong", {}, `🔥 ${r.streak}`), h("span", { class: "muted small" }, t("dias seguidos"))),
      h("div", {}, h("strong", {}, r.lastSevenDays.problems), h("span", { class: "muted small" }, t("problemas em 7 dias"))),
      h("div", {}, h("strong", {}, accuracy === null ? "—" : `${accuracy}%`), h("span", { class: "muted small" }, t("acertos em 7 dias"))),
      h("div", {}, h("strong", {}, r.lastSevenDays.activeDays), h("span", { class: "muted small" }, t("dias ativos"))),
    ),
    h(
      "ul",
      { class: "report-subjects" },
      r.subjects.map((s) =>
        h(
          "li",
          {},
          h("div", { class: "report-subject-head" }, h("strong", {}, `${s.icon} ${s.name}`), levelBadge(s.level)),
          h("progress-bar", { value: String(s.percent), label: t("{nome}: {p}% para o próximo nível", { nome: s.name, p: s.percent }), level: s.level.slug }),
          h("p", { class: "muted small" }, `${t("{a} de {b} habilidades dominadas", { a: s.masteredSkills, b: s.totalSkills })} · ${s.xp} XP`),
          s.needsAttention.length
            ? h("p", { class: "attention" }, t("Pede atenção: "), s.needsAttention.join(", "))
            : null,
        ),
      ),
    ),
    extra,
  );
}

function classroomManager(profileId) {
  if (store.state.me?.isGuest) {
    return h(
      "p",
      { class: "muted small" },
      t("Para participar de turmas da escola, um responsável precisa "),
      h("a", { href: "#/salvar" }, t("salvar o progresso com uma conta")),
      ".",
    );
  }
  const list = h("ul", { class: "class-list" });
  const error = h("p", { class: "form-error", role: "alert", hidden: true });
  const input = h("input", { name: "code", maxlength: "12", autocomplete: "off", placeholder: t("Código da turma"), "aria-label": t("Código da turma") });

  async function refresh() {
    const items = await api.profileClassrooms(profileId);
    list.replaceChildren(
      ...(items.length
        ? items.map((c) => {
            const leave = h("button", { type: "button", class: "btn btn-ghost btn-small" }, t("Sair da turma"));
            leave.addEventListener("click", async () => {
              await api.leaveClassroom(profileId, c.id);
              refresh();
            });
            return h("li", {}, h("span", {}, `🏫 ${c.name}`), leave);
          })
        : [h("li", { class: "muted small" }, t("Não participa de nenhuma turma."))]),
    );
  }

  const form = h(
    "form",
    {
      class: "inline-form",
      onSubmit: async (e) => {
        e.preventDefault();
        error.hidden = true;
        try {
          await api.joinClassroom(profileId, input.value);
          input.value = "";
          refresh();
        } catch (err) {
          error.textContent = errorMessage(err);
          error.hidden = false;
        }
      },
    },
    input,
    h("button", { type: "submit", class: "btn btn-small" }, t("Entrar na turma")),
  );

  refresh();
  return h(
    "details",
    { class: "class-manager" },
    h("summary", {}, t("Turmas da escola")),
    h("p", { class: "muted small" }, t("Ao entrar numa turma, o professor passa a ver o apelido e o progresso deste perfil (nunca o seu e-mail). Você pode sair quando quiser.")),
    list,
    form,
    error,
  );
}

/** Atalhos do plano Família em cada perfil. */
function familyActions(r) {
  return h(
    "div",
    { class: "actions-left family-actions" },
    h("a", { href: `#/semana/${r.id}`, class: "btn btn-small" }, t("📊 Relatório da semana")),
    h("a", { href: `#/semana/${r.id}`, class: "btn btn-small btn-ghost" }, t("🎯 Treino de reforço")),
  );
}

export async function guardianPage() {
  const guest = store.state.me?.isGuest;
  const [reports, plan, billing] = await Promise.all([
    api.guardianReport(),
    guest ? null : api.myPlan().catch(() => null),
    guest ? null : api.billingStatus().catch(() => null),
  ]);
  return h(
    "section",
    { class: "page" },
    h("h1", {}, t("Área do responsável")),
    h(
      "p",
      { class: "muted" },
      t("Acompanhe o progresso de cada perfil. "),
      h("a", { href: "#/turmas" }, t("É professor? Crie e acompanhe suas turmas.")),
      " ",
      h("a", { href: "#/bncc" }, t("Veja o currículo do jogo alinhado à BNCC.")),
      " ",
      h("a", { href: "#/fichas" }, t("Fichas para imprimir.")),
    ),
    planPromo(plan, "familia", billing),
    reports.length
      ? h("div", { class: "report-grid" }, reports.map((r) => profileReportCard(r, [familyActions(r), classroomManager(r.id)])))
      : h("p", { class: "card" }, t("Nenhum perfil ainda. "), h("a", { href: "#/perfis" }, t("Crie o primeiro perfil."))),
  );
}
