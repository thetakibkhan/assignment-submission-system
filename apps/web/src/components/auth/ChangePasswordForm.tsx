"use client";

import { type FormEvent, useState } from "react";
import { browserApiBaseUrl } from "@/lib/api-routing";

interface ChangePasswordResponse {
  redirectPath: string;
}

function isChangePasswordResponse(value: unknown): value is ChangePasswordResponse {
  if (typeof value !== "object" || value === null) {
    return false;
  }

  const response = value as Record<string, unknown>;

  return typeof response.redirectPath === "string";
}

export function ChangePasswordForm() {
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [isCurrentPasswordVisible, setIsCurrentPasswordVisible] = useState(false);
  const [isNewPasswordVisible, setIsNewPasswordVisible] = useState(false);
  const [isConfirmPasswordVisible, setIsConfirmPasswordVisible] = useState(false);
  const [message, setMessage] = useState<string | null>(null);

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setIsSubmitting(true);
    setMessage(null);

    const formData = new FormData(event.currentTarget);
    const currentPassword = String(formData.get("currentPassword") ?? "");
    const newPassword = String(formData.get("newPassword") ?? "");
    const confirmPassword = String(formData.get("confirmPassword") ?? "");

    if (newPassword !== confirmPassword) {
      setMessage("The new password and confirmation do not match.");
      setIsSubmitting(false);
      return;
    }

    try {
      const response = await fetch(browserApiBaseUrl + "/api/auth/change-password", {
        body: JSON.stringify({ currentPassword, newPassword }),
        credentials: "include",
        headers: {
          "Content-Type": "application/json",
        },
        method: "POST",
      });

      if (!response.ok) {
        setMessage("The password could not be changed. Check the current password and password requirements.");
        return;
      }

      const result: unknown = await response.json();

      if (!isChangePasswordResponse(result)) {
        setMessage("The password was changed, but the next page could not be determined.");
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
      <section aria-labelledby="change-password-heading" className="login-card">
        <header className="login-card__header">
          <p className="login-card__eyebrow">Required account step</p>
          <h1 id="change-password-heading">Set a new password</h1>
          <p>Your administrator issued a temporary password. Choose a new private password to continue.</p>
        </header>

        <form onSubmit={handleSubmit}>
          <div className="login-field">
            <label htmlFor="currentPassword">Temporary password</label>
            <div className="login-password-input">
              <input autoComplete="current-password" id="currentPassword" name="currentPassword" required type={isCurrentPasswordVisible ? "text" : "password"} />
              <button aria-label={isCurrentPasswordVisible ? "Hide temporary password" : "Show temporary password"} className="login-password-toggle" onClick={() => setIsCurrentPasswordVisible((isVisible) => !isVisible)} type="button">
                {isCurrentPasswordVisible ? "Hide" : "Show"}
              </button>
            </div>
          </div>
          <div className="login-field">
            <label htmlFor="newPassword">New password</label>
            <div className="login-password-input">
              <input autoComplete="new-password" id="newPassword" minLength={8} name="newPassword" required type={isNewPasswordVisible ? "text" : "password"} />
              <button aria-label={isNewPasswordVisible ? "Hide new password" : "Show new password"} className="login-password-toggle" onClick={() => setIsNewPasswordVisible((isVisible) => !isVisible)} type="button">
                {isNewPasswordVisible ? "Hide" : "Show"}
              </button>
            </div>
          </div>
          <div className="login-field">
            <label htmlFor="confirmPassword">Confirm new password</label>
            <div className="login-password-input">
              <input autoComplete="new-password" id="confirmPassword" minLength={8} name="confirmPassword" required type={isConfirmPasswordVisible ? "text" : "password"} />
              <button aria-label={isConfirmPasswordVisible ? "Hide confirmed password" : "Show confirmed password"} className="login-password-toggle" onClick={() => setIsConfirmPasswordVisible((isVisible) => !isVisible)} type="button">
                {isConfirmPasswordVisible ? "Hide" : "Show"}
              </button>
            </div>
          </div>

          <button className="login-submit-button" disabled={isSubmitting} type="submit">
            {isSubmitting ? "Updating password…" : "Continue securely"}
          </button>
        </form>

        <p aria-live="polite" className="login-message">{message}</p>
      </section>
    </main>
  );
}
