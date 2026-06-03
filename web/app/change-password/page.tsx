"use client";

import { useState, type FormEvent } from "react";
import { useRouter } from "next/navigation";
import { ApiError, AuthRequiredError, changePassword } from "@/lib/api";
import { useRequireAuth } from "@/lib/use-auth";

export default function ChangePasswordPage() {
  const router = useRouter();
  const status = useRequireAuth(); // accessible to any authenticated user; redirects if not signed in
  const [currentPassword, setCurrentPassword] = useState("");
  const [newPassword, setNewPassword] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);

  if (status === "checking") {
    return <main className="mx-auto max-w-sm px-6 py-10 text-sm text-muted">Loading…</main>;
  }

  async function handleSubmit(event: FormEvent) {
    event.preventDefault();
    setError(null);
    setSubmitting(true);
    try {
      await changePassword(currentPassword, newPassword);
      router.replace("/organizations");
    } catch (err) {
      if (err instanceof AuthRequiredError) {
        router.replace("/login");
        return;
      }
      // 401 → current password wrong; 422 → new-password validation message.
      setError(err instanceof ApiError ? err.message : "Something went wrong. Please try again.");
      setSubmitting(false);
    }
  }

  return (
    <main className="mx-auto flex min-h-screen max-w-sm flex-col justify-center gap-6 px-6 py-10">
      <header>
        <h1 className="text-2xl font-bold text-foreground">Change your password</h1>
        <p className="text-sm text-muted">Choose a new password to continue.</p>
      </header>

      <form onSubmit={handleSubmit} className="flex flex-col gap-4 rounded-xl border border-border bg-surface p-6">
        <label className="flex flex-col gap-1 text-sm">
          <span className="font-medium text-foreground">Current password</span>
          <input
            type="password"
            value={currentPassword}
            onChange={(e) => setCurrentPassword(e.target.value)}
            required
            autoComplete="current-password"
            className="rounded-md border border-border px-3 py-2 outline-none focus-visible:ring-2 focus-visible:ring-primary"
          />
        </label>
        <label className="flex flex-col gap-1 text-sm">
          <span className="font-medium text-foreground">New password</span>
          <input
            type="password"
            value={newPassword}
            onChange={(e) => setNewPassword(e.target.value)}
            required
            autoComplete="new-password"
            className="rounded-md border border-border px-3 py-2 outline-none focus-visible:ring-2 focus-visible:ring-primary"
          />
        </label>

        {error && (
          <p role="alert" className="rounded-md border border-red-200 bg-red-50 px-3 py-2 text-sm text-red-700">
            {error}
          </p>
        )}

        <button
          type="submit"
          disabled={submitting}
          className="rounded-md bg-primary px-4 py-2 font-medium text-white transition-colors hover:bg-primary-hover disabled:cursor-not-allowed disabled:opacity-60"
        >
          {submitting ? "Saving…" : "Change password"}
        </button>
      </form>
    </main>
  );
}
