// Renderizadores por tipo de desafio. Cada tipo do backend tem aqui uma função que monta a
// área de resposta: "numero", "multipla-escolha", "completar-sequencia", "verdadeiro-falso",
// "ordenar", "grade" e "fracao-visual".
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
import { fractionFigure } from "./figure.js";
import { decimalSeparator, t } from "../i18n.js";

const MAX_CHARS = 8;
// Vírgula decimal em português e espanhol; ponto em inglês (a API aceita os dois).
const SEP = decimalSeparator();

function numericInput({ onSubmit, label = t("Sua resposta"), negative = false, decimal = false }) {
  let value = "";
  let locked = false;

  const display = h("output", { class: "answer-display empty", "aria-live": "polite", "aria-label": label }, "?");
  const keypad = h("numeric-keypad", { negative, decimal });

  const update = () => {
    display.textContent = value || "?";
    display.classList.toggle("empty", !value);
  };

  const press = (key) => {
    if (locked) return;
    if (/^[0-9]$/.test(key) && value.length < MAX_CHARS) value = value === "0" ? key : value === "−0" ? `−${key}` : value + key;
    else if (key === "back") value = value.slice(0, -1);
    else if (key === "minus" && negative) value = value.startsWith("−") ? value.slice(1) : `−${value}`;
    else if (key === "comma" && decimal && !value.includes(SEP)) value = (value === "" || value === "−" ? `${value}0` : value) + SEP;
    else if (key === "ok" && /[0-9]/.test(value)) onSubmit(value.endsWith(SEP) ? value.slice(0, -1) : value);
    update();
  };

  keypad.addEventListener("keypad", (e) => press(e.detail));

  return {
    node: h("div", { class: "answer-numero" }, display, keypad),
    handleKey(e) {
      if (/^[0-9]$/.test(e.key)) press(e.key);
      else if (e.key === "Backspace") press("back");
      else if (e.key === "Enter") press("ok");
      else if (e.key === "-") press("minus");
      else if (e.key === "," || e.key === ".") press("comma");
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

function choiceInput(choices, { onSubmit, big = false, keys = [] }) {
  let locked = false;
  let chosen = null;

  const buttons = choices.map((choice, i) =>
    h(
      "button",
      {
        type: "button",
        class: `choice${big ? " choice-big" : ""}${choice.length > 40 ? " choice-long" : ""}`,
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
    node: h("div", { class: `choices${choices.length === 2 ? " choices-two" : ""}`, role: "group", "aria-label": t("Opções de resposta") }, buttons),
    handleKey(e) {
      const extra = keys.findIndex((k) => k === e.key.toLowerCase());
      const index = extra >= 0 ? extra : Number(e.key) - 1;
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
  const blank = h("span", { class: "seq-item seq-blank", "aria-label": t("lacuna") }, "?");
  const items = sequence.map((item) => (item === null ? blank : h("span", { class: "seq-item" }, item)));
  const row = h("div", { class: "sequence", role: "list", "aria-label": t("Sequência") }, items.map((i) => h("span", { role: "listitem", class: "seq-cell" }, i)));
  return { row, blank };
}

/** Botão "Confirmar" padrão para tipos em que a resposta é montada aos poucos. */
function confirmButton(onClick) {
  return h("button", { type: "button", class: "btn btn-primary btn-block confirm-btn", disabled: true, onClick }, t("Confirmar resposta"));
}

// ---------- Ordenar ----------
function orderInput(items, { onSubmit }) {
  let locked = false;
  const placed = [];
  const pool = h("div", { class: "order-pool", role: "group", "aria-label": t("Itens para ordenar") });
  const slots = h("ol", { class: "order-slots", "aria-label": t("Sua ordem") });
  const confirm = confirmButton(() => !locked && placed.length === items.length && onSubmit(placed.join("|")));
  const solution = h("p", { class: "order-solution", hidden: true });

  function render() {
    pool.replaceChildren(
      ...items
        .filter((it) => !placed.includes(it))
        .map((it) => h("button", { type: "button", class: "chip-btn", disabled: locked, onClick: () => move(it) }, it)),
    );
    slots.replaceChildren(
      ...items.map((_, i) =>
        h(
          "li",
          { class: `order-slot${placed[i] ? " filled" : ""}` },
          placed[i]
            ? h("button", { type: "button", class: "chip-btn placed", disabled: locked, "aria-label": t("{item} (tocar para tirar)", { item: placed[i] }), onClick: () => remove(placed[i]) }, placed[i])
            : h("span", { class: "muted" }, String(i + 1)),
        ),
      ),
    );
    confirm.disabled = locked || placed.length !== items.length;
  }

  const move = (it) => {
    if (locked) return;
    placed.push(it);
    render();
  };
  const remove = (it) => {
    if (locked) return;
    placed.splice(placed.indexOf(it), 1);
    render();
  };

  render();
  return {
    node: h("div", { class: "answer-order" }, h("p", { class: "muted small" }, t("Toque nos itens na ordem certa. Toque de novo para tirar.")), slots, pool, solution, confirm),
    handleKey(e) {
      const free = items.filter((it) => !placed.includes(it));
      const index = Number(e.key) - 1;
      if (Number.isInteger(index) && index >= 0 && index < free.length) move(free[index]);
      else if (e.key === "Backspace" && placed.length) remove(placed.at(-1));
      else if (e.key === "Enter") confirm.click();
      else return false;
      return true;
    },
    lock() {
      locked = true;
      render();
    },
    unlock() {
      locked = false;
      render();
    },
    reveal(result) {
      slots.classList.add(result.correct ? "correct" : "wrong");
      if (!result.correct) {
        solution.textContent = t("Ordem certa: {ordem}", { ordem: result.correctAnswer.split("|").join("  →  ") });
        solution.hidden = false;
      }
    },
  };
}

// ---------- Grade (Sudoku / quadrado mágico) ----------
function gridInput(grid, { onSubmit }) {
  const n = grid.size;
  const values = grid.cells.map((c) => c ?? null);
  let selected = values.findIndex((v) => v === null);
  let locked = false;

  const cells = values.map((_, i) => {
    const r = Math.floor(i / n);
    const c = i % n;
    const given = grid.cells[i] !== null;
    const classes = ["cell"];
    if (given) classes.push("given");
    if ((c + 1) % grid.boxCols === 0 && c !== n - 1) classes.push("box-right");
    if ((r + 1) % grid.boxRows === 0 && r !== n - 1) classes.push("box-bottom");
    return h("button", {
      type: "button",
      class: classes.join(" "),
      disabled: given,
      "aria-label": t("Linha {r}, coluna {c}", { r: r + 1, c: c + 1 }),
      onClick: () => select(i),
    });
  });

  const board = h("div", { class: `grid-board grid-${n}`, role: "grid", style: { "--n": String(n) } }, cells);
  const palette = h(
    "div",
    { class: "grid-palette", role: "group", "aria-label": t("Números") },
    Array.from({ length: grid.maxValue }, (_, k) => h("button", { type: "button", class: "key", onClick: () => set(k + 1) }, String(k + 1))),
    h("button", { type: "button", class: "key key-back", "aria-label": t("Apagar"), onClick: () => set(null) }, "⌫"),
  );
  const confirm = confirmButton(() => !locked && values.every((v) => v !== null) && onSubmit(values.join(",")));
  const target = grid.target ? h("p", { class: "muted small center" }, t("Soma mágica: {n}", { n: grid.target })) : null;

  function render() {
    cells.forEach((cell, i) => {
      cell.textContent = values[i] ?? "";
      cell.classList.toggle("selected", i === selected && !locked);
      if (grid.cells[i] === null) cell.setAttribute("aria-label", `${t("Linha {r}, coluna {c}", { r: Math.floor(i / n) + 1, c: (i % n) + 1 })}: ${values[i] ?? t("vazia")}`);
    });
    confirm.disabled = locked || values.some((v) => v === null);
  }

  function select(i) {
    if (locked || grid.cells[i] !== null) return;
    selected = i;
    render();
  }

  function set(v) {
    if (locked || selected < 0 || grid.cells[selected] !== null) return;
    values[selected] = v;
    // Avança para a próxima célula vazia, para agilizar.
    if (v !== null) {
      const next = values.findIndex((x, i) => x === null && i > selected);
      if (next >= 0) selected = next;
    }
    render();
  }

  function moveSelection(dr, dc) {
    let r = Math.floor(selected / n);
    let c = selected % n;
    for (let step = 0; step < n * n; step++) {
      r = (r + dr + n) % n;
      c = (c + dc + n) % n;
      if (grid.cells[r * n + c] === null) {
        selected = r * n + c;
        break;
      }
    }
    render();
  }

  render();
  return {
    node: h("div", { class: "answer-grid" }, target, board, palette, confirm),
    handleKey(e) {
      const digit = Number(e.key);
      if (Number.isInteger(digit) && digit >= 1 && digit <= grid.maxValue) set(digit);
      else if (e.key === "Backspace" || e.key === "Delete") set(null);
      else if (e.key === "ArrowRight") moveSelection(0, 1);
      else if (e.key === "ArrowLeft") moveSelection(0, -1);
      else if (e.key === "ArrowDown") moveSelection(1, 0);
      else if (e.key === "ArrowUp") moveSelection(-1, 0);
      else if (e.key === "Enter") confirm.click();
      else return false;
      return true;
    },
    lock() {
      locked = true;
      palette.querySelectorAll("button").forEach((b) => (b.disabled = true));
      render();
    },
    unlock() {
      locked = false;
      palette.querySelectorAll("button").forEach((b) => (b.disabled = false));
      render();
    },
    reveal(result) {
      board.classList.add(result.correct ? "correct" : "wrong");
      if (result.correct) return;
      // Mostra uma solução, destacando as células diferentes da resposta dada.
      const solution = result.correctAnswer.split(",").map(Number);
      cells.forEach((cell, i) => {
        if (grid.cells[i] !== null) return;
        if (values[i] !== solution[i]) {
          cell.classList.add("fixed");
          cell.textContent = String(solution[i]);
        }
      });
    },
  };
}

// ---------- Fração visual (pintar) ----------
function paintInput(figure, { onSubmit }) {
  let locked = false;
  const painted = new Set();
  const counter = h("p", { class: "muted center", "aria-live": "polite" });
  const confirm = confirmButton(() => !locked && painted.size > 0 && onSubmit(String(painted.size)));

  const svg = fractionFigure(figure, {
    interactive: true,
    isFilled: (i) => painted.has(i),
    onToggle: (i) => {
      if (locked) return;
      painted.has(i) ? painted.delete(i) : painted.add(i);
      update();
    },
  });

  function update() {
    svg.update();
    counter.textContent = t("{a} de {b} partes pintadas", { a: painted.size, b: figure.parts });
    confirm.disabled = locked || painted.size === 0;
  }
  update();

  return {
    node: h("div", { class: "answer-paint" }, h("p", { class: "muted small center" }, t("Toque nas partes para pintar ou despintar.")), svg.node, counter, confirm),
    handleKey(e) {
      if (e.key === "Enter") {
        confirm.click();
        return true;
      }
      return false;
    },
    lock() {
      locked = true;
      confirm.disabled = true;
    },
    unlock() {
      locked = false;
      update();
    },
    reveal(result) {
      svg.node.classList.add(result.correct ? "correct" : "wrong");
    },
  };
}

const renderers = {
  numero(problem, opts) {
    return numericInput({ ...opts, negative: problem.acceptsNegative, decimal: problem.acceptsDecimal });
  },

  "multipla-escolha"(problem, opts) {
    const short = problem.choices.every((c) => [...c].length <= 4);
    return choiceInput(problem.choices, { ...opts, big: short });
  },

  "verdadeiro-falso"(problem, opts) {
    // Atalhos de teclado: V/F (verdadeiro/falso, verdadero/falso) ou T/F (true/false).
    return choiceInput(problem.choices, { ...opts, big: true, keys: [problem.choices[0][0].toLowerCase(), problem.choices[1][0].toLowerCase()] });
  },

  ordenar(problem, opts) {
    return orderInput(problem.choices, opts);
  },

  grade(problem, opts) {
    return gridInput(problem.grid, opts);
  },

  "fracao-visual"(problem, opts) {
    return paintInput(problem.figure, opts);
  },

  "completar-sequencia"(problem, opts) {
    const { row, blank } = sequenceRow(problem.sequence);
    const input = problem.choices
      ? choiceInput(problem.choices, { ...opts, big: true })
      : numericInput({ ...opts, label: t("Número que falta"), negative: problem.acceptsNegative, decimal: problem.acceptsDecimal });
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
  if (!render) throw new Error(t("Tipo de desafio desconhecido: {tipo}", { tipo: problem.type }));
  return render(problem, opts);
}

/** Figura somente leitura (para "que fração está pintada?"). */
export function readOnlyFigure(figure) {
  return fractionFigure(figure, { interactive: false, isFilled: (i) => i < figure.filled }).node;
}
