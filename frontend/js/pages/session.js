import { api } from "../api.js";
import { append, errorMessage, h } from "../dom.js";
import { navigate } from "../router.js";
import { readOnlyFigure, renderAnswer } from "../problem-types/index.js";
import { loading } from "./shared.js";
import { getPrefs } from "../prefs.js";
import { speak, voiceSupported } from "../voice.js";
import { confettiFrom, floatText } from "../effects.js";
import { t } from "../i18n.js";

const PRAISE = [t("Muito bem!"), t("Isso aí!"), t("Mandou bem!"), t("Excelente!"), t("Certinho!"), t("Show!")];
const ENCOURAGE = [t("Quase! Vamos ver como faz."), t("Não foi dessa vez — errar faz parte de aprender."), t("Boa tentativa! Olha a explicação.")];

const pick = (list) => list[Math.floor(Math.random() * list.length)];

// Depois que um desafio novo aparece, ignora toques na área de resposta por um instante.
// No celular, um toque duplo em t("Continuar") (ou o toque que chega atrasado enquanto a tela
// troca) caía numa opção do desafio seguinte e contava como resposta errada.
const INPUT_GUARD_MS = 600;

const MODE_LABEL = {
  guardiao: t("🛡️ Desafio do Guardião"),
  nivelamento: t("🧭 Teste de nivelamento"),
  diario: t("📆 Desafio do dia · XP em dobro"),
  revisao: t("🧩 Revisão"),
};

/** Texto lido em voz alta: enunciado, figuras/código e as opções. */
function readable(problem) {
  const parts = [problem.prompt];
  if (problem.visual) parts.push(problem.visual.replace(/\n/g, ". "));
  if (problem.code) parts.push(problem.code.replace(/←/g, ` ${t("recebe")} `).replace(/\n/g, ". "));
  if (problem.choices && problem.type !== "ordenar") parts.push(t("Opções: {lista}.", { lista: problem.choices.join(", ") }));
  if (problem.type === "ordenar") parts.push(t("Itens: {lista}.", { lista: problem.choices.join(", ") }));
  return parts.join(" ");
}

export async function sessionPage({ sessionId }) {
  const results = []; // resultados conhecidos nesta visita (para colorir os pontinhos)
  let current = null; // { problem, answer: renderer, feedbackShown, finished }

  const dots = h("ol", { class: "dots", "aria-hidden": "true" });
  const counter = h("span", { class: "counter" });
  // Sequência de acertos nesta visita ("🔥 3 seguidas").
  let streak = 0;
  const combo = h("span", { class: "combo", hidden: true, "aria-live": "polite" });
  const body = h("div", { class: "play-body" }, loading(t("Preparando o desafio…")));
  const live = h("div", { class: "visually-hidden", "aria-live": "polite" });

  const root = h(
    "section",
    { class: "play" },
    h(
      "header",
      { class: "play-top" },
      h("a", { href: "#/", class: "btn btn-ghost btn-icon", "aria-label": t("Sair da sessão"), onClick: exit }, "✕"),
      dots,
      combo,
      counter,
    ),
    body,
    live,
  );

  function exit(e) {
    e.preventDefault();
    history.length > 1 ? history.back() : navigate("/");
  }

  function renderDots(number, total) {
    dots.replaceChildren(
      ...Array.from({ length: total }, (_, i) => {
        const n = i + 1;
        const known = results[i];
        const cls = known === true ? "ok" : known === false ? "bad" : n < number ? "done" : n === number ? "current" : "";
        return h("li", { class: `dot ${cls}` });
      }),
    );
    counter.textContent = `${number}/${total}`;
  }

  async function loadNext() {
    let next;
    try {
      next = await api.next(sessionId);
    } catch (err) {
      body.replaceChildren(h("div", { class: "card" }, h("h1", {}, t("Ops!")), h("p", {}, errorMessage(err)), h("a", { href: "#/", class: "btn" }, t("Voltar"))));
      return;
    }
    if (next.done) {
      navigate(`/resultado/${sessionId}`, { replace: true });
      return;
    }
    showProblem(next.problem);
  }

  function showProblem(problem) {
    renderDots(problem.number, problem.total);
    live.textContent = "";

    const hintsList = h("ol", { class: "hints-list" }, problem.hints.map((hint) => h("li", {}, hint)));
    const hintButton = h("button", { type: "button", class: "btn btn-ghost hint-btn" });
    const updateHintButton = (shown) => {
      hintButton.textContent = `💡 ${t("Dica")} (${shown}/${problem.hintsTotal})`;
      hintButton.disabled = shown >= problem.hintsTotal;
    };
    updateHintButton(problem.hints.length);
    hintButton.addEventListener("click", async () => {
      hintButton.disabled = true;
      try {
        const res = await api.hint(sessionId, problem.id);
        hintsList.replaceChildren(...res.hints.map((hint) => h("li", {}, hint)));
        updateHintButton(res.hints.length);
        live.textContent = `${t("Dica")}: ${res.hints.at(-1)}`;
      } catch (err) {
        live.textContent = errorMessage(err);
        updateHintButton(problem.hints.length);
      }
    });

    const feedback = h("div", { class: "feedback", hidden: true, role: "status", "aria-live": "assertive" });

    const answer = renderAnswer(problem, { onSubmit: (value) => submit(value) });
    const readyAt = performance.now() + INPUT_GUARD_MS;
    const settling = () => performance.now() < readyAt;
    current = { problem, answer, feedbackShown: false, finished: false, advancing: false, hintButton, feedback, settling };

    async function submit(value) {
      answer.lock();
      hintButton.disabled = true;
      let result;
      try {
        result = await api.answer(sessionId, problem.id, value);
      } catch (err) {
        if (err.status === 409) return loadNext(); // já respondido (ex.: outra aba)
        answer.unlock();
        updateHintButton(problem.hints.length);
        live.textContent = errorMessage(err);
        return;
      }
      results[problem.number - 1] = result.correct;
      renderDots(problem.number, problem.total);
      answer.reveal(result, value);
      showFeedback(result);
      celebrate(result);
    }

    function celebrate(result) {
      streak = result.correct ? streak + 1 : 0;
      combo.hidden = streak < 2;
      if (streak >= 2) {
        combo.textContent = `🔥 ${t("{n} seguidas", { n: streak })}`;
        combo.classList.remove("bump");
        void combo.offsetWidth; // reinicia a animação
        combo.classList.add("bump");
      }
      if (!result.correct) return;
      const xpTag = feedback.querySelector(".xp");
      if (xpTag) floatText(xpTag, `+${result.xpEarned} XP`);
      // Confete maior a cada 3 acertos seguidos; menor nos demais.
      confettiFrom(feedback.querySelector(".feedback-title") ?? feedback, { count: streak % 3 === 0 ? 90 : 40, spread: streak % 3 === 0 ? 1.2 : 0.7 });
    }

    function showFeedback(result) {
      current.feedbackShown = true;
      current.finished = result.sessionFinished;
      const continueBtn = h("button", { type: "button", class: "btn btn-primary btn-block", onClick: next }, result.sessionFinished ? t("Ver resultado") : t("Continuar"));
      current.continueBtn = continueBtn;
      feedback.className = `feedback ${result.correct ? "is-correct" : "is-wrong"}`;
      feedback.replaceChildren();
      append(feedback, [
        h("p", { class: "feedback-title" }, result.correct ? `✅ ${pick(PRAISE)}` : `💭 ${problem.mode === "etapa" || problem.mode === "revisao" ? pick(ENCOURAGE) : t("Não foi dessa vez.")}`),
        result.correct ? null : h("p", {}, t("A resposta certa é "), h("strong", {}, result.correctAnswer), "."),
        h("p", { class: "explanation" }, result.explanation),
        h("p", { class: "xp" }, `+${result.xpEarned} XP`),
        !result.correct && !result.sessionFinished && (problem.mode === "etapa" || problem.mode === "revisao") ? h("p", { class: "muted small" }, t("Agora vem um parecido para você tentar de novo!")) : null,
        continueBtn,
      ]);
      feedback.hidden = false;
      continueBtn.focus({ preventScroll: true });
      feedback.scrollIntoView({ behavior: "smooth", block: "nearest" });
    }

    function next() {
      // Um só avanço por desafio: toques repetidos em t("Continuar") não fazem nada.
      if (current.advancing) return;
      current.advancing = true;
      if (current.continueBtn) {
        current.continueBtn.disabled = true;
        if (!current.finished) current.continueBtn.textContent = t("Carregando…");
      }
      if (current.finished) navigate(`/resultado/${sessionId}`, { replace: true });
      else loadNext();
    }
    current.next = next;

    const answerArea = h("div", { class: "answer-area" }, answer.node);
    answerArea.addEventListener(
      "click",
      (e) => {
        if (!settling()) return;
        e.preventDefault();
        e.stopPropagation();
      },
      true, // captura: barra o toque antes de chegar ao botão da opção
    );

    const card = h(
      "article",
      { class: "card problem-card", "aria-labelledby": "prompt" },
      MODE_LABEL[problem.mode] ? h("p", { class: `mode-tag mode-${problem.mode}` }, MODE_LABEL[problem.mode]) : null,
      h("p", { class: "skill-tag" }, problem.skill.name),
      h("h1", { id: "prompt", class: "prompt", tabindex: "-1" }, problem.prompt),
      voiceSupported ? h("button", { type: "button", class: "btn btn-ghost btn-small listen-btn", onClick: () => speak(readable(problem)) }, t("🔊 Ouvir")) : null,
      problem.visual ? h("p", { class: "visual", "aria-label": t("Figura do problema") }, problem.visual) : null,
      problem.code ? h("pre", { class: "code-block", "aria-label": t("Programa") }, h("code", {}, problem.code)) : null,
      problem.figure && problem.type !== "fracao-visual" ? h("div", { class: "figure-wrap" }, readOnlyFigure(problem.figure)) : null,
      answerArea,
      problem.hintsTotal > 0
        ? h("div", { class: "hints" }, hintButton, hintsList)
        : h("p", { class: "muted small" }, t("No teste de nivelamento não há dicas. Tudo bem não saber — responda o que achar!")),
    );

    body.replaceChildren(card, feedback);
    // Começa sempre do topo: a tela não fica rolada onde estava o t("Continuar") do desafio anterior.
    window.scrollTo({ top: 0, behavior: "instant" });
    card.querySelector("#prompt").focus({ preventScroll: true });
    if (getPrefs().autoRead) speak(readable(problem));
  }

  function onKey(e) {
    if (!current || e.ctrlKey || e.metaKey || e.altKey) return;
    if (e.target instanceof HTMLButtonElement && e.key === "Enter") return; // o botão já trata
    if (current.feedbackShown) {
      if (e.key === "Enter") {
        e.preventDefault();
        current.next();
      }
      return;
    }
    if (current.settling()) return;
    if (current.answer.handleKey(e)) e.preventDefault();
  }

  document.addEventListener("keydown", onKey);
  loadNext();

  return { node: root, cleanup: () => document.removeEventListener("keydown", onKey) };
}
