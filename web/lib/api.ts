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

// --- Learning loop ---

export interface LessonDetail {
  id: string;
  title: string;
  order: number;
  type: string;
  videoUrl: string;
  durationSeconds: number;
}

export interface ModuleDetail {
  id: string;
  title: string;
  order: number;
  lessons: LessonDetail[];
}

export interface CourseDetail {
  id: string;
  title: string;
  slug: string;
  status: string;
  modules: ModuleDetail[];
}

export interface PublicationSummary {
  id: string;
  kind: string;
  contentId: string;
  title: string;
  status: string;
  publishedAt: string | null;
}

export interface AssignmentResult {
  publicationId: string;
  enrolled: number;
  skipped: number;
}

export interface EnrollmentSummary {
  enrollmentId: string;
  publicationId: string;
  courseTitle: string;
  courseSlug: string;
  progressPercent: number;
  status: string;
}

export interface EnrolledLesson {
  id: string;
  title: string;
  order: number;
  videoUrl: string;
  durationSeconds: number;
  completed: boolean;
}

export interface EnrolledModule {
  id: string;
  title: string;
  order: number;
  lessons: EnrolledLesson[];
}

export interface EnrolledCourseDetail {
  enrollmentId: string;
  courseTitle: string;
  status: string;
  progressPercent: number;
  modules: EnrolledModule[];
}

export interface CompleteLessonResult {
  enrollmentId: string;
  lessonId: string;
  progressPercent: number;
  status: string;
}

// Authoring (instructor)
export function createCourse(title: string, slug: string): Promise<CourseDetail> {
  return authFetch<CourseDetail>("/api/v1/courses", { method: "POST", body: { title, slug } });
}

export function addModule(courseId: string, title: string): Promise<ModuleDetail> {
  return authFetch<ModuleDetail>(`/api/v1/courses/${courseId}/modules`, { method: "POST", body: { title } });
}

export function addLesson(
  courseId: string,
  moduleId: string,
  lesson: { title: string; videoUrl: string; durationSeconds: number },
): Promise<LessonDetail> {
  return authFetch<LessonDetail>(`/api/v1/courses/${courseId}/modules/${moduleId}/lessons`, { method: "POST", body: lesson });
}

// Publishing + assignment (L&D)
export function createPublication(courseId: string, title: string): Promise<PublicationSummary> {
  return authFetch<PublicationSummary>("/api/v1/publications", { method: "POST", body: { courseId, title } });
}

export function publishPublication(publicationId: string): Promise<PublicationSummary> {
  return authFetch<PublicationSummary>(`/api/v1/publications/${publicationId}/publish`, { method: "POST", body: {} });
}

export function assignPublication(publicationId: string, userIds: string[]): Promise<AssignmentResult> {
  return authFetch<AssignmentResult>(`/api/v1/publications/${publicationId}/assign`, { method: "POST", body: { userIds } });
}

// Learner
export function getMyEnrollments(): Promise<EnrollmentSummary[]> {
  return authFetch<EnrollmentSummary[]>("/api/v1/me/enrollments");
}

export function getEnrolledCourse(enrollmentId: string): Promise<EnrolledCourseDetail> {
  return authFetch<EnrolledCourseDetail>(`/api/v1/enrollments/${enrollmentId}`);
}

export function completeLesson(enrollmentId: string, lessonId: string): Promise<CompleteLessonResult> {
  return authFetch<CompleteLessonResult>(`/api/v1/enrollments/${enrollmentId}/lessons/${lessonId}/complete`, { method: "POST", body: {} });
}
