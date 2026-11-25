// Página inicial (landing). Mantém só o essencial:
// 1. links antigos do jogo (/#/...) continuam funcionando, levando para /jogar;
// 2. quem já está jogando neste aparelho vê "Continuar jogando" em vez de "Jogar agora";
// 3. guarda de onde a pessoa veio (utm/indicação), enviado depois no cadastro;
// 4. Google Analytics com aviso de cookies (só nas páginas que usam este arquivo: landing,
//    professores e blog; o jogo em /jogar não carrega).
import { captureOrigin } from "./origin.js";

captureOrigin();

// Idioma da página (as páginas em /en e /es têm lang="en"/"es"); os links para o jogo levam o mesmo idioma.
const LANG = (document.documentElement.lang || "pt").slice(0, 2);
const TEXT = {
  pt: { continueAs: "Continuar como {nome}", continue: "Continuar jogando", saveAndCreate: "Salvar progresso e criar turma", myClasses: "Ir para minhas turmas" },
  en: { continueAs: "Continue as {nome}", continue: "Keep playing", saveAndCreate: "Save progress and create a class", myClasses: "Go to my classes" },
  es: { continueAs: "Seguir como {nome}", continue: "Seguir jugando", saveAndCreate: "Guardar progreso y crear una clase", myClasses: "Ir a mis clases" },
}[LANG] ?? {};
const game = (hash) => `/jogar?lang=${LANG}${hash}`;

// Import dinâmico: se o arquivo falhar (ex.: offline sem cache), o resto da página segue normal.
import("./analytics.js")
  .then((m) => m.initAnalytics())
  .catch(() => {});

if (location.hash.startsWith("#/")) {
  location.replace(game(location.hash));
}

async function personalize() {
  try {
    const res = await fetch("/api/me", { credentials: "same-origin", headers: { Accept: "application/json" } });
    if (!res.ok) return;
    const me = await res.json();
    const name = me.profiles?.[0]?.nickname;
    for (const link of document.querySelectorAll('[data-cta="play"]')) {
      link.href = game("#/");
      link.textContent = name && me.profiles.length === 1 ? TEXT.continueAs.replace("{nome}", name) : TEXT.continue;
    }
    for (const link of document.querySelectorAll('[data-cta="login"], [data-cta="signup"]')) {
      link.hidden = true;
    }
    // Página de professores: quem já tem conta vai direto para as turmas.
    for (const link of document.querySelectorAll('[data-cta="teacher"]')) {
      link.href = game(me.isGuest ? "#/salvar" : "#/turmas");
      link.textContent = me.isGuest ? TEXT.saveAndCreate : TEXT.myClasses;
    }
  } catch {
    /* sem conexão ou API fora: a página continua funcionando com os links padrão */
  }
}

personalize();

// Planos: durante o lançamento (sem cobrança), avisa que os planos pagos estão grátis.
async function launchNote() {
  try {
    const res = await fetch("/api/billing/status", { headers: { Accept: "application/json" } });
    if (!res.ok) return;
    const { mode } = await res.json();
    for (const el of document.querySelectorAll("[data-launch-note]")) el.hidden = mode !== "lancamento";
  } catch {
    /* sem a API, a página continua sem o aviso */
  }
}

launchNote();
