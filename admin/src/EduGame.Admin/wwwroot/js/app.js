import { api, ApiError } from "./api.js";
import { columnChart, meter } from "./charts.js";
import { ago, dateTime, day, h, localDay, num, pct, shortDay, weekday } from "./dom.js";

const root = document.getElementById("app");
const SOURCES = { direto: "Acesso direto", compartilhar: "Compartilhado no jogo", "(sem registro)": "Antes do registro de origem" };
const sourceLabel = (source) => SOURCES[source] ?? source ?? "—";
const MODES = { etapa: "Etapas", guardiao: "Desafio do guardião", nivelamento: "Nivelamento", diario: "Desafio do dia", revisao: "Revisão" };
const prefs = loadPrefs();

// ---------- Tema (claro/escuro), lembrado só neste navegador ----------
function loadPrefs() {
  try {
    return JSON.parse(localStorage.getItem("edugame-admin") || "{}");
  } catch {
    return {};
  }
}
function savePrefs() {
  try {
    localStorage.setItem("edugame-admin", JSON.stringify(prefs));
  } catch {
    /* modo privado: sem problema */
  }
}
function applyTheme() {
  if (prefs.theme) document.documentElement.dataset.theme = prefs.theme;
  else delete document.documentElement.dataset.theme;
}
function currentTheme() {
  return prefs.theme ?? (matchMedia("(prefers-color-scheme: dark)").matches ? "dark" : "light");
}
applyTheme();

// ---------- Início: sessão e rotas ----------
let session = null;

async function boot() {
  try {
    session = await api.session();
  } catch {
    root.replaceChildren(h("main", { class: "center" }, h("p", { class: "error" }, "Não foi possível falar com o painel. Tente de novo em instantes.")));
    return;
  }
  if (!session.authenticated) return renderLogin();
  window.addEventListener("hashchange", route);
  route();
}

function renderLogin() {
  const error = h("p", { class: "error", role: "alert", hidden: true });
  const user = h("input", { id: "user", name: "username", autocomplete: "username", required: true });
  const pass = h("input", { id: "pass", name: "password", type: "password", autocomplete: "current-password", required: true });
  const button = h("button", { type: "submit", class: "primary" }, "Entrar");
  const form = h(
    "form",
    {
      class: "card login",
      onsubmit: async (e) => {
        e.preventDefault();
        button.disabled = true;
        error.hidden = true;
        try {
          await api.login(user.value, pass.value);
          location.hash = "#/visao";
          location.reload();
        } catch (err) {
          error.textContent = err.status === 429 ? "Muitas tentativas. Espere alguns minutos." : err.message;
          error.hidden = false;
          button.disabled = false;
          pass.select();
        }
      },
    },
    h("h1", {}, "Painel admin"),
    h("p", { class: "muted" }, "Educa Game · acompanhamento de uso"),
    session.configured
      ? [h("label", { for: "user" }, "Usuário"), user, h("label", { for: "pass" }, "Senha"), pass, error, button]
      : h("p", { class: "notice" }, session.problem),
  );
  root.replaceChildren(h("main", { class: "center" }, form));
  if (session.configured) user.focus();
}

const TABS = [
  ["visao", "Visão geral"],
  ["usuarios", "Usuários"],
  ["ranking", "Ranking"],
  ["conteudo", "Conteúdo"],
];

function shell(active, content) {
  const themeBtn = h(
    "button",
    {
      class: "ghost",
      type: "button",
      "aria-label": "Alternar tema claro/escuro",
      onclick: () => {
        prefs.theme = currentTheme() === "dark" ? "light" : "dark";
        savePrefs();
        applyTheme();
        route(); // redesenha os gráficos com as cores do tema
      },
    },
    currentTheme() === "dark" ? "☀️ Claro" : "🌙 Escuro",
  );
  return h(
    "div",
    { class: "layout" },
    h(
      "header",
      { class: "topbar" },
      h("div", { class: "brand" }, h("strong", {}, "Educa Game"), h("span", { class: "badge" }, "super admin")),
      h(
        "nav",
        { "aria-label": "Seções" },
        TABS.map(([slug, label]) => h("a", { href: `#/${slug}`, "aria-current": active === slug ? "page" : null }, label)),
      ),
      h(
        "div",
        { class: "top-actions" },
        themeBtn,
        h(
          "button",
          {
            class: "ghost",
            type: "button",
            onclick: async () => {
              await api.logout().catch(() => {});
              location.hash = "";
              location.reload();
            },
          },
          "Sair",
        ),
      ),
    ),
    h("main", { class: "content", id: "main" }, content),
  );
}

let routeToken = 0;
async function route() {
  const token = ++routeToken;
  const [, page = "visao", id] = (location.hash || "#/visao").split("/");
  const views = { visao: overviewPage, usuarios: id ? () => userPage(id) : usersPage, ranking: rankingPage, conteudo: contentPage };
  const view = views[page] ?? overviewPage;
  const active = TABS.some(([slug]) => slug === page) ? page : "visao";
  root.replaceChildren(shell(active, h("p", { class: "muted loading" }, "Carregando…")));
  try {
    const content = await view();
    if (token !== routeToken) return;
    root.replaceChildren(shell(active, content));
  } catch (err) {
    if (token !== routeToken) return;
    if (err instanceof ApiError && err.status === 401) return renderLogin();
    root.replaceChildren(shell(active, h("p", { class: "error" }, `Não foi possível carregar: ${err.message}`)));
  }
}

// ---------- Componentes ----------
function tile(label, value, sub, delta) {
  return h(
    "div",
    { class: "tile" },
    h("span", { class: "tile-label" }, label),
    h("strong", { class: "tile-value" }, value),
    sub ? h("span", { class: "tile-sub" }, sub) : null,
    delta ?? null,
  );
}

/** Variação contra o período anterior, com seta e texto (nunca só cor). */
function deltaBadge(now, before, label = "vs. 7 dias anteriores") {
  if (!before && !now) return null;
  if (!before) return h("span", { class: "delta" }, "sem dados na semana anterior");
  const change = Math.round(((now - before) / before) * 100);
  const dir = change > 0 ? "up" : change < 0 ? "down" : "flat";
  const arrow = { up: "▲", down: "▼", flat: "■" }[dir];
  return h("span", { class: `delta ${dir}` }, `${arrow} ${change > 0 ? "+" : ""}${change}% ${label}`);
}

function card(title, body, { sub, actions, wide } = {}) {
  return h(
    "section",
    { class: `card${wide ? " wide" : ""}` },
    h("div", { class: "card-head" }, h("div", {}, h("h2", {}, title), sub ? h("p", { class: "muted small" }, sub) : null), actions ?? null),
    body,
  );
}

function segmented(name, options, value, onChange) {
  return h(
    "div",
    { class: "segmented", role: "radiogroup", "aria-label": name },
    options.map(([v, label]) =>
      h("button", { type: "button", role: "radio", "aria-checked": String(String(v) === String(value)), onclick: () => onChange(v) }, label),
    ),
  );
}

function who(row) {
  return h(
    "span",
    { class: "who" },
    h("span", { class: "avatar", "aria-hidden": "true" }, row.avatar || row.avatars?.split(" ")[0] || "👤"),
    h("span", {}, h("strong", {}, row.nickname ?? (row.nicknames || "(sem perfil)")), " ", accountBadge(row)),
  );
}

function accountBadge(row) {
  if (row.isGuest) return h("span", { class: "pill guest" }, "convidado");
  return h("span", { class: "pill account" }, row.converted ? "conta (era convidado)" : "conta");
}

function emptyRow(cols, text) {
  return h("tr", {}, h("td", { colspan: cols, class: "empty" }, text));
}

// ---------- Visão geral ----------
async function overviewPage() {
  const days = prefs.days ?? 30;
  const [o, s, origins] = await Promise.all([api.overview(), api.series(days, 12), api.origins(days)]);
  const accuracy = pct(o.correct7d, o.answers7d);

  const tiles = h(
    "div",
    { class: "tiles" },
    tile("Contas cadastradas", num(o.accounts), `${num(o.guests)} ${o.guests === 1 ? "convidado" : "convidados"} · ${num(o.profiles)} ${o.profiles === 1 ? "perfil" : "perfis"}`),
    tile("Ativos hoje", num(o.activeToday), `${num(o.visitsToday)} acessos hoje`),
    tile("Ativos em 7 dias", num(o.active7d), `${num(o.active30d)} em 30 dias`, deltaBadge(o.active7d, o.activePrev7d)),
    tile("Respostas hoje", num(o.answersToday), `${num(o.playersToday)} ${o.playersToday === 1 ? "jogador respondeu" : "jogadores responderam"}`),
    tile("Respostas em 7 dias", num(o.answers7d), `${num(o.answersTotal)} desde o início`, deltaBadge(o.answers7d, o.answersPrev7d)),
    tile("Acertos em 7 dias", accuracy, o.avgSeconds7d != null ? `${String(o.avgSeconds7d).replace(".", ",")} s por resposta, em média` : "sem respostas"),
    tile("Novos em 7 dias", num(o.newAccounts7d + o.newGuests7d), `${num(o.newAccounts7d)} contas · ${num(o.newGuests7d)} convidados`),
    tile("Convidados que salvaram", num(o.converted), o.converted + o.guests ? `${pct(o.converted, o.converted + o.guests)} dos que começaram como convidado` : "nenhum convidado ainda"),
    tile("Partidas em 7 dias", num(o.sessions7d), `${pct(o.finished7d, o.sessions7d)} concluídas`),
    tile("Turmas", num(o.classrooms), `${num(o.classroomMembers)} alunos · ${num(o.assignments)} tarefas`),
    tile("Planos pagos ativos", num(o.paying), "concedidos com o comando \"plano\""),
  );

  const visitsNote =
    o.visitsSince === null
      ? "Os acessos começam a ser contados a partir desta versão do jogo."
      : `Acessos contados desde ${day(o.visitsSince)}. Contamos 1 acesso cada vez que alguém abre o jogo (ou entra/cria conta).`;

  const rangeControl = segmented("Período dos gráficos diários", [[14, "14 dias"], [30, "30 dias"], [90, "90 dias"]], days, (v) => {
    prefs.days = v;
    savePrefs();
    route();
  });

  const answersDaily = columnChart({
    title: "Perguntas respondidas por dia",
    series: [{ name: "Respostas", cls: "s1" }],
    data: s.daily.map((d) => ({
      date: d.date,
      label: `${weekday(d.date)}, ${day(d.date)}`,
      values: [d.answers],
      tip: [`${pct(d.correct, d.answers)} de acerto`, `${num(d.players)} ${d.players === 1 ? "jogador" : "jogadores"}`],
    })),
    xLabel: (d) => shortDay(d.date),
  });
  const answersWeekly = columnChart({
    title: "Perguntas respondidas por semana",
    series: [{ name: "Respostas", cls: "s1" }],
    data: s.weekly.map((w) => ({
      date: w.weekStart,
      label: `Semana de ${day(w.weekStart)}`,
      values: [w.answers],
      tip: [`${pct(w.correct, w.answers)} de acerto`, `${num(w.players)} jogadores · ${num(w.activeUsers)} pessoas ativas`],
    })),
    xLabel: (d) => shortDay(d.date),
  });
  const activeDaily = columnChart({
    title: "Pessoas ativas por dia",
    series: [{ name: "Pessoas ativas", cls: "s1" }],
    data: s.daily.map((d) => ({ date: d.date, label: `${weekday(d.date)}, ${day(d.date)}`, values: [d.activeUsers], tip: [`${num(d.visits)} acessos no total`] })),
    xLabel: (d) => shortDay(d.date),
  });
  const signups = columnChart({
    title: "Novos cadastros por dia",
    series: [
      { name: "Contas", cls: "s1" },
      { name: "Convidados", cls: "s2" },
    ],
    data: s.daily.map((d) => ({ date: d.date, label: `${weekday(d.date)}, ${day(d.date)}`, values: [d.newAccounts, d.newGuests] })),
    xLabel: (d) => shortDay(d.date),
  });
  const hours = columnChart({
    title: "Respostas por horário do dia",
    series: [{ name: "Respostas", cls: "s1" }],
    data: s.hours.map((x) => ({ label: `${String(x.hour).padStart(2, "0")}h às ${String(x.hour).padStart(2, "0")}h59`, hour: x.hour, values: [x.answers] })),
    xLabel: (d) => `${d.hour}h`,
    labelEvery: 3,
  });

  return h(
    "div",
    { class: "page" },
    h("div", { class: "page-head" }, h("h1", {}, "Visão geral"), h("p", { class: "muted small" }, `Hoje é ${day(o.today)} (horário de Brasília). ${visitsNote}`)),
    tiles,
    h("div", { class: "filters" }, h("span", { class: "muted small" }, "Gráficos diários:"), rangeControl),
    h(
      "div",
      { class: "grid" },
      card("Perguntas respondidas por dia", answersDaily, { sub: `Últimos ${days} dias`, wide: true }),
      card("Perguntas respondidas por semana", answersWeekly, { sub: "Últimas 12 semanas (começam na segunda-feira)" }),
      card("Pessoas ativas por dia", activeDaily, { sub: "Pessoas diferentes que abriram o jogo" }),
      card("Novos cadastros por dia", signups, { sub: "Contas com e-mail e convidados (só apelido)" }),
      card("Respostas por horário do dia", hours, { sub: "Últimos 30 dias, horário de Brasília" }),
      card("De onde vêm as pessoas", originsView(origins), {
        sub: `Novos usuários dos últimos ${days} dias, pela origem do primeiro acesso (utm_source, indicação ou site de origem)`,
        wide: true,
      }),
    ),
  );
}

function originsView({ sources, campaigns }) {
  const total = sources.reduce((a, x) => a + x.users, 0);
  const max = Math.max(1, ...sources.map((x) => x.users));
  const table = h(
    "div",
    { class: "table-scroll" },
    h(
      "table",
      { class: "data" },
      h(
        "thead",
        {},
        h("tr", {}, h("th", {}, "Origem"), h("th", { class: "n" }, "Novos"), h("th", { class: "n" }, "Contas"), h("th", { class: "n" }, "Convidados"), h("th", { class: "n" }, "Jogaram"), h("th", { class: "n" }, "Voltaram")),
      ),
      h(
        "tbody",
        {},
        sources.length
          ? sources.map((x) =>
              h(
                "tr",
                {},
                h("td", {}, h("strong", {}, sourceLabel(x.source))),
                h("td", { class: "n" }, h("span", { class: "acc" }, `${num(x.users)} · ${pct(x.users, total)}`, meter((x.users / max) * 100, 100, `${x.users} novos`))),
                h("td", { class: "n" }, num(x.accounts)),
                h("td", { class: "n" }, num(x.guests)),
                h("td", { class: "n" }, pct(x.played, x.users)),
                h("td", { class: "n" }, pct(x.returned, x.users)),
              ),
            )
          : emptyRow(6, "Ninguém novo no período."),
      ),
    ),
  );
  const campaignTable = campaigns.length
    ? h(
        "details",
        { class: "table-view" },
        h("summary", {}, `Campanhas (utm_medium / utm_campaign): ${campaigns.length}`),
        h(
          "div",
          { class: "table-scroll" },
          h(
            "table",
            {},
            h("thead", {}, h("tr", {}, h("th", {}, "Origem"), h("th", {}, "Meio"), h("th", {}, "Campanha"), h("th", { class: "n" }, "Novos"), h("th", { class: "n" }, "Jogaram"), h("th", { class: "n" }, "Voltaram"))),
            h(
              "tbody",
              {},
              campaigns.map((c) =>
                h("tr", {}, h("td", {}, sourceLabel(c.source)), h("td", {}, c.medium || "—"), h("td", {}, c.campaign || "—"), h("td", { class: "n" }, num(c.users)), h("td", { class: "n" }, pct(c.played, c.users)), h("td", { class: "n" }, pct(c.returned, c.users))),
              ),
            ),
          ),
        ),
      )
    : null;
  return h(
    "div",
    {},
    table,
    h("p", { class: "muted small" }, "Jogaram = responderam pelo menos uma pergunta. Voltaram = abriram o jogo em 2 dias ou mais. Para marcar um link: ?utm_source=whatsapp&utm_campaign=grupo-professores"),
    campaignTable,
  );
}

// ---------- Usuários ----------
async function usersPage() {
  const params = { q: prefs.q ?? "", type: prefs.type ?? "todos", sort: prefs.sort ?? "recentes", page: prefs.page ?? 1 };
  const data = await api.users(params);
  const pages = Math.max(1, Math.ceil(data.total / data.pageSize));

  const update = (patch) => {
    Object.assign(prefs, patch);
    savePrefs();
    route();
  };
  const search = h("input", { type: "search", placeholder: "Buscar por e-mail ou apelido", value: params.q, "aria-label": "Buscar usuários" });
  let timer;
  search.addEventListener("input", () => {
    clearTimeout(timer);
    timer = setTimeout(() => update({ q: search.value, page: 1 }), 350);
  });

  const sortSelect = h(
    "select",
    { "aria-label": "Ordenar", onchange: (e) => update({ sort: e.target.value, page: 1 }) },
    [
      ["recentes", "Mais recentes"],
      ["ultimo-acesso", "Último acesso"],
      ["respostas", "Mais respostas"],
      ["xp", "Mais XP"],
    ].map(([v, label]) => h("option", { value: v, selected: v === params.sort }, label)),
  );

  const rows = data.items.map((u) =>
    h(
      "tr",
      { class: "clickable", tabindex: 0, onclick: () => (location.hash = `#/usuarios/${u.id}`), onkeydown: (e) => e.key === "Enter" && (location.hash = `#/usuarios/${u.id}`) },
      h("td", {}, who(u), u.profiles > 1 ? h("div", { class: "muted small" }, `${u.profiles} perfis`) : null),
      h("td", {}, u.email ?? h("span", { class: "muted" }, "—")),
      h("td", {}, localDay(u.createdAt), h("div", { class: "muted small" }, `${ago(u.createdAt)}${u.source ? ` · ${sourceLabel(u.source)}` : ""}`)),
      h("td", {}, ago(u.lastSeenAt)),
      h("td", { class: "n" }, num(u.solved)),
      h("td", { class: "n" }, pct(u.correct, u.solved)),
      h("td", { class: "n" }, num(u.xp)),
      h("td", { class: "n" }, num(u.visits30d), h("div", { class: "muted small" }, `${u.days30d} ${u.days30d === 1 ? "dia" : "dias"}`)),
    ),
  );

  setTimeout(() => {
    if (params.q) {
      search.focus();
      search.setSelectionRange(search.value.length, search.value.length);
    }
  });

  return h(
    "div",
    { class: "page" },
    h("div", { class: "page-head" }, h("h1", {}, "Usuários"), h("p", { class: "muted small" }, `${num(data.total)} encontrados`)),
    h(
      "div",
      { class: "filters" },
      search,
      segmented("Tipo de usuário", [["todos", "Todos"], ["conta", "Contas"], ["convidado", "Convidados"]], params.type, (v) => update({ type: v, page: 1 })),
      sortSelect,
    ),
    h(
      "div",
      { class: "card table-card" },
      h(
        "div",
        { class: "table-scroll" },
        h(
          "table",
          { class: "data" },
          h(
            "thead",
            {},
            h("tr", {}, h("th", {}, "Jogador"), h("th", {}, "E-mail"), h("th", {}, "Cadastro"), h("th", {}, "Último acesso"), h("th", { class: "n" }, "Respostas"), h("th", { class: "n" }, "Acertos"), h("th", { class: "n" }, "XP"), h("th", { class: "n" }, "Acessos (30d)")),
          ),
          h("tbody", {}, rows.length ? rows : emptyRow(8, "Ninguém encontrado com esses filtros.")),
        ),
      ),
      pages > 1
        ? h(
            "div",
            { class: "pager" },
            h("button", { type: "button", class: "ghost", disabled: params.page <= 1, onclick: () => update({ page: params.page - 1 }) }, "← Anterior"),
            h("span", { class: "muted small" }, `Página ${params.page} de ${pages}`),
            h("button", { type: "button", class: "ghost", disabled: params.page >= pages, onclick: () => update({ page: params.page + 1 }) }, "Próxima →"),
          )
        : null,
    ),
  );
}

async function userPage(id) {
  const { user, profiles, activity, sessions, subscriptions } = await api.user(id);
  const solved = profiles.reduce((a, p) => a + p.solved, 0);
  const correct = profiles.reduce((a, p) => a + p.correct, 0);

  const chart = columnChart({
    title: "Problemas resolvidos por dia",
    series: [{ name: "Problemas", cls: "s1" }],
    data: activity.map((d) => ({ date: d.date, label: `${weekday(d.date)}, ${day(d.date)}`, values: [d.solved], tip: [`${pct(d.correct, d.solved)} de acerto`, `${num(d.visits)} acessos`] })),
    xLabel: (d) => shortDay(d.date),
    height: 180,
  });

  return h(
    "div",
    { class: "page" },
    h("a", { href: "#/usuarios", class: "back" }, "← Usuários"),
    h(
      "div",
      { class: "page-head" },
      h("h1", {}, user.email ?? profiles[0]?.nickname ?? "Convidado", " ", accountBadge(user)),
      h("p", { class: "muted small" }, `Cadastro em ${dateTime(user.createdAt)} · último acesso ${ago(user.lastSeenAt)}${user.consentAcceptedAt ? ` · termos aceitos em ${dateTime(user.consentAcceptedAt)}` : ""}`),
      user.source
        ? h("p", { class: "muted small" }, `Origem: ${sourceLabel(user.source)}${user.medium ? ` · meio ${user.medium}` : ""}${user.campaign ? ` · campanha ${user.campaign}` : ""}${user.referrer ? ` · veio de ${user.referrer}` : ""}`)
        : null,
    ),
    h(
      "div",
      { class: "tiles" },
      tile("Problemas resolvidos", num(solved), `${pct(correct, solved)} de acerto`),
      tile("Acessos", num(user.visitsTotal), `${user.days30d} ${user.days30d === 1 ? "dia ativo" : "dias ativos"} nos últimos 30`),
      tile("Perfis", num(profiles.length), user.classroomsOwned ? `${user.classroomsOwned} turmas criadas` : "nenhuma turma criada"),
    ),
    card("Últimos 30 dias", chart, { wide: true }),
    subscriptions?.length
      ? card(
          "Planos",
          h("ul", { class: "plain-list" }, subscriptions.map((s) => h("li", {}, h("strong", {}, s.plan), ` · ${s.active ? "ativo" : "encerrado"} · desde ${localDay(s.startsAt)}${s.endsAt ? ` até ${localDay(s.endsAt)}` : " sem fim"}${s.note ? ` · ${s.note}` : ""}`))),
          { wide: true },
        )
      : null,
    h(
      "div",
      { class: "grid" },
      profiles.map((p) =>
        card(
          `${p.avatar} ${p.nickname}`,
          h(
            "dl",
            { class: "facts" },
            fact("Níveis", p.levels ?? "ainda não jogou"),
            fact("Problemas", `${num(p.solved)} · ${pct(p.correct, p.solved)} de acerto`),
            fact("XP", num(p.xp)),
            fact("Dias jogando", num(p.activeDays)),
            fact("Último jogo", p.lastPlayed ? day(p.lastPlayed) : "—"),
            fact("Medalhas", num(p.medals)),
            fact("Turmas", num(p.classrooms)),
            p.schoolGrade ? fact("Ano escolar", `${p.schoolGrade}º ano`) : null,
            fact("Criado em", dateTime(p.createdAt)),
          ),
        ),
      ),
    ),
    card(
      "Partidas recentes",
      h(
        "div",
        { class: "table-scroll" },
        h(
          "table",
          { class: "data" },
          h("thead", {}, h("tr", {}, h("th", {}, "Quando"), h("th", {}, "Perfil"), h("th", {}, "Modo"), h("th", {}, "Matéria / etapa"), h("th", { class: "n" }, "Acertos"), h("th", { class: "n" }, "XP"), h("th", {}, "Estrelas"))),
          h(
            "tbody",
            {},
            sessions.length
              ? sessions.map((s) =>
                  h(
                    "tr",
                    {},
                    h("td", {}, dateTime(s.startedAt)),
                    h("td", {}, s.nickname),
                    h("td", {}, MODES[s.mode] ?? s.mode, s.finished ? null : h("span", { class: "pill muted-pill" }, "não concluída")),
                    h("td", {}, s.subject, s.stage ? h("div", { class: "muted small" }, s.stage) : null),
                    h("td", { class: "n" }, `${s.correct}/${s.problems}`),
                    h("td", { class: "n" }, num(s.xp)),
                    h("td", {}, s.stars != null ? h("span", { "aria-label": `${s.stars} de 3 estrelas` }, "★".repeat(s.stars) + "☆".repeat(3 - s.stars)) : "—"),
                  ),
                )
              : emptyRow(7, "Nenhuma partida ainda."),
          ),
        ),
      ),
      { wide: true },
    ),
  );
}

function fact(label, value) {
  return [h("dt", {}, label), h("dd", {}, value)];
}

// ---------- Ranking ----------
async function rankingPage() {
  const params = { by: prefs.rankBy ?? "xp", days: prefs.rankDays ?? 0, guests: prefs.rankGuests ?? true };
  const data = await api.ranking(params);
  const update = (patch) => {
    Object.assign(prefs, patch);
    savePrefs();
    route();
  };
  const place = (i) => ["🥇", "🥈", "🥉"][i] ?? `${i + 1}º`;
  const guestToggle = h("input", { type: "checkbox", id: "rank-guests", checked: params.guests, onchange: (e) => update({ rankGuests: e.target.checked }) });

  const rows = data.items.map((r, i) =>
    h(
      "tr",
      { class: "clickable", tabindex: 0, onclick: () => (location.hash = `#/usuarios/${r.userId}`), onkeydown: (e) => e.key === "Enter" && (location.hash = `#/usuarios/${r.userId}`) },
      h("td", { class: "place" }, place(i)),
      h("td", {}, who(r), r.email ? h("div", { class: "muted small" }, r.email) : null),
      h("td", { class: "small" }, r.levels ?? "—"),
      h("td", { class: "n" }, num(r.xp)),
      h("td", { class: "n" }, num(r.solved)),
      h("td", { class: "n" }, h("span", { class: "acc" }, pct(r.correct, r.solved), meter((r.correct / r.solved) * 100, 100, `${pct(r.correct, r.solved)} de acerto`))),
      h("td", { class: "n" }, num(r.activeDays)),
      h("td", { class: "n" }, num(r.medals)),
      h("td", {}, day(r.lastPlayed)),
    ),
  );

  return h(
    "div",
    { class: "page" },
    h(
      "div",
      { class: "page-head" },
      h("h1", {}, "Ranking"),
      h("p", { class: "muted small" }, params.by === "acertos" ? `Por % de acertos, só entra quem respondeu pelo menos ${data.minForAccuracy} perguntas no período.` : "Os 50 primeiros perfis de jogador. Clique para ver os detalhes."),
    ),
    h(
      "div",
      { class: "filters" },
      segmented("Ordenar por", [["xp", "XP"], ["respostas", "Respostas"], ["acertos", "% de acertos"], ["dias", "Dias jogando"]], params.by, (v) => update({ rankBy: v })),
      segmented("Período", [[0, "Sempre"], [7, "7 dias"], [30, "30 dias"], [90, "90 dias"]], params.days, (v) => update({ rankDays: v })),
      h("label", { class: "check" }, guestToggle, "Incluir convidados"),
    ),
    h(
      "div",
      { class: "card table-card" },
      h(
        "div",
        { class: "table-scroll" },
        h(
          "table",
          { class: "data" },
          h(
            "thead",
            {},
            h("tr", {}, h("th", {}, "#"), h("th", {}, "Jogador"), h("th", {}, "Nível"), h("th", { class: "n" }, "XP"), h("th", { class: "n" }, "Respostas"), h("th", { class: "n" }, "Acertos"), h("th", { class: "n" }, "Dias"), h("th", { class: "n" }, "Medalhas"), h("th", {}, "Último jogo")),
          ),
          h("tbody", {}, rows.length ? rows : emptyRow(9, "Ninguém jogou nesse período.")),
        ),
      ),
    ),
  );
}

// ---------- Conteúdo ----------
async function contentPage() {
  const days = prefs.contentDays ?? 30;
  const d = await api.insights(days);
  const minAnswers = 10;
  const rated = d.skills.filter((s) => s.answers >= minAnswers);
  const hardest = [...rated].sort((a, b) => a.correct / a.answers - b.correct / b.answers).slice(0, 10);
  const practiced = [...d.skills].sort((a, b) => b.answers - a.answers).slice(0, 10);
  const range = segmented("Período", [[7, "7 dias"], [30, "30 dias"], [90, "90 dias"], [365, "1 ano"]], days, (v) => {
    prefs.contentDays = v;
    savePrefs();
    route();
  });

  const skillTable = (list, empty) =>
    h(
      "div",
      { class: "table-scroll" },
      h(
        "table",
        { class: "data" },
        h("thead", {}, h("tr", {}, h("th", {}, "Habilidade"), h("th", { class: "n" }, "Respostas"), h("th", { class: "n" }, "Acertos"), h("th", { class: "n" }, "Tempo (mediana)"), h("th", { class: "n" }, "Dicas/resp."))),
        h(
          "tbody",
          {},
          list.length
            ? list.map((s) =>
                h(
                  "tr",
                  {},
                  h("td", {}, h("strong", {}, s.name), h("div", { class: "muted small" }, `${s.subject} · ${s.level}`), s.bncc?.length ? h("div", { class: "bncc small" }, `BNCC: ${s.bncc.join(", ")}`) : null),
                  h("td", { class: "n" }, num(s.answers)),
                  h("td", { class: "n" }, h("span", { class: "acc" }, pct(s.correct, s.answers), meter((s.correct / s.answers) * 100))),
                  h("td", { class: "n" }, s.medianSeconds == null ? "—" : s.medianSeconds < 1 ? "< 1 s" : `${String(s.medianSeconds).replace(".", ",")} s`),
                  h("td", { class: "n" }, s.avgHints != null ? String(s.avgHints).replace(".", ",") : "—"),
                ),
              )
            : emptyRow(5, empty),
        ),
      ),
    );

  // Distribuição de níveis: uma barra por nível, agrupada por matéria.
  const bySubject = Object.groupBy(d.levels, (l) => l.subject);
  const levelBlocks = Object.entries(bySubject).map(([subject, levels]) => {
    const total = levels.reduce((a, l) => a + l.profiles, 0);
    const max = Math.max(1, ...levels.map((l) => l.profiles));
    return h(
      "div",
      { class: "level-block" },
      h("h3", {}, subject),
      h(
        "ul",
        { class: "hbars" },
        levels.map((l) => h("li", {}, h("span", { class: "hbar-label" }, l.level), meter((l.profiles / max) * 100, 100, `${l.level}: ${l.profiles} perfis`), h("span", { class: "hbar-value" }, `${num(l.profiles)} · ${pct(l.profiles, total)}`))),
      ),
    );
  });

  return h(
    "div",
    { class: "page" },
    h("div", { class: "page-head" }, h("h1", {}, "Conteúdo"), h("p", { class: "muted small" }, "Onde os jogadores têm mais dificuldade e o que mais jogam.")),
    h("div", { class: "filters" }, range),
    h(
      "div",
      { class: "grid" },
      card("Habilidades mais difíceis", skillTable(hardest, `Nenhuma habilidade com ${minAnswers}+ respostas no período.`), { sub: `Menor % de acertos (mínimo de ${minAnswers} respostas)`, wide: true }),
      card("Habilidades mais praticadas", skillTable(practiced, "Ninguém respondeu nada no período."), { sub: "Mais respostas no período", wide: true }),
      card(
        "Etapas mais jogadas",
        h(
          "div",
          { class: "table-scroll" },
          h(
            "table",
            { class: "data" },
            h("thead", {}, h("tr", {}, h("th", {}, "Etapa"), h("th", { class: "n" }, "Partidas"), h("th", { class: "n" }, "Concluídas"), h("th", { class: "n" }, "Estrelas (média)"), h("th", { class: "n" }, "Jogadores"))),
            h(
              "tbody",
              {},
              d.stages.length
                ? d.stages.map((s) =>
                    h(
                      "tr",
                      {},
                      h("td", {}, h("strong", {}, `${s.subjectIcon} ${s.name}`), h("div", { class: "muted small" }, `${s.region} · ${s.level}`)),
                      h("td", { class: "n" }, num(s.sessions)),
                      h("td", { class: "n" }, pct(s.finished, s.sessions)),
                      h("td", { class: "n" }, s.avgStars != null ? String(s.avgStars).replace(".", ",") : "—"),
                      h("td", { class: "n" }, num(s.players)),
                    ),
                  )
                : emptyRow(5, "Nenhuma etapa jogada no período."),
            ),
          ),
        ),
        { sub: "As 15 com mais partidas no período", wide: true },
      ),
      card(
        "Modos de jogo",
        h(
          "div",
          { class: "table-scroll" },
          h(
            "table",
            { class: "data" },
            h("thead", {}, h("tr", {}, h("th", {}, "Modo"), h("th", { class: "n" }, "Partidas"), h("th", { class: "n" }, "Concluídas"), h("th", { class: "n" }, "Acertos"), h("th", { class: "n" }, "Jogadores"))),
            h(
              "tbody",
              {},
              d.modes.length
                ? d.modes.map((m) =>
                    h("tr", {}, h("td", {}, MODES[m.mode] ?? m.mode), h("td", { class: "n" }, num(m.sessions)), h("td", { class: "n" }, pct(m.finished, m.sessions)), h("td", { class: "n" }, pct(m.correct, m.problems)), h("td", { class: "n" }, num(m.players))),
                  )
                : emptyRow(5, "Nenhuma partida no período."),
            ),
          ),
        ),
      ),
      card("Jogadores por nível", h("div", { class: "levels" }, levelBlocks), { sub: "Nível atual de cada perfil (todo o período)" }),
      card(
        "Medalhas conquistadas",
        h(
          "div",
          { class: "table-scroll" },
          h(
            "table",
            { class: "data" },
            h("thead", {}, h("tr", {}, h("th", {}, "Medalha"), h("th", { class: "n" }, "Perfis"), h("th", {}, "Última"))),
            h(
              "tbody",
              {},
              d.medals.length
                ? d.medals.map((m) => h("tr", {}, h("td", {}, m.slug.replaceAll("-", " ")), h("td", { class: "n" }, num(m.profiles)), h("td", {}, ago(m.lastAt))))
                : emptyRow(3, "Nenhuma medalha ainda."),
            ),
          ),
        ),
        { sub: "Todo o período" },
      ),
    ),
  );
}

boot();
