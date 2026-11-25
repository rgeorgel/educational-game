// Tarefas da turma (professor) e relatório por habilidade/BNCC, mais a exportação em planilha.
import { api } from "../api.js";
import { errorMessage, h } from "../dom.js";
import { navigate } from "../router.js";
import { fmtDate, getLang, locale, t, tn } from "../i18n.js";

const dateFmt = new Intl.DateTimeFormat(locale(), { day: "2-digit", month: "2-digit", timeZone: "UTC" });
export const dueText = (d) => (d ? t("até {data}", { data: dateFmt.format(new Date(`${d}T00:00:00Z`)) }) : t("sem prazo"));
export const starsText = (n) => "★".repeat(n) + "☆".repeat(3 - n);
const STATUS = { feita: t("✅ feita"), tentou: t("🟨 tentou"), pendente: t("⬜ não começou") };

function todayIso() {
  const d = new Date();
  return new Date(d.getTime() - d.getTimezoneOffset() * 60000).toISOString().slice(0, 10);
}

/** Formulário "passar tarefa": etapa (agrupada por matéria e nível), estrelas, prazo e recado. */
async function assignmentForm(classroomId, onError) {
  const stages = await api.stageCatalog();
  const groups = new Map();
  for (const s of stages) {
    const key = `${s.subjectIcon} ${s.subjectName} · ${s.levelIcon} ${s.levelName}`;
    if (!groups.has(key)) groups.set(key, []);
    groups.get(key).push(s);
  }
  const stageSelect = h(
    "select",
    { name: "stageId", required: true, "aria-label": t("Etapa") },
    h("option", { value: "" }, t("Escolha a etapa…")),
    [...groups].map(([label, list]) => h("optgroup", { label }, list.map((s) => h("option", { value: String(s.id) }, `${s.regionIcon} ${s.regionName} — ${s.name}`)))),
  );
  const starsSelect = h(
    "select",
    { name: "minStars", "aria-label": t("Estrelas mínimas") },
    [1, 2, 3].map((n) => h("option", { value: String(n) }, `${starsText(n)} (${tn(n, "{n} estrela", "{n} estrelas")})`)),
  );
  const due = h("input", { type: "date", name: "dueDate", min: todayIso(), "aria-label": t("Prazo (opcional)") });
  const note = h("input", { name: "note", maxlength: "140", placeholder: t("Recado para a turma (opcional)"), "aria-label": t("Recado") });
  const submit = h("button", { type: "submit", class: "btn btn-primary btn-small" }, t("Passar tarefa"));

  const form = h(
    "form",
    {
      class: "assignment-form",
      onSubmit: async (e) => {
        e.preventDefault();
        submit.disabled = true;
        try {
          await api.createAssignment(classroomId, {
            stageId: Number(stageSelect.value),
            minStars: Number(starsSelect.value),
            dueDate: due.value || null,
            note: note.value.trim() || null,
          });
          navigate(`/turmas/${classroomId}`);
        } catch (err) {
          onError(errorMessage(err));
          submit.disabled = false;
        }
      },
    },
    h("label", {}, h("span", { class: "small muted" }, t("Etapa")), stageSelect),
    h("div", { class: "assignment-form-row" }, h("label", {}, h("span", { class: "small muted" }, t("Para contar como feita")), starsSelect), h("label", {}, h("span", { class: "small muted" }, t("Prazo (opcional)")), due)),
    note,
    submit,
  );
  return form;
}

function assignmentItem(classroomId, a) {
  const remove = h("button", { type: "button", class: "btn btn-ghost btn-small" }, t("Apagar"));
  remove.addEventListener("click", async () => {
    if (!confirm(t("Apagar a tarefa \"{nome}\"?", { nome: a.stage.name }))) return;
    await api.deleteAssignment(classroomId, a.id);
    navigate(`/turmas/${classroomId}`);
  });
  const pct = a.total ? Math.round((a.done / a.total) * 100) : 0;
  return h(
    "li",
    { class: `assignment${a.active ? "" : " assignment-closed"}` },
    h(
      "div",
      { class: "assignment-head" },
      h("div", {}, h("strong", {}, `${a.stage.subjectIcon} ${a.stage.name}`), h("p", { class: "muted small" }, `${a.stage.regionName} · ${a.stage.levelName} · ${starsText(a.minStars)} · ${dueText(a.dueDate)}${a.active ? "" : ` (${t("encerrada")})`}`)),
      remove,
    ),
    a.note ? h("p", { class: "small" }, `“${a.note}”`) : null,
    h("progress-bar", { value: String(pct), label: t("{a} de {b} alunos fizeram", { a: a.done, b: a.total }) }),
    h(
      "details",
      {},
      h("summary", { class: "small" }, a.total === 1 ? t("{a} de {b} aluno fez", { a: a.done, b: a.total }) : t("{a} de {b} alunos fizeram", { a: a.done, b: a.total })),
      a.students.length
        ? h("ul", { class: "assignment-students" }, a.students.map((s) => h("li", {}, `${s.avatar} ${s.nickname}`, h("span", { class: "muted small" }, ` · ${STATUS[s.status]}${s.status === "tentou" ? ` (${starsText(s.bestStars)})` : ""}`))))
        : h("p", { class: "muted small" }, t("Ainda não há alunos na turma.")),
    ),
  );
}

export async function assignmentsSection(report) {
  const error = h("p", { class: "form-error", role: "alert", hidden: true });
  const showError = (text) => {
    error.textContent = text;
    error.hidden = false;
  };
  const form = await assignmentForm(report.id, showError);
  return h(
    "section",
    { class: "card stack" },
    h("h2", {}, t("📚 Tarefas")),
    h("p", { class: "muted small" }, t("Peça uma etapa para a turma. Os alunos veem a tarefa na tela inicial e podem jogá-la mesmo que ainda não tenham chegado nela no mapa.")),
    form,
    error,
    report.assignments.length ? h("ul", { class: "assignment-list" }, report.assignments.map((a) => assignmentItem(report.id, a))) : h("p", { class: "muted small" }, t("Nenhuma tarefa ainda.")),
  );
}

export function skillsSection(report) {
  if (!report.skills.length) return null;
  const rows = report.skills.map((s) =>
    h(
      "tr",
      {},
      h("td", {}, h("strong", {}, `${s.subjectIcon} ${s.name}`), h("div", { class: "muted small" }, s.levelName), s.bncc.length ? h("div", { class: "small bncc-inline" }, s.bncc.join(", ")) : null),
      h("td", { class: "n" }, s.practiced),
      h("td", { class: "n" }, s.mastered),
      h("td", { class: "n" }, s.needsReview),
      h("td", { class: "n" }, s.attempts ? `${Math.round((s.correct / s.attempts) * 100)}%` : "—"),
    ),
  );
  return h(
    "section",
    { class: "card" },
    h("h2", {}, t("Por habilidade (BNCC)")),
    h("p", { class: "muted small" }, t("Habilidades que a turma já praticou, com os códigos da BNCC. "), h("a", { href: "#/bncc" }, t("Ver o currículo completo")), "."),
    h(
      "div",
      { class: "table-scroll" },
      h(
        "table",
        { class: "class-skills" },
        h("thead", {}, h("tr", {}, h("th", {}, t("Habilidade")), h("th", { class: "n" }, t("Praticaram")), h("th", { class: "n" }, t("Dominam")), h("th", { class: "n" }, t("Pedem revisão")), h("th", { class: "n" }, t("Acertos")))),
        h("tbody", {}, rows),
      ),
    ),
  );
}

export function exportSection(report) {
  const plan = report.plan;
  const link = (tipo, label) => h("a", { class: "btn btn-small", href: `/api/classrooms/${report.id}/export.csv?tipo=${tipo}&lang=${getLang()}`, download: "" }, label);
  return h(
    "section",
    { class: "card stack" },
    h("h2", {}, t("⬇️ Baixar planilha")),
    plan.canExport
      ? [
          h("p", { class: "muted small" }, t("Abre no Excel ou no Google Planilhas. Só apelidos e progresso, sem dados pessoais.")),
          h("div", { class: "actions-left" }, link("alunos", t("Alunos (nível, acertos, tarefas)")), link("habilidades", t("Habilidades (BNCC)"))),
        ]
      : h("p", { class: "muted small" }, t("Exportar a planilha da turma faz parte do plano Professor Pro. "), h("a", { href: "#/planos" }, t("Ver planos")), "."),
  );
}

export function planBanner(plan) {
  if (!plan?.enforced) return null;
  const until = plan.endsAt ? ` ${t("até {data}", { data: fmtDate(plan.endsAt) })}` : "";
  // Mostra os limites de turma quando a cobrança está ligada.
  const limits = tn(plan.maxClassrooms, "Até {n} turma, {m} tarefas em aberto por turma", "Até {n} turmas, {m} tarefas em aberto por turma", { m: plan.maxActiveAssignmentsPerClassroom });
  return h(
    "p",
    { class: "card plan-banner small" },
    h("strong", {}, `${t("Plano: {nome}", { nome: plan.name })}${until}. `),
    `${limits}${plan.canExport ? t(", com planilhas") : ""}.`,
  );
}
