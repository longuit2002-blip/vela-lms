"use client";

import { useEffect } from "react";
import { useRouter } from "next/navigation";
import { useQuery } from "@tanstack/react-query";
import { ApiError, AuthRequiredError, getMyOrganization, logout } from "@/lib/api";
import { useRequireAuth } from "@/lib/use-auth";

export default function MyOrganizationPage() {
  const router = useRouter();
  const status = useRequireAuth();

  const { data: organization, isPending, isError, error } = useQuery({
    queryKey: ["my-organization"],
    queryFn: getMyOrganization,
    enabled: status === "authenticated",
    retry: false,
  });

  // Route based on auth/gate failures surfaced by the query.
  useEffect(() => {
    if (!isError) return;
    if (error instanceof AuthRequiredError) {
      router.replace("/login");
    } else if (error instanceof ApiError && error.status === 403) {
      // Forced first-login password change gate.
      router.replace("/change-password");
    }
  }, [isError, error, router]);

  async function handleSignOut() {
    await logout();
    router.replace("/login");
  }

  return (
    <main className="mx-auto flex max-w-3xl flex-col gap-8 px-6 py-10">
      <header className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold text-foreground">My organization</h1>
          <p className="text-sm text-muted">Your tenant.</p>
        </div>
        <button
          type="button"
          onClick={handleSignOut}
          className="rounded-md border border-border px-3 py-2 text-sm font-medium text-foreground transition-colors hover:bg-surface"
        >
          Sign out
        </button>
      </header>

      <section className="rounded-xl border border-border bg-surface p-6">
        {status === "checking" || isPending ? (
          <p className="text-center text-sm text-muted">Loading…</p>
        ) : isError ? (
          <p role="alert" className="text-center text-sm text-red-700">
            Could not load your organization.
          </p>
        ) : (
          <dl className="grid grid-cols-[8rem_1fr] gap-y-3 text-sm">
            <dt className="font-medium text-muted">Name</dt>
            <dd className="text-foreground">{organization.name}</dd>
            <dt className="font-medium text-muted">Slug</dt>
            <dd className="text-foreground">{organization.slug}</dd>
            <dt className="font-medium text-muted">Status</dt>
            <dd className="text-foreground">{organization.status}</dd>
          </dl>
        )}
      </section>
    </main>
  );
}
