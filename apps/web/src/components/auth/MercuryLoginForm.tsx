"use client";

import { type FormEvent, useEffect, useRef, useState } from "react";

interface LoginResponse {
  redirectPath: string;
  role: string;
}

interface Particle {
  opacity: number;
  speed: number;
  x: number;
  y: number;
}

const roleRoutes = new Set(["/admin", "/teacher", "/student"]);

function isLoginResponse(value: unknown): value is LoginResponse {
  if (typeof value !== "object" || value === null) {
    return false;
  }

  const response = value as Record<string, unknown>;

  return typeof response.redirectPath === "string"
    && typeof response.role === "string"
    && roleRoutes.has(response.redirectPath);
}

export function MercuryLoginForm() {
  const canvasReference = useRef<HTMLCanvasElement | null>(null);
  const [isPasswordVisible, setIsPasswordVisible] = useState(false);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [message, setMessage] = useState<string | null>(null);

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
    const institutionalId = String(formData.get("institutionalId") ?? "");
    const password = String(formData.get("password") ?? "");
    const apiBaseUrl = (process.env.NEXT_PUBLIC_API_BASE_URL ?? "http://localhost:5112").replace(/\/$/, "");

    try {
      const response = await fetch(apiBaseUrl + "/api/auth/login", {
        body: JSON.stringify({ institutionalId, password }),
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
        <span>Secure academic workspace</span>
      </header>

      <section aria-labelledby="login-heading" className="login-card">
        <header className="login-card__header">
          <p className="login-card__eyebrow">Account access</p>
          <h1 id="login-heading">Welcome back</h1>
          <p>Sign in to manage assignments, submissions, and academic progress.</p>
        </header>

        <form onSubmit={handleSubmit}>
          <div className="login-field">
            <label htmlFor="institutionalId">Institutional ID</label>
            <input autoComplete="username" id="institutionalId" name="institutionalId" placeholder="STU-2026-001" required type="text" />
          </div>

          <div className="login-field">
            <label htmlFor="password">Password</label>
            <div className="login-password-input">
              <input autoComplete="current-password" id="password" name="password" placeholder="••••••••" required type={isPasswordVisible ? "text" : "password"} />
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
        <footer className="login-card__footer">Accounts are created and managed by an administrator.</footer>
      </section>
    </main>
  );
}
