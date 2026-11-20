// Estado global mínimo com padrão observer.

const PROFILE_KEY = "edugame.profileId";

function readStorage(key) {
  try {
    return localStorage.getItem(key);
  } catch {
    return null;
  }
}

function writeStorage(key, value) {
  try {
    if (value === null) localStorage.removeItem(key);
    else localStorage.setItem(key, value);
  } catch {
    /* modo privado ou armazenamento bloqueado: segue sem lembrar */
  }
}

const state = {
  /** { email, profiles: [...] } ou null se não logado */
  me: null,
  /** perfil escolhido ("quem vai jogar?") */
  profileId: readStorage(PROFILE_KEY),
};

const subscribers = new Set();

export const store = {
  get state() {
    return state;
  },

  subscribe(fn) {
    subscribers.add(fn);
    return () => subscribers.delete(fn);
  },

  setMe(me) {
    state.me = me;
    if (me && state.profileId && !me.profiles.some((p) => p.id === state.profileId)) this.setProfile(null);
    notify();
  },

  setProfile(id) {
    state.profileId = id;
    writeStorage(PROFILE_KEY, id);
    notify();
  },

  get profile() {
    return state.me?.profiles.find((p) => p.id === state.profileId) ?? null;
  },

  addProfile(profile) {
    state.me.profiles.push(profile);
    notify();
  },

  removeProfile(id) {
    state.me.profiles = state.me.profiles.filter((p) => p.id !== id);
    if (state.profileId === id) this.setProfile(null);
    notify();
  },

  signOut() {
    state.me = null;
    this.setProfile(null);
  },
};

function notify() {
  subscribers.forEach((fn) => fn(state));
}
