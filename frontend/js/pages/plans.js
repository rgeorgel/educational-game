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

/** "Seu plano: Família · Ver planos" */
export function planLine(plan) {
  if (!plan) return null;
  return h("p", { class: "muted small plan-line" }, "Seu plano: ", h("strong", {}, plan.name), " · ", h("a", { href: "#/planos" }, "ver planos"));
}

export async function plansPage() {
  const [offers, mine] = await Promise.all([api.plans(), store.state.me ? api.myPlan().catch(() => null) : null]);
  const error = h("p", { class: "form-error", role: "alert", hidden: true });
  const isGuest = store.state.me?.isGuest;
  const current = mine?.chosen ?? (mine?.plans?.some((p) => p.slug !== "gratis") ? mine.slug : "gratis");

  const card = (offer) => {
    const isCurrent = mine && (offer.slug === current || mine.plans?.some((p) => p.slug === offer.slug && offer.slug !== "gratis"));
    let action;
    if (!offer.selfService) {
      action = h("p", { class: "muted small" }, "Para escolas e redes de ensino: fale com a gente.");
    } else if (!store.state.me) {
      action = h("a", { class: "btn btn-primary", href: "#/cadastro" }, "Criar conta");
    } else if (isGuest) {
      action = h("a", { class: "btn btn-primary", href: "#/salvar" }, "Salvar progresso para escolher");
    } else if (isCurrent) {
      action = h("p", { class: "plan-current" }, "✓ Seu plano atual");
    } else {
      const btn = h("button", { type: "button", class: `btn ${offer.slug === "gratis" ? "" : "btn-primary"}` }, offer.slug === "gratis" ? "Voltar para o Grátis" : `Escolher ${offer.name}`);
      btn.addEventListener("click", async () => {
        btn.disabled = true;
        try {
          await api.choosePlan(offer.slug);
          navigate("/planos");
        } catch (err) {
          error.textContent = errorMessage(err);
          error.hidden = false;
          btn.disabled = false;
        }
      });
      action = btn;
    }
    return h(
      "article",
      { class: `card plan-card${offer.slug === "familia" || offer.slug === "professor" ? " plan-featured" : ""}${isCurrent ? " plan-is-current" : ""}` },
      h("p", { class: "muted small" }, offer.audience),
      h("h2", {}, offer.name),
      h("p", { class: "plan-price" }, offer.priceLabel, offer.slug !== "gratis" && offer.selfService ? h("span", { class: "plan-free-now" }, "grátis durante o lançamento") : null),
      h("ul", { class: "plan-features" }, (FEATURES[offer.slug] ?? []).map((f) => h("li", {}, f))),
      action,
    );
  };

  return h(
    "section",
    { class: "page" },
    h("h1", {}, "Planos"),
    h(
      "p",
      { class: "muted" },
      "Para as crianças, o jogo é sempre grátis e sem anúncios. Os planos são para os adultos que acompanham: famílias e professores.",
    ),
    h(
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
