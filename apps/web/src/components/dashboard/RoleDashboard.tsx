"use client";

import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";

interface RoleDashboardProps {
  endpoint: string;
  role: string;
}

export function RoleDashboard({ endpoint, role }: RoleDashboardProps) {
  const router = useRouter();
  const [message, setMessage] = useState("Verifying your access…");
  const [isVerified, setIsVerified] = useState(false);

  useEffect(() => {
    const apiBaseUrl = (process.env.NEXT_PUBLIC_API_BASE_URL ?? "http://localhost:5112").replace(/\/$/, "");
    let isCurrent = true;

    async function verifyAccess() {
      try {
        const response = await fetch(apiBaseUrl + endpoint, {
          credentials: "include",
        });

        if (!response.ok) {
          router.replace("/");
          return;
        }

        if (isCurrent) {
          setIsVerified(true);
          setMessage("Your access has been verified.");
        }
      } catch {
        if (isCurrent) {
          setMessage("The service is unavailable. Please try again.");
        }
      }
    }

    void verifyAccess();

    return () => {
      isCurrent = false;
    };
  }, [endpoint, router]);

  return (
    <main className="role-dashboard-shell">
      <section aria-live="polite" className="role-dashboard-card">
        <p className="login-card__eyebrow">{role} workspace</p>
        <h1>{role} dashboard</h1>
        <p>{message}</p>
        {isVerified && <p className="role-dashboard-status">Ready for Epic 2.</p>}
      </section>
    </main>
  );
}
