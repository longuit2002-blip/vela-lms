"use client";

import { useState, type FormEvent } from "react";
import { useRouter } from "next/navigation";
import { ApiError, login } from "@/lib/api";

export default function LoginPage() {
  const router = useRouter();
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);

  async function handleSubmit(event: FormEvent) {
    event.preventDefault();
    setError(null);
    setSubmitting(true);
    try {
      const session = await login(email, password);
      router.replace(session.mustChangePassword ? "/change-password" : "/organizations");
    } catch (err) {
      // Enumeration-safe: the API returns the same message for wrong password vs unknown email.
      setError(err instanceof ApiError ? err.message : "Something went wrong. Please try again.");
      setSubmitting(false);
    }
  }

  return (
    <main className="mx-auto flex min-h-screen max-w-sm flex-col justify-center gap-6 px-6 py-10">
      <header>
        <h1 className="text-2xl font-bold text-foreground">Sign in</h1>
        <p className="text-sm text-muted">Use your organization account.</p>
      </header>

      <form onSubmit={handleSubmit} className="flex flex-col gap-4 rounded-xl border border-border bg-surface p-6">
        <label className="flex flex-col gap-1 text-sm">
          <span className="font-medium text-foreground">Email</span>
          <input
            type="email"
            value={email}
            onChange={(e) => setEmail(e.target.value)}
            required
            autoComplete="username"
            className="rounded-md border border-border px-3 py-2 outline-none focus-visible:ring-2 focus-visible:ring-primary"
            placeholder="you@company.com"
          />
        </label>
        <label className="flex flex-col gap-1 text-sm">
          <span className="font-medium text-foreground">Password</span>
          <input
            type="password"
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            required
            autoComplete="current-password"
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
          {submitting ? "Signing in…" : "Sign in"}
        </button>
      </form>
    </main>
  );
}
