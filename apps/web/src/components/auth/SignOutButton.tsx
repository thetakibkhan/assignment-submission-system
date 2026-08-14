"use client";

import { useRouter } from "next/navigation";
import { useState } from "react";
import { browserApiBaseUrl } from "@/lib/api-routing";

export function SignOutButton({ className }: { className?: string }) {
  const router = useRouter();
  const [isSigningOut, setIsSigningOut] = useState(false);

  async function signOut() {
    setIsSigningOut(true);

    try {
      const response = await fetch(browserApiBaseUrl + "/api/auth/logout", {
        credentials: "include",
        method: "POST",
      });

      if (!response.ok) {
        throw new Error("Sign out failed.");
      }

      router.replace("/");
      router.refresh();
    } catch {
      setIsSigningOut(false);
    }
  }

  return (
    <button className={className} disabled={isSigningOut} onClick={() => void signOut()} type="button">
      {isSigningOut ? "Signing out…" : "Sign out"}
    </button>
  );
}
