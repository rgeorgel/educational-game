// Service worker: deixa o jogo instalável e abre a "casca" do app mesmo sem internet.
//
// Estratégia (sempre "rede primeiro", para nunca servir versão velha quando há conexão):
// - Arquivos do app (HTML/CSS/JS/ícones): rede → cache; sem rede, usa o cache.
// - Leituras da API de progresso/mapa (GET): rede → cache separado; sem rede, mostra os últimos dados vistos.
// - Respostas e outras escritas (POST/PUT/DELETE): só rede (o servidor confere as respostas).
// Ao sair da conta, o app pede para apagar o cache de dados (aparelhos compartilhados).

const VERSION = "v18";
const SHELL_CACHE = `edugame-shell-${VERSION}`;
const DATA_CACHE = "edugame-data";

const SHELL = [
  "/",
  "/index.html",
  "/en/",
  "/es/",
  "/jogar",
  "/jogar.html",
  "/css/landing.css",
  "/js/landing.js",
  "/js/analytics.js",
  "/manifest.webmanifest",
  "/css/tokens.css",
  "/css/base.css",
  "/css/components.css",
  "/css/pages.css",
  "/css/game.css",
  "/js/effects.js",
  "/js/share.js",
  "/js/origin.js",
  "/js/i18n.js",
  "/js/i18n/en.js",
  "/js/i18n/es.js",
  "/js/main.js",
  "/js/api.js",
  "/js/dom.js",
  "/js/prefs.js",
  "/js/router.js",
  "/js/store.js",
  "/js/voice.js",
  "/js/components/index.js",
  "/js/components/history-chart.js",
  "/js/problem-types/index.js",
  "/js/problem-types/figure.js",
  "/js/pages/auth.js",
  "/js/pages/assignments.js",
  "/js/pages/avatar.js",
  "/js/pages/plans.js",
  "/js/pages/weekly.js",
  "/js/pages/worksheets.js",
  "/js/pages/bncc.js",
  "/js/pages/classrooms.js",
  "/js/pages/games.js",
  "/js/pages/guardian.js",
  "/js/pages/home.js",
  "/js/pages/map.js",
  "/js/pages/profiles.js",
  "/js/pages/progress.js",
  "/js/pages/session.js",
  "/js/pages/settings.js",
  "/js/pages/shared.js",
  "/js/pages/summary.js",
  "/assets/icon.svg",
  "/assets/icon-192.png",
  "/assets/icon-512.png",
];

// Leituras da API que podem ser mostradas offline (últimos dados vistos).
const CACHEABLE_API = [/^\/api\/me$/, /^\/api\/subjects$/, /^\/api\/profiles\/[^/]+\/(progress|history|achievements|daily|avatar|games)/, /^\/api\/profiles\/[^/]+\/subjects\/[^/]+\/map$/];

self.addEventListener("install", (event) => {
  event.waitUntil(
    caches
      .open(SHELL_CACHE)
      .then((cache) => cache.addAll(SHELL))
      .then(() => self.skipWaiting()),
  );
});

self.addEventListener("activate", (event) => {
  event.waitUntil(
    caches
      .keys()
      .then((keys) => Promise.all(keys.filter((k) => k.startsWith("edugame-shell-") && k !== SHELL_CACHE).map((k) => caches.delete(k))))
      .then(() => self.clients.claim()),
  );
});

self.addEventListener("message", (event) => {
  if (event.data?.type === "clear-data") event.waitUntil(caches.delete(DATA_CACHE));
});

self.addEventListener("fetch", (event) => {
  const request = event.request;
  const url = new URL(request.url);
  if (request.method !== "GET" || url.origin !== self.location.origin) return;

  if (url.pathname.startsWith("/api/")) {
    if (CACHEABLE_API.some((re) => re.test(url.pathname))) event.respondWith(networkFirst(request, DATA_CACHE, () => offlineApi(request)));
    return; // demais chamadas da API: direto na rede
  }

  // Navegação: sem rede, entrega a página do cache (o jogo em /jogar; o resto cai na landing do idioma).
  if (request.mode === "navigate") {
    const lang = url.pathname.match(/^\/(en|es)(\/|$)/)?.[1];
    const page = url.pathname.startsWith("/jogar") ? "/jogar.html" : lang ? `/${lang}/` : "/index.html";
    event.respondWith(networkFirst(request, SHELL_CACHE, () => caches.match(page)));
    return;
  }

  event.respondWith(networkFirst(request, SHELL_CACHE, () => Response.error()));
});

async function networkFirst(request, cacheName, fallback) {
  try {
    const response = await fetch(request);
    if (response.ok) {
      const cache = await caches.open(cacheName);
      cache.put(request, response.clone());
      return response;
    }
    // Servidor reiniciando (deploy): 502/503/504 → usa a cópia guardada, se houver.
    if (response.status >= 502 && response.status <= 504) {
      const cached = await caches.match(request);
      if (cached) return cached;
    }
    return response;
  } catch {
    const cached = await caches.match(request);
    return cached ?? fallback(request);
  }
}

// Aviso de "sem conexão" no idioma do app (o app manda o idioma no cabeçalho X-Lang).
const OFFLINE = {
  pt: { title: "Sem conexão", detail: "Você está offline. Conecte-se à internet para continuar jogando." },
  en: { title: "No connection", detail: "You're offline. Connect to the internet to keep playing." },
  es: { title: "Sin conexión", detail: "No tienes conexión. Conéctate a internet para seguir jugando." },
};

function offlineApi(request) {
  const message = OFFLINE[request.headers.get("X-Lang")] ?? OFFLINE.pt;
  return new Response(JSON.stringify(message), {
    status: 503,
    headers: { "Content-Type": "application/problem+json" },
  });
}
