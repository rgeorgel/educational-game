// Leitura em voz alta com a Web Speech API (funciona offline na maioria dos aparelhos).

const synth = typeof window !== "undefined" ? window.speechSynthesis : undefined;

export const voiceSupported = Boolean(synth && "SpeechSynthesisUtterance" in window);

function ptVoice() {
  const voices = synth?.getVoices() ?? [];
  return voices.find((v) => v.lang === "pt-BR") ?? voices.find((v) => v.lang?.startsWith("pt")) ?? null;
}

/** Troca símbolos matemáticos por palavras para a leitura ficar natural. */
export function speakable(text) {
  return text
    .replace(/×/g, " vezes ")
    .replace(/÷/g, " dividido por ")
    .replace(/−/g, " menos ")
    .replace(/(\d)\s*\+\s*(\d)/g, "$1 mais $2")
    .replace(/(\d)\/(\d)/g, "$1 sobre $2")
    .replace(/²/g, " ao quadrado")
    .replace(/³/g, " ao cubo")
    .replace(/√/g, " raiz quadrada de ")
    .replace(/≥/g, " maior ou igual a ")
    .replace(/\?\s*$/, "?");
}

export function speak(text) {
  if (!voiceSupported || !text) return;
  synth.cancel();
  const u = new SpeechSynthesisUtterance(speakable(text));
  u.lang = "pt-BR";
  const voice = ptVoice();
  if (voice) u.voice = voice;
  u.rate = 0.95;
  synth.speak(u);
}

export function stopSpeaking() {
  synth?.cancel();
}
