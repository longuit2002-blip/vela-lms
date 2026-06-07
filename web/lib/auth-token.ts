// In-memory access token store. Deliberately NOT localStorage/cookie-readable-by-JS: a short-lived
// access token held in memory is the OWASP-aligned posture (XSS can't exfiltrate it from storage).
// The refresh token lives in an httpOnly cookie the browser manages; we never see it here.

let accessToken: string | null = null;

export function getAccessToken(): string | null {
  return accessToken;
}

export function setAccessToken(token: string | null): void {
  accessToken = token;
}

export function clearAccessToken(): void {
  accessToken = null;
}
