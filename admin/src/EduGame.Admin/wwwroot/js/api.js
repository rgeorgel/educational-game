export class ApiError extends Error {
  constructor(status, message) {
    super(message);
    this.status = status;
  }
}

async function request(method, url, body) {
  const res = await fetch(url, {
    method,
    credentials: "same-origin",
    headers: body ? { "content-type": "application/json" } : {},
    body: body ? JSON.stringify(body) : undefined,
  });
  if (res.status === 204) return null;
  const data = await res.json().catch(() => null);
  if (!res.ok) throw new ApiError(res.status, data?.detail || data?.title || `Erro ${res.status}`);
  return data;
}

const qs = (params) => new URLSearchParams(Object.entries(params).filter(([, v]) => v !== undefined && v !== null && v !== "")).toString();

export const api = {
  session: () => request("GET", "/api/session"),
  login: (username, password) => request("POST", "/api/login", { username, password }),
  logout: () => request("POST", "/api/logout"),
  overview: () => request("GET", "/api/overview"),
  series: (days, weeks) => request("GET", `/api/series?${qs({ days, weeks })}`),
  users: (params) => request("GET", `/api/users?${qs(params)}`),
  user: (id) => request("GET", `/api/users/${encodeURIComponent(id)}`),
  ranking: (params) => request("GET", `/api/ranking?${qs(params)}`),
  insights: (days) => request("GET", `/api/insights?${qs({ days })}`),
};
