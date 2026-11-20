// "Currículo e BNCC": todas as habilidades do jogo, por tópico e nível, com as habilidades da BNCC relacionadas.
// Para pais e professores planejarem o uso do jogo junto com a escola.
import { api } from "../api.js";
import { h } from "../dom.js";

/** Lista de códigos BNCC como etiquetas (o resumo aparece ao passar o mouse e na lista abaixo). */
export function bnccTags(refs) {
  if (!refs?.length) return h("span", { class: "muted small" }, "Raciocínio lógico complementar (sem habilidade BNCC direta)");
  return h(
    "ul",
    { class: "bncc-tags", "aria-label": "Habilidades da BNCC" },
    refs.map((b) => h("li", { title: `${b.code} (${b.grade}, ${b.component}): ${b.summary}` }, b.code)),
  );
}

function skillItem(skill) {
  return h(
    "li",
    { class: "bncc-skill" },
    h("div", {}, h("strong", {}, skill.name), h("p", { class: "muted small" }, skill.description)),
    bnccTags(skill.bncc),
    skill.bncc.length
      ? h(
          "details",
          { class: "bncc-details" },
          h("summary", {}, "O que diz cada habilidade"),
          h(
            "ul",
            {},
            skill.bncc.map((b) => h("li", {}, h("strong", {}, b.code), ` · ${b.grade} · ${b.component}: ${b.summary}`)),
          ),
        )
      : null,
  );
}

export async function bnccPage() {
  const subjects = await api.curriculum();
  const allCodes = new Set(subjects.flatMap((s) => s.levels.flatMap((l) => l.skills.flatMap((k) => k.bncc.map((b) => b.code)))));

  return h(
    "section",
    { class: "page" },
    h("h1", {}, "Currículo e BNCC"),
    h(
      "p",
      { class: "muted" },
      `Cada habilidade do jogo e as habilidades da BNCC relacionadas (${allCodes.size} no total, do 1º ano do Ensino Fundamental ao Ensino Médio). `,
      "Matemática segue a BNCC de Matemática; Lógica usa a BNCC de Matemática e o complemento de Computação (pensamento computacional).",
    ),
    h(
      "p",
      { class: "muted small" },
      "Os resumos são versões curtas, não o texto oficial. O alinhamento é uma referência para planejar as aulas e pode ser ajustado com a sua escola.",
    ),
    subjects.map((subject) =>
      h(
        "section",
        { class: "card bncc-subject" },
        h("h2", {}, `${subject.icon} ${subject.name}`),
        subject.levels.map((level) =>
          h("div", { class: "bncc-level" }, h("h3", {}, `${level.icon} ${level.name}`), h("ul", { class: "bncc-skills" }, level.skills.map(skillItem))),
        ),
      ),
    ),
  );
}
