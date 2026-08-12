"use client";

import { useCallback, useEffect, useMemo, useState, type FormEvent } from "react";
import { useRouter } from "next/navigation";
import { SignOutButton } from "@/components/auth/SignOutButton";

type AssignmentStatus = "Draft" | "Published";
type Assignment = { id: string; classCourseId: string; subjectId: string; title: string; description: string | null; deadline: string | null; maximumMarks: number | null; allowSubmissionUpdates: boolean | null; status: AssignmentStatus };
type Scope = { classCourseId: string; classCourseName: string; subjectId: string; subjectName: string };
const apiBaseUrl = (process.env.NEXT_PUBLIC_API_BASE_URL ?? "http://localhost:5112").replace(/\/$/, "");

async function request<T>(path: string, options?: RequestInit): Promise<T> {
  const response = await fetch(apiBaseUrl + path, { ...options, credentials: "include", headers: { "Content-Type": "application/json", ...options?.headers } });
  if (!response.ok) {
    const problem: unknown = await response.json().catch(() => null);
    const detail = typeof problem === "object" && problem !== null && "detail" in problem ? String(problem.detail) : "The requested action could not be completed.";
    throw new Error(detail);
  }
  return response.status === 204 ? undefined as T : response.json() as Promise<T>;
}

export function TeacherAssignmentWorkspace() {
  const router = useRouter();
  const [assignments, setAssignments] = useState<Assignment[]>([]);
  const [scopes, setScopes] = useState<Scope[]>([]);
  const [view, setView] = useState<AssignmentStatus>("Draft");
  const [showForm, setShowForm] = useState(false);
  const [editingAssignment, setEditingAssignment] = useState<Assignment | null>(null);
  const [message, setMessage] = useState("Loading your assignment workspace…");
  const visibleAssignments = useMemo(() => assignments.filter((assignment) => assignment.status === view), [assignments, view]);
  const load = useCallback(async () => {
    try {
      const [loadedAssignments, loadedScopes] = await Promise.all([request<Assignment[]>("/api/teacher/assignments"), request<Scope[]>("/api/teacher/assignments/scopes")]);
      setAssignments(loadedAssignments); setScopes(loadedScopes);
      setMessage(loadedScopes.length ? "Your assignment workspace is ready." : "You have no active teaching responsibilities yet. Ask an administrator to assign one.");
    } catch (error) {
      if (error instanceof Error && /unauthor|forbidden/i.test(error.message)) { router.replace("/"); return; }
      setMessage(error instanceof Error ? error.message : "The service is unavailable.");
    }
  }, [router]);
  useEffect(() => { const timer = window.setTimeout(() => { void load(); }, 0); return () => window.clearTimeout(timer); }, [load]);
  function openCreateForm() {
    setEditingAssignment(null);
    setShowForm(true);
  }
  function openEditForm(assignment: Assignment) {
    setEditingAssignment(assignment);
    setShowForm(true);
  }
  async function create(event: FormEvent<HTMLFormElement>) {
    event.preventDefault(); const form = new FormData(event.currentTarget);
    const scope = scopes.find((item) => item.classCourseId + ":" + item.subjectId === String(form.get("scope")));
    if (!scope) { setMessage("Choose one of your assigned Class/Course and Subject combinations."); return; }
    try {
      const body = JSON.stringify({ classCourseId: scope.classCourseId, subjectId: scope.subjectId, title: String(form.get("title") ?? ""), description: String(form.get("description") ?? "") || null, deadline: String(form.get("deadline") ?? "") ? new Date(String(form.get("deadline"))).toISOString() : null, maximumMarks: String(form.get("maximumMarks") ?? "") ? Number(form.get("maximumMarks")) : null, allowSubmissionUpdates: form.get("updates") === "yes" ? true : form.get("updates") === "no" ? false : null });
      if (editingAssignment) {
        await request<Assignment>("/api/teacher/assignments/" + editingAssignment.id, { method: "PUT", body });
      } else {
        await request<Assignment>("/api/teacher/assignments", { method: "POST", body });
      }
      event.currentTarget.reset(); setEditingAssignment(null); setShowForm(false); setView("Draft"); await load(); setMessage(editingAssignment ? "Draft updated." : "Draft saved. Publish it when every required detail is ready.");
    } catch (error) { setMessage(error instanceof Error ? error.message : "The draft could not be saved."); }
  }
  async function action(assignment: Assignment, operation: "publish" | "unpublish" | "delete") {
    if (operation === "delete" && !window.confirm(`Delete “${assignment.title}”? This cannot be undone.`)) return;
    try { await request<void>("/api/teacher/assignments/" + assignment.id + (operation === "delete" ? "" : "/" + operation), { method: operation === "delete" ? "DELETE" : "POST" }); await load(); setMessage(operation === "publish" ? "Assignment published for eligible students." : operation === "unpublish" ? "Assignment returned to Draft." : "Draft deleted."); }
    catch (error) { setMessage(error instanceof Error ? error.message : "The action could not be completed."); }
  }
  return <main className="teacher-console">
    <header className="teacher-console__header"><div><p className="login-card__eyebrow">Teacher workspace</p><h1>Assignments</h1><p>Prepare work privately, then publish it to eligible students.</p></div><div className="teacher-console__actions"><SignOutButton className="workspace-sign-out" /><button className="teacher-console__primary" disabled={!scopes.length} onClick={() => showForm ? (setShowForm(false), setEditingAssignment(null)) : openCreateForm()} type="button">{showForm ? "Close form" : "Create assignment"}</button></div></header>
    <p aria-live="polite" className="teacher-console__message">{message}</p>
    {showForm && <section className="teacher-panel teacher-panel--form"><h2>{editingAssignment ? "Edit draft assignment" : "New draft assignment"}</h2><p>Scope and title save a Draft. Publishing requires every field.</p><form key={editingAssignment?.id ?? "new"} onSubmit={create}><label>Teaching scope<select defaultValue={editingAssignment ? editingAssignment.classCourseId + ":" + editingAssignment.subjectId : ""} name="scope" required><option disabled value="">Choose Class/Course and Subject</option>{scopes.map((scope) => <option key={scope.classCourseId + scope.subjectId} value={scope.classCourseId + ":" + scope.subjectId}>{scope.classCourseName} · {scope.subjectName}</option>)}</select></label><label>Title<input defaultValue={editingAssignment?.title ?? ""} maxLength={200} name="title" required /></label><label>Description<textarea defaultValue={editingAssignment?.description ?? ""} maxLength={5000} name="description" rows={5} /></label><div className="teacher-form-grid"><label>Deadline<input defaultValue={editingAssignment?.deadline ? new Date(editingAssignment.deadline).toISOString().slice(0, 16) : ""} name="deadline" type="datetime-local" /></label><label>Maximum marks<input defaultValue={editingAssignment?.maximumMarks ?? ""} min="0.01" name="maximumMarks" step="0.01" type="number" /></label></div><fieldset><legend>Allow updates before deadline?</legend><label><input defaultChecked={editingAssignment?.allowSubmissionUpdates === true} name="updates" type="radio" value="yes" /> Yes</label><label><input defaultChecked={editingAssignment?.allowSubmissionUpdates === false} name="updates" type="radio" value="no" /> No</label></fieldset><button type="submit">{editingAssignment ? "Save changes" : "Save Draft"}</button></form></section>}
    <section className="teacher-panel"><div className="teacher-panel__toolbar"><div><h2>Your assignments</h2><p>Drafts are private; published work is visible to eligible students.</p></div><div className="teacher-tabs"><button className={view === "Draft" ? "is-active" : ""} onClick={() => setView("Draft")} type="button">Drafts</button><button className={view === "Published" ? "is-active" : ""} onClick={() => setView("Published")} type="button">Published</button></div></div><div className="teacher-assignment-list">{visibleAssignments.length === 0 ? <p className="teacher-empty">No {view.toLowerCase()} assignments yet.</p> : visibleAssignments.map((assignment) => <article className="teacher-assignment" key={assignment.id}><div><span className={assignment.status === "Published" ? "teacher-status teacher-status--published" : "teacher-status"}>{assignment.status}</span><h3>{assignment.title}</h3><p>{assignment.description || "No description added yet."}</p><small>{assignment.deadline ? "Deadline: " + new Date(assignment.deadline).toLocaleString() : "Deadline not set"} · {assignment.maximumMarks ? assignment.maximumMarks + " marks" : "Marks not set"}</small></div><div className="teacher-assignment__actions">{assignment.status === "Draft" ? <><button onClick={() => void action(assignment, "publish")} type="button">Publish</button><button className="teacher-assignment__subtle" onClick={() => openEditForm(assignment)} type="button">Edit Draft</button><button className="teacher-assignment__subtle" onClick={() => void action(assignment, "delete")} type="button">Delete</button></> : <button className="teacher-assignment__subtle" onClick={() => void action(assignment, "unpublish")} type="button">Return to Draft</button>}</div></article>)}</div></section>
  </main>;
}
