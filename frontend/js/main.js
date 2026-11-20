// Ponto de entrada: monta o cabeçalho, registra as rotas e verifica a sessão do usuário.
import "./components/index.js";
import { api, onUnauthorized } from "./api.js";
import { h } from "./dom.js";
import { applyPrefs } from "./prefs.js";
import { authPage, saveProgressPage } from "./pages/auth.js";
import { avatarPage } from "./pages/avatar.js";
import { classroomPage, classroomsPage } from "./pages/classrooms.js";
import { guardianPage } from "./pages/guardian.js";
import { settingsPage } from "./pages/settings.js";
import { avatarView } from "./pages/shared.js";
import { stopSpeaking } from "./voice.js";
import { homePage } from "./pages/home.js";
import { mapPage } from "./pages/map.js";
import { profilesPage } from "./pages/profiles.js";
import { progressPage } from "./pages/progress.js";
import { sessionPage } from "./pages/session.js";
import { summaryPage } from "./pages/summary.js";
import { currentPath, navigate, route, setGuard, start } from "./router.js";
import { store } from "./store.js";

applyPrefs();

const PUBLIC = ["/entrar", "/conta", "/cadastro"];
// Páginas do adulto: não exigem escolher um perfil de jogador.
const NO_PROFILE_NEEDED = ["/perfis", "/responsavel", "/turmas", "/config", "/salvar"];

route("/", homePage, { title: "Início" });
route("/entrar", () => authPage("jogar"), { title: "Jogar" });
route("/conta", () => authPage("entrar"), { title: "Entrar" });
route("/cadastro", () => authPage("criar"), { title: "Criar conta" });
route("/perfis", profilesPage, { title: "Perfis" });
route("/mapa/:subject", mapPage, { title: "Mapa" });
route("/jogar/:sessionId", sessionPage, { title: "Jogando" });
route("/resultado/:sessionId", summaryPage, { title: "Resultado" });
route("/progresso", progressPage, { title: "Progresso" });
route("/avatar", avatarPage, { title: "Meu avatar" });
route("/salvar", saveProgressPage, { title: "Salvar progresso" });
route("/config", settingsPage, { title: "Configurações" });
route("/responsavel", guardianPage, { title: "Área do responsável" });
route("/turmas", classroomsPage, { title: "Turmas" });
route("/turmas/:id", classroomPage, { title: "Turma" });

setGuard((path) => {
  if (!store.state.me) return PUBLIC.includes(path) ? null : "/entrar";
  if (PUBLIC.includes(path)) return "/perfis";
  if (!store.profile && !NO_PROFILE_NEEDED.some((p) => path === p || path.startsWith(`${p}/`))) return "/perfis";
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

  // Link com ícone + texto; no celular só o ícone aparece (o texto vira o rótulo acessível).
  const link = (href, icon, label, current, extra = {}) =>
    h(
      "a",
      { href, class: "nav-link", title: label, "aria-label": label, "aria-current": current ? "page" : null, ...extra },
      h("span", { class: "nav-ico", "aria-hidden": "true" }, icon),
      h("span", { class: "nav-text" }, label),
    );

  const nav = [];
  if (me && profile && !playing) {
    nav.push(
      link("#/", "🏠", "Início", path === "/"),
      link("#/progresso", "📈", "Progresso", path === "/progresso"),
      h("a", { href: "#/avatar", class: "profile-chip", title: "Meu avatar" }, avatarView(profile, "sm"), h("span", { class: "nav-text" }, profile.nickname)),
    );
  }
  if (me && !playing) {
    if (me.isGuest) {
      nav.push(
        h(
          "a",
          { href: "#/salvar", class: "btn btn-primary btn-small save-btn", title: "Salvar progresso", "aria-label": "Salvar progresso", "aria-current": path === "/salvar" ? "page" : null },
          h("span", { "aria-hidden": "true" }, "💾"),
          h("span", { class: "nav-text" }, " Salvar progresso"),
        ),
      );
    }
    nav.push(
      link("#/perfis", "👥", "Perfis", path === "/perfis"),
      link("#/responsavel", "👪", "Responsável", path.startsWith("/responsavel") || path.startsWith("/turmas")),
      link("#/config", "⚙️", "Configurações", path === "/config", { class: "nav-link nav-settings" }),
      h("button", { type: "button", class: "btn btn-ghost btn-small logout-btn", onClick: logout, title: "Sair", "aria-label": "Sair" }, h("span", { class: "nav-ico", "aria-hidden": "true" }, "🚪"), h("span", { class: "nav-text" }, "Sair")),
    );
  }

  header.replaceChildren(
    h(
      "div",
      { class: "container header-inner" },
      h("a", { href: me ? "#/" : "/", class: "brand", "aria-label": "Expedição Numérica" }, h("span", { "aria-hidden": "true" }, "🧭"), h("span", { class: "brand-text" }, " Expedição Numérica")),
      h("nav", { class: "header-nav", "aria-label": "Principal" }, nav),
    ),
  );
  document.body.classList.toggle("is-playing", playing);
}

async function logout() {
  // Convidado não tem como entrar de novo: sair apaga o progresso deste aparelho.
  if (store.state.me?.isGuest) {
    const ok = confirm("Você está jogando sem conta. Se sair, o progresso deste aparelho será APAGADO e não dá para recuperar.\n\nPara guardar, cancele e use \"Salvar progresso\".\n\nSair e apagar mesmo assim?");
    if (!ok) return;
    try {
      await api.deleteAccount();
    } finally {
      store.signOut();
      navigate("/entrar");
    }
    return;
  }
  try {
    await api.logout();
    // Aparelhos compartilhados: apaga os dados de progresso guardados para uso offline.
    navigator.serviceWorker?.controller?.postMessage({ type: "clear-data" });
  } finally {
    store.signOut();
    navigate("/entrar");
  }
}

store.subscribe(renderHeader);
window.addEventListener("hashchange", () => {
  stopSpeaking();
  renderHeader();
});

// ---------- PWA ----------
if ("serviceWorker" in navigator) {
  window.addEventListener("load", () => {
    navigator.serviceWorker.register("/sw.js").catch((err) => console.warn("Service worker não registrado:", err));
  });
}

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
