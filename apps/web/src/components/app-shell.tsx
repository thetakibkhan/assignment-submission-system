"use client";

import { BookOpen, LayoutDashboard, type LucideIcon, ShieldCheck, UserRoundPlus, Users } from "lucide-react";
import { createContext, type ReactNode, useContext, useState } from "react";

export type DashboardSection = "overview" | "accounts" | "academic" | "enrollment" | "responsibilities";

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
];

export function AppShell({ children }: { children: ReactNode }) {
  const [activeSection, setActiveSection] = useState<DashboardSection>("overview");

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
          <p className="app-shell__footer">Single-institution workspace</p>
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
