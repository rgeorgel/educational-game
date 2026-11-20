// Utilitários de DOM. Tudo é criado com createElement/textContent — nunca innerHTML com dados.

/**
 * Cria um elemento.
 * h("button", { class: "btn", onClick: fn, "aria-label": "Fechar" }, "×")
 * Props especiais: class, style (objeto → CSSOM, compatível com a CSP), dataset, on<Evento>.
 */
export function h(tag, props = {}, ...children) {
  const el = document.createElement(tag);
  for (const [key, value] of Object.entries(props ?? {})) {
    if (value === null || value === undefined || value === false) continue;
    if (key === "class") el.className = value;
    else if (key === "style") for (const [p, v] of Object.entries(value)) el.style.setProperty(p, v);
    else if (key === "dataset") Object.assign(el.dataset, value);
    else if (key.startsWith("on") && typeof value === "function") el.addEventListener(key.slice(2).toLowerCase(), value);
    else if (key in el && typeof value !== "string") el[key] = value;
    else el.setAttribute(key, value === true ? "" : value);
  }
  append(el, children);
  return el;
}

export function append(parent, children) {
  for (const child of [children].flat(Infinity)) {
    if (child === null || child === undefined || child === false) continue;
    parent.append(child instanceof Node ? child : document.createTextNode(String(child)));
  }
  return parent;
}

export function clear(el) {
  el.replaceChildren();
  return el;
}

/** Lê os campos de um <form> como objeto. */
export function formData(form) {
  return Object.fromEntries(new FormData(form).entries());
}

/** Mensagem de erro amigável a partir de um ApiError (ProblemDetails) ou erro genérico. */
export function errorMessage(err) {
  const problem = err?.problem;
  if (problem?.errors) {
    const first = Object.values(problem.errors).flat()[0];
    if (first) return first;
  }
  return problem?.detail || problem?.title || err?.message || "Algo deu errado. Tente de novo.";
}
