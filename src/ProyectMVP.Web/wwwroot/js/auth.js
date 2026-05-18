const API_PREFIX = "/proxy/api/v1";
const TOKEN_KEY = "proyectmvp_token";
const USER_KEY = "proyectmvp_user";

function getToken() {
  return sessionStorage.getItem(TOKEN_KEY);
}

function getUser() {
  const raw = sessionStorage.getItem(USER_KEY);
  if (!raw) {
    return null;
  }

  try {
    return JSON.parse(raw);
  } catch {
    return null;
  }
}

function isLoggedIn() {
  return Boolean(getToken());
}

function setSession(loginResult) {
  sessionStorage.setItem(TOKEN_KEY, loginResult.accessToken);
  sessionStorage.setItem(
    USER_KEY,
    JSON.stringify({
      username: loginResult.username,
      countryId: loginResult.countryId,
      countryIsoCode: loginResult.countryIsoCode,
      identityProvider: loginResult.identityProvider
    })
  );
}

function clearSession() {
  sessionStorage.removeItem(TOKEN_KEY);
  sessionStorage.removeItem(USER_KEY);
}

async function apiFetch(path, options = {}) {
  const headers = new Headers(options.headers || {});
  const token = getToken();
  if (token) {
    headers.set("Authorization", `Bearer ${token}`);
  }

  if (options.body && !headers.has("Content-Type")) {
    headers.set("Content-Type", "application/json");
  }

  const response = await fetch(`${API_PREFIX}${path}`, { ...options, headers });

  if (response.status === 401) {
    clearSession();
    updateAuthUi();
    throw new Error("Sesión expirada. Inicie sesión de nuevo.");
  }

  return response;
}

async function login(username, password) {
  const response = await fetch(`${API_PREFIX}/auth/login`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ username, password })
  });

  if (!response.ok) {
    const err = await response.json().catch(() => ({}));
    throw new Error(err.message || "Credenciales inválidas.");
  }

  const data = await response.json();
  setSession(data);
  updateAuthUi();
  return data;
}

function logout() {
  clearSession();
  updateAuthUi();
  document.getElementById("stolen-reports-panel").hidden = true;
  document.getElementById("stolen-reports-list").innerHTML = "";
}

function updateAuthUi() {
  const loggedIn = isLoggedIn();
  const user = getUser();

  document.getElementById("login-panel").hidden = loggedIn;
  document.getElementById("user-panel").hidden = !loggedIn;
  document.getElementById("search-form").hidden = !loggedIn;

  if (loggedIn && user) {
    document.getElementById("user-label").textContent =
      `${user.username} · ${user.countryIsoCode} (${user.identityProvider})`;
  }
}

document.getElementById("login-form").addEventListener("submit", async (e) => {
  e.preventDefault();
  const status = document.getElementById("auth-status");
  status.textContent = "Iniciando sesión...";
  status.style.color = "rgba(255,255,255,0.95)";

  try {
    await login(
      document.getElementById("username-input").value,
      document.getElementById("password-input").value
    );
    status.textContent = "Sesión iniciada.";
    const plate = document.getElementById("plate-input").value || "ABC123";
    if (typeof window.onPoliceLoggedIn === "function") {
      window.onPoliceLoggedIn(plate);
    }
  } catch (err) {
    status.textContent = err.message;
    status.style.color = "#ffb4a2";
  }
});

document.getElementById("logout-btn").addEventListener("click", () => {
  logout();
  document.getElementById("status").textContent = "Sesión cerrada.";
  if (typeof window.clearMapResults === "function") {
    window.clearMapResults();
  }
});

updateAuthUi();

window.ProyectMvpAuth = {
  apiFetch,
  isLoggedIn,
  getUser,
  updateAuthUi
};
