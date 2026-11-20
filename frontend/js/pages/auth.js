import { api } from "../api.js";
import { errorMessage, formData, h } from "../dom.js";
import { navigate } from "../router.js";
import { store } from "../store.js";

export function authPage() {
  let mode = "login";

  const error = h("p", { class: "form-error", role: "alert", hidden: true });
  const title = h("h1", {}, "Entrar");
  const submit = h("button", { type: "submit", class: "btn btn-primary btn-block" }, "Entrar");

  const consent = h(
    "label",
    { class: "checkbox", hidden: true },
    h("input", { type: "checkbox", name: "acceptTerms", value: "true" }),
    h(
      "span",
      {},
      "Sou maior de idade e responsável pelas crianças que vão jogar. Autorizo o uso dos dados de progresso (apelido e respostas) apenas para o funcionamento do jogo.",
    ),
  );

  const passwordHint = h("small", { class: "muted", hidden: true }, "Mínimo de 8 caracteres.");

  const form = h(
    "form",
    { class: "stack", novalidate: true, onSubmit: onSubmit },
    h("label", { class: "field" }, h("span", {}, "E-mail do responsável"), h("input", { type: "email", name: "email", autocomplete: "email", required: true })),
    h(
      "label",
      { class: "field" },
      h("span", {}, "Senha"),
      h("input", { type: "password", name: "password", autocomplete: "current-password", required: true, minlength: "8" }),
      passwordHint,
    ),
    consent,
    error,
    submit,
  );

  const tabLogin = h("button", { type: "button", role: "tab", class: "tab active", "aria-selected": "true", onClick: () => setMode("login") }, "Entrar");
  const tabRegister = h("button", { type: "button", role: "tab", class: "tab", "aria-selected": "false", onClick: () => setMode("register") }, "Criar conta");

  function setMode(next) {
    mode = next;
    const register = mode === "register";
    title.textContent = register ? "Criar conta" : "Entrar";
    submit.textContent = register ? "Criar conta" : "Entrar";
    consent.hidden = !register;
    passwordHint.hidden = !register;
    form.elements.password.autocomplete = register ? "new-password" : "current-password";
    tabLogin.classList.toggle("active", !register);
    tabRegister.classList.toggle("active", register);
    tabLogin.setAttribute("aria-selected", String(!register));
    tabRegister.setAttribute("aria-selected", String(register));
    error.hidden = true;
  }

  async function onSubmit(e) {
    e.preventDefault();
    const data = formData(form);
    error.hidden = true;

    if (!data.email || !data.password) return showError("Preencha e-mail e senha.");
    if (mode === "register" && !data.acceptTerms) return showError("Para criar a conta, marque a autorização.");

    submit.disabled = true;
    try {
      const me = mode === "register" ? await api.register(data.email, data.password, true) : await api.login(data.email, data.password);
      store.setMe(me);
      navigate("/perfis");
    } catch (err) {
      showError(errorMessage(err));
    } finally {
      submit.disabled = false;
    }
  }

  function showError(msg) {
    error.textContent = msg;
    error.hidden = false;
  }

  return h(
    "section",
    { class: "page-narrow auth" },
    h("div", { class: "hero" }, h("div", { class: "hero-icon", "aria-hidden": "true" }, "🧭"), h("p", { class: "muted" }, "Matemática e lógica em uma expedição cheia de desafios.")),
    h(
      "div",
      { class: "card" },
      h("div", { class: "tabs", role: "tablist" }, tabLogin, tabRegister),
      title,
      form,
      h("p", { class: "muted small" }, "A conta é do adulto responsável. As crianças jogam com perfis (só apelido e avatar, sem dados pessoais)."),
    ),
  );
}
