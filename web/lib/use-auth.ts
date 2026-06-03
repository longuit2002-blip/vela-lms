"use client";

import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { getAccessToken } from "./auth-token";
import { restoreSession } from "./api";

type AuthStatus = "checking" | "authenticated";

/**
 * Ensures an authenticated session for a protected page. If the in-memory access token is missing
 * (e.g. after a full page reload), it attempts a silent refresh from the httpOnly cookie; if that
 * fails, it redirects to the login page. Returns "checking" until the session is confirmed.
 */
export function useRequireAuth(): AuthStatus {
  const router = useRouter();
  const [status, setStatus] = useState<AuthStatus>("checking");

  useEffect(() => {
    let active = true;

    (async () => {
      if (getAccessToken()) {
        if (active) setStatus("authenticated");
        return;
      }

      const restored = await restoreSession();
      if (!active) return;

      if (restored) setStatus("authenticated");
      else router.replace("/login");
    })();

    return () => {
      active = false;
    };
  }, [router]);

  return status;
}
