import { api } from "../api.js";
import { errorMessage, formData, h } from "../dom.js";
import { navigate } from "../router.js";
import { store } from "../store.js";
import { avatarView } from "./shared.js";
import { getLang, t } from "../i18n.js";

/** Ano escolar 1–12: "3º ano (Fundamental)" / "Grade 3" / "3.º grado". */
function schoolGradeLabel(year) {
  if (getLang() === "pt") return year <= 9 ? `${year}º ano (Fundamental)` : `${year - 9}ª série (Médio)`;
  return t("{n}º ano", { n: year });
}

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
          avatarView(p, "lg"),
          h("span", { class: "profile-name" }, p.nickname),
        ),
      ),
    ),
  );

  const error = h("p", { class: "form-error", role: "alert", hidden: true });

  const grades = [["", t("Prefiro não dizer")], ...Array.from({ length: 12 }, (_, i) => [String(i + 1), schoolGradeLabel(i + 1)])];

  const form = h(
    "form",
    { class: "stack", novalidate: true, onSubmit: onCreate },
    h("label", { class: "field" }, h("span", {}, t("Apelido")), h("input", { name: "nickname", maxlength: "20", autocomplete: "off", required: true, placeholder: t("Ex.: Luna") })),
    h("small", { class: "muted" }, t("Use um apelido, não o nome completo.")),
    h(
      "fieldset",
      { class: "avatar-picker" },
      h("legend", {}, t("Escolha um avatar")),
      avatars.map((a, i) =>
        h("label", { class: "avatar-option" }, h("input", { type: "radio", name: "avatar", value: a, checked: i === 0 }), h("span", { "aria-hidden": "true" }, a), h("span", { class: "visually-hidden" }, t("Avatar {n}", { n: i + 1 }))),
      ),
    ),
    h(
      "label",
      { class: "field" },
      h("span", {}, t("Ano escolar (opcional)")),
      h("select", { name: "schoolGrade" }, grades.map(([value, label]) => h("option", { value }, label))),
    ),
    error,
    h("button", { type: "submit", class: "btn btn-primary btn-block" }, t("Criar perfil")),
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
    h("summary", {}, creating ? t("Crie o primeiro perfil") : t("+ Novo perfil")),
    form,
  );

  return h(
    "section",
    { class: "page-narrow" },
    h("h1", {}, creating ? t("Quem vai jogar?") : t("Quem vai jogar agora?")),
    profiles.length ? list : h("p", { class: "muted" }, t("Cada criança tem seu próprio perfil e seu próprio progresso.")),
    createSection,
  );
}
