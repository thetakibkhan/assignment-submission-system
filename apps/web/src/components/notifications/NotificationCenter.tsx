"use client";

import { Bell, CheckCheck, RefreshCw } from "lucide-react";
import { useCallback, useEffect, useState } from "react";

type Notification = {
  assignmentId: string;
  createdAt: string;
  id: string;
  isRead: boolean;
  message: string;
  submissionId: string | null;
};

const apiBaseUrl = (process.env.NEXT_PUBLIC_API_BASE_URL ?? "http://localhost:5112").replace(/\/$/, "");

export function NotificationCenter({ destination }: { destination: "/student" | "/teacher" }) {
  const [isOpen, setIsOpen] = useState(false);
  const [items, setItems] = useState<Notification[]>([]);
  const [message, setMessage] = useState("");

  const load = useCallback(async () => {
    try {
      const response = await fetch(apiBaseUrl + "/api/notifications", { credentials: "include" });
      if (!response.ok) {
        throw new Error("Notifications are unavailable.");
      }

      setItems(await response.json() as Notification[]);
      setMessage("");
    } catch {
      setMessage("Notifications are unavailable.");
    }
  }, []);

  useEffect(() => {
    const timer = window.setTimeout(() => void load(), 0);
    return () => window.clearTimeout(timer);
  }, [load]);

  async function markAsRead(id: string) {
    const response = await fetch(apiBaseUrl + "/api/notifications/" + encodeURIComponent(id) + "/read", { credentials: "include", method: "POST" });
    if (response.ok) {
      setItems((current) => current.map((item) => item.id === id ? { ...item, isRead: true } : item));
    }
  }

  async function markAllAsRead() {
    const response = await fetch(apiBaseUrl + "/api/notifications/read-all", { credentials: "include", method: "POST" });
    if (response.ok) {
      setItems((current) => current.map((item) => ({ ...item, isRead: true })));
    }
  }

  const unreadCount = items.filter((item) => !item.isRead).length;

  return <div className="notification-center">
    <button aria-expanded={isOpen} aria-label="Open notifications" className="notification-center__trigger" onClick={() => setIsOpen((current) => !current)} type="button">
      <Bell aria-hidden="true" size={17} />
      {unreadCount > 0 && <span>{unreadCount}</span>}
    </button>
    {isOpen && <section aria-label="Notifications" className="notification-center__panel">
      <header><div><strong>Notifications</strong><p>{unreadCount ? `${unreadCount} unread` : "You are up to date"}</p></div><div><button aria-label="Refresh notifications" onClick={() => void load()} type="button"><RefreshCw size={15} /></button><button disabled={!unreadCount} onClick={() => void markAllAsRead()} type="button"><CheckCheck size={15} /> Read all</button></div></header>
      {message && <p className="notification-center__message">{message}</p>}
      {!message && items.length === 0 && <p className="notification-center__empty">No notifications yet.</p>}
      <div className="notification-center__list">{items.map((item) => <a className={item.isRead ? "" : "is-unread"} href={destination + "?assignment=" + encodeURIComponent(item.assignmentId)} key={item.id} onClick={() => void markAsRead(item.id)}><span>{item.message}</span><small>{new Date(item.createdAt).toLocaleString()}</small></a>)}</div>
    </section>}
  </div>;
}
