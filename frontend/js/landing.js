// Página inicial (landing). Mantém só o essencial:
// 1. links antigos do jogo (/#/...) continuam funcionando, levando para /jogar;
// 2. quem já está jogando neste aparelho vê "Continuar jogando" em vez de "Jogar agora".

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
  } catch {
    /* sem conexão ou API fora: a página continua funcionando com os links padrão */
  }
}

personalize();
