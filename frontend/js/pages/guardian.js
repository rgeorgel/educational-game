import { api } from "../api.js";
import { errorMessage, h } from "../dom.js";
import { avatarView, levelBadge } from "./shared.js";

const dateFmt = new Intl.DateTimeFormat("pt-BR", { day: "numeric", month: "long" });

function lastActiveText(date) {
  if (!date) return "Ainda não jogou";
  const d = new Date(`${date}T12:00:00`);
  const today = new Date();
  const diff = Math.round((new Date(today.toDateString()) - new Date(d.toDateString())) / 86400000);
  if (diff === 0) return "Jogou hoje";
  if (diff === 1) return "Jogou ontem";
  return `Jogou pela última vez em ${dateFmt.format(d)}`;
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
      h("div", {}, h("strong", {}, `🔥 ${r.streak}`), h("span", { class: "muted small" }, "dias seguidos")),
      h("div", {}, h("strong", {}, r.lastSevenDays.problems), h("span", { class: "muted small" }, "problemas em 7 dias")),
      h("div", {}, h("strong", {}, accuracy === null ? "—" : `${accuracy}%`), h("span", { class: "muted small" }, "acertos em 7 dias")),
      h("div", {}, h("strong", {}, r.lastSevenDays.activeDays), h("span", { class: "muted small" }, "dias ativos")),
    ),
    h(
      "ul",
      { class: "report-subjects" },
      r.subjects.map((s) =>
        h(
          "li",
          {},
          h("div", { class: "report-subject-head" }, h("strong", {}, `${s.icon} ${s.name}`), levelBadge(s.level)),
          h("progress-bar", { value: String(s.percent), label: `${s.name}: ${s.percent}% para o próximo nível`, level: s.level.slug }),
          h("p", { class: "muted small" }, `${s.masteredSkills} de ${s.totalSkills} habilidades dominadas · ${s.xp} XP`),
          s.needsAttention.length
            ? h("p", { class: "attention" }, "Pede atenção: ", s.needsAttention.join(", "))
            : null,
        ),
      ),
    ),
    extra,
  );
}

function classroomManager(profileId) {
  const list = h("ul", { class: "class-list" });
  const error = h("p", { class: "form-error", role: "alert", hidden: true });
  const input = h("input", { name: "code", maxlength: "12", autocomplete: "off", placeholder: "Código da turma", "aria-label": "Código da turma" });

  async function refresh() {
    const items = await api.profileClassrooms(profileId);
    list.replaceChildren(
      ...(items.length
        ? items.map((c) => {
            const leave = h("button", { type: "button", class: "btn btn-ghost btn-small" }, "Sair da turma");
            leave.addEventListener("click", async () => {
              await api.leaveClassroom(profileId, c.id);
              refresh();
            });
            return h("li", {}, h("span", {}, `🏫 ${c.name}`), leave);
          })
        : [h("li", { class: "muted small" }, "Não participa de nenhuma turma.")]),
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
    h("button", { type: "submit", class: "btn btn-small" }, "Entrar na turma"),
  );

  refresh();
  return h(
    "details",
    { class: "class-manager" },
    h("summary", {}, "Turmas da escola"),
    h("p", { class: "muted small" }, "Ao entrar numa turma, o professor passa a ver o apelido e o progresso deste perfil (nunca o seu e-mail). Você pode sair quando quiser."),
    list,
    form,
    error,
  );
}

export async function guardianPage() {
  const reports = await api.guardianReport();
  return h(
    "section",
    { class: "page" },
    h("h1", {}, "Área do responsável"),
    h(
      "p",
      { class: "muted" },
      "Acompanhe o progresso de cada perfil. ",
      h("a", { href: "#/turmas" }, "É professor? Crie e acompanhe suas turmas."),
    ),
    reports.length
      ? h("div", { class: "report-grid" }, reports.map((r) => profileReportCard(r, classroomManager(r.id))))
      : h("p", { class: "card" }, "Nenhum perfil ainda. ", h("a", { href: "#/perfis" }, "Crie o primeiro perfil.")),
  );
}
