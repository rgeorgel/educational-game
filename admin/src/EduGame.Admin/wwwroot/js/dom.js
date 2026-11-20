// Monta elementos sem innerHTML (o painel roda com CSP rígida).
const SVG_NS = "http://www.w3.org/2000/svg";
const SVG_TAGS = new Set(["svg", "g", "rect", "path", "line", "text", "title"]);

export function h(tag, attrs = {}, ...children) {
  const el = SVG_TAGS.has(tag) ? document.createElementNS(SVG_NS, tag) : document.createElement(tag);
  for (const [key, value] of Object.entries(attrs ?? {})) {
    if (value === null || value === undefined || value === false) continue;
    if (key.startsWith("on") && typeof value === "function") el.addEventListener(key.slice(2), value);
    else if (key === "class") el.setAttribute("class", value);
    else if (key in el && !SVG_TAGS.has(tag) && typeof value !== "string") el[key] = value;
    else el.setAttribute(key, value === true ? "" : String(value));
  }
  append(el, children);
  return el;
}

export function append(el, children) {
  for (const child of children.flat(Infinity)) {
    if (child === null || child === undefined || child === false) continue;
    el.append(child instanceof Node ? child : String(child));
  }
  return el;
}

const nf = new Intl.NumberFormat("pt-BR");
export const num = (n) => nf.format(n ?? 0);
export const pct = (part, total) => (total ? `${Math.round((part / total) * 100)}%` : "—");

const TZ = "America/Sao_Paulo";
const dateTimeFmt = new Intl.DateTimeFormat("pt-BR", { timeZone: TZ, day: "2-digit", month: "2-digit", year: "numeric", hour: "2-digit", minute: "2-digit" });
const dayFmt = new Intl.DateTimeFormat("pt-BR", { timeZone: "UTC", day: "2-digit", month: "2-digit", year: "numeric" });
const shortDayFmt = new Intl.DateTimeFormat("pt-BR", { timeZone: "UTC", day: "2-digit", month: "2-digit" });
const weekdayFmt = new Intl.DateTimeFormat("pt-BR", { timeZone: "UTC", weekday: "short" });

const localDayFmt = new Intl.DateTimeFormat("pt-BR", { timeZone: TZ, day: "2-digit", month: "2-digit", year: "numeric" });
export const localDay = (iso) => (iso ? localDayFmt.format(new Date(iso)) : "—");
export const dateTime = (iso) => (iso ? dateTimeFmt.format(new Date(iso)) : "—");
/** Datas "AAAA-MM-DD" (dia local já calculado no servidor). */
export const day = (d) => (d ? dayFmt.format(new Date(`${d}T00:00:00Z`)) : "—");
export const shortDay = (d) => shortDayFmt.format(new Date(`${d}T00:00:00Z`));
export const weekday = (d) => weekdayFmt.format(new Date(`${d}T00:00:00Z`)).replace(".", "");

/** "há 3 dias", "hoje"… para últimos acessos. */
export function ago(iso) {
  if (!iso) return "nunca";
  const diff = Date.now() - new Date(iso).getTime();
  const min = Math.round(diff / 60000);
  if (min < 2) return "agora";
  if (min < 60) return `há ${min} min`;
  const hours = Math.round(min / 60);
  if (hours < 24) return `há ${hours} h`;
  const days = Math.round(hours / 24);
  if (days < 45) return `há ${days} ${days === 1 ? "dia" : "dias"}`;
  const months = Math.round(days / 30);
  return `há ${months} ${months === 1 ? "mês" : "meses"}`;
}
