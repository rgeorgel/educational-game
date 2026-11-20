import { api } from "../api.js";
import { errorMessage, formData, h } from "../dom.js";
import { navigate } from "../router.js";
import { store } from "../store.js";
import { profileReportCard } from "./guardian.js";
import { assignmentsSection, exportSection, planBanner, skillsSection } from "./assignments.js";

export async function classroomsPage() {
  if (store.state.me?.isGuest) {
    return h(
      "section",
      { class: "page-narrow" },
      h("h1", {}, "Minhas turmas"),
      h("p", { class: "card" }, "Para criar turmas, primeiro ", h("a", { href: "#/salvar" }, "crie uma conta"), ". Contas de convidado não podem criar turmas."),
    );
  }
  const [classes, plan] = await Promise.all([api.classrooms(), api.myPlan().catch(() => null)]);
  const error = h("p", { class: "form-error", role: "alert", hidden: true });

  const form = h(
    "form",
    {
      class: "inline-form",
      onSubmit: async (e) => {
        e.preventDefault();
        error.hidden = true;
        try {
          const created = await api.createClassroom(formData(form).name);
          navigate(`/turmas/${created.id}`);
        } catch (err) {
          error.textContent = errorMessage(err);
          error.hidden = false;
        }
      },
    },
    h("input", { name: "name", maxlength: "60", placeholder: "Ex.: 5º ano B", "aria-label": "Nome da turma", required: true }),
    h("button", { type: "submit", class: "btn btn-primary btn-small" }, "Criar turma"),
  );

  return h(
    "section",
    { class: "page-narrow" },
    h("h1", {}, "Minhas turmas"),
    h("p", { class: "muted" }, "Crie uma turma e passe o código para as famílias. Cada responsável decide se o perfil do filho entra na turma."),
    h("p", { class: "muted small" }, "Todas as habilidades do jogo estão ligadas à BNCC: ", h("a", { href: "#/bncc" }, "ver currículo e BNCC"), "."),
    planBanner(plan),
    h("div", { class: "card stack" }, form, error),
    classes.length
      ? h(
          "ul",
          { class: "class-cards" },
          classes.map((c) =>
            h(
              "li",
              {},
              h(
                "a",
                { class: "card class-card", href: `#/turmas/${c.id}` },
                h("strong", {}, `🏫 ${c.name}`),
                h("span", { class: "muted small" }, `${c.members} ${c.members === 1 ? "aluno" : "alunos"} · código `, h("code", {}, c.code)),
              ),
            ),
          ),
        )
      : h("p", { class: "muted" }, "Você ainda não tem turmas."),
  );
}

export async function classroomPage({ id }) {
  const report = await api.classroomReport(id);
  const assignments = await assignmentsSection(report);
  const error = h("p", { class: "form-error", role: "alert", hidden: true });

  const del = h("button", { type: "button", class: "btn btn-danger btn-small" }, "Apagar turma");
  del.addEventListener("click", async () => {
    if (!confirm(`Apagar a turma "${report.name}"? Os perfis dos alunos não são apagados.`)) return;
    try {
      await api.deleteClassroom(id);
      navigate("/turmas");
    } catch (err) {
      error.textContent = errorMessage(err);
      error.hidden = false;
    }
  });

  const students = report.students.map((s) => {
    const remove = h("button", { type: "button", class: "btn btn-ghost btn-small" }, "Remover da turma");
    remove.addEventListener("click", async () => {
      if (!confirm(`Remover ${s.nickname} da turma?`)) return;
      await api.removeStudent(id, s.id);
      navigate(`/turmas/${id}`);
    });
    return profileReportCard(s, h("div", { class: "actions-left" }, remove));
  });

  const attention = report.students.flatMap((s) => s.subjects.flatMap((sub) => sub.attentionSkills ?? sub.needsAttention.map((name) => ({ name, bncc: [] }))));
  const bnccBySkill = Object.fromEntries(attention.map((a) => [a.name, a.bncc]));
  const counts = Object.entries(attention.reduce((acc, a) => ({ ...acc, [a.name]: (acc[a.name] ?? 0) + 1 }), {})).sort((a, b) => b[1] - a[1]).slice(0, 5);

  return h(
    "section",
    { class: "page" },
    h("p", {}, h("a", { href: "#/turmas" }, "← Minhas turmas")),
    h("h1", {}, `🏫 ${report.name}`),
    h(
      "div",
      { class: "card class-code" },
      h("p", { class: "muted small" }, "Código para as famílias entrarem na turma:"),
      h("p", { class: "code-big" }, report.code),
      h("p", { class: "muted small" }, "Na Área do responsável, em cada perfil: \"Turmas da escola\" → digite o código."),
    ),
    assignments,
    counts.length
      ? h("section", { class: "card" }, h("h2", {}, "O que a turma mais precisa praticar"), h("ul", {}, counts.map(([skill, n]) => h("li", {}, `${skill} — ${n} ${n === 1 ? "aluno" : "alunos"}`, bnccBySkill[skill]?.length ? h("span", { class: "muted small" }, ` · BNCC: ${bnccBySkill[skill].join(", ")}`) : null))))
      : null,
    skillsSection(report),
    students.length ? h("div", { class: "report-grid" }, students) : h("p", { class: "card muted" }, "Nenhum aluno entrou ainda."),
    exportSection(report),
    error,
    h("div", { class: "actions-left" }, del),
  );
}
