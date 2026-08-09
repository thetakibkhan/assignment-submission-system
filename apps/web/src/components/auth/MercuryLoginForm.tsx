"use client";

import { type CSSProperties, type FormEvent, useEffect, useRef, useState } from "react";

const blobs = [
  { size: 230, left: "7%", top: "10%", delay: "-8s", duration: "21s" },
  { size: 170, left: "72%", top: "12%", delay: "-14s", duration: "26s" },
  { size: 270, left: "64%", top: "65%", delay: "-3s", duration: "24s" },
  { size: 150, left: "15%", top: "71%", delay: "-18s", duration: "19s" },
  { size: 195, left: "41%", top: "42%", delay: "-11s", duration: "28s" },
  { size: 125, left: "87%", top: "46%", delay: "-6s", duration: "22s" },
];

export function MercuryLoginForm() {
  const blobReferences = useRef<Array<HTMLDivElement | null>>([]);
  const [message, setMessage] = useState<string | null>(null);

  useEffect(() => {
    function handlePointerMove(event: PointerEvent) {
      const offsetX = event.clientX / window.innerWidth - 0.5;
      const offsetY = event.clientY / window.innerHeight - 0.5;

      blobReferences.current.forEach((blob, index) => {
        if (blob) {
          const speed = (index + 1) * 9;
          blob.style.setProperty("--pointer-x", `${offsetX * speed}px`);
          blob.style.setProperty("--pointer-y", `${offsetY * speed}px`);
        }
      });
    }

    window.addEventListener("pointermove", handlePointerMove);
    return () => window.removeEventListener("pointermove", handlePointerMove);
  }, []);

  function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setMessage("Sign-in is being connected to the secure API.");
  }

  return (
    <main className="mercury-login">
      <svg aria-hidden="true" className="sr-only">
        <defs>
          <filter id="mercury-goo">
            <feGaussianBlur in="SourceGraphic" result="blur" stdDeviation="12" />
            <feColorMatrix
              in="blur"
              mode="matrix"
              result="goo"
              values="1 0 0 0 0 0 1 0 0 0 0 0 1 0 0 0 0 0 19 -9"
            />
            <feComposite in="SourceGraphic" in2="goo" operator="atop" />
          </filter>
        </defs>
      </svg>

      <div aria-hidden="true" className="mercury-stage">
        {blobs.map((blob, index) => (
          <div
            className="mercury-blob"
            key={`${blob.left}-${blob.top}`}
            ref={(element) => {
              blobReferences.current[index] = element;
            }}
            style={{
              "--blob-size": `${blob.size}px`,
              "--blob-left": blob.left,
              "--blob-top": blob.top,
              "--blob-delay": blob.delay,
              "--blob-duration": blob.duration,
            } as CSSProperties}
          />
        ))}
      </div>

      <section aria-labelledby="login-heading" className="mercury-card">
        <header className="mb-12">
          <p className="mercury-kicker">Assignment System · Secure Access</p>
          <h1 id="login-heading" className="mercury-title">
            WELCOME
            <br />
            BACK
          </h1>
          <p className="mt-5 max-w-sm text-sm leading-6 text-white/55">
            Sign in to manage assignments, submissions, and academic progress.
          </p>
        </header>

        <form noValidate onSubmit={handleSubmit}>
          <label className="mercury-field">
            <span>Email address</span>
            <input autoComplete="email" name="email" placeholder="name@school.edu" required type="email" />
          </label>

          <label className="mercury-field">
            <span>Password</span>
            <input autoComplete="current-password" name="password" placeholder="••••••••" required type="password" />
          </label>

          <button className="mercury-button" type="submit">
            <span>Sign in securely</span>
          </button>
        </form>

        <p aria-live="polite" className="mt-6 min-h-5 text-xs text-white/55">
          {message}
        </p>

        <footer className="mercury-footer">
          <span>Protected academic workspace</span>
          <span>Role-based access</span>
        </footer>
      </section>
    </main>
  );
}
