"use client";

import { useState, type FormEvent } from "react";
import { ApiError } from "@/lib/api";
import { useCreateOrganization, useOrganizations } from "@/lib/use-organizations";

export default function OrganizationsPage() {
  const { data: organizations, isPending, isError, error } = useOrganizations();
  const createOrganization = useCreateOrganization();

  const [name, setName] = useState("");
  const [slug, setSlug] = useState("");
  const [formError, setFormError] = useState<string | null>(null);

  async function handleSubmit(event: FormEvent) {
    event.preventDefault();
    setFormError(null);
    try {
      await createOrganization.mutateAsync({ name, slug });
      setName("");
      setSlug("");
    } catch (err) {
      setFormError(err instanceof ApiError ? err.message : "Failed to create organization.");
    }
  }

  return (
    <main className="mx-auto flex max-w-3xl flex-col gap-8 px-6 py-10">
      <header>
        <h1 className="text-2xl font-bold text-foreground">Organizations</h1>
        <p className="text-sm text-muted">Walking skeleton — create and list organizations.</p>
      </header>

      <form
        onSubmit={handleSubmit}
        className="flex flex-col gap-3 rounded-xl border border-border bg-surface p-5 sm:flex-row sm:items-end"
      >
        <label className="flex flex-1 flex-col gap-1 text-sm">
          <span className="font-medium text-foreground">Name</span>
          <input
            value={name}
            onChange={(e) => setName(e.target.value)}
            required
            className="rounded-md border border-border px-3 py-2 outline-none focus-visible:ring-2 focus-visible:ring-primary"
            placeholder="Acme Corp"
          />
        </label>
        <label className="flex flex-1 flex-col gap-1 text-sm">
          <span className="font-medium text-foreground">Slug</span>
          <input
            value={slug}
            onChange={(e) => setSlug(e.target.value)}
            required
            className="rounded-md border border-border px-3 py-2 outline-none focus-visible:ring-2 focus-visible:ring-primary"
            placeholder="acme-corp"
          />
        </label>
        <button
          type="submit"
          disabled={createOrganization.isPending}
          className="rounded-md bg-primary px-4 py-2 font-medium text-white transition-colors hover:bg-primary-hover disabled:cursor-not-allowed disabled:opacity-60"
        >
          {createOrganization.isPending ? "Creating…" : "Create"}
        </button>
      </form>

      {formError && (
        <p role="alert" className="rounded-md border border-red-200 bg-red-50 px-4 py-2 text-sm text-red-700">
          {formError}
        </p>
      )}

      <section className="rounded-xl border border-border bg-surface">
        {isPending ? (
          <p className="px-5 py-8 text-center text-sm text-muted">Loading…</p>
        ) : isError ? (
          <p role="alert" className="px-5 py-8 text-center text-sm text-red-700">
            Could not load organizations. {error instanceof Error ? error.message : ""} Is the API running?
          </p>
        ) : organizations.length === 0 ? (
          <p className="px-5 py-8 text-center text-sm text-muted">No organizations yet.</p>
        ) : (
          <table className="w-full text-left text-sm">
            <thead className="border-b border-border text-xs uppercase tracking-wide text-muted">
              <tr>
                <th className="px-5 py-3 font-semibold">Name</th>
                <th className="px-5 py-3 font-semibold">Slug</th>
                <th className="px-5 py-3 font-semibold">Status</th>
              </tr>
            </thead>
            <tbody>
              {organizations.map((org) => (
                <tr key={org.id} className="border-b border-border last:border-0">
                  <td className="px-5 py-3 text-foreground">{org.name}</td>
                  <td className="px-5 py-3 text-muted">{org.slug}</td>
                  <td className="px-5 py-3 text-muted">{org.status}</td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </section>
    </main>
  );
}
