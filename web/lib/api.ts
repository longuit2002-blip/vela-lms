export interface Organization {
  id: string;
  name: string;
  slug: string;
  status: string;
  createdAt: string;
}

export interface CreateOrganizationInput {
  name: string;
  slug: string;
}

const API_BASE = process.env.NEXT_PUBLIC_API_BASE_URL ?? "http://localhost:5080";

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

async function parseJson(response: Response): Promise<unknown> {
  const text = await response.text();
  const data = text ? JSON.parse(text) : null;

  if (!response.ok) {
    const title =
      (data && typeof data === "object" && "title" in data && typeof data.title === "string"
        ? data.title
        : null) ?? `Request failed (${response.status}).`;
    throw new ApiError(response.status, title, data);
  }

  return data;
}

export async function listOrganizations(): Promise<Organization[]> {
  const response = await fetch(`${API_BASE}/api/v1/organizations`, { cache: "no-store" });
  return (await parseJson(response)) as Organization[];
}

export async function createOrganization(input: CreateOrganizationInput): Promise<Organization> {
  const response = await fetch(`${API_BASE}/api/v1/organizations`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(input),
  });
  return (await parseJson(response)) as Organization;
}
