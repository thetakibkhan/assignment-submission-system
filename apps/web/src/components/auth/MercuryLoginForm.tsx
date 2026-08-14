"use client";

import { type FormEvent, useEffect, useRef, useState } from "react";
import { BouncyAccordion } from "@/components/ui/be-ui-bouncy-accordion";
import { browserApiBaseUrl } from "@/lib/api-routing";
import { recruiterDemoAccounts, type DemoAccount } from "@/lib/demo-accounts";

interface LoginResponse {
  requiresPasswordChange: boolean;
  redirectPath: string;
  role: string;
}

interface Particle {
  opacity: number;
  speed: number;
  x: number;
  y: number;
}

const roleRoutes = new Set(["/admin", "/change-password", "/teacher", "/student"]);

function isLoginResponse(value: unknown): value is LoginResponse {
  if (typeof value !== "object" || value === null) {
    return false;
  }

  const response = value as Record<string, unknown>;

  return typeof response.redirectPath === "string"
    && typeof response.role === "string"
    && typeof response.requiresPasswordChange === "boolean"
    && roleRoutes.has(response.redirectPath);
}

export function MercuryLoginForm() {
  const canvasReference = useRef<HTMLCanvasElement | null>(null);
  const [isPasswordVisible, setIsPasswordVisible] = useState(false);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [institutionalId, setInstitutionalId] = useState("");
  const [message, setMessage] = useState<string | null>(null);
  const [openDemoAccountId, setOpenDemoAccountId] = useState<string | null>(null);
  const [password, setPassword] = useState("");

  useEffect(() => {
    const canvas = canvasReference.current;
    const context = canvas?.getContext("2d");

    if (!canvas || !context) {
      return;
    }

    const canvasElement = canvas;
    const drawingContext = context;
    let animationFrameId = 0;
    let particles: Particle[] = [];

    function resizeCanvas() {
      canvasElement.width = window.innerWidth;
      canvasElement.height = window.innerHeight;
      const particleCount = Math.max(24, Math.floor((canvasElement.width * canvasElement.height) / 12000));

      particles = Array.from({ length: particleCount }, (_, index) => ({
        opacity: 0.15 + (index % 5) * 0.05,
        speed: 0.08 + (index % 6) * 0.035,
        x: ((index * 97) % canvasElement.width) + 0.5,
        y: ((index * 193) % canvasElement.height) + 0.5,
      }));
    }

    function drawParticles() {
      drawingContext.clearRect(0, 0, canvasElement.width, canvasElement.height);

      for (const particle of particles) {
        particle.y -= particle.speed;

        if (particle.y < 0) {
          particle.y = canvasElement.height;
        }

        drawingContext.fillStyle = "rgba(250, 250, 250, " + particle.opacity + ")";
        drawingContext.fillRect(particle.x, particle.y, 1, 2);
      }

      animationFrameId = window.requestAnimationFrame(drawParticles);
    }

    resizeCanvas();
    animationFrameId = window.requestAnimationFrame(drawParticles);
    window.addEventListener("resize", resizeCanvas);

    return () => {
      window.cancelAnimationFrame(animationFrameId);
      window.removeEventListener("resize", resizeCanvas);
    };
  }, []);

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setIsSubmitting(true);
    setMessage(null);

    const formData = new FormData(event.currentTarget);
    const submittedInstitutionalId = String(formData.get("institutionalId") ?? "");
    const submittedPassword = String(formData.get("password") ?? "");
    try {
      const response = await fetch(browserApiBaseUrl + "/api/auth/login", {
        body: JSON.stringify({ institutionalId: submittedInstitutionalId, password: submittedPassword }),
        credentials: "include",
        headers: {
          "Content-Type": "application/json",
        },
        method: "POST",
      });

      if (!response.ok) {
        setMessage("Sign-in failed. Check your institutional ID and password, or contact an administrator.");
        return;
      }

      const result: unknown = await response.json();

      if (!isLoginResponse(result)) {
        setMessage("Sign-in could not be completed. Please try again.");
        return;
      }

      window.location.assign(result.redirectPath);
    } catch {
      setMessage("The secure sign-in service is unavailable. Please try again shortly.");
    } finally {
      setIsSubmitting(false);
    }
  }

  function selectDemoAccount(account: DemoAccount) {
    setInstitutionalId(account.institutionalId);
    setPassword(account.password);
    setMessage(null);
    setOpenDemoAccountId(null);
  }

  const demoAccountAccordionItems = [{
    id: "demo-accounts",
    title: "Demo accounts",
    description: <div className="demo-accounts__content">
      <p>Choose an account to fill the sign-in form.</p>
      <div className="demo-accounts__list">
        {recruiterDemoAccounts.map((account) => (
          <button key={account.institutionalId} onClick={() => selectDemoAccount(account)} type="button">
            <span>{account.role}</span>
            <strong>{account.institutionalId}</strong>
            <code>{account.password}</code>
          </button>
        ))}
      </div>
    </div>,
  }];

  return (
    <main className="login-shell">
      <div aria-hidden="true" className="login-vignette" />
      <div aria-hidden="true" className="login-grid-lines">
        <span />
        <span />
        <span />
        <span />
        <span />
        <span />
      </div>
      <canvas aria-hidden="true" className="login-particles" ref={canvasReference} />

      <header className="login-header">
        <span>Assignment Submission System</span>
      </header>

      <section aria-labelledby="login-heading" className="login-card">
        <header className="login-card__header">
          <h1 id="login-heading">Welcome back</h1>
          <p>Sign in to manage assignments, submissions, and academic progress.</p>
        </header>

        <form onSubmit={handleSubmit}>
          <div className="login-field">
            <label htmlFor="institutionalId">Institutional ID</label>
            <input autoComplete="username" id="institutionalId" name="institutionalId" onChange={(event) => setInstitutionalId(event.target.value)} placeholder="STU-2026-001" required type="text" value={institutionalId} />
          </div>

          <div className="login-field">
            <label htmlFor="password">Password</label>
            <div className="login-password-input">
              <input autoComplete="current-password" id="password" name="password" onChange={(event) => setPassword(event.target.value)} placeholder="••••••••" required type={isPasswordVisible ? "text" : "password"} value={password} />
              <button aria-label={isPasswordVisible ? "Hide password" : "Show password"} className="login-password-toggle" onClick={() => setIsPasswordVisible((isVisible) => !isVisible)} type="button">
                {isPasswordVisible ? "Hide" : "Show"}
              </button>
            </div>
          </div>

          <button className="login-submit-button" disabled={isSubmitting} type="submit">
            {isSubmitting ? "Signing in…" : "Sign in"}
          </button>
        </form>

        <p aria-live="polite" className="login-message">{message}</p>
        <BouncyAccordion
          className="demo-accounts"
          classNames={{
            chevron: "text-zinc-400",
            content: "border-t border-zinc-800",
            item: "border border-zinc-800 bg-zinc-950/75",
            title: "text-zinc-50",
            trigger: "px-4 hover:bg-zinc-900/70",
          }}
          items={demoAccountAccordionItems}
          onValueChange={setOpenDemoAccountId}
          value={openDemoAccountId}
        />
        <footer className="login-card__footer">Accounts are created and managed by an administrator.</footer>
      </section>
    </main>
  );
}
