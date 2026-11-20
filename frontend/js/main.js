// Ponto de entrada: monta o cabeçalho, registra as rotas e verifica a sessão do usuário.
import "./components/index.js";
import { api, onUnauthorized } from "./api.js";
import { h } from "./dom.js";
import { authPage } from "./pages/auth.js";
import { homePage } from "./pages/home.js";
import { mapPage } from "./pages/map.js";
import { profilesPage } from "./pages/profiles.js";
import { progressPage } from "./pages/progress.js";
import { sessionPage } from "./pages/session.js";
import { summaryPage } from "./pages/summary.js";
import { currentPath, navigate, route, setGuard, start } from "./router.js";
import { store } from "./store.js";

const PUBLIC = ["/entrar"];
const NO_PROFILE_NEEDED = ["/perfis"];

route("/", homePage, { title: "Início" });
route("/entrar", authPage, { title: "Entrar" });
route("/perfis", profilesPage, { title: "Perfis" });
route("/mapa/:subject", mapPage, { title: "Mapa" });
route("/jogar/:sessionId", sessionPage, { title: "Jogando" });
route("/resultado/:sessionId", summaryPage, { title: "Resultado" });
route("/progresso", progressPage, { title: "Progresso" });

setGuard((path) => {
  if (!store.state.me) return PUBLIC.includes(path) ? null : "/entrar";
  if (PUBLIC.includes(path)) return "/perfis";
  if (!store.profile && !NO_PROFILE_NEEDED.includes(path)) return "/perfis";
  return null;
});

onUnauthorized(() => {
  store.signOut();
  navigate("/entrar");
});

// ---------- Cabeçalho ----------
const header = document.getElementById("app-header");

function renderHeader() {
  const { me } = store.state;
  const profile = store.profile;
  const path = currentPath();
  const playing = path.startsWith("/jogar/");

  const nav = [];
  if (me && profile && !playing) {
    nav.push(
      h("a", { href: "#/", class: "nav-link", "aria-current": path === "/" ? "page" : null }, "Início"),
      h("a", { href: "#/progresso", class: "nav-link", "aria-current": path === "/progresso" ? "page" : null }, "Progresso"),
      h("a", { href: "#/perfis", class: "profile-chip", title: "Trocar de perfil" }, h("span", { "aria-hidden": "true" }, profile.avatar), h("span", {}, profile.nickname)),
    );
  }
  if (me && !playing) {
    nav.push(h("button", { type: "button", class: "btn btn-ghost btn-small", onClick: logout }, "Sair"));
  }

  header.replaceChildren(
    h(
      "div",
      { class: "container header-inner" },
      h("a", { href: "#/", class: "brand" }, h("span", { "aria-hidden": "true" }, "🧭"), " Expedição Numérica"),
      h("nav", { class: "header-nav", "aria-label": "Principal" }, nav),
    ),
  );
  document.body.classList.toggle("is-playing", playing);
}

async function logout() {
  try {
    await api.logout();
  } finally {
    store.signOut();
    navigate("/entrar");
  }
}

store.subscribe(renderHeader);
window.addEventListener("hashchange", renderHeader);

// ---------- Inicialização ----------
async function boot() {
  try {
    store.setMe(await api.me());
  } catch {
    store.setMe(null);
  }
  renderHeader();
  start(document.getElementById("app"));
}

boot();
