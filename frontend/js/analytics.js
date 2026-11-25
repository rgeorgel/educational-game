// Google Analytics só nas páginas de divulgação (landing, professores e blog), nunca no jogo (/jogar).
// O GA só é carregado depois que a pessoa aceita no aviso de cookies (LGPD). Até lá, nenhum
// script do Google é baixado e nenhum cookie é criado. A escolha fica no localStorage e pode ser
// mudada pelo link "Cookies" no rodapé. A política de privacidade (seção 8) descreve isso.

const GA_ID = "G-2F1VBP26SG";
const KEY = "edugame.analytics"; // "granted" | "denied"

function choice() {
  try {
    return localStorage.getItem(KEY);
  } catch {
    return null;
  }
}

function saveChoice(value) {
  try {
    localStorage.setItem(KEY, value);
  } catch {
    /* navegação privada sem armazenamento: vale só para esta página */
  }
}

let loaded = false;

function loadAnalytics() {
  if (loaded) return;
  loaded = true;
  window.dataLayer = window.dataLayer || [];
  window.gtag = function gtag() {
    window.dataLayer.push(arguments);
  };
  window.gtag("js", new Date());
  window.gtag("config", GA_ID, {
    // Cookies só neste subdomínio (sem isso o GA usaria o domínio principal inteiro).
    cookie_domain: location.hostname,
    allow_google_signals: false,
    allow_ad_personalization_signals: false,
  });
  const script = document.createElement("script");
  script.async = true;
  script.src = `https://www.googletagmanager.com/gtag/js?id=${encodeURIComponent(GA_ID)}`;
  document.head.append(script);
}

// Ao recusar depois de ter aceitado: apaga os cookies do GA (_ga, _ga_XXXX) deste site.
function clearAnalyticsCookies() {
  for (const part of document.cookie.split(";")) {
    const name = part.split("=")[0].trim();
    if (!name.startsWith("_ga")) continue;
    for (const domain of ["", `; domain=${location.hostname}`]) {
      document.cookie = `${name}=; expires=Thu, 01 Jan 1970 00:00:00 GMT; path=/${domain}`;
    }
  }
}

let banner = null;

// Textos do aviso no idioma da página (pt, en ou es).
const LANG = (document.documentElement.lang || "pt").slice(0, 2);
const TEXT = {
  pt: {
    label: "Aviso de cookies",
    body: "Com sua permissão, usamos o Google Analytics nas páginas de apresentação e no blog, para saber quantas pessoas visitam e de onde vêm. O jogo das crianças não tem nenhum rastreador.",
    more: "Saiba mais",
    privacy: "/privacidade#cookies",
    deny: "Recusar",
    accept: "Aceitar",
  },
  en: {
    label: "Cookie notice",
    body: "With your permission, we use Google Analytics on the information pages and the blog to know how many people visit and where they come from. The children's game has no trackers at all.",
    more: "Learn more",
    privacy: "/en/privacy#cookies",
    deny: "Decline",
    accept: "Accept",
  },
  es: {
    label: "Aviso de cookies",
    body: "Con tu permiso, usamos Google Analytics en las páginas de presentación y en el blog, para saber cuántas personas nos visitan y de dónde vienen. El juego de los niños no tiene ningún rastreador.",
    more: "Más información",
    privacy: "/es/privacidad#cookies",
    deny: "Rechazar",
    accept: "Aceptar",
  },
}[LANG] ?? {};

function showBanner() {
  if (banner) {
    banner.hidden = false;
    banner.querySelector("button")?.focus();
    return;
  }
  banner = document.createElement("section");
  banner.className = "lp-consent";
  banner.setAttribute("aria-label", TEXT.label);
  // Textos fixos deste arquivo (sem dados de fora), então innerHTML é seguro aqui.
  banner.innerHTML = `
    <p>${TEXT.body} <a href="${TEXT.privacy}">${TEXT.more}</a></p>
    <div class="lp-consent-actions">
      <button type="button" class="lp-btn lp-btn-ghost lp-btn-small" data-consent="denied">${TEXT.deny}</button>
      <button type="button" class="lp-btn lp-btn-primary lp-btn-small" data-consent="granted">${TEXT.accept}</button>
    </div>`;
  banner.addEventListener("click", (event) => {
    const value = event.target.closest("[data-consent]")?.dataset.consent;
    if (!value) return;
    const before = choice();
    saveChoice(value);
    banner.hidden = true;
    if (value === "granted") {
      loadAnalytics();
    } else if (before === "granted" || loaded) {
      // O GA já estava rodando nesta página: desliga (para ele não regravar os cookies),
      // apaga os cookies e recarrega para tirá-lo da página.
      window[`ga-disable-${GA_ID}`] = true;
      clearAnalyticsCookies();
      location.reload();
    }
  });
  document.body.append(banner);
}

function addFooterLink() {
  const nav = document.querySelector(".lp-footer nav");
  if (!nav) return;
  const button = document.createElement("button");
  button.type = "button";
  button.className = "lp-footer-link";
  button.textContent = "Cookies";
  button.addEventListener("click", showBanner);
  nav.append(button);
}

export function initAnalytics() {
  addFooterLink();
  const current = choice();
  if (current === "granted") loadAnalytics();
  else if (current === "denied") clearAnalyticsCookies(); // sobra de um aceite anterior
  else showBanner();
}
