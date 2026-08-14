"use client";

import { Archive, Bell, ChevronRight, GripVertical, Trash2 } from "lucide-react";
import { useCallback, useEffect, useRef, useState } from "react";
import { browserApiBaseUrl } from "@/lib/api-routing";

type Notification = {
  assignmentId: string;
  createdAt: string;
  id: string;
  isRead: boolean;
  message: string;
  submissionId: string | null;
};

export function NotificationCenter({ destination }: { destination: "/student" | "/teacher" }) {
  const [activeId, setActiveId] = useState<string | null>(null);
  const [isOpen, setIsOpen] = useState(false);
  const [items, setItems] = useState<Notification[]>([]);
  const [message, setMessage] = useState("");
  const notificationCenterRef = useRef<HTMLDivElement>(null);

  const load = useCallback(async () => {
    try {
      const response = await fetch(browserApiBaseUrl + "/api/notifications", { credentials: "include" });
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

  useEffect(() => {
    if (!isOpen) {
      return;
    }

    function closeWhenClickingOutside(event: PointerEvent) {
      if (notificationCenterRef.current?.contains(event.target as Node)) {
        return;
      }

      setIsOpen(false);
      setActiveId(null);
    }

    document.addEventListener("pointerdown", closeWhenClickingOutside);
    return () => document.removeEventListener("pointerdown", closeWhenClickingOutside);
  }, [isOpen]);

  async function markAsRead(id: string) {
    const response = await fetch(
      browserApiBaseUrl + "/api/notifications/" + encodeURIComponent(id) + "/read",
      { credentials: "include", method: "POST" });

    if (response.ok) {
      setItems((current) => current.map((item) => item.id === id ? { ...item, isRead: true } : item));
    }
  }

  async function deleteNotification(item: Notification) {
    if (!window.confirm("Delete this notification? This cannot be undone.")) {
      return;
    }

    const response = await fetch(
      browserApiBaseUrl + "/api/notifications/" + encodeURIComponent(item.id),
      { credentials: "include", method: "DELETE" });

    if (response.ok) {
      setItems((current) => current.filter((notification) => notification.id !== item.id));
      setActiveId(null);
      setMessage("");
      return;
    }

    setMessage("The notification could not be deleted.");
  }

  async function markAllAsRead() {
    const response = await fetch(browserApiBaseUrl + "/api/notifications/read-all", {
      credentials: "include",
      method: "POST"
    });

    if (response.ok) {
      setItems((current) => current.map((item) => ({ ...item, isRead: true })));
      setActiveId(null);
    }
  }

  async function openNotification(item: Notification) {
    if (!item.isRead) {
      await markAsRead(item.id);
    }

    setIsOpen(false);
    window.location.assign(destination + "?assignment=" + encodeURIComponent(item.assignmentId));
  }

  const unreadCount = items.filter((item) => !item.isRead).length;

  return <div className="notification-center" ref={notificationCenterRef}>
    <button
      aria-expanded={isOpen}
      aria-haspopup="dialog"
      aria-label="Open notifications"
      className="notification-center__trigger"
      onClick={() => {
        const nextOpenState = !isOpen;
        setIsOpen(nextOpenState);
        if (nextOpenState) {
          void (async () => {
            await load();
            await markAllAsRead();
          })();
        } else {
          setActiveId(null);
        }
      }}
      type="button">
      <Bell aria-hidden="true" size={17} />
      {unreadCount > 0 && <span>{unreadCount}</span>}
    </button>
    {isOpen && <section aria-label="Notifications" className="notification-center__panel" role="dialog">
      <header>
        <div><strong>Notifications</strong>{unreadCount > 0 && <p>{unreadCount} unread</p>}</div>
      </header>
      {message && <p className="notification-center__message">{message}</p>}
      {!message && items.length === 0 && <p className="notification-center__empty">No notifications yet.</p>}
      <ul className="notification-center__list">
        {items.map((item) => {
          const isActive = activeId === item.id;
          return <li className={item.isRead ? "" : "is-unread"} key={item.id}>
            <button className="notification-center__item" onClick={() => void openNotification(item)} type="button">
              <span>{item.message}</span>
              <small>{new Date(item.createdAt).toLocaleString()}</small>
            </button>
            <div className="notification-center__item-actions">
              {isActive ? <>
                <button aria-label="Mark notification as read" className="notification-center__icon-button" disabled={item.isRead} onClick={() => void markAsRead(item.id)} type="button"><Archive size={15} /></button>
                <button aria-label="Delete notification" className="notification-center__icon-button notification-center__icon-button--delete" onClick={() => void deleteNotification(item)} type="button"><Trash2 size={15} /></button>
                <button aria-label="Open related assignment" className="notification-center__icon-button" onClick={() => void openNotification(item)} type="button"><ChevronRight size={15} /></button>
              </> : <button aria-label="Show notification actions" className="notification-center__icon-button" onClick={() => setActiveId(item.id)} type="button"><GripVertical size={15} /></button>}
            </div>
          </li>;
        })}
      </ul>
    </section>}
  </div>;
}
