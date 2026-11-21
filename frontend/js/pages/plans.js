// Página de planos e avisos de "recurso de plano". Por enquanto escolher um plano não cobra nada:
// a pessoa escolhe e o plano já vale. Depois o pagamento será feito pelo Stripe.
import { api } from "../api.js";
import { errorMessage, h } from "../dom.js";
import { navigate } from "../router.js";
import { store } from "../store.js";

const FEATURES = {
  gratis: ["Jogar tudo: Matemática e Lógica, todos os níveis", "Até 3 perfis", "1 turma com até 2 tarefas abertas", "Relatório básico dos últimos 7 dias"],
  professor: ["Até 20 turmas", "Até 50 tarefas abertas por turma", "Relatório da turma por habilidade (BNCC)", "Planilhas da turma (Excel e Google Planilhas)", "Fichas para imprimir com gabarito"],
  familia: ["Até 8 perfis (todos os filhos)", "Relatório da semana, comparado com a semana anterior", "Treino de reforço: você escolhe a habilidade e ela aparece para a criança", "Fichas para imprimir com gabarito"],
  escola: ["Tudo do Professor Pro e da Família", "Até 300 turmas", "Contrato e nota fiscal"],
};

/** Aviso para quando o recurso é de outro plano (erro 403 da API com "plan"). */
export function lockedCard(err) {
  const plan = err?.problem?.plan;
  if (!plan) return null;
  return h(
    "div",
    { class: "card plan-locked" },
    h("p", {}, "🔒 ", errorMessage(err)),
    h("a", { href: "#/planos", class: "btn btn-primary btn-small" }, "Ver planos"),
  );
}

const PROMO = {
  familia: { icon: "👪", title: "Plano Família", price: "R$ 9,90/mês", text: "Relatório da semana de cada filho, treino de reforço escolhido por você, fichas para imprimir e até 8 perfis." },
  professor: { icon: "🏫", title: "Professor Pro", price: "R$ 19,90/mês", text: "Até 20 turmas, tarefas para a turma, relatório por habilidade da BNCC, planilhas e fichas para imprimir." },
};

/**
 * Destaque dos planos nas áreas do adulto. Quem ainda não tem o plano vê um cartão com o que ganha;
 * quem já tem vê só a linha "Seu plano".
 * @param {"familia"|"professor"} focus plano mais útil para quem está nesta tela
 */
export function planPromo(plan, focus, billing) {
  if (!plan) return null;
  const has = plan.paid?.includes(focus) || plan.chosen === focus || plan.plans?.some((p) => p.slug === focus || p.slug === "escola");
  if (has) return planLine(plan);
  const promo = PROMO[focus];
  const launch = billing?.mode === "lancamento";
  return h(
    "section",
    { class: "card plan-promo" },
    h("span", { class: "plan-promo-icon", "aria-hidden": "true" }, promo.icon),
    h(
      "div",
      {},
      h("h2", {}, promo.title, " ", h("span", { class: "plan-promo-price" }, launch ? "grátis durante o lançamento" : promo.price)),
      h("p", { class: "small" }, promo.text),
      h("p", { class: "muted small" }, `Seu plano agora: ${plan.name}.`),
    ),
    h("a", { href: "#/planos", class: "btn btn-primary" }, "Conhecer os planos"),
  );
}

const shortDate = (iso) => new Date(iso).toLocaleDateString("pt-BR", { day: "2-digit", month: "2-digit" });

/** "Seu plano: Família (teste grátis até 03/12) · Ver planos" */
export function planLine(plan) {
  if (!plan) return null;
  const trial = plan.trials?.find((t) => t.slug === plan.slug);
  return h(
    "p",
    { class: "muted small plan-line" },
    "Seu plano: ",
    h("strong", {}, plan.name),
    trial ? ` (teste grátis até ${shortDate(trial.endsAt)})` : null,
    " · ",
    h("a", { href: "#/planos" }, "ver planos"),
  );
}

/** Volta do Stripe (?pagamento=sucesso|cancelado): mostra o aviso e limpa o endereço. */
function paymentReturn() {
  const params = new URLSearchParams(location.search);
  const result = params.get("pagamento");
  if (!result) return null;
  history.replaceState(null, "", `${location.pathname}${location.hash}`);
  return result;
}

/** Depois do pagamento, o Stripe avisa o servidor em alguns segundos: espera o plano aparecer. */
async function waitForPaidPlan(tries = 10) {
  for (let i = 0; i < tries; i++) {
    const mine = await api.myPlan().catch(() => null);
    if (mine?.paid?.length) return mine;
    await new Promise((r) => setTimeout(r, 1500));
  }
  return null;
}

export async function plansPage() {
  const returned = paymentReturn();
  const [offers, billing] = await Promise.all([api.plans(), api.billingStatus().catch(() => ({ mode: "lancamento", paidPlans: [] }))]);
  const stripe = billing.mode === "stripe";
  let mine = store.state.me ? await api.myPlan().catch(() => null) : null;
  if (returned === "sucesso" && stripe && store.state.me && !mine?.paid?.length) mine = (await waitForPaidPlan()) ?? mine;

  const error = h("p", { class: "form-error", role: "alert", hidden: true });
  const showError = (err) => {
    error.textContent = errorMessage(err);
    error.hidden = false;
  };
  const isGuest = store.state.me?.isGuest;
  const paid = mine?.paid ?? [];
  const current = paid[0] ?? mine?.chosen ?? (mine?.plans?.some((p) => p.slug !== "gratis") ? mine.slug : "gratis");

  const busy = (btn, fn) =>
    btn.addEventListener("click", async () => {
      btn.disabled = true;
      try {
        await fn();
      } catch (err) {
        showError(err);
        btn.disabled = false;
      }
    });

  const card = (offer) => {
    const isPaid = paid.includes(offer.slug);
    const isCurrent = mine && (isPaid || offer.slug === current || mine.plans?.some((p) => p.slug === offer.slug && offer.slug !== "gratis"));
    const paidOffer = stripe && billing.paidPlans.includes(offer.slug);
    const trial = mine?.trials?.find((t) => t.slug === offer.slug);
    const trialNote = trial ? h("p", { class: "plan-current" }, `✓ Teste grátis até ${shortDate(trial.endsAt)}`) : null;
    let action;
    if (!offer.selfService) {
      action = h("p", { class: "muted small" }, "Para escolas e redes de ensino: fale com a gente.");
    } else if (!store.state.me) {
      action = h("a", { class: "btn btn-primary", href: "#/cadastro" }, "Criar conta");
    } else if (isGuest) {
      action = h("a", { class: "btn btn-primary", href: "#/salvar" }, offer.slug === "gratis" ? "Salvar progresso" : "Salvar progresso para assinar");
    } else if (isPaid) {
      const manage = h("button", { type: "button", class: "btn" }, "Gerenciar assinatura");
      busy(manage, async () => location.assign((await api.billingPortal()).url));
      action = [h("p", { class: "plan-current" }, "✓ Assinatura ativa"), manage];
    } else if (isCurrent && !(paidOffer && (mine?.chosen === offer.slug || trial))) {
      action = trialNote ?? h("p", { class: "plan-current" }, "✓ Seu plano atual");
    } else if (paidOffer) {
      // Quem escolheu este plano no lançamento ou está em teste grátis também vê "Assinar" para continuar depois.
      const btn = h("button", { type: "button", class: "btn btn-primary" }, `Assinar ${offer.name}`);
      busy(btn, async () => location.assign((await api.checkout(offer.slug)).url));
      action = [trialNote ?? (mine?.chosen === offer.slug ? h("p", { class: "muted small" }, "Você escolheu este plano no lançamento.") : null), btn];
    } else if (offer.slug === "gratis" && stripe && paid.length) {
      action = h("p", { class: "muted small" }, "Para voltar ao Grátis, cancele em \"Gerenciar assinatura\".");
    } else {
      const btn = h("button", { type: "button", class: `btn ${offer.slug === "gratis" ? "" : "btn-primary"}` }, offer.slug === "gratis" ? "Voltar para o Grátis" : `Escolher ${offer.name}`);
      busy(btn, async () => {
        await api.choosePlan(offer.slug);
        navigate("/planos");
      });
      action = btn;
    }
    return h(
      "article",
      { class: `card plan-card${offer.slug === "familia" || offer.slug === "professor" ? " plan-featured" : ""}${isCurrent ? " plan-is-current" : ""}` },
      h("p", { class: "muted small" }, offer.audience),
      h("h2", {}, offer.name),
      h("p", { class: "plan-price" }, offer.priceLabel, offer.slug !== "gratis" && offer.selfService && !stripe ? h("span", { class: "plan-free-now" }, "grátis durante o lançamento") : null),
      h("ul", { class: "plan-features" }, (FEATURES[offer.slug] ?? []).map((f) => h("li", {}, f))),
      action,
    );
  };

  const banner =
    returned === "sucesso"
      ? h("p", { class: "card notice success small" }, paid.length ? "🎉 Pagamento confirmado! Sua assinatura já está ativa." : "Pagamento recebido. A confirmação pode levar alguns minutos; atualize a página daqui a pouco.")
      : returned === "cancelado"
        ? h("p", { class: "card notice small" }, "Pagamento cancelado. Nada foi cobrado.")
        : null;

  return h(
    "section",
    { class: "page" },
    h("h1", {}, "Planos"),
    h(
      "p",
      { class: "muted" },
      "Para as crianças, o jogo é sempre grátis e sem anúncios. Os planos são para os adultos que acompanham: famílias e professores.",
    ),
    banner,
    stripe
      ? h("p", { class: "card notice small" }, h("strong", {}, "Pagamento seguro pelo Stripe "), "(cartão). Sem fidelidade: cancele quando quiser em \"Gerenciar assinatura\" e o plano vale até o fim do período pago.")
      : h(
          "p",
          { class: "card notice small" },
          h("strong", {}, "Lançamento: "),
          "escolher um plano não cobra nada agora. Quando o pagamento começar, avisaremos antes e você decide se continua.",
        ),
    mine ? planLine(mine) : null,
    error,
    h("div", { class: "plan-grid" }, offers.map(card)),
  );
}
