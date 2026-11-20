// Web Components reutilizáveis (light DOM, para herdar o CSS global).

/** <progress-bar value="40" label="40% para Médio" level="iniciante"></progress-bar> */
class ProgressBar extends HTMLElement {
  static observedAttributes = ["value", "label", "level", "mark"];

  connectedCallback() {
    this.render();
  }

  attributeChangedCallback() {
    if (this.isConnected) this.render();
  }

  render() {
    const value = Math.max(0, Math.min(100, Number(this.getAttribute("value")) || 0));
    const label = this.getAttribute("label") ?? `${value}%`;
    this.setAttribute("role", "progressbar");
    this.setAttribute("aria-valuemin", "0");
    this.setAttribute("aria-valuemax", "100");
    this.setAttribute("aria-valuenow", String(value));
    this.setAttribute("aria-label", label);

    const fill = document.createElement("span");
    fill.className = "progress-fill";
    fill.style.setProperty("width", `${value}%`);
    const track = document.createElement("span");
    track.className = "progress-track";
    track.append(fill);

    // Marca opcional (ex.: onde o Desafio do Guardião abre).
    const mark = this.getAttribute("mark");
    if (mark !== null) {
      const tick = document.createElement("span");
      tick.className = `progress-mark${value >= Number(mark) ? " reached" : ""}`;
      tick.style.setProperty("left", `${Math.max(0, Math.min(100, Number(mark)))}%`);
      tick.setAttribute("aria-hidden", "true");
      tick.title = "O Desafio do Guardião abre aqui";
      track.append(tick);
    }
    this.replaceChildren(track);
  }
}

/** <star-rating stars="2"></star-rating> */
class StarRating extends HTMLElement {
  static observedAttributes = ["stars", "max"];

  connectedCallback() {
    this.render();
  }

  attributeChangedCallback() {
    if (this.isConnected) this.render();
  }

  render() {
    const stars = Number(this.getAttribute("stars")) || 0;
    const max = Number(this.getAttribute("max")) || 3;
    this.setAttribute("role", "img");
    this.setAttribute("aria-label", `${stars} de ${max} estrelas`);
    this.replaceChildren(
      ...Array.from({ length: max }, (_, i) => {
        const s = document.createElement("span");
        s.className = i < stars ? "star on" : "star";
        s.textContent = "★";
        s.setAttribute("aria-hidden", "true");
        return s;
      }),
    );
  }
}

/**
 * <numeric-keypad></numeric-keypad>
 * Teclado numérico na tela. Dispara "keypad" com detail = "0".."9" | "back" | "ok".
 */
class NumericKeypad extends HTMLElement {
  connectedCallback() {
    if (this.childElementCount) return;
    this.setAttribute("role", "group");
    this.setAttribute("aria-label", "Teclado numérico");
    const keys = ["1", "2", "3", "4", "5", "6", "7", "8", "9", "back", "0", "ok"];
    // Teclas extras só quando o problema aceita (para não entregar dicas).
    if (this.hasAttribute("negative")) keys.push("minus");
    if (this.hasAttribute("decimal")) keys.push("comma");
    const labels = { back: "⌫", ok: "OK", minus: "−", comma: "," };
    const aria = { back: "Apagar", ok: "Confirmar resposta", minus: "Sinal de menos", comma: "Vírgula" };
    for (const key of keys) {
      const btn = document.createElement("button");
      btn.type = "button";
      btn.className = `key key-${key}`;
      btn.dataset.key = key;
      btn.textContent = labels[key] ?? key;
      if (aria[key]) btn.setAttribute("aria-label", aria[key]);
      btn.addEventListener("click", () => this.dispatchEvent(new CustomEvent("keypad", { detail: key, bubbles: true })));
      this.append(btn);
    }
  }

  set disabled(value) {
    this.querySelectorAll("button").forEach((b) => (b.disabled = value));
  }
}

customElements.define("progress-bar", ProgressBar);
customElements.define("star-rating", StarRating);
customElements.define("numeric-keypad", NumericKeypad);
