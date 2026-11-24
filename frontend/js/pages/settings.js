import { api } from "../api.js";
import { errorMessage, h } from "../dom.js";
import { getPrefs, setPrefs } from "../prefs.js";
import { navigate } from "../router.js";
import { store } from "../store.js";
import { listVoices, onVoicesChanged, speak, voiceSupported } from "../voice.js";

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

const SAMPLE = "Olá! Quanto é 3 mais 4?";

/** Lista as vozes em português do aparelho, da melhor para a pior. */
function voicePicker(prefs) {
  const select = h("select", { id: "voice-select" });
  const field = h("label", { class: "field" }, h("span", {}, "Voz"), select);
  const hint = h("p", { class: "muted small", hidden: true }, "Vozes marcadas com 🌐 precisam de internet. Sem conexão, usamos uma voz do aparelho.");

  function fill() {
    const voices = listVoices();
    const chosen = getPrefs().voiceURI;
    const auto = voices[0] ? `Automática (${voices[0].name})` : "Automática";
    select.replaceChildren(
      h("option", { value: "" }, auto),
      ...voices.map((v) => h("option", { value: v.voiceURI }, `${v.name}${v.localService ? "" : " 🌐"}`)),
    );
    select.value = voices.some((v) => v.voiceURI === chosen) ? chosen : "";
    hint.hidden = !voices.some((v) => !v.localService);
  }

  select.addEventListener("change", () => {
    setPrefs({ voiceURI: select.value });
    speak(SAMPLE);
  });
  fill();
  // No Chrome as vozes chegam depois; paramos de ouvir quando a página sai da tela.
  let shown = false;
  const stop = onVoicesChanged(() => {
    if (select.isConnected) shown = true;
    else if (shown) return stop();
    fill();
  });
  return h("div", { class: "stack" }, field, hint);
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
        voicePicker(prefs),
        radioGroup("voiceRate", "Velocidade", [["0.8", "Devagar"], ["0.95", "Normal"], ["1.1", "Rápida"]], String(prefs.voiceRate), (v) => {
          setPrefs({ voiceRate: Number(v) });
          speak(SAMPLE);
        }),
        h("button", { type: "button", class: "btn", onClick: () => speak(SAMPLE) }, "🔊 Testar a voz"),
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
      h("p", { class: "muted" }, "Das crianças, guardamos apenas apelido, avatar, ano escolar (opcional) e o progresso no jogo, e nada disso é compartilhado com terceiros. Turmas só veem os perfis que você escolher."),
      h(
        "p",
        { class: "small" },
        h("a", { href: "/privacidade", target: "_blank", rel: "noopener" }, "Política de privacidade"),
        " · ",
        h("a", { href: "/termos", target: "_blank", rel: "noopener" }, "Termos de uso"),
      ),
      deleteError,
      deleteBtn,
    ),
  );
}
