import { api } from "../api.js";
import { errorMessage, formData, h } from "../dom.js";
import { navigate } from "../router.js";
import { store } from "../store.js";
import { sitePath, t } from "../i18n.js";

const CONSENT_TEXT =
  t("Sou maior de idade e responsável pelas crianças que vão jogar. Autorizo o uso dos dados de progresso (apelido e respostas) apenas para o funcionamento do jogo.");

/** Autorização com links para os termos e a política (abrem em outra aba para não perder o formulário). */
function consentLabel() {
  return h(
    "label",
    { class: "checkbox" },
    h("input", { type: "checkbox", name: "acceptTerms", value: "true" }),
    h(
      "span",
      {},
      CONSENT_TEXT,
      ` ${t("Li e aceito os")} `,
      h("a", { href: sitePath("terms"), target: "_blank", rel: "noopener" }, t("termos de uso")),
      ` ${t("e a")} `,
      h("a", { href: sitePath("privacy"), target: "_blank", rel: "noopener" }, t("política de privacidade")),
      ".",
    ),
  );
}

/** Grade de avatares (rádio) reaproveitada na tela de início. */
function avatarPicker(avatars) {
  return h(
    "fieldset",
    { class: "avatar-picker" },
    h("legend", {}, t("Escolha um avatar")),
    avatars.map((a, i) =>
      h("label", { class: "avatar-option" }, h("input", { type: "radio", name: "avatar", value: a, checked: i === 0 }), h("span", { "aria-hidden": "true" }, a), h("span", { class: "visually-hidden" }, t("Avatar {n}", { n: i + 1 }))),
    ),
  );
}

/** Tela de início. A aba inicial pode ser "jogar" (padrão), "entrar" ou "criar". */
export async function authPage(initialTab) {
  const firstTab = ["jogar", "entrar", "criar"].includes(initialTab) ? initialTab : "jogar";
  const avatars = await api.avatars().catch(() => ["🦊", "🐼", "🐯", "🐸"]);

  // ---------- Jogar agora (sem cadastro) ----------
  const playError = h("p", { class: "form-error", role: "alert", hidden: true });
  const playButton = h("button", { type: "submit", class: "btn btn-primary btn-block btn-big" }, t("Começar a jogar"));
  const playForm = h(
    "form",
    {
      class: "stack",
      novalidate: true,
      onSubmit: async (e) => {
        e.preventDefault();
        const data = formData(playForm);
        playError.hidden = true;
        if (!data.nickname?.trim()) return showError(playError, t("Escolha um apelido para começar."));
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
    h("label", { class: "field" }, h("span", {}, t("Como você quer ser chamado?")), h("input", { name: "nickname", maxlength: "20", autocomplete: "off", placeholder: t("Ex.: Luna"), required: true })),
    h("small", { class: "muted" }, t("Use um apelido, não o nome completo.")),
    avatarPicker(avatars),
    playError,
    playButton,
    h("p", { class: "muted small" }, t("Sem cadastro: o progresso fica guardado neste aparelho. Quando quiser jogar em outro aparelho ou não correr o risco de perder, é só salvar o progresso com uma conta.")),
  );

  // ---------- Entrar / Criar conta ----------
  let accountMode = "entrar";
  const accountError = h("p", { class: "form-error", role: "alert", hidden: true });
  const accountTitle = h("h2", {}, t("Entrar"));
  const accountSubmit = h("button", { type: "submit", class: "btn btn-primary btn-block" }, t("Entrar"));
  const consent = consentLabel();
  consent.hidden = true;
  const passwordHint = h("small", { class: "muted", hidden: true }, t("Mínimo de 8 caracteres."));
  const forgotLink = h("a", { href: "#/esqueci", class: "small forgot-link" }, t("Esqueci minha senha"));

  const accountForm = h(
    "form",
    { class: "stack", novalidate: true, onSubmit: onAccountSubmit },
    h("label", { class: "field" }, h("span", {}, t("E-mail")), h("input", { type: "email", name: "email", autocomplete: "email", required: true })),
    h(
      "label",
      { class: "field" },
      h("span", {}, t("Senha")),
      h("input", { type: "password", name: "password", autocomplete: "current-password", required: true, minlength: "8" }),
      passwordHint,
    ),
    forgotLink,
    consent,
    accountError,
    accountSubmit,
  );

  async function onAccountSubmit(e) {
    e.preventDefault();
    const data = formData(accountForm);
    accountError.hidden = true;
    if (!data.email || !data.password) return showError(accountError, t("Preencha e-mail e senha."));
    if (accountMode === "criar" && !data.acceptTerms) return showError(accountError, t("Para criar a conta, marque a autorização."));
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
    jogar: h("div", {}, h("h1", {}, t("Bora jogar!")), playForm),
    conta: h("div", {}, accountTitle, accountForm),
  };
  const tabPlay = h("button", { type: "button", role: "tab", class: "tab" }, t("Jogar agora"));
  const tabLogin = h("button", { type: "button", role: "tab", class: "tab" }, t("Entrar"));
  const tabCreate = h("button", { type: "button", role: "tab", class: "tab" }, t("Criar conta"));

  function setTab(tab) {
    const isPlay = tab === "jogar";
    panels.jogar.hidden = !isPlay;
    panels.conta.hidden = isPlay;
    if (!isPlay) {
      accountMode = tab;
      const create = tab === "criar";
      accountTitle.textContent = create ? t("Criar conta (responsável)") : t("Entrar");
      accountSubmit.textContent = create ? t("Criar conta") : t("Entrar");
      consent.hidden = !create;
      passwordHint.hidden = !create;
      forgotLink.hidden = create;
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
    h("div", { class: "hero" }, h("div", { class: "hero-icon", "aria-hidden": "true" }, "🧭"), h("p", { class: "muted" }, t("Matemática e lógica em uma expedição cheia de desafios."))),
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
    return h("section", { class: "page-narrow" }, h("h1", {}, t("Progresso salvo ✅")), h("p", { class: "muted" }, t("Sua conta ({email}) já guarda o progresso. Entre com ela em qualquer aparelho.", { email: me?.email ?? "" })), h("a", { href: "#/", class: "btn btn-primary" }, t("Voltar ao início")));
  }

  const error = h("p", { class: "form-error", role: "alert", hidden: true });
  const submit = h("button", { type: "submit", class: "btn btn-primary btn-block" }, t("Salvar meu progresso"));
  const form = h(
    "form",
    {
      class: "stack",
      novalidate: true,
      onSubmit: async (e) => {
        e.preventDefault();
        const data = formData(form);
        error.hidden = true;
        if (!data.email || !data.password) return showError(error, t("Preencha e-mail e senha."));
        if (!data.acceptTerms) return showError(error, t("Peça para um responsável marcar a autorização."));
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
    h("label", { class: "field" }, h("span", {}, t("E-mail do responsável")), h("input", { type: "email", name: "email", autocomplete: "email", required: true })),
    h("label", { class: "field" }, h("span", {}, t("Senha")), h("input", { type: "password", name: "password", autocomplete: "new-password", required: true, minlength: "8" }), h("small", { class: "muted" }, t("Mínimo de 8 caracteres."))),
    consentLabel(),
    error,
    submit,
  );

  return h(
    "section",
    { class: "page-narrow" },
    h("h1", {}, t("💾 Salvar progresso")),
    h(
      "div",
      { class: "card stack" },
      h("p", {}, t("Hoje o seu progresso está guardado só neste aparelho. Com uma conta você:")),
      h("ul", { class: "benefits" }, h("li", {}, t("não perde nada se trocar ou limpar o aparelho;")), h("li", {}, t("continua jogando no celular, no tablet ou no computador;")), h("li", {}, t("pode participar de turmas da escola."))),
      h("p", { class: "muted small" }, t("Tudo o que você já conquistou continua: medalhas, estrelas, níveis e XP.")),
      form,
    ),
  );
}

/** t("Esqueci minha senha"): pede o e-mail e manda o link. A resposta é sempre a mesma (não revela quem tem conta). */
export function forgotPasswordPage() {
  const error = h("p", { class: "form-error", role: "alert", hidden: true });
  const submit = h("button", { type: "submit", class: "btn btn-primary btn-block" }, t("Enviar link"));
  const done = h("div", { class: "notice success card stack", role: "status", hidden: true });
  const form = h(
    "form",
    {
      class: "stack",
      novalidate: true,
      onSubmit: async (e) => {
        e.preventDefault();
        const { email } = formData(form);
        error.hidden = true;
        if (!email?.trim()) return showError(error, t("Informe o e-mail da conta."));
        submit.disabled = true;
        try {
          const res = await api.forgotPassword(email.trim());
          done.replaceChildren(
            h("p", {}, "📬 ", res?.message ?? t("Se houver uma conta com este e-mail, enviamos um link para criar uma senha nova.")),
            h("p", { class: "muted small" }, t("O link vale por 2 horas. Não chegou? Confira a caixa de spam ou peça de novo em alguns minutos.")),
          );
          done.hidden = false;
          form.hidden = true;
        } catch (err) {
          showError(error, errorMessage(err));
        } finally {
          submit.disabled = false;
        }
      },
    },
    h("label", { class: "field" }, h("span", {}, t("E-mail da conta")), h("input", { type: "email", name: "email", autocomplete: "email", required: true })),
    error,
    submit,
  );
  return h(
    "section",
    { class: "page-narrow" },
    h("h1", {}, t("🔑 Esqueci minha senha")),
    h("div", { class: "card stack" }, h("p", {}, t("Informe o e-mail da conta. Vamos mandar um link para você criar uma senha nova.")), form, done),
    h("p", { class: "small" }, h("a", { href: "#/conta" }, t("← Voltar para entrar"))),
  );
}

/** Link do e-mail: escolhe a senha nova e já entra. */
export function resetPasswordPage({ userId, token }) {
  const error = h("p", { class: "form-error", role: "alert", hidden: true });
  const submit = h("button", { type: "submit", class: "btn btn-primary btn-block" }, t("Salvar nova senha"));
  const form = h(
    "form",
    {
      class: "stack",
      novalidate: true,
      onSubmit: async (e) => {
        e.preventDefault();
        const { password, confirm } = formData(form);
        error.hidden = true;
        if (!password || password.length < 8) return showError(error, t("A senha precisa ter pelo menos 8 caracteres."));
        if (password !== confirm) return showError(error, t("As duas senhas não são iguais."));
        submit.disabled = true;
        try {
          store.signOut();
          store.setMe(await api.resetPassword(userId, token, password));
          navigate("/perfis", { replace: true });
        } catch (err) {
          showError(error, errorMessage(err));
          if (err?.status === 400 && /link/i.test(errorMessage(err))) error.append(" ", h("a", { href: "#/esqueci" }, t("Pedir novo link")));
        } finally {
          submit.disabled = false;
        }
      },
    },
    h("label", { class: "field" }, h("span", {}, t("Nova senha")), h("input", { type: "password", name: "password", autocomplete: "new-password", required: true, minlength: "8" }), h("small", { class: "muted" }, t("Mínimo de 8 caracteres."))),
    h("label", { class: "field" }, h("span", {}, t("Repita a nova senha")), h("input", { type: "password", name: "confirm", autocomplete: "new-password", required: true, minlength: "8" })),
    error,
    submit,
  );
  return h("section", { class: "page-narrow" }, h("h1", {}, t("🔑 Criar nova senha")), h("div", { class: "card stack" }, form));
}
