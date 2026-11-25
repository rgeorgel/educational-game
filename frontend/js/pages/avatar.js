import { api } from "../api.js";
import { errorMessage, h } from "../dom.js";
import { store } from "../store.js";
import { avatarView } from "./shared.js";
import { t } from "../i18n.js";

export async function avatarPage() {
  const id = store.profile.id;
  const options = await api.avatarOptions(id);
  const choice = { avatar: options.profile.avatar, accessory: options.profile.accessory, color: options.profile.color };

  const preview = h("div", { class: "avatar-preview" });
  const status = h("p", { class: "muted center", role: "status" });
  const error = h("p", { class: "form-error", role: "alert", hidden: true });

  function previewProfile() {
    const acc = options.accessories.find((a) => a.slug === choice.accessory);
    return { avatar: choice.avatar, accessoryIcon: acc?.value ?? "", color: choice.color };
  }

  function render() {
    preview.replaceChildren(avatarView(previewProfile(), "xl"));
    page?.querySelectorAll("[data-group]").forEach((btn) => {
      const selected = choice[btn.dataset.group] === btn.dataset.value;
      btn.classList.toggle("selected", selected);
      btn.setAttribute("aria-pressed", String(selected));
    });
  }

  function item(group, value, label, content, unlocked = true, xpRequired = 0) {
    return h(
      "button",
      {
        type: "button",
        class: `item-btn${unlocked ? "" : " locked"}`,
        dataset: { group, value },
        disabled: !unlocked,
        "aria-label": unlocked ? label : t("{nome}: desbloqueia com {xp} XP", { nome: label, xp: xpRequired }),
        onClick: () => {
          choice[group] = value;
          render();
        },
      },
      content,
      unlocked ? null : h("span", { class: "item-lock" }, `🔒 ${xpRequired} XP`),
    );
  }

  const save = h("button", { type: "button", class: "btn btn-primary btn-block" }, t("Salvar meu avatar"));
  save.addEventListener("click", async () => {
    save.disabled = true;
    error.hidden = true;
    try {
      const profile = await api.updateAvatar(id, choice);
      store.updateProfile(profile);
      status.textContent = t("Avatar salvo! ✨");
    } catch (err) {
      error.textContent = errorMessage(err);
      error.hidden = false;
    } finally {
      save.disabled = false;
    }
  });

  const next = options.accessories.concat(options.colors).filter((i) => !i.unlocked).sort((a, b) => a.xpRequired - b.xpRequired)[0];

  let page = null;
  page = h(
    "section",
    { class: "page-narrow" },
    h("h1", {}, t("Meu avatar")),
    h(
      "div",
      { class: "card stack center" },
      preview,
      h("p", {}, t("Você tem ⚡ {xp} XP.", { xp: options.xp })),
      next ? h("p", { class: "muted small" }, t("Próximo item: {nome} com {xp} XP.", { nome: next.name, xp: next.xpRequired })) : h("p", { class: "muted small" }, t("Você desbloqueou tudo! 🎉")),
    ),
    h("section", { class: "card stack" }, h("h2", {}, t("Bicho")), h("div", { class: "item-grid" }, options.avatars.map((a) => item("avatar", a, t("Avatar {n}", { n: a }), h("span", { class: "item-emoji" }, a))))),
    h(
      "section",
      { class: "card stack" },
      h("h2", {}, t("Acessório")),
      h("div", { class: "item-grid" }, options.accessories.map((a) => item("accessory", a.slug, a.name, h("span", { class: "item-emoji" }, a.value || "—"), a.unlocked, a.xpRequired))),
    ),
    h(
      "section",
      { class: "card stack" },
      h("h2", {}, t("Cor de fundo")),
      h("div", { class: "item-grid" }, options.colors.map((c) => item("color", c.slug, c.name, h("span", { class: `color-swatch color-${c.value}` }, ""), c.unlocked, c.xpRequired))),
    ),
    error,
    save,
    status,
  );
  render();
  return page;
}
