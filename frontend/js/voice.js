// Leitura em voz alta com a Web Speech API (funciona offline na maioria dos aparelhos).
// Cada aparelho traz vozes diferentes, e a primeira da lista costuma ser a pior. Por isso
// damos nota a cada voz e usamos a melhor, a menos que a pessoa tenha escolhido outra.

import { getLang, locale, t } from "./i18n.js";
import { getPrefs } from "./prefs.js";

const synth = typeof window !== "undefined" ? window.speechSynthesis : undefined;

export const voiceSupported = Boolean(synth && "SpeechSynthesisUtterance" in window);

// Algumas vozes do macOS/iOS que soam bem piores que as demais (os mesmos nomes em vários idiomas).
const WEAK = /\b(eddy|flo|grandma|grandpa|reed|rocko|sandy|shelley|vovó|vovô|abuela|abuelo|bahh|bells|boing|bubbles|cellos|jester|organ|trinoids|whisper|wobble|zarvox|albert|bad news|good news|superstar|junior|ralph|fred|kathy)\b/i;

// Variante preferida de cada idioma: pt-BR, en-US e, no espanhol, qualquer uma (es-ES, es-MX…).
const PREFERRED = { pt: "pt-br", en: "en-us", es: null };

function lang(v) {
  return (v.lang ?? "").replace("_", "-").toLowerCase();
}

/** Nota da voz: quanto maior, mais natural costuma soar no idioma do app. */
function score(v) {
  const l = lang(v);
  const name = v.name ?? "";
  const code = getLang();
  const preferred = PREFERRED[code];
  let s = !l.startsWith(code) ? 0 : preferred === null || l === preferred ? 100 : 50;
  if (!s) return 0;
  if (/natural|neural/i.test(name)) s += 40; // Edge: "Microsoft Francisca Online (Natural)"
  if (/premium/i.test(name)) s += 35; // macOS/iOS: vozes baixadas em alta qualidade
  if (/enhanced|aprimorad|melhorad/i.test(name)) s += 30;
  if (/google/i.test(name)) s += 25; // Chrome: "Google português do Brasil", "Google US English"
  if (WEAK.test(name)) s -= 20;
  if (v.default) s += 2;
  return s;
}

/** Vozes no idioma do app, da melhor para a pior. */
export function listVoices() {
  const voices = synth?.getVoices() ?? [];
  return voices
    .map((v) => ({ v, s: score(v) }))
    .filter((x) => x.s > 0)
    .sort((a, b) => b.s - a.s)
    .map((x) => x.v);
}

/** Avisa quando o navegador termina de carregar as vozes (no Chrome elas chegam depois). */
export function onVoicesChanged(callback) {
  synth?.addEventListener?.("voiceschanged", callback);
  return () => synth?.removeEventListener?.("voiceschanged", callback);
}

function pickVoice({ localOnly = false } = {}) {
  let voices = listVoices();
  if (localOnly) voices = voices.filter((v) => v.localService);
  const chosen = getPrefs().voiceURI;
  return (chosen && voices.find((v) => v.voiceURI === chosen)) || voices[0] || null;
}

/** Troca símbolos matemáticos por palavras para a leitura ficar natural. */
export function speakable(text) {
  return text
    .replace(/×/g, ` ${t("vezes")} `)
    .replace(/÷/g, ` ${t("dividido por")} `)
    .replace(/−/g, ` ${t("menos")} `)
    .replace(/(\d)\s*\+\s*(\d)/g, `$1 ${t("mais")} $2`)
    .replace(/(\d)\/(\d)/g, `$1 ${t("sobre")} $2`)
    .replace(/²/g, ` ${t("ao quadrado")}`)
    .replace(/³/g, ` ${t("ao cubo")}`)
    .replace(/√/g, ` ${t("raiz quadrada de")} `)
    .replace(/≥/g, ` ${t("maior ou igual a")} `)
    .replace(/\?\s*$/, "?");
}

function utter(text, voice) {
  const u = new SpeechSynthesisUtterance(text);
  u.lang = voice?.lang ?? locale();
  if (voice) u.voice = voice;
  u.rate = getPrefs().voiceRate ?? 0.95;
  return u;
}

export function speak(text) {
  if (!voiceSupported || !text) return;
  synth.cancel();
  const spoken = speakable(text);
  const voice = pickVoice();
  const u = utter(spoken, voice);
  // Vozes on-line (Google, "Natural" do Edge) falham sem internet: tenta de novo com uma do aparelho.
  if (voice && !voice.localService) {
    u.onerror = (e) => {
      if (e.error === "interrupted" || e.error === "canceled") return;
      const local = pickVoice({ localOnly: true });
      synth.speak(utter(spoken, local));
    };
  }
  synth.speak(u);
}

export function stopSpeaking() {
  synth?.cancel();
}
