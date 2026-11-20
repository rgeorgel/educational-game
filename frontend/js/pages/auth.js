import { api } from "../api.js";
import { errorMessage, formData, h } from "../dom.js";
import { navigate } from "../router.js";
import { store } from "../store.js";

const CONSENT_TEXT =
  "Sou maior de idade e responsável pelas crianças que vão jogar. Autorizo o uso dos dados de progresso (apelido e respostas) apenas para o funcionamento do jogo.";

/** Grade de avatares (rádio) reaproveitada na tela de início. */
function avatarPicker(avatars) {
  return h(
    "fieldset",
    { class: "avatar-picker" },
    h("legend", {}, "Escolha um avatar"),
    avatars.map((a, i) =>
      h("label", { class: "avatar-option" }, h("input", { type: "radio", name: "avatar", value: a, checked: i === 0 }), h("span", { "aria-hidden": "true" }, a), h("span", { class: "visually-hidden" }, `Avatar ${i + 1}`)),
    ),
  );
}

/** Tela de início. A aba inicial pode ser "jogar" (padrão), "entrar" ou "criar". */
export async function authPage(initialTab) {
  const firstTab = ["jogar", "entrar", "criar"].includes(initialTab) ? initialTab : "jogar";
  const avatars = await api.avatars().catch(() => ["🦊", "🐼", "🐯", "🐸"]);

  // ---------- Jogar agora (sem cadastro) ----------
  const playError = h("p", { class: "form-error", role: "alert", hidden: true });
  const playButton = h("button", { type: "submit", class: "btn btn-primary btn-block btn-big" }, "Começar a jogar");
  const playForm = h(
    "form",
    {
      class: "stack",
      novalidate: true,
      onSubmit: async (e) => {
        e.preventDefault();
        const data = formData(playForm);
        playError.hidden = true;
        if (!data.nickname?.trim()) return showError(playError, "Escolha um apelido para começar.");
        playButton.disabled = true;
        try {
          const me = await api.startAsGuest(data.nickname, data.avatar);
          store.setMe(me);
          store.setProfile(me.profiles[0].id);
          navigate("/");
        } catch (err) {
          showError(playError, errorMessage(err));
        } finally {
          playButton.disabled = false;
        }
      },
    },
    h("label", { class: "field" }, h("span", {}, "Como você quer ser chamado?"), h("input", { name: "nickname", maxlength: "20", autocomplete: "off", placeholder: "Ex.: Luna", required: true })),
    h("small", { class: "muted" }, "Use um apelido, não o nome completo."),
    avatarPicker(avatars),
    playError,
    playButton,
    h("p", { class: "muted small" }, "Sem cadastro: o progresso fica guardado neste aparelho. Quando quiser jogar em outro aparelho ou não correr o risco de perder, é só salvar o progresso com uma conta."),
  );

  // ---------- Entrar / Criar conta ----------
  let accountMode = "entrar";
  const accountError = h("p", { class: "form-error", role: "alert", hidden: true });
  const accountTitle = h("h2", {}, "Entrar");
  const accountSubmit = h("button", { type: "submit", class: "btn btn-primary btn-block" }, "Entrar");
  const consent = h("label", { class: "checkbox", hidden: true }, h("input", { type: "checkbox", name: "acceptTerms", value: "true" }), h("span", {}, CONSENT_TEXT));
  const passwordHint = h("small", { class: "muted", hidden: true }, "Mínimo de 8 caracteres.");

  const accountForm = h(
    "form",
    { class: "stack", novalidate: true, onSubmit: onAccountSubmit },
    h("label", { class: "field" }, h("span", {}, "E-mail"), h("input", { type: "email", name: "email", autocomplete: "email", required: true })),
    h(
      "label",
      { class: "field" },
      h("span", {}, "Senha"),
      h("input", { type: "password", name: "password", autocomplete: "current-password", required: true, minlength: "8" }),
      passwordHint,
    ),
    consent,
    accountError,
    accountSubmit,
  );

  async function onAccountSubmit(e) {
    e.preventDefault();
    const data = formData(accountForm);
    accountError.hidden = true;
    if (!data.email || !data.password) return showError(accountError, "Preencha e-mail e senha.");
    if (accountMode === "criar" && !data.acceptTerms) return showError(accountError, "Para criar a conta, marque a autorização.");
    accountSubmit.disabled = true;
    try {
      const me = accountMode === "criar" ? await api.register(data.email, data.password, true) : await api.login(data.email, data.password);
      store.setMe(me);
      navigate("/perfis");
    } catch (err) {
      showError(accountError, errorMessage(err));
    } finally {
      accountSubmit.disabled = false;
    }
  }

  // ---------- Abas ----------
  const panels = {
    jogar: h("div", {}, h("h1", {}, "Bora jogar!"), playForm),
    conta: h("div", {}, accountTitle, accountForm),
  };
  const tabPlay = h("button", { type: "button", role: "tab", class: "tab" }, "Jogar agora");
  const tabLogin = h("button", { type: "button", role: "tab", class: "tab" }, "Entrar");
  const tabCreate = h("button", { type: "button", role: "tab", class: "tab" }, "Criar conta");

  function setTab(tab) {
    const isPlay = tab === "jogar";
    panels.jogar.hidden = !isPlay;
    panels.conta.hidden = isPlay;
    if (!isPlay) {
      accountMode = tab;
      const create = tab === "criar";
      accountTitle.textContent = create ? "Criar conta (responsável)" : "Entrar";
      accountSubmit.textContent = create ? "Criar conta" : "Entrar";
      consent.hidden = !create;
      passwordHint.hidden = !create;
      accountForm.elements.password.autocomplete = create ? "new-password" : "current-password";
      accountError.hidden = true;
    }
    for (const [btn, name] of [[tabPlay, "jogar"], [tabLogin, "entrar"], [tabCreate, "criar"]]) {
      btn.classList.toggle("active", name === tab);
      btn.setAttribute("aria-selected", String(name === tab));
    }
  }
  tabPlay.addEventListener("click", () => setTab("jogar"));
  tabLogin.addEventListener("click", () => setTab("entrar"));
  tabCreate.addEventListener("click", () => setTab("criar"));
  setTab(firstTab);

  return h(
    "section",
    { class: "page-narrow auth" },
    h("div", { class: "hero" }, h("div", { class: "hero-icon", "aria-hidden": "true" }, "🧭"), h("p", { class: "muted" }, "Matemática e lógica em uma expedição cheia de desafios.")),
    h("div", { class: "card" }, h("div", { class: "tabs", role: "tablist" }, tabPlay, tabLogin, tabCreate), panels.jogar, panels.conta),
  );
}

function showError(box, msg) {
  box.textContent = msg;
  box.hidden = false;
}

/** Página "Salvar progresso": transforma o convidado em conta com e-mail e senha. */
export function saveProgressPage() {
  const me = store.state.me;
  if (!me?.isGuest) {
    return h("section", { class: "page-narrow" }, h("h1", {}, "Progresso salvo ✅"), h("p", { class: "muted" }, `Sua conta (${me?.email ?? ""}) já guarda o progresso. Entre com ela em qualquer aparelho.`), h("a", { href: "#/", class: "btn btn-primary" }, "Voltar ao início"));
  }

  const error = h("p", { class: "form-error", role: "alert", hidden: true });
  const submit = h("button", { type: "submit", class: "btn btn-primary btn-block" }, "Salvar meu progresso");
  const form = h(
    "form",
    {
      class: "stack",
      novalidate: true,
      onSubmit: async (e) => {
        e.preventDefault();
        const data = formData(form);
        error.hidden = true;
        if (!data.email || !data.password) return showError(error, "Preencha e-mail e senha.");
        if (!data.acceptTerms) return showError(error, "Peça para um responsável marcar a autorização.");
        submit.disabled = true;
        try {
          store.setMe(await api.saveProgress(data.email, data.password, true));
          navigate("/salvar");
        } catch (err) {
          showError(error, errorMessage(err));
        } finally {
          submit.disabled = false;
        }
      },
    },
    h("label", { class: "field" }, h("span", {}, "E-mail do responsável"), h("input", { type: "email", name: "email", autocomplete: "email", required: true })),
    h("label", { class: "field" }, h("span", {}, "Senha"), h("input", { type: "password", name: "password", autocomplete: "new-password", required: true, minlength: "8" }), h("small", { class: "muted" }, "Mínimo de 8 caracteres.")),
    h("label", { class: "checkbox" }, h("input", { type: "checkbox", name: "acceptTerms", value: "true" }), h("span", {}, CONSENT_TEXT)),
    error,
    submit,
  );

  return h(
    "section",
    { class: "page-narrow" },
    h("h1", {}, "💾 Salvar progresso"),
    h(
      "div",
      { class: "card stack" },
      h("p", {}, "Hoje o seu progresso está guardado só neste aparelho. Com uma conta você:"),
      h("ul", { class: "benefits" }, h("li", {}, "não perde nada se trocar ou limpar o aparelho;"), h("li", {}, "continua jogando no celular, no tablet ou no computador;"), h("li", {}, "pode participar de turmas da escola.")),
      h("p", { class: "muted small" }, "Tudo o que você já conquistou continua: medalhas, estrelas, níveis e XP."),
      form,
    ),
  );
}
