import type { ReactNode } from "react";

interface DarkGradientBgProps {
  children: ReactNode;
}

export function DarkGradientBg({ children }: DarkGradientBgProps) {
  return (
    <div className="dark-gradient-bg relative min-h-screen overflow-hidden bg-black text-white">
      <div aria-hidden="true" className="dark-gradient-bg__texture" />
      <div aria-hidden="true" className="dark-gradient-bg__grid" />
      <div aria-hidden="true" className="dark-gradient-bg__glow" />
      <div className="relative z-10 min-h-screen">{children}</div>
    </div>
  );
}
