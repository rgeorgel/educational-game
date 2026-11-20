import { api } from "../api.js";
import { append, errorMessage, h } from "../dom.js";
import { navigate } from "../router.js";
import { readOnlyFigure, renderAnswer } from "../problem-types/index.js";
import { loading } from "./shared.js";

const PRAISE = ["Muito bem!", "Isso aí!", "Mandou bem!", "Excelente!", "Certinho!", "Show!"];
const ENCOURAGE = ["Quase! Vamos ver como faz.", "Não foi dessa vez — errar faz parte de aprender.", "Boa tentativa! Olha a explicação."];

const pick = (list) => list[Math.floor(Math.random() * list.length)];

const MODE_LABEL = {
  guardiao: "🛡️ Desafio do Guardião",
  nivelamento: "🧭 Teste de nivelamento",
};

export async function sessionPage({ sessionId }) {
  const results = []; // resultados conhecidos nesta visita (para colorir os pontinhos)
  let current = null; // { problem, answer: renderer, feedbackShown, finished }

  const dots = h("ol", { class: "dots", "aria-hidden": "true" });
  const counter = h("span", { class: "counter" });
  const body = h("div", { class: "play-body" }, loading("Preparando o desafio…"));
  const live = h("div", { class: "visually-hidden", "aria-live": "polite" });

  const root = h(
    "section",
    { class: "play" },
    h(
      "header",
      { class: "play-top" },
      h("a", { href: "#/", class: "btn btn-ghost btn-icon", "aria-label": "Sair da sessão", onClick: exit }, "✕"),
      dots,
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
      body.replaceChildren(h("div", { class: "card" }, h("h1", {}, "Ops!"), h("p", {}, errorMessage(err)), h("a", { href: "#/", class: "btn" }, "Voltar")));
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

    const hintsList = h("ol", { class: "hints-list" }, problem.hints.map((t) => h("li", {}, t)));
    const hintButton = h("button", { type: "button", class: "btn btn-ghost hint-btn" });
    const updateHintButton = (shown) => {
      hintButton.textContent = `💡 Dica (${shown}/${problem.hintsTotal})`;
      hintButton.disabled = shown >= problem.hintsTotal;
    };
    updateHintButton(problem.hints.length);
    hintButton.addEventListener("click", async () => {
      hintButton.disabled = true;
      try {
        const res = await api.hint(sessionId, problem.id);
        hintsList.replaceChildren(...res.hints.map((t) => h("li", {}, t)));
        updateHintButton(res.hints.length);
        live.textContent = `Dica: ${res.hints.at(-1)}`;
      } catch (err) {
        live.textContent = errorMessage(err);
        updateHintButton(problem.hints.length);
      }
    });

    const feedback = h("div", { class: "feedback", hidden: true, role: "status", "aria-live": "assertive" });

    const answer = renderAnswer(problem, { onSubmit: (value) => submit(value) });
    current = { problem, answer, feedbackShown: false, finished: false, hintButton, feedback };

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
    }

    function showFeedback(result) {
      current.feedbackShown = true;
      current.finished = result.sessionFinished;
      const continueBtn = h("button", { type: "button", class: "btn btn-primary btn-block", onClick: next }, result.sessionFinished ? "Ver resultado" : "Continuar");
      feedback.className = `feedback ${result.correct ? "is-correct" : "is-wrong"}`;
      feedback.replaceChildren();
      append(feedback, [
        h("p", { class: "feedback-title" }, result.correct ? `✅ ${pick(PRAISE)}` : `💭 ${problem.mode === "etapa" ? pick(ENCOURAGE) : "Não foi dessa vez."}`),
        result.correct ? null : h("p", {}, "A resposta certa é ", h("strong", {}, result.correctAnswer), "."),
        h("p", { class: "explanation" }, result.explanation),
        h("p", { class: "xp" }, `+${result.xpEarned} XP`),
        !result.correct && !result.sessionFinished && problem.mode === "etapa" ? h("p", { class: "muted small" }, "Agora vem um parecido para você tentar de novo!") : null,
        continueBtn,
      ]);
      feedback.hidden = false;
      continueBtn.focus({ preventScroll: true });
      feedback.scrollIntoView({ behavior: "smooth", block: "nearest" });
    }

    function next() {
      if (current.finished) navigate(`/resultado/${sessionId}`, { replace: true });
      else loadNext();
    }
    current.next = next;

    const card = h(
      "article",
      { class: "card problem-card", "aria-labelledby": "prompt" },
      MODE_LABEL[problem.mode] ? h("p", { class: `mode-tag mode-${problem.mode}` }, MODE_LABEL[problem.mode]) : null,
      h("p", { class: "skill-tag" }, problem.skill.name),
      h("h1", { id: "prompt", class: "prompt", tabindex: "-1" }, problem.prompt),
      problem.visual ? h("p", { class: "visual", "aria-label": "Figura do problema" }, problem.visual) : null,
      problem.figure && problem.type !== "fracao-visual" ? h("div", { class: "figure-wrap" }, readOnlyFigure(problem.figure)) : null,
      h("div", { class: "answer-area" }, answer.node),
      problem.hintsTotal > 0
        ? h("div", { class: "hints" }, hintButton, hintsList)
        : h("p", { class: "muted small" }, "No teste de nivelamento não há dicas. Tudo bem não saber — responda o que achar!"),
    );

    body.replaceChildren(card, feedback);
    card.querySelector("#prompt").focus({ preventScroll: true });
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
    if (current.answer.handleKey(e)) e.preventDefault();
  }

  document.addEventListener("keydown", onKey);
  loadNext();

  return { node: root, cleanup: () => document.removeEventListener("keydown", onKey) };
}
