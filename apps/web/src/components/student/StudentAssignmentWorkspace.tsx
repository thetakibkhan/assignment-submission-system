"use client";

import { useRouter } from "next/navigation";
import { type FormEvent, useCallback, useEffect, useMemo, useState } from "react";
import { SignOutButton } from "@/components/auth/SignOutButton";

type View = "open" | "past";
type Submission = { id: string; status: string; submittedAt: string; textAnswer: string | null; updatedAt: string };
type Assignment = { allowSubmissionUpdates: boolean; classCourseName: string; deadline: string; deadlinePassed: boolean; description: string; id: string; maximumMarks: number; studentState: string; subjectName: string; teacherName: string; title: string };

const apiBaseUrl = (process.env.NEXT_PUBLIC_API_BASE_URL ?? "http://localhost:5112").replace(/\/$/, "");

async function request<T>(path: string, options?: RequestInit): Promise<T> {
  const response = await fetch(apiBaseUrl + path, { ...options, credentials: "include" });

  if (!response.ok) {
    const problem: unknown = await response.json().catch(() => null);
    const detail = typeof problem === "object" && problem !== null && "detail" in problem
      ? String(problem.detail)
      : response.status === 401 || response.status === 403 ? "Unauthorized" : "Unavailable";
    throw new Error(detail);
  }

  return response.json() as Promise<T>;
}

function deadlineLabel(assignment: Assignment): string {
  if (assignment.deadlinePassed) return "Deadline passed";
  const days = Math.ceil((new Date(assignment.deadline).getTime() - Date.now()) / 86_400_000);
  return days <= 1 ? "Due within 24 hours" : "Due in " + days + " days";
}

export function StudentAssignmentWorkspace() {
  const router = useRouter();
  const [assignments, setAssignments] = useState<Assignment[]>([]);
  const [activeView, setActiveView] = useState<View>("open");
  const [message, setMessage] = useState("Loading your assignments…");
  const [selectedAssignment, setSelectedAssignment] = useState<Assignment | null>(null);
  const [submission, setSubmission] = useState<Submission | null>(null);
  const [submissionText, setSubmissionText] = useState("");
  const [isSavingSubmission, setIsSavingSubmission] = useState(false);
  const visibleAssignments = useMemo(() => assignments.filter((assignment) => assignment.deadlinePassed === (activeView === "past")), [activeView, assignments]);

  const handleRequestError = useCallback((error: unknown): boolean => {
    if (error instanceof Error && error.message === "Unauthorized") {
      router.replace("/");
      return true;
    }
    return false;
  }, [router]);

  const loadAssignments = useCallback(async () => {
    try {
      const loaded = await request<Assignment[]>("/api/student/assignments");
      setAssignments(loaded);
      setMessage(loaded.length ? "Your assignment list is up to date." : "No assignments are available for your active enrollments.");
    } catch (error) {
      if (!handleRequestError(error)) setMessage("The assignment service is unavailable. Please try again.");
    }
  }, [handleRequestError]);

  useEffect(() => {
    const timer = window.setTimeout(() => void loadAssignments(), 0);
    return () => window.clearTimeout(timer);
  }, [loadAssignments]);

  function changeView(view: View) {
    setActiveView(view);
    setSelectedAssignment(null);
    setSubmission(null);
    setSubmissionText("");
  }

  async function selectAssignment(assignment: Assignment) {
    setSelectedAssignment(assignment);
    setSubmission(null);
    setSubmissionText("");
    try {
      const currentSubmission = await request<Submission>("/api/student/assignments/" + assignment.id + "/submission");
      setSubmission(currentSubmission);
      setSubmissionText(currentSubmission.textAnswer ?? "");
    } catch (error) {
      if (!handleRequestError(error) && error instanceof Error && error.message !== "Unavailable") {
        setMessage("Your submission could not be loaded. Try selecting the assignment again.");
      }
    }
  }

  async function saveSubmission(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (selectedAssignment === null) return;
    setIsSavingSubmission(true);
    try {
      const formData = new FormData();
      formData.append("textAnswer", submissionText);
      const saved = await request<Submission>("/api/student/assignments/" + selectedAssignment.id + "/submission", { body: formData, method: submission === null ? "POST" : "PUT" });
      const isNewSubmission = submission === null;
      setSubmission(saved);
      setSubmissionText(saved.textAnswer ?? "");
      setAssignments((current) => current.map((assignment) => assignment.id === selectedAssignment.id ? { ...assignment, studentState: saved.status } : assignment));
      setSelectedAssignment((current) => current === null ? null : { ...current, studentState: saved.status });
      setMessage(isNewSubmission ? "Your work has been submitted." : "Your submission has been updated.");
    } catch (error) {
      if (!handleRequestError(error)) setMessage(error instanceof Error ? error.message : "Your submission could not be saved.");
    } finally {
      setIsSavingSubmission(false);
    }
  }

  const canEdit = selectedAssignment !== null && !selectedAssignment.deadlinePassed && (submission === null || (selectedAssignment.allowSubmissionUpdates && submission.status === "Submitted"));

  return <main className="student-console">
    <header className="student-console__header"><div><p className="login-card__eyebrow">Student workspace</p><h1>My assignments</h1><p>See what needs attention, understand each deadline, and review past work.</p></div><div className="student-console__actions"><SignOutButton className="workspace-sign-out" /><button onClick={() => void loadAssignments()} type="button">Refresh</button></div></header>
    <p aria-live="polite" className="student-console__message">{message}</p>
    <div aria-label="Assignment timing" className="student-tabs" role="tablist"><button aria-selected={activeView === "open"} className={activeView === "open" ? "is-active" : ""} onClick={() => changeView("open")} role="tab" type="button">Open</button><button aria-selected={activeView === "past"} className={activeView === "past" ? "is-active" : ""} onClick={() => changeView("past")} role="tab" type="button">Past</button></div>
    <div className="student-workspace"><section aria-label={activeView + " assignments"} className="student-assignment-list">{visibleAssignments.length === 0 ? <div className="student-empty"><h2>No {activeView} assignments</h2><p>{activeView === "open" ? "There is no current work for your active enrollments." : "Assignments appear here after their deadlines pass."}</p></div> : visibleAssignments.map((assignment) => <button aria-pressed={selectedAssignment?.id === assignment.id} className="student-assignment-row" key={assignment.id} onClick={() => void selectAssignment(assignment)} type="button"><span className="student-assignment-row__context">{assignment.classCourseName} · {assignment.subjectName}</span><strong>{assignment.title}</strong><span>{deadlineLabel(assignment)} · {assignment.studentState}</span></button>)}</section>
      <aside aria-live="polite" className="student-detail">{selectedAssignment ? <><div className="student-detail__heading"><span className={selectedAssignment.deadlinePassed ? "student-status student-status--past" : "student-status"}>{deadlineLabel(selectedAssignment)}</span><h2>{selectedAssignment.title}</h2><p>{selectedAssignment.classCourseName} · {selectedAssignment.subjectName}</p></div><dl className="student-detail__facts"><div><dt>Deadline</dt><dd>{new Date(selectedAssignment.deadline).toLocaleString()}</dd></div><div><dt>Maximum marks</dt><dd>{selectedAssignment.maximumMarks}</dd></div><div><dt>Teacher</dt><dd>{selectedAssignment.teacherName}</dd></div><div><dt>Status</dt><dd>{selectedAssignment.studentState}</dd></div></dl><section><h3>Instructions</h3><p>{selectedAssignment.description}</p></section><p className="student-detail__policy">{selectedAssignment.allowSubmissionUpdates ? "Updates are allowed until the deadline." : "Your submission cannot be changed after it is sent."}</p><section className="student-submission-panel"><h3>{submission === null ? "Submit your work" : "Your submission"}</h3>{canEdit ? <form onSubmit={saveSubmission}><label htmlFor="submission-text">Your answer</label><textarea id="submission-text" maxLength={10000} onChange={(event) => setSubmissionText(event.target.value)} placeholder="Write your answer here…" required value={submissionText} /><button disabled={isSavingSubmission} type="submit">{isSavingSubmission ? "Saving…" : submission === null ? "Submit work" : "Update submission"}</button></form> : <p>{selectedAssignment.deadlinePassed ? "Submission is closed because the deadline has passed." : submission?.status !== "Submitted" ? "This submission is currently being reviewed." : "Your teacher has disabled submission updates for this assignment."}</p>}{submission && <p className="student-submission-panel__metadata">Submitted {new Date(submission.submittedAt).toLocaleString()}{submission.updatedAt !== submission.submittedAt ? " · Updated " + new Date(submission.updatedAt).toLocaleString() : ""}</p>}</section></> : <div className="student-detail__placeholder"><h2>Select an assignment</h2><p>Choose an item to see its instructions, deadline, marks, and submission policy.</p></div>}</aside>
    </div>
  </main>;
}
