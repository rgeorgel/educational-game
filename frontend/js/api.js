// Cliente da API. Mesma origem (nginx faz o proxy de /api), autenticação por cookie HttpOnly.
import { originForSignup } from "./origin.js";

export class ApiError extends Error {
  constructor(status, problem) {
    super(problem?.detail || problem?.title || `Erro ${status}`);
    this.status = status;
    this.problem = problem;
  }
}

const listeners = new Set();

/** Chamado quando a sessão expira (401 fora das rotas de login). */
export function onUnauthorized(fn) {
  listeners.add(fn);
  return () => listeners.delete(fn);
}

// Durante um deploy a API fica alguns segundos fora. O nginx responde 503 com o cabeçalho
// X-Api-Indisponivel (a requisição nem chegou à API), então dá para tentar de novo com segurança.
// Leituras (GET) também tentam de novo em erros de rede e 502/504. Espera no máximo ~25 s.
const RETRY_DELAYS = [500, 1000, 1500, 2000, 3000, 4000, 5000, 8000];
const sleep = (ms) => new Promise((resolve) => setTimeout(resolve, ms));

function shouldRetry(method, res) {
  if (!res) return method === "GET"; // erro de rede: só repete leituras
  if (res.headers.get("x-api-indisponivel") === "1") return true;
  return method === "GET" && [502, 503, 504].includes(res.status);
}

async function send(method, path, body) {
  for (let attempt = 0; ; attempt++) {
    let res = null;
    try {
      res = await fetch(`/api${path}`, {
        method,
        credentials: "same-origin",
        headers: body === undefined ? { Accept: "application/json" } : { "Content-Type": "application/json", Accept: "application/json" },
        body: body === undefined ? undefined : JSON.stringify(body),
      });
    } catch {
      res = null;
    }
    if (attempt >= RETRY_DELAYS.length || !shouldRetry(method, res)) return res;
    reconnecting(true);
    await sleep(RETRY_DELAYS[attempt]);
  }
}

/** Aviso discreto "Reconectando…" enquanto o servidor reinicia. */
let banner = null;
function reconnecting(on) {
  if (typeof document === "undefined") return;
  if (on && !banner) {
    banner = document.createElement("div");
    banner.className = "reconnecting";
    banner.setAttribute("role", "status");
    banner.textContent = "Reconectando ao servidor…";
    document.body.append(banner);
  } else if (!on && banner) {
    banner.remove();
    banner = null;
  }
}

async function request(method, path, body) {
  const res = await send(method, path, body);
  reconnecting(false);
  if (!res) {
    throw new ApiError(0, { title: "Sem conexão", detail: "Não foi possível falar com o servidor. Verifique sua internet." });
  }

  if (res.status === 204) return null;
  const isJson = res.headers.get("content-type")?.includes("json");
  const data = isJson ? await res.json() : null;

  if (!res.ok) {
    // 401 em /me é o esperado quando ninguém está logado (a inicialização trata); nas demais, a sessão expirou.
    if (res.status === 401 && !path.startsWith("/auth/") && path !== "/me") listeners.forEach((fn) => fn());
    if (res.status === 429 && !data) throw new ApiError(429, { title: "Muitas tentativas", detail: "Espere um minuto e tente de novo." });
    throw new ApiError(res.status, data);
  }
  return data;
}

const get = (p) => request("GET", p);
const post = (p, body = {}) => request("POST", p, body);
const del = (p) => request("DELETE", p);

export const api = {
  me: () => get("/me"),
  register: (email, password, acceptTerms) => post("/auth/register", { email, password, acceptTerms, origin: originForSignup() }),
  login: (email, password) => post("/auth/login", { email, password }),
  startAsGuest: (nickname, avatar) => post("/auth/guest", { nickname, avatar, origin: originForSignup() }),
  saveProgress: (email, password, acceptTerms) => post("/auth/upgrade", { email, password, acceptTerms }),
  logout: () => post("/auth/logout"),
  forgotPassword: (email) => post("/auth/forgot-password", { email }),
  resetPassword: (userId, token, password) => post("/auth/reset-password", { userId, token, password }),

  avatars: () => get("/profiles/avatars"),
  createProfile: (profile) => post("/profiles", profile),
  deleteProfile: (id) => del(`/profiles/${id}`),

  subjects: () => get("/subjects"),
  curriculum: () => get("/curriculum"),
  map: (profileId, subject) => get(`/profiles/${profileId}/subjects/${encodeURIComponent(subject)}/map`),
  progress: (profileId) => get(`/profiles/${profileId}/progress`),

  daily: (profileId) => get(`/profiles/${profileId}/daily`),
  startDaily: (profileId, subject) => post(`/profiles/${profileId}/subjects/${encodeURIComponent(subject)}/daily`),
  startReview: (profileId, subject) => post(`/profiles/${profileId}/subjects/${encodeURIComponent(subject)}/review`),

  avatarOptions: (profileId) => get(`/profiles/${profileId}/avatar`),
  updateAvatar: (profileId, avatar) => request("PUT", `/profiles/${profileId}/avatar`, avatar),

  guardianReport: () => get("/guardian/report"),
  profileClassrooms: (profileId) => get(`/profiles/${profileId}/classrooms`),
  joinClassroom: (profileId, code) => post(`/profiles/${profileId}/classrooms`, { code }),
  leaveClassroom: (profileId, classroomId) => del(`/profiles/${profileId}/classrooms/${classroomId}`),
  classrooms: () => get("/classrooms"),
  createClassroom: (name) => post("/classrooms", { name }),
  classroomReport: (id) => get(`/classrooms/${id}/report`),
  deleteClassroom: (id) => del(`/classrooms/${id}`),
  removeStudent: (id, profileId) => del(`/classrooms/${id}/members/${profileId}`),
  createAssignment: (id, body) => post(`/classrooms/${id}/assignments`, body),
  deleteAssignment: (id, assignmentId) => del(`/classrooms/${id}/assignments/${assignmentId}`),
  stageCatalog: () => get("/catalog/stages"),
  profileAssignments: (profileId) => get(`/profiles/${profileId}/assignments`),
  myPlan: () => get("/me/plan"),
  plans: () => get("/plans"),
  billingStatus: () => get("/billing/status"),
  checkout: (plan) => post("/billing/checkout", { plan }),
  billingPortal: () => post("/billing/portal"),
  choosePlan: (plan) => request("PUT", "/me/plan", { plan }),
  weeklyReport: (profileId) => get(`/profiles/${profileId}/weekly-report`),
  goals: (profileId) => get(`/profiles/${profileId}/goals`),
  createGoal: (profileId, skillSlug, subjectSlug) => post(`/profiles/${profileId}/goals`, { skillSlug, subjectSlug }),
  deleteGoal: (profileId, goalId) => del(`/profiles/${profileId}/goals/${goalId}`),
  startGoal: (profileId, goalId) => post(`/profiles/${profileId}/goals/${goalId}/start`),
  worksheet: (params) => get(`/worksheets?${new URLSearchParams(params)}`),
  deleteAccount: () => del("/me"),

  history: (profileId, days = 14) => get(`/profiles/${profileId}/history?days=${days}`),
  achievements: (profileId) => get(`/profiles/${profileId}/achievements`),

  startSession: (profileId, stageId) => post(`/profiles/${profileId}/sessions`, { stageId }),
  startGuardian: (profileId, subject) => post(`/profiles/${profileId}/subjects/${encodeURIComponent(subject)}/guardian`),
  startPlacement: (profileId, subject) => post(`/profiles/${profileId}/subjects/${encodeURIComponent(subject)}/placement`),
  skipPlacement: (profileId, subject) => post(`/profiles/${profileId}/subjects/${encodeURIComponent(subject)}/placement/skip`),
  next: (sessionId) => get(`/sessions/${sessionId}/next`),
  hint: (sessionId, problemId) => post(`/sessions/${sessionId}/problems/${problemId}/hint`),
  answer: (sessionId, problemId, answer) => post(`/sessions/${sessionId}/problems/${problemId}/answer`, { answer }),
  summary: (sessionId) => get(`/sessions/${sessionId}/summary`),
};
