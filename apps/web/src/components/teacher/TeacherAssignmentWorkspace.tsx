"use client";

import { useCallback, useEffect, useMemo, useState, type FormEvent } from "react";
import { useRouter } from "next/navigation";
import { SignOutButton } from "@/components/auth/SignOutButton";
import { NotificationCenter } from "@/components/notifications/NotificationCenter";
import { filterDashboardItems } from "@/components/dashboard/dashboard-filtering";
import {
  getSubmissionReviewSectionVisibility,
  getTeacherWorkspacePanelVisibility,
  selectSubmissionAfterRefresh,
} from "@/components/teacher/teacher-workspace-view";
import { browserApiBaseUrl } from "@/lib/api-routing";

type AssignmentStatus = "Draft" | "Published";
type Assignment = { id: string; academicClassId: string; subjectId: string; title: string; description: string | null; deadline: string | null; maximumMarks: number | null; allowSubmissionUpdates: boolean | null; canReturnToDraft: boolean; status: AssignmentStatus };
type Scope = { academicClassId: string; academicClassName: string; subjectId: string; subjectName: string };
type TeacherDashboard = { draftAssignments: number; publishedAssignments: number; submissionsNeedingReview: number; submissionsUnderReview: number; };
type SubmissionAttachment = { fileName: string; id: string };
type Submission = { attachmentFileName: string | null; attachments: SubmissionAttachment[]; feedback: string | null; id: string; marks: number | null; status: "Submitted" | "UnderReview" | "Graded"; studentName: string; submittedAt: string; textAnswer: string | null; updatedAt: string };

function missingPublishFields(assignment: Assignment): string[] {
  const missing: string[] = [];
  if (!assignment.description?.trim()) missing.push("description");
  if (!assignment.deadline || new Date(assignment.deadline).getTime() <= Date.now()) missing.push("future deadline");
  if (!assignment.maximumMarks || assignment.maximumMarks <= 0) missing.push("maximum marks");
  if (assignment.allowSubmissionUpdates === null) missing.push("update policy");
  return missing;
}

async function request<T>(path: string, options?: RequestInit): Promise<T> {
  const response = await fetch(browserApiBaseUrl + path, { ...options, credentials: "include", headers: { "Content-Type": "application/json", ...options?.headers } });
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
  const [dashboard, setDashboard] = useState<TeacherDashboard | null>(null);
  const [view, setView] = useState<AssignmentStatus>("Draft");
  const [assignmentSearch, setAssignmentSearch] = useState("");
  const [showForm, setShowForm] = useState(false);
  const [editingAssignment, setEditingAssignment] = useState<Assignment | null>(null);
  const [message, setMessage] = useState("Loading your assignment workspace…");
  const [reviewAssignment, setReviewAssignment] = useState<Assignment | null>(null);
  const [reviewSubmissions, setReviewSubmissions] = useState<Submission[]>([]);
  const [selectedSubmission, setSelectedSubmission] = useState<Submission | null>(null);
  const [isReviewSaving, setIsReviewSaving] = useState(false);
  const [blockedReturnAssignmentId, setBlockedReturnAssignmentId] = useState<string | null>(null);
  const panelVisibility = getTeacherWorkspacePanelVisibility(reviewAssignment !== null);
  const reviewSectionVisibility = getSubmissionReviewSectionVisibility({
    hasAttachment: Boolean(selectedSubmission?.attachments.length),
    isGraded: selectedSubmission?.status === "Graded",
  });
  const visibleAssignments = useMemo(() => filterDashboardItems(assignments, assignmentSearch, view), [assignments, assignmentSearch, view]);
  const draftCount = assignments.filter((assignment) => assignment.status === "Draft").length;
  const publishedCount = assignments.filter((assignment) => assignment.status === "Published").length;
  const reviewCount = reviewSubmissions.filter((submission) => submission.status !== "Graded").length;
  const load = useCallback(async () => {
    try {
      const [loadedAssignments, loadedScopes, loadedDashboard] = await Promise.all([request<Assignment[]>("/api/teacher/assignments"), request<Scope[]>("/api/teacher/assignments/scopes"), request<TeacherDashboard>("/api/dashboard/teacher")]);
      setAssignments(loadedAssignments); setScopes(loadedScopes); setDashboard(loadedDashboard);
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
    event.preventDefault(); const formElement = event.currentTarget; const form = new FormData(formElement);
    const scope = scopes.find((item) => item.academicClassId + ":" + item.subjectId === String(form.get("scope")));
    if (!scope) { setMessage("Choose one of your assigned Class and Subject combinations."); return; }
    try {
      const body = JSON.stringify({ academicClassId: scope.academicClassId, subjectId: scope.subjectId, title: String(form.get("title") ?? ""), description: String(form.get("description") ?? "") || null, deadline: String(form.get("deadline") ?? "") ? new Date(String(form.get("deadline"))).toISOString() : null, maximumMarks: String(form.get("maximumMarks") ?? "") ? Number(form.get("maximumMarks")) : null, allowSubmissionUpdates: form.get("updates") === "yes" ? true : form.get("updates") === "no" ? false : null });
      if (editingAssignment) {
        await request<Assignment>("/api/teacher/assignments/" + editingAssignment.id, { method: "PUT", body });
      } else {
        await request<Assignment>("/api/teacher/assignments", { method: "POST", body });
      }
      formElement.reset(); setEditingAssignment(null); setShowForm(false); setView("Draft"); await load(); setMessage(editingAssignment ? "Draft updated." : "Draft saved. Publish it when every required detail is ready.");
    } catch (error) { setMessage(error instanceof Error ? error.message : "The draft could not be saved."); }
  }
  async function openReview(assignment: Assignment, selectedSubmissionId: string | null = null) { try { const items = await request<Submission[]>("/api/teacher/assignments/" + assignment.id + "/submissions"); setShowForm(false); setEditingAssignment(null); setReviewAssignment(assignment); setReviewSubmissions(items); setSelectedSubmission(selectSubmissionAfterRefresh(items, selectedSubmissionId)); setMessage(items.length ? "Select student work to review." : "No submissions for this assignment yet."); if (selectedSubmissionId === null) window.scrollTo({ behavior: "smooth", top: 0 }); } catch (error) { setMessage(error instanceof Error ? error.message : "Could not load submissions."); } }
  function closeReview() { setReviewAssignment(null); setReviewSubmissions([]); setSelectedSubmission(null); setMessage("Your assignment workspace is ready."); window.scrollTo({ behavior: "smooth", top: 0 }); }
  async function reviewAction(action: "start-review" | "grade" | "reopen") { if (!selectedSubmission || !reviewAssignment) return; const selectedSubmissionId = selectedSubmission.id; setIsReviewSaving(true); try { await request<void>("/api/teacher/submissions/" + selectedSubmissionId + "/" + action, { method: "POST" }); await openReview(reviewAssignment, selectedSubmissionId); setMessage(action === "grade" ? "Result finalized and shared with the student." : action === "reopen" ? "Result reopened for correction." : "Review started."); } catch (error) { setMessage(error instanceof Error ? error.message : "Review action failed."); } finally { setIsReviewSaving(false); } }
  async function saveReview(event: FormEvent<HTMLFormElement>) { event.preventDefault(); if (!selectedSubmission || !reviewAssignment) return; const selectedSubmissionId = selectedSubmission.id; const form = new FormData(event.currentTarget); const marks = String(form.get("marks") ?? "").trim(); setIsReviewSaving(true); try { await request<void>("/api/teacher/submissions/" + selectedSubmissionId + "/review", { method: "PUT", body: JSON.stringify({ marks: marks ? Number(marks) : null, feedback: String(form.get("feedback") ?? "") || null }) }); await openReview(reviewAssignment, selectedSubmissionId); setMessage("Review saved."); } catch (error) { setMessage(error instanceof Error ? error.message : "Review could not be saved."); } finally { setIsReviewSaving(false); } }
  function publishOrComplete(assignment: Assignment) {
    const missing = missingPublishFields(assignment);
    if (missing.length === 0) { void action(assignment, "publish"); return; }
    openEditForm(assignment);
    setMessage("Complete this draft before publishing: " + missing.join(", ") + ".");
    window.scrollTo({ behavior: "smooth", top: 0 });
  }
  async function action(assignment: Assignment, operation: "publish" | "unpublish" | "delete") {
    if (operation === "delete" && !window.confirm(`Delete “${assignment.title}”? This cannot be undone.`)) return;
    try { await request<void>("/api/teacher/assignments/" + assignment.id + (operation === "delete" ? "" : "/" + operation), { method: operation === "delete" ? "DELETE" : "POST" }); if (operation === "publish") setView("Published"); if (operation === "unpublish") setView("Draft"); await load(); setMessage(operation === "publish" ? "Assignment published for eligible students." : operation === "unpublish" ? "Assignment returned to Draft." : "Draft deleted."); }
    catch (error) { setMessage(error instanceof Error ? error.message : "The action could not be completed."); }
  }
  return <main className="teacher-console">
    <header className="teacher-console__header"><div><p className="login-card__eyebrow">Teacher workspace</p><h1>{reviewAssignment ? "Review submissions" : "Assignments"}</h1><p>{reviewAssignment ? reviewAssignment.title : "Prepare work privately, then publish it to eligible students."}</p></div><div className="teacher-console__actions"><NotificationCenter destination="/teacher" /><SignOutButton className="workspace-sign-out" /><button className="teacher-console__primary" disabled={!reviewAssignment && !scopes.length} onClick={() => reviewAssignment ? closeReview() : showForm ? (setShowForm(false), setEditingAssignment(null)) : openCreateForm()} type="button">{reviewAssignment ? "Back to assignments" : showForm ? "Close form" : "Create assignment"}</button></div></header>
    <p aria-live="polite" className="teacher-console__message">{message}</p>
    {panelVisibility.assignmentManagement && <>
    <section aria-label="Teacher overview" className="workspace-overview"><article><span>Drafts</span><strong>{dashboard?.draftAssignments ?? draftCount}</strong></article><article><span>Published</span><strong>{dashboard?.publishedAssignments ?? publishedCount}</strong></article><article><span>Needs review</span><strong>{dashboard?.submissionsNeedingReview ?? reviewCount}</strong></article></section>
    {showForm && <section className="teacher-panel teacher-panel--form"><h2>{editingAssignment ? "Edit draft assignment" : "New draft assignment"}</h2><p>Scope and title save a Draft. Publishing requires every field.</p><form key={editingAssignment?.id ?? "new"} onSubmit={create}><label>Teaching scope<select defaultValue={editingAssignment ? editingAssignment.academicClassId + ":" + editingAssignment.subjectId : ""} name="scope" required><option disabled value="">Choose Class and Subject</option>{scopes.map((scope) => <option key={scope.academicClassId + scope.subjectId} value={scope.academicClassId + ":" + scope.subjectId}>{scope.academicClassName} · {scope.subjectName}</option>)}</select></label><label>Title<input defaultValue={editingAssignment?.title ?? ""} maxLength={200} name="title" required /></label><label>Description<textarea defaultValue={editingAssignment?.description ?? ""} maxLength={5000} name="description" rows={5} /></label><div className="teacher-form-grid"><label>Deadline<input defaultValue={editingAssignment?.deadline ? new Date(editingAssignment.deadline).toISOString().slice(0, 16) : ""} name="deadline" type="datetime-local" /></label><label>Maximum marks<input defaultValue={editingAssignment?.maximumMarks ?? ""} min="0.01" name="maximumMarks" step="0.01" type="number" /></label></div><fieldset><legend>Allow updates before deadline?</legend><label><input defaultChecked={editingAssignment?.allowSubmissionUpdates === true} name="updates" type="radio" value="yes" /> Yes</label><label><input defaultChecked={editingAssignment?.allowSubmissionUpdates === false} name="updates" type="radio" value="no" /> No</label></fieldset><button type="submit">{editingAssignment ? "Save changes" : "Save Draft"}</button></form></section>}
    {!showForm && <section className="teacher-panel"><div className="teacher-panel__toolbar"><div><h2>Your assignments</h2><p>Drafts are private; published work is visible to eligible students.</p></div><div className="teacher-tabs"><button className={view === "Draft" ? "is-active" : ""} onClick={() => setView("Draft")} type="button">Drafts</button><button className={view === "Published" ? "is-active" : ""} onClick={() => setView("Published")} type="button">Published</button></div></div><label className="workspace-filter">Search assignments<input aria-label="Search assignments" onChange={(event) => setAssignmentSearch(event.target.value)} placeholder="Search title" value={assignmentSearch} /></label><div className="teacher-assignment-list">{visibleAssignments.length === 0 ? <p className="teacher-empty">No {view.toLowerCase()} assignments yet.</p> : visibleAssignments.map((assignment) => <article className="teacher-assignment" key={assignment.id}><div><span className={assignment.status === "Published" ? "teacher-status teacher-status--published" : "teacher-status"}>{assignment.status}</span><h3>{assignment.title}</h3><p>{assignment.description || "No description added yet."}</p><small>{assignment.deadline ? "Deadline: " + new Date(assignment.deadline).toLocaleString() : "Deadline not set"} · {assignment.maximumMarks ? assignment.maximumMarks + " marks" : "Marks not set"}</small>{assignment.status === "Draft" && missingPublishFields(assignment).length > 0 && <p className="teacher-assignment__readiness">Before publishing: add {missingPublishFields(assignment).join(", ")}.</p>}</div><div className="teacher-assignment__actions">{assignment.status === "Draft" ? <><button onClick={() => publishOrComplete(assignment)} type="button">{missingPublishFields(assignment).length ? "Complete draft" : "Publish"}</button><button className="teacher-assignment__subtle" onClick={() => openEditForm(assignment)} type="button">Edit Draft</button><button className="teacher-assignment__subtle" onClick={() => void action(assignment, "delete")} type="button">Delete</button></> : <><button onClick={() => void openReview(assignment)} type="button">Review submissions</button><button aria-describedby={blockedReturnAssignmentId === assignment.id ? "return-to-draft-" + assignment.id : undefined} aria-disabled={!assignment.canReturnToDraft} className={!assignment.canReturnToDraft ? "teacher-assignment__subtle teacher-assignment__subtle--disabled" : "teacher-assignment__subtle"} onClick={() => { if (!assignment.canReturnToDraft) { setBlockedReturnAssignmentId(assignment.id); return; } void action(assignment, "unpublish"); }} type="button">Return to Draft</button>{blockedReturnAssignmentId === assignment.id && <p className="teacher-assignment__action-note" id={"return-to-draft-" + assignment.id}>This assignment cannot return to Draft because student work has been submitted.</p>}</>}</div></article>)}</div></section>}
    </>}
    {panelVisibility.submissionReview && reviewAssignment && <section className="teacher-panel teacher-review-panel"><div className="teacher-panel__toolbar"><div><h2>{reviewAssignment.title}</h2><p>Select a student submission, then review and finalize the result.</p></div></div><div className="teacher-review-grid"><div className="teacher-review-queue">{reviewSubmissions.map((item) => <button aria-pressed={selectedSubmission?.id === item.id} key={item.id} onClick={() => setSelectedSubmission(item)} type="button"><strong>{item.studentName}</strong><span>{item.status === "UnderReview" ? "Under review" : item.status}</span>{item.attachments.length > 0 && <small>{item.attachments.length} attachment{item.attachments.length === 1 ? "" : "s"}</small>}</button>)}{reviewSubmissions.length === 0 && <p className="teacher-empty">No submitted work yet.</p>}</div><div className="teacher-review-detail">{selectedSubmission ? <><h3>{selectedSubmission.studentName}</h3><p className="teacher-review-detail__date">Submitted {new Date(selectedSubmission.submittedAt).toLocaleString()}</p><div className="teacher-review-content">{reviewSectionVisibility.writtenResponse && <section className="teacher-review-content__section"><div className="teacher-review-content__heading"><h4>Written response</h4><span>Student answer</span></div><div className="teacher-review-answer"><p>{selectedSubmission.textAnswer || "No written answer provided."}</p></div></section>}{reviewSectionVisibility.attachment && selectedSubmission.attachments.length > 0 && <section className="teacher-review-content__section"><div className="teacher-review-content__heading"><h4>Attachments</h4><span>{selectedSubmission.attachments.length} submitted file{selectedSubmission.attachments.length === 1 ? "" : "s"}</span></div><div className="teacher-review-files">{selectedSubmission.attachments.map((attachment) => <div className="teacher-review-file" key={attachment.id}><span aria-hidden="true" className="teacher-review-file__icon">File</span><div className="teacher-review-file__info"><strong title={attachment.fileName}>{attachment.fileName}</strong><span>Supporting document</span></div><a className="teacher-review-detail__attachment" href={browserApiBaseUrl + "/api/teacher/submissions/" + selectedSubmission.id + "/attachments/" + attachment.id}>Download</a></div>)}</div></section>}</div>{selectedSubmission.status === "Submitted" && <button disabled={isReviewSaving} onClick={() => void reviewAction("start-review")} type="button">Start review</button>}{selectedSubmission.status === "UnderReview" && <form key={selectedSubmission.id + selectedSubmission.updatedAt} onSubmit={saveReview}><label>Marks out of {reviewAssignment.maximumMarks}<input defaultValue={selectedSubmission.marks ?? ""} max={reviewAssignment.maximumMarks ?? undefined} min="0" name="marks" step="0.01" type="number" /></label><label>Feedback (optional)<textarea defaultValue={selectedSubmission.feedback ?? ""} maxLength={5000} name="feedback" rows={5} /></label><div className="teacher-review-actions"><div className="teacher-review-action-group"><button className="teacher-review-action teacher-review-action--save" disabled={isReviewSaving} type="submit">Save review</button><p>Keep marks and feedback private while you continue reviewing.</p></div><div className="teacher-review-action-group"><button className="teacher-review-action teacher-review-action--finalize" disabled={isReviewSaving || selectedSubmission.marks === null} onClick={() => void reviewAction("grade")} type="button">Finalize grade</button><p>Marks and feedback become final; students see them after the deadline.</p></div></div></form>}{reviewSectionVisibility.finalResult && <><section className="teacher-review-result"><div className="teacher-review-result__header"><div><h4>Final result</h4><p className="teacher-review-result__score"><strong>{selectedSubmission.marks}</strong><span> / {reviewAssignment.maximumMarks}</span></p></div><span className="teacher-review-result__status">Graded</span></div><div className="teacher-review-result__feedback"><h5>Teacher feedback</h5><p>{selectedSubmission.feedback || "No feedback provided."}</p></div></section><button className="teacher-review-action teacher-review-action--secondary" onClick={() => void reviewAction("reopen")} type="button">Reopen for correction</button></>}</> : <p className="teacher-empty">Select a submission.</p>}</div></div></section>}
  </main>;
}
