// Preferências deste aparelho (tema, tamanho do texto, movimento, voz).
// Ficam no navegador: são conveniências por dispositivo, não dados do jogador.

const KEY = "edugame.prefs";

const DEFAULTS = {
  theme: "sistema", // sistema | claro | escuro
  text: "normal", // normal | grande
  motion: "normal", // normal | reduzido
  autoRead: false, // ler enunciados em voz alta automaticamente
  sounds: true,
};

function read() {
  try {
    return { ...DEFAULTS, ...JSON.parse(localStorage.getItem(KEY) ?? "{}") };
  } catch {
    return { ...DEFAULTS };
  }
}

let current = read();

export function getPrefs() {
  return current;
}

export function setPrefs(changes) {
  current = { ...current, ...changes };
  try {
    localStorage.setItem(KEY, JSON.stringify(current));
  } catch {
    /* armazenamento indisponível: vale só para esta visita */
  }
  applyPrefs();
}

export function applyPrefs() {
  const root = document.documentElement;
  if (current.theme === "claro") root.dataset.theme = "light";
  else if (current.theme === "escuro") root.dataset.theme = "dark";
  else delete root.dataset.theme;
  root.dataset.text = current.text;
  root.dataset.motion = current.motion;
}
