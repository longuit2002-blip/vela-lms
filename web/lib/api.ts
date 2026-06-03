import { clearAccessToken, getAccessToken, setAccessToken } from "./auth-token";

// Same-origin: requests go to the Next.js origin and are proxied to the API by next.config rewrites.
const API_BASE = "";

export interface Organization {
  id: string;
  name: string;
  slug: string;
  status: string;
  createdAt: string;
}

interface AuthResponse {
  accessToken: string;
  accessTokenExpiresAt: string;
  mustChangePassword: boolean;
}

export interface Session {
  mustChangePassword: boolean;
}

/** Carries the HTTP status and the parsed problem+json (when present) from a failed request. */
export class ApiError extends Error {
  constructor(
    readonly status: number,
    message: string,
    readonly problem?: unknown,
  ) {
    super(message);
    this.name = "ApiError";
  }
}

/** Thrown when the session can't be established/refreshed — callers redirect to login. */
export class AuthRequiredError extends Error {
  constructor() {
    super("Authentication required.");
    this.name = "AuthRequiredError";
  }
}

function stringField(body: unknown, field: string): string | null {
  return body && typeof body === "object" && field in body && typeof (body as Record<string, unknown>)[field] === "string"
    ? ((body as Record<string, string>)[field])
    : null;
}

async function readProblem(response: Response): Promise<{ title: string; body: unknown }> {
  const text = await response.text();
  let body: unknown = null;
  if (text) {
    try {
      body = JSON.parse(text);
    } catch {
      // non-JSON body
    }
  }
  // Prefer the human-facing detail (e.g. "Incorrect email or password."), fall back to title.
  const title = stringField(body, "detail") ?? stringField(body, "title") ?? `Request failed (${response.status}).`;
  return { title, body };
}

// Shared in-flight refresh so concurrent 401s trigger a single /refresh (no stampede).
let refreshInFlight: Promise<boolean> | null = null;

function refreshSession(): Promise<boolean> {
  refreshInFlight ??= (async () => {
    try {
      const response = await fetch(`${API_BASE}/api/v1/auth/refresh`, {
        method: "POST",
        credentials: "include",
      });
      if (!response.ok) return false;
      const data = (await response.json()) as AuthResponse;
      setAccessToken(data.accessToken);
      return true;
    } catch {
      return false;
    } finally {
      refreshInFlight = null;
    }
  })();
  return refreshInFlight;
}

interface RequestOptions {
  method?: string;
  body?: unknown;
  /** Internal: prevents infinite refresh recursion. */
  retrying?: boolean;
}

/** Authenticated fetch: attaches the bearer token and transparently refreshes once on 401. */
async function authFetch<T>(path: string, options: RequestOptions = {}): Promise<T> {
  const token = getAccessToken();
  const response = await fetch(`${API_BASE}${path}`, {
    method: options.method ?? "GET",
    credentials: "include",
    headers: {
      ...(options.body !== undefined ? { "Content-Type": "application/json" } : {}),
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
    },
    body: options.body !== undefined ? JSON.stringify(options.body) : undefined,
    cache: "no-store",
  });

  if (response.status === 401 && !options.retrying) {
    const refreshed = await refreshSession();
    if (refreshed) return authFetch<T>(path, { ...options, retrying: true });
    clearAccessToken();
    throw new AuthRequiredError();
  }

  if (!response.ok) {
    const { title, body } = await readProblem(response);
    throw new ApiError(response.status, title, body);
  }

  const text = await response.text();
  return (text ? JSON.parse(text) : null) as T;
}

// --- Auth ---

export async function login(email: string, password: string): Promise<Session> {
  const response = await fetch(`${API_BASE}/api/v1/auth/login`, {
    method: "POST",
    credentials: "include",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ email, password }),
  });
  if (!response.ok) {
    const { title, body } = await readProblem(response);
    throw new ApiError(response.status, title, body);
  }
  const data = (await response.json()) as AuthResponse;
  setAccessToken(data.accessToken);
  return { mustChangePassword: data.mustChangePassword };
}

/** Attempts to restore a session from the refresh cookie (called on app load). */
export async function restoreSession(): Promise<boolean> {
  return refreshSession();
}

export async function logout(): Promise<void> {
  try {
    await fetch(`${API_BASE}/api/v1/auth/logout`, { method: "POST", credentials: "include" });
  } finally {
    clearAccessToken();
  }
}

export async function changePassword(currentPassword: string, newPassword: string): Promise<Session> {
  const data = await authFetch<AuthResponse>("/api/v1/auth/change-password", {
    method: "POST",
    body: { currentPassword, newPassword },
  });
  setAccessToken(data.accessToken);
  return { mustChangePassword: data.mustChangePassword };
}

// --- Organization ---

export function getMyOrganization(): Promise<Organization> {
  return authFetch<Organization>("/api/v1/organizations/me");
}
