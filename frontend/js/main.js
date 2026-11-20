// Fase 0: verifica se API e banco estão no ar e lista os tópicos.

/** Cria um elemento com texto (sem innerHTML, para evitar injeção). */
function el(tag, { className, text } = {}, children = []) {
  const node = document.createElement(tag);
  if (className) node.className = className;
  if (text !== undefined) node.textContent = text;
  node.append(...children);
  return node;
}

function statusItem(label, ok) {
  return el("li", {}, [
    el("span", { className: `dot ${ok ? "ok" : "fail"}` }),
    el("span", { text: `${label}: ${ok ? "ok" : "indisponível"}` }),
  ]);
}

async function loadHealth() {
  const list = document.getElementById("health");
  try {
    const res = await fetch("/api/health");
    const data = await res.json();
    list.replaceChildren(
      statusItem("API", true),
      ...data.checks.map((c) => statusItem(c.name === "database" ? "Banco de dados" : c.name, c.status === "Healthy")),
    );
  } catch {
    list.replaceChildren(statusItem("API", false));
  }
}

async function loadSubjects() {
  const box = document.getElementById("subjects");
  try {
    const res = await fetch("/api/subjects");
    if (!res.ok) throw new Error(res.statusText);
    const subjects = await res.json();
    box.replaceChildren(
      ...subjects.map((s) =>
        el("div", {}, [
          el("h3", { text: `${s.icon} ${s.name}` }),
          el(
            "ul",
            {},
            s.levels.map((l) => el("li", { text: `${l.icon} ${l.name} (rating ${l.minRating}+)` })),
          ),
        ]),
      ),
    );
  } catch {
    box.textContent = "Não foi possível carregar os tópicos.";
  }
}

loadHealth();
loadSubjects();
