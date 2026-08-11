"use client";

import { AdminSetupConsole } from "@/components/admin/AdminSetupConsole";
import { useAppShell } from "@/components/app-shell";

export function Dashboard() {
  const { activeSection } = useAppShell();

  return <AdminSetupConsole activeSection={activeSection} />;
}
