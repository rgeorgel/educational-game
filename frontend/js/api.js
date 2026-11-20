// Cliente da API. Mesma origem (nginx faz o proxy de /api), autenticação por cookie HttpOnly.

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

async function request(method, path, body) {
  let res;
  try {
    res = await fetch(`/api${path}`, {
      method,
      credentials: "same-origin",
      headers: body === undefined ? { Accept: "application/json" } : { "Content-Type": "application/json", Accept: "application/json" },
      body: body === undefined ? undefined : JSON.stringify(body),
    });
  } catch {
    throw new ApiError(0, { title: "Sem conexão", detail: "Não foi possível falar com o servidor. Verifique sua internet." });
  }

  if (res.status === 204) return null;
  const isJson = res.headers.get("content-type")?.includes("json");
  const data = isJson ? await res.json() : null;

  if (!res.ok) {
    if (res.status === 401 && !path.startsWith("/auth/")) listeners.forEach((fn) => fn());
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
  register: (email, password, acceptTerms) => post("/auth/register", { email, password, acceptTerms }),
  login: (email, password) => post("/auth/login", { email, password }),
  logout: () => post("/auth/logout"),

  avatars: () => get("/profiles/avatars"),
  createProfile: (profile) => post("/profiles", profile),
  deleteProfile: (id) => del(`/profiles/${id}`),

  subjects: () => get("/subjects"),
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
