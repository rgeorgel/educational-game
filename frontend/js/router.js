// Roteador por hash: #/mapa/matematica → { subject: "matematica" }

const routes = [];
let outlet = null;
let cleanup = null;
let renderId = 0;
let guard = () => null;

/**
 * Registra uma rota. O handler recebe os parâmetros e devolve um Node
 * ou { node, cleanup } (para remover listeners ao sair da página).
 */
export function route(pattern, handler, { title } = {}) {
  const keys = [];
  const regex = new RegExp(
    "^" +
      pattern.replace(/\/:([a-zA-Z]+)/g, (_, key) => {
        keys.push(key);
        return "/([^/]+)";
      }) +
      "/?$",
  );
  routes.push({ regex, keys, handler, title });
}

/** Função chamada antes de cada rota; pode devolver um caminho para redirecionar. */
export function setGuard(fn) {
  guard = fn;
}

export function navigate(path, { replace = false } = {}) {
  const hash = `#${path}`;
  if (location.hash === hash) render();
  else if (replace) location.replace(hash);
  else location.hash = hash;
}

export function currentPath() {
  return location.hash.slice(1) || "/";
}

export function start(element) {
  outlet = element;
  window.addEventListener("hashchange", render);
  render();
}

async function render() {
  if (!outlet) return; // ainda não iniciou (ex.: 401 durante a inicialização)
  const id = ++renderId;
  const path = currentPath();

  const redirect = guard(path);
  if (redirect && redirect !== path) {
    navigate(redirect, { replace: true });
    return;
  }

  const match = findRoute(path) ?? findRoute("/");
  const params = Object.fromEntries(match.route.keys.map((k, i) => [k, decodeURIComponent(match.values[i])]));

  cleanup?.();
  cleanup = null;

  let view;
  try {
    view = await match.route.handler(params);
  } catch (err) {
    console.error(err);
    view = errorView(err);
  }
  if (id !== renderId) {
    view?.cleanup?.();
    return; // outra navegação aconteceu enquanto carregava
  }

  const node = view instanceof Node ? view : view.node;
  cleanup = view instanceof Node ? null : (view.cleanup ?? null);
  node.classList?.add("page-enter"); // transição suave entre telas (game.css)
  outlet.replaceChildren(node);
  document.title = match.route.title ? `${match.route.title} · Educa Game` : "Educa Game";

  // Acessibilidade: leva o foco ao título da nova página.
  const heading = outlet.querySelector("h1");
  if (heading) {
    heading.tabIndex = -1;
    heading.focus({ preventScroll: true });
  }
  window.scrollTo(0, 0);
}

function findRoute(path) {
  for (const route of routes) {
    const m = route.regex.exec(path);
    if (m) return { route, values: m.slice(1) };
  }
  return null;
}

function errorView(err) {
  const box = document.createElement("section");
  box.className = "card page-narrow";
  const title = document.createElement("h1");
  title.textContent = "Ops! Algo deu errado.";
  const msg = document.createElement("p");
  msg.textContent = err?.message || "Tente recarregar a página.";
  const back = document.createElement("a");
  back.href = "#/";
  back.className = "btn";
  back.textContent = "Voltar ao início";
  box.append(title, msg, back);
  return box;
}
