// Página inicial (landing). Mantém só o essencial:
// 1. links antigos do jogo (/#/...) continuam funcionando, levando para /jogar;
// 2. quem já está jogando neste aparelho vê "Continuar jogando" em vez de "Jogar agora";
// 3. guarda de onde a pessoa veio (utm/indicação), enviado depois no cadastro.
import { captureOrigin } from "./origin.js";

captureOrigin();

if (location.hash.startsWith("#/")) {
  location.replace(`/jogar${location.hash}`);
}

async function personalize() {
  try {
    const res = await fetch("/api/me", { credentials: "same-origin", headers: { Accept: "application/json" } });
    if (!res.ok) return;
    const me = await res.json();
    const name = me.profiles?.[0]?.nickname;
    for (const link of document.querySelectorAll('[data-cta="play"]')) {
      link.href = "/jogar#/";
      link.textContent = name && me.profiles.length === 1 ? `Continuar como ${name}` : "Continuar jogando";
    }
    for (const link of document.querySelectorAll('[data-cta="login"], [data-cta="signup"]')) {
      link.hidden = true;
    }
    // Página de professores: quem já tem conta vai direto para as turmas.
    for (const link of document.querySelectorAll('[data-cta="teacher"]')) {
      link.href = me.isGuest ? "/jogar#/salvar" : "/jogar#/turmas";
      link.textContent = me.isGuest ? "Salvar progresso e criar turma" : "Ir para minhas turmas";
    }
  } catch {
    /* sem conexão ou API fora: a página continua funcionando com os links padrão */
  }
}

personalize();
