import { api } from "../api.js";
import { errorMessage, h } from "../dom.js";
import { getPrefs, setPrefs } from "../prefs.js";
import { navigate } from "../router.js";
import { store } from "../store.js";
import { speak, voiceSupported } from "../voice.js";

function radioGroup(name, legend, options, value, onChange) {
  return h(
    "fieldset",
    { class: "option-group" },
    h("legend", {}, legend),
    options.map(([v, label]) =>
      h(
        "label",
        { class: "option" },
        h("input", { type: "radio", name, value: v, checked: v === value, onChange: () => onChange(v) }),
        h("span", {}, label),
      ),
    ),
  );
}

export function settingsPage() {
  const prefs = getPrefs();

  const voice = voiceSupported
    ? h(
        "section",
        { class: "card stack" },
        h("h2", {}, "Leitura em voz alta"),
        h(
          "label",
          { class: "checkbox" },
          h("input", { type: "checkbox", checked: prefs.autoRead, onChange: (e) => setPrefs({ autoRead: e.target.checked }) }),
          h("span", {}, "Ler cada enunciado automaticamente (bom para quem está aprendendo a ler)."),
        ),
        h("p", { class: "muted small" }, "Em todo problema há também um botão 🔊 para ouvir quando quiser."),
        h("button", { type: "button", class: "btn", onClick: () => speak("Olá! Quanto é 3 mais 4?") }, "🔊 Testar a voz"),
      )
    : h("section", { class: "card" }, h("h2", {}, "Leitura em voz alta"), h("p", { class: "muted" }, "Este navegador não oferece leitura em voz alta."));

  const deleteError = h("p", { class: "form-error", role: "alert", hidden: true });
  const guest = store.state.me?.isGuest;
  const deleteBtn = h("button", { type: "button", class: "btn btn-danger" }, guest ? "Apagar o progresso deste aparelho" : "Apagar minha conta e todos os dados");
  deleteBtn.addEventListener("click", async () => {
    if (!confirm(guest ? "Isso apaga todo o progresso deste convidado. Não dá para desfazer. Continuar?" : "Isso apaga a conta do responsável e o progresso de TODOS os perfis. Não dá para desfazer. Continuar?")) return;
    deleteBtn.disabled = true;
    try {
      await api.deleteAccount();
      store.signOut();
      navigate("/entrar");
    } catch (err) {
      deleteError.textContent = errorMessage(err);
      deleteError.hidden = false;
      deleteBtn.disabled = false;
    }
  });

  return h(
    "section",
    { class: "page-narrow" },
    h("h1", {}, "Configurações"),
    h(
      "section",
      { class: "card stack" },
      h("h2", {}, "Aparência"),
      radioGroup("theme", "Tema", [["sistema", "Igual ao aparelho"], ["claro", "Claro"], ["escuro", "Escuro"]], prefs.theme, (v) => setPrefs({ theme: v })),
      radioGroup("text", "Tamanho do texto", [["normal", "Normal"], ["grande", "Grande"]], prefs.text, (v) => setPrefs({ text: v })),
      radioGroup("motion", "Animações", [["normal", "Normais"], ["reduzido", "Reduzidas"]], prefs.motion, (v) => setPrefs({ motion: v })),
      h("p", { class: "muted small" }, "Estas escolhas valem só para este aparelho."),
    ),
    voice,
    h(
      "section",
      { class: "card stack" },
      h("h2", {}, "Privacidade"),
      h("p", { class: "muted" }, "Guardamos apenas apelido, avatar, ano escolar (opcional) e o progresso no jogo. Nada é compartilhado com terceiros. Turmas só veem os perfis que você escolher."),
      deleteError,
      deleteBtn,
    ),
  );
}
