// Renderizadores por tipo de desafio. Cada tipo do backend ("numero", "multipla-escolha",
// "completar-sequencia") tem aqui uma função que monta a área de resposta.
//
// Contrato de cada renderizador:
//   render(problem, { onSubmit }) → {
//     node,                 // elemento da área de resposta
//     handleKey(event),     // teclado físico; devolve true se tratou a tecla
//     lock(),               // impede novas respostas (enquanto envia / após responder)
//     unlock(),             // libera de novo (ex.: erro de rede)
//     reveal(result, given) // mostra certo/errado depois da resposta
//   }

import { h } from "../dom.js";

const MAX_DIGITS = 6;

function numericInput({ onSubmit, label = "Sua resposta" }) {
  let value = "";
  let locked = false;

  const display = h("output", { class: "answer-display empty", "aria-live": "polite", "aria-label": label }, "?");
  const keypad = h("numeric-keypad");

  const update = () => {
    display.textContent = value || "?";
    display.classList.toggle("empty", !value);
  };

  const press = (key) => {
    if (locked) return;
    if (/^[0-9]$/.test(key) && value.length < MAX_DIGITS) value = value === "0" ? key : value + key;
    else if (key === "back") value = value.slice(0, -1);
    else if (key === "ok" && value) onSubmit(value);
    update();
  };

  keypad.addEventListener("keypad", (e) => press(e.detail));

  return {
    node: h("div", { class: "answer-numero" }, display, keypad),
    display,
    handleKey(e) {
      if (/^[0-9]$/.test(e.key)) press(e.key);
      else if (e.key === "Backspace") press("back");
      else if (e.key === "Enter") press("ok");
      else return false;
      return true;
    },
    lock() {
      locked = true;
      keypad.disabled = true;
    },
    unlock() {
      locked = false;
      keypad.disabled = false;
    },
    reveal(result) {
      display.classList.add(result.correct ? "correct" : "wrong");
    },
  };
}

function choiceInput(choices, { onSubmit, big = false }) {
  let locked = false;
  let chosen = null;

  const buttons = choices.map((choice, i) =>
    h(
      "button",
      {
        type: "button",
        class: `choice${big ? " choice-big" : ""}`,
        dataset: { value: choice },
        "aria-keyshortcuts": String(i + 1),
        onClick: () => pick(choice),
      },
      choice,
    ),
  );

  const pick = (choice) => {
    if (locked) return;
    chosen = choice;
    buttons.forEach((b) => b.classList.toggle("selected", b.dataset.value === choice));
    onSubmit(choice);
  };

  return {
    node: h("div", { class: "choices", role: "group", "aria-label": "Opções de resposta" }, buttons),
    handleKey(e) {
      const index = Number(e.key) - 1;
      if (Number.isInteger(index) && index >= 0 && index < choices.length) {
        pick(choices[index]);
        return true;
      }
      return false;
    },
    lock() {
      locked = true;
      buttons.forEach((b) => (b.disabled = true));
    },
    unlock() {
      locked = false;
      buttons.forEach((b) => (b.disabled = false));
    },
    reveal(result) {
      for (const b of buttons) {
        if (b.dataset.value === result.correctAnswer) b.classList.add("correct");
        else if (b.dataset.value === chosen) b.classList.add("wrong");
      }
    },
  };
}

function sequenceRow(sequence) {
  const blank = h("span", { class: "seq-item seq-blank", "aria-label": "lacuna" }, "?");
  const items = sequence.map((item) => (item === null ? blank : h("span", { class: "seq-item" }, item)));
  const row = h("div", { class: "sequence", role: "list", "aria-label": "Sequência" }, items.map((i) => h("span", { role: "listitem", class: "seq-cell" }, i)));
  return { row, blank };
}

const renderers = {
  numero(problem, opts) {
    return numericInput(opts);
  },

  "multipla-escolha"(problem, opts) {
    const short = problem.choices.every((c) => [...c].length <= 4);
    return choiceInput(problem.choices, { ...opts, big: short });
  },

  "completar-sequencia"(problem, opts) {
    const { row, blank } = sequenceRow(problem.sequence);
    const input = problem.choices ? choiceInput(problem.choices, { ...opts, big: true }) : numericInput({ ...opts, label: "Número que falta" });
    return {
      ...input,
      node: h("div", { class: "answer-sequence" }, row, input.node),
      reveal(result, given) {
        input.reveal(result, given);
        blank.textContent = result.correctAnswer;
        blank.classList.add(result.correct ? "correct" : "filled");
      },
    };
  },
};

export function renderAnswer(problem, opts) {
  const render = renderers[problem.type];
  if (!render) throw new Error(`Tipo de desafio desconhecido: ${problem.type}`);
  return render(problem, opts);
}
