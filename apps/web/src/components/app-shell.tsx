"use client";

import { BookOpen, LayoutDashboard, LogOut, type LucideIcon, ShieldCheck, UserRoundPlus, Users } from "lucide-react";
import { useRouter } from "next/navigation";
import { createContext, type ReactNode, useContext, useState } from "react";

export type DashboardSection = "overview" | "accounts" | "academic" | "enrollment" | "responsibilities" | "submissions";

type AppShellContextValue = {
  activeSection: DashboardSection;
};

const AppShellContext = createContext<AppShellContextValue | null>(null);

const navigation: Array<{ icon: LucideIcon; label: string; section: DashboardSection }> = [
  { icon: LayoutDashboard, label: "Overview", section: "overview" },
  { icon: Users, label: "Accounts", section: "accounts" },
  { icon: BookOpen, label: "Academic structure", section: "academic" },
  { icon: UserRoundPlus, label: "Enrollments", section: "enrollment" },
  { icon: ShieldCheck, label: "Teacher scope", section: "responsibilities" },
  { icon: BookOpen, label: "Submissions", section: "submissions" },
];

export function AppShell({ children }: { children: ReactNode }) {
  const router = useRouter();
  const [activeSection, setActiveSection] = useState<DashboardSection>("overview");
  const [isSigningOut, setIsSigningOut] = useState(false);

  async function signOut() {
    setIsSigningOut(true);

    try {
      const apiBaseUrl = (process.env.NEXT_PUBLIC_API_BASE_URL ?? "http://localhost:5112").replace(/\/$/, "");
      const response = await fetch(apiBaseUrl + "/api/auth/logout", {
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
    <AppShellContext.Provider value={{ activeSection }}>
      <div className="app-shell">
        <aside className="app-shell__sidebar">
          <div className="app-shell__brand">
            <span>ASS</span>
            <strong>Academic control</strong>
          </div>
          <nav aria-label="Administration navigation" className="app-shell__nav">
            {navigation.map((item) => {
              const Icon = item.icon;

              return (
              <button
                aria-current={activeSection === item.section ? "page" : undefined}
                className={activeSection === item.section ? "is-active" : ""}
                key={item.section}
                onClick={() => setActiveSection(item.section)}
                type="button"
              >
                <Icon aria-hidden="true" size={16} strokeWidth={1.75} />
                {item.label}
              </button>
              );
            })}
          </nav>
          <div className="app-shell__bottom">
            <button className="app-shell__sign-out" disabled={isSigningOut} onClick={() => void signOut()} type="button">
              <LogOut aria-hidden="true" size={16} strokeWidth={1.75} />
              {isSigningOut ? "Signing out…" : "Sign out"}
            </button>
            <p className="app-shell__footer">Single-institution workspace</p>
          </div>
        </aside>
        <section className="app-shell__content">{children}</section>
      </div>
    </AppShellContext.Provider>
  );
}

export function useAppShell() {
  const context = useContext(AppShellContext);

  if (context === null) {
    throw new Error("Dashboard components must be rendered inside AppShell.");
  }

  return context;
}
