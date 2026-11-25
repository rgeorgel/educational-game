// "Compartilhar resultado" (estilo Wordle): quadradinhos de acertos e erros, sem apelido,
// sem enunciados e sem respostas, mais o link do jogo. Ajuda o jogo a se espalhar por indicação.
import { h } from "./dom.js";
import { locale, sitePath, t } from "./i18n.js";

const SQUARES = { certo: "🟩", dica: "🟨", erro: "🟥" };
const dayFmt = new Intl.DateTimeFormat(locale(), { day: "2-digit", month: "2-digit", timeZone: "UTC" });

/** Link do jogo com a origem marcada (aparece no painel admin como "compartilhar"). */
export function shareUrl(campaign) {
  return `${location.origin}${sitePath("home")}?utm_source=compartilhar&utm_medium=app&utm_campaign=${encodeURIComponent(campaign)}`;
}

export function shareText(s) {
  const squares = s.marks.map((m) => SQUARES[m]).filter(Boolean).join("");
  const level = s.progress?.level ? `${s.progress.level.icon} ${s.progress.level.name}` : "";
  const subject = `${s.progress?.icon ?? ""} ${s.subjectName}`.trim();
  const score = t("{a} de {b}", { a: s.correct, b: s.total });

  if (s.mode === "diario") {
    const date = s.dailyDate ? ` ${dayFmt.format(new Date(`${s.dailyDate}T00:00:00Z`))}` : "";
    return [`🧭 Educa Game · ${t("Desafio do dia")}${date}`, [subject, level].filter(Boolean).join(" · "), `${squares} ${score}`, `${t("Consegue fazer melhor?")} ${shareUrl("desafio-do-dia")}`].join("\n");
  }
  if (s.mode === "guardiao") {
    return [
      `🧭 Educa Game · 🏆 ${t("Venci o Guardião!")}`,
      `${subject} · ${t("agora no nível {nivel}", { nivel: s.resultLevel ? `${s.resultLevel.icon} ${s.resultLevel.name}` : level })}`,
      `${squares} ${score}`,
      `${t("Jogue também:")} ${shareUrl("guardiao")}`,
    ].join("\n");
  }
  const stars = "★".repeat(s.stars) + "☆".repeat(3 - s.stars);
  return [`🧭 Educa Game · ${subject}`, `${s.stage?.name ?? t("Etapa")}: ${stars} (${score})`, squares, `${t("Jogue também:")} ${shareUrl("etapa")}`].join("\n");
}

/** Vale a pena compartilhar? (resultado bom e sessão terminada) */
export function canShare(s) {
  if (!s.finished || !s.marks?.length) return false;
  if (s.mode === "diario") return true;
  if (s.mode === "guardiao") return Boolean(s.guardianPassed);
  if (s.mode === "etapa") return s.stars > 0;
  return false;
}

export function shareBox(s) {
  const text = shareText(s);
  const status = h("p", { class: "muted small share-status", role: "status" });
  const whatsapp = h("a", { class: "btn btn-small", href: `https://wa.me/?text=${encodeURIComponent(text)}`, target: "_blank", rel: "noopener noreferrer", hidden: true }, t("Enviar no WhatsApp"));
  const button = h("button", { type: "button", class: "btn btn-primary btn-small" }, t("📤 Compartilhar resultado"));

  button.addEventListener("click", async () => {
    if (navigator.share) {
      try {
        await navigator.share({ text });
        status.textContent = t("Obrigado por compartilhar!");
        return;
      } catch (err) {
        if (err?.name === "AbortError") return; // a pessoa fechou a janela de compartilhar
      }
    }
    try {
      await navigator.clipboard.writeText(text);
      status.textContent = t("Copiado! Cole no WhatsApp, Instagram ou onde quiser.");
    } catch {
      status.textContent = t("Não deu para copiar automaticamente. Use o botão do WhatsApp.");
    }
    whatsapp.hidden = false;
  });

  return h(
    "div",
    { class: "share-box" },
    h("pre", { class: "share-preview", "aria-label": t("Prévia do texto que será compartilhado") }, text),
    h("p", { class: "muted small" }, t("Não aparece seu apelido nem as respostas, só os acertos.")),
    h("div", { class: "actions" }, button, whatsapp),
    status,
  );
}
