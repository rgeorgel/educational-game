// Origem do primeiro acesso (primeiro toque): utm_source/medium/campaign (ou ?ref=) e o site de origem.
// Fica só neste aparelho até a pessoa começar a jogar ou criar conta; então vai junto no cadastro
// para o painel admin mostrar quais canais de divulgação trazem gente. Nada pessoal é guardado.
const KEY = "edugame-origem";

function read() {
  try {
    return JSON.parse(localStorage.getItem(KEY) || "null");
  } catch {
    return null;
  }
}

/** Guarda a origem desta visita se ainda não houver uma (chame ao abrir qualquer página). */
export function captureOrigin() {
  if (read()) return;
  const params = new URLSearchParams(location.search);
  const source = params.get("utm_source") || params.get("ref");
  let referrer = null;
  try {
    const ref = document.referrer ? new URL(document.referrer) : null;
    if (ref && ref.host !== location.host) referrer = ref.origin; // só o domínio, nunca a página
  } catch {
    /* referrer inválido: ignora */
  }
  if (!source && !referrer) return; // acesso direto: não guarda, para uma campanha futura ainda contar
  const origin = {
    source: source || null,
    medium: params.get("utm_medium") || null,
    campaign: params.get("utm_campaign") || null,
    referrer,
  };
  try {
    localStorage.setItem(KEY, JSON.stringify(origin));
  } catch {
    /* modo privado: segue sem origem */
  }
}

/** Origem para enviar no cadastro (ou null = acesso direto). */
export function originForSignup() {
  return read();
}
