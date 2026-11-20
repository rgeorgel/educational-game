import { api } from "../api.js";
import { errorMessage, formData, h } from "../dom.js";
import { navigate } from "../router.js";
import { store } from "../store.js";

export async function profilesPage() {
  const avatars = await api.avatars();
  const profiles = store.state.me?.profiles ?? [];
  const creating = profiles.length === 0;

  const list = h(
    "ul",
    { class: "profile-list" },
    profiles.map((p) =>
      h(
        "li",
        {},
        h(
          "button",
          {
            type: "button",
            class: "profile-card",
            onClick: () => {
              store.setProfile(p.id);
              navigate("/");
            },
          },
          h("span", { class: "avatar", "aria-hidden": "true" }, p.avatar),
          h("span", { class: "profile-name" }, p.nickname),
        ),
      ),
    ),
  );

  const error = h("p", { class: "form-error", role: "alert", hidden: true });

  const grades = [["", "Prefiro não dizer"], ...Array.from({ length: 12 }, (_, i) => [String(i + 1), i < 9 ? `${i + 1}º ano (Fundamental)` : `${i - 8}ª série (Médio)`])];

  const form = h(
    "form",
    { class: "stack", novalidate: true, onSubmit: onCreate },
    h("label", { class: "field" }, h("span", {}, "Apelido"), h("input", { name: "nickname", maxlength: "20", autocomplete: "off", required: true, placeholder: "Ex.: Luna" })),
    h("small", { class: "muted" }, "Use um apelido, não o nome completo."),
    h(
      "fieldset",
      { class: "avatar-picker" },
      h("legend", {}, "Escolha um avatar"),
      avatars.map((a, i) =>
        h("label", { class: "avatar-option" }, h("input", { type: "radio", name: "avatar", value: a, checked: i === 0 }), h("span", { "aria-hidden": "true" }, a), h("span", { class: "visually-hidden" }, `Avatar ${i + 1}`)),
      ),
    ),
    h(
      "label",
      { class: "field" },
      h("span", {}, "Ano escolar (opcional)"),
      h("select", { name: "schoolGrade" }, grades.map(([value, label]) => h("option", { value }, label))),
    ),
    error,
    h("button", { type: "submit", class: "btn btn-primary btn-block" }, "Criar perfil"),
  );

  async function onCreate(e) {
    e.preventDefault();
    const data = formData(form);
    error.hidden = true;
    try {
      const profile = await api.createProfile({
        nickname: data.nickname,
        avatar: data.avatar,
        schoolGrade: data.schoolGrade ? Number(data.schoolGrade) : null,
      });
      store.addProfile(profile);
      store.setProfile(profile.id);
      navigate("/");
    } catch (err) {
      error.textContent = errorMessage(err);
      error.hidden = false;
    }
  }

  const createSection = h(
    "details",
    { class: "card create-profile", open: creating },
    h("summary", {}, creating ? "Crie o primeiro perfil" : "+ Novo perfil"),
    form,
  );

  return h(
    "section",
    { class: "page-narrow" },
    h("h1", {}, creating ? "Quem vai jogar?" : "Quem vai jogar agora?"),
    profiles.length ? list : h("p", { class: "muted" }, "Cada criança tem seu próprio perfil e seu próprio progresso."),
    createSection,
  );
}
