// Ponto de entrada: monta o cabeçalho, registra as rotas e verifica a sessão do usuário.
import "./components/index.js";
import { api, onUnauthorized } from "./api.js";
import { h } from "./dom.js";
import { getLang, LANGUAGES, setLang, sitePath, t } from "./i18n.js";
import { applyPrefs } from "./prefs.js";
import { captureOrigin } from "./origin.js";
import { authPage, forgotPasswordPage, resetPasswordPage, saveProgressPage } from "./pages/auth.js";
import { avatarPage } from "./pages/avatar.js";
import { bnccPage } from "./pages/bncc.js";
import { plansPage } from "./pages/plans.js";
import { weeklyPage } from "./pages/weekly.js";
import { worksheetsPage } from "./pages/worksheets.js";
import { classroomPage, classroomsPage } from "./pages/classrooms.js";
import { guardianPage } from "./pages/guardian.js";
import { settingsPage } from "./pages/settings.js";
import { avatarView } from "./pages/shared.js";
import { stopSpeaking } from "./voice.js";
import { gamesPage } from "./pages/games.js";
import { homePage } from "./pages/home.js";
import { mapPage } from "./pages/map.js";
import { profilesPage } from "./pages/profiles.js";
import { progressPage } from "./pages/progress.js";
import { sessionPage } from "./pages/session.js";
import { summaryPage } from "./pages/summary.js";
import { currentPath, navigate, route, setGuard, start } from "./router.js";
import { store } from "./store.js";

applyPrefs();
captureOrigin();
// Textos fixos do jogar.html (o resto da página é montado já no idioma certo).
document.querySelector(".skip-link")?.replaceChildren(t("Pular para o conteúdo"));
document.querySelector("noscript p")?.replaceChildren(t("Este jogo precisa de JavaScript para funcionar."));

const PUBLIC = ["/entrar", "/conta", "/cadastro"];
// Páginas do adulto: não exigem escolher um perfil de jogador.
const NO_PROFILE_NEEDED = ["/perfis", "/responsavel", "/turmas", "/config", "/salvar", "/bncc", "/planos", "/semana", "/fichas"];

route("/", homePage, { title: "Início" });
route("/entrar", () => authPage("jogar"), { title: "Jogar" });
route("/conta", () => authPage("entrar"), { title: "Entrar" });
route("/cadastro", () => authPage("criar"), { title: "Criar conta" });
route("/esqueci", forgotPasswordPage, { title: "Esqueci minha senha" });
route("/nova-senha/:userId/:token", resetPasswordPage, { title: "Nova senha" });
route("/perfis", profilesPage, { title: "Perfis" });
route("/jogos", gamesPage, { title: "Jogos" });
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
route("/bncc", bnccPage, { title: "Currículo e BNCC" });
route("/planos", plansPage, { title: "Planos" });
route("/semana/:profileId", weeklyPage, { title: "Relatório da semana" });
route("/fichas", worksheetsPage, { title: "Fichas para imprimir" });

// Abertas para todos, com ou sem conta (ex.: professor avaliando o jogo).
const OPEN = ["/bncc", "/planos"];

setGuard((path) => {
  if (OPEN.includes(path)) return null;
  // Link do e-mail de nova senha: abre mesmo com outra conta (ou convidado) neste aparelho.
  if (path.startsWith("/nova-senha/")) return null;
  if (path === "/esqueci") return store.state.me && !store.state.me.isGuest ? "/perfis" : null;
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
      link("#/", "🏠", t("Início"), path === "/"),
      link("#/jogos", "🎮", t("Jogos"), path === "/jogos"),
      link("#/progresso", "📈", t("Progresso"), path === "/progresso"),
      h("a", { href: "#/avatar", class: "profile-chip", title: t("Meu avatar") }, avatarView(profile, "sm"), h("span", { class: "nav-text" }, profile.nickname)),
    );
  }
  if (me && !playing) {
    if (me.isGuest) {
      nav.push(
        h(
          "a",
          { href: "#/salvar", class: "btn btn-primary btn-small save-btn", title: t("Salvar progresso"), "aria-label": t("Salvar progresso"), "aria-current": path === "/salvar" ? "page" : null },
          h("span", { "aria-hidden": "true" }, "💾"),
          h("span", { class: "nav-text" }, ` ${t("Salvar progresso")}`),
        ),
      );
    }
    nav.push(
      link("#/perfis", "👥", t("Perfis"), path === "/perfis"),
      link("#/responsavel", "👪", t("Responsável"), path.startsWith("/responsavel") || path.startsWith("/turmas") || path.startsWith("/semana") || ["/bncc", "/fichas"].includes(path)),
      me.isGuest ? null : link("#/planos", "⭐", t("Planos"), path === "/planos", { class: "nav-link nav-plans" }),
      link("#/config", "⚙️", t("Configurações"), path === "/config", { class: "nav-link nav-settings" }),
      h("button", { type: "button", class: "btn btn-ghost btn-small logout-btn", onClick: logout, title: t("Sair"), "aria-label": t("Sair") }, h("span", { class: "nav-ico", "aria-hidden": "true" }, "🚪"), h("span", { class: "nav-text" }, t("Sair"))),
    );
  }
  // Antes de entrar (sem conta), o idioma fica à mão no cabeçalho; depois, em Configurações.
  if (!me && !playing) nav.push(languagePicker());

  header.replaceChildren(
    h(
      "div",
      { class: "container header-inner" },
      h("a", { href: me ? "#/" : sitePath("home"), class: "brand", "aria-label": "Educa Game" }, h("span", { "aria-hidden": "true" }, "🧭"), h("span", { class: "brand-text" }, " Educa Game")),
      h("nav", { class: "header-nav", "aria-label": t("Principal") }, nav),
    ),
  );
  document.body.classList.toggle("is-playing", playing);
}

/** Seletor de idioma compacto (🌐 + lista). Trocar recarrega a página no idioma novo. */
export function languagePicker() {
  return h(
    "label",
    { class: "lang-picker", title: t("Idioma") },
    h("span", { "aria-hidden": "true" }, "🌐"),
    h("span", { class: "visually-hidden" }, t("Idioma")),
    h(
      "select",
      { class: "lang-select", onChange: (e) => setLang(e.target.value) },
      LANGUAGES.map((l) => h("option", { value: l.code, selected: l.code === getLang() }, l.label)),
    ),
  );
}

async function logout() {
  // Convidado não tem como entrar de novo: sair apaga o progresso deste aparelho.
  if (store.state.me?.isGuest) {
    const ok = confirm(t("Você está jogando sem conta. Se sair, o progresso deste aparelho será APAGADO e não dá para recuperar.\n\nPara guardar, cancele e use \"Salvar progresso\".\n\nSair e apagar mesmo assim?"));
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
