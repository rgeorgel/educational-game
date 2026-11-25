// Idioma do app: português do Brasil (padrão), inglês ou espanhol.
//
// Os textos ficam escritos em português no código, dentro de t("…"). O português é a chave:
// js/i18n/en.js e js/i18n/es.js trazem a tradução de cada frase. Sem tradução, aparece o português.
// Valores variáveis usam {nome}: t("Olá, {nome}!", { nome }).
//
// O idioma escolhido fica no aparelho e vai para a API no cabeçalho X-Lang (problemas, nomes do
// mapa e mensagens de erro chegam traduzidos). Ordem de escolha: ?lang= no endereço (links das
// páginas /en e /es), a escolha salva, o idioma do navegador e, por fim, o inglês.

import en from "./i18n/en.js";
import es from "./i18n/es.js";

const KEY = "edugame.lang";

export const LANGUAGES = [
  { code: "pt", label: "Português", locale: "pt-BR" },
  { code: "en", label: "English", locale: "en-US" },
  { code: "es", label: "Español", locale: "es" },
];

const DICTIONARIES = { en, es };

function normalize(tag) {
  const t = String(tag ?? "").toLowerCase();
  return LANGUAGES.find((l) => t === l.code || t.startsWith(`${l.code}-`))?.code ?? null;
}

function read() {
  try {
    return localStorage.getItem(KEY);
  } catch {
    return null;
  }
}

function save(code) {
  try {
    localStorage.setItem(KEY, code);
  } catch {
    /* armazenamento indisponível: vale só para esta visita */
  }
}

function detect() {
  const fromUrl = typeof location !== "undefined" ? normalize(new URLSearchParams(location.search).get("lang")) : null;
  if (fromUrl) {
    save(fromUrl);
    return fromUrl;
  }
  const saved = normalize(read());
  if (saved) return saved;
  const browser = typeof navigator !== "undefined" ? (navigator.languages ?? [navigator.language]) : [];
  for (const tag of browser) {
    const code = normalize(tag);
    if (code) return code;
  }
  return "en";
}

let current = detect();

if (typeof document !== "undefined") document.documentElement.lang = locale();

/** Código do idioma atual: "pt", "en" ou "es". */
export function getLang() {
  return current;
}

/** Tag BCP 47 do idioma atual, para datas, números e voz ("pt-BR", "en-US", "es"). */
export function locale() {
  return LANGUAGES.find((l) => l.code === current).locale;
}

/** Troca o idioma e recarrega a página (tudo, inclusive os dados da API, vem no novo idioma). */
export function setLang(code) {
  const next = normalize(code) ?? "pt";
  save(next);
  if (next === current) return;
  current = next;
  // Tira um ?lang= antigo do endereço, senão ele venceria a escolha nova ao recarregar.
  const url = new URL(location.href);
  url.searchParams.delete("lang");
  // Mesmo endereço (só muda o #): replace não recarregaria a página.
  if (url.toString() === location.href) location.reload();
  else location.replace(url.toString());
}

/**
 * Traduz uma frase escrita em português.
 * t("Olá, {nome}!", { nome: "Ana" }) → "Hello, Ana!" em inglês.
 */
export function t(text, vars) {
  const dict = DICTIONARIES[current];
  let out = dict?.[text] ?? text;
  if (vars) out = out.replace(/\{(\w+)\}/g, (m, k) => (k in vars ? String(vars[k]) : m));
  return out;
}

/** Escolhe singular ou plural: tn(n, "{n} estrela", "{n} estrelas"). */
export function tn(n, one, many, vars = {}) {
  return t(n === 1 ? one : many, { n, ...vars });
}

/** Data no idioma atual: fmtDate("2026-11-24", { day: "numeric", month: "short" }). */
export function fmtDate(value, options = { day: "2-digit", month: "2-digit", year: "numeric" }) {
  const date = typeof value === "string" && /^\d{4}-\d{2}-\d{2}$/.test(value) ? new Date(`${value}T12:00:00`) : new Date(value);
  return date.toLocaleDateString(locale(), options);
}

/** Número no idioma atual (vírgula decimal em português e espanhol, ponto em inglês). */
export function fmtNumber(value, options) {
  return Number(value).toLocaleString(locale(), options);
}

/** Separador decimal digitado no teclado do jogo. */
export function decimalSeparator() {
  return current === "en" ? "." : ",";
}

/** Endereço de uma página estática no idioma atual (landing, professores, termos, privacidade, blog). */
export function sitePath(page) {
  const paths = {
    pt: { home: "/", teachers: "/professores", terms: "/termos", privacy: "/privacidade", blog: "/blog" },
    en: { home: "/en/", teachers: "/en/teachers", terms: "/en/terms", privacy: "/en/privacy", blog: "/en/blog" },
    es: { home: "/es/", teachers: "/es/profesores", terms: "/es/terminos", privacy: "/es/privacidad", blog: "/es/blog" },
  };
  return paths[current][page];
}
