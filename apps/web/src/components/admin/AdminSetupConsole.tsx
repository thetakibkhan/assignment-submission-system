"use client";

import type { DashboardSection } from "@/components/app-shell";
import { useRouter } from "next/navigation";
import { type FormEvent, useCallback, useEffect, useMemo, useState } from "react";

type Account = {
  email: string | null;
  fullName: string;
  id: string;
  institutionalId: string;
  isActive: boolean;
  mustChangePassword: boolean;
  role: string;
};

type AcademicRecord = {
  code: string;
  id: string;
  isArchived: boolean;
  name: string;
};

type AccountAction = "create" | "manage" | "update";

type CreatedAccount = {
  institutionalId: string;
  role: string;
  temporaryPassword: string;
};

const apiBaseUrl = (process.env.NEXT_PUBLIC_API_BASE_URL ?? "http://localhost:5112").replace(/\/$/, "");

async function request<T>(path: string, options?: RequestInit): Promise<T> {
  const response = await fetch(apiBaseUrl + path, {
    ...options,
    credentials: "include",
    headers: {
      "Content-Type": "application/json",
      ...options?.headers,
    },
  });

  if (!response.ok) {
    const problem: unknown = await response.json().catch(() => null);
    const detail = typeof problem === "object" && problem !== null && "detail" in problem
      ? String(problem.detail)
      : "The requested action could not be completed.";
    throw new Error(detail);
  }

  if (response.status === 204) {
    return undefined as T;
  }

  return response.json() as Promise<T>;
}

export function AdminSetupConsole({ activeSection }: { activeSection: DashboardSection }) {
  const router = useRouter();
  const [accounts, setAccounts] = useState<Account[]>([]);
  const [classCourses, setClassCourses] = useState<AcademicRecord[]>([]);
  const [subjects, setSubjects] = useState<AcademicRecord[]>([]);
  const [message, setMessage] = useState("Loading academic setup…");
  const [temporaryCredential, setTemporaryCredential] = useState<CreatedAccount | null>(null);
  const [accountAction, setAccountAction] = useState<AccountAction | null>(null);
  const [selectedAccountId, setSelectedAccountId] = useState("");
  const [selectedClassCourseId, setSelectedClassCourseId] = useState("");
  const [selectedSubjectId, setSelectedSubjectId] = useState("");

  const selectedAccount = useMemo(
    () => accounts.find((account) => account.id === selectedAccountId),
    [accounts, selectedAccountId],
  );
  const selectedClassCourse = useMemo(
    () => classCourses.find((classCourse) => classCourse.id === selectedClassCourseId),
    [classCourses, selectedClassCourseId],
  );
  const selectedSubject = useMemo(
    () => subjects.find((subject) => subject.id === selectedSubjectId),
    [subjects, selectedSubjectId],
  );

  const activeStudents = accounts.filter((account) => account.isActive && account.role === "Student");
  const activeTeachers = accounts.filter((account) => account.isActive && account.role === "Teacher");
  const availableClassCourses = classCourses.filter((classCourse) => !classCourse.isArchived);
  const availableSubjects = subjects.filter((subject) => !subject.isArchived);

  const loadSetup = useCallback(async () => {
    try {
      const [loadedAccounts, loadedClassCourses, loadedSubjects] = await Promise.all([
        request<Account[]>("/api/admin/users"),
        request<AcademicRecord[]>("/api/admin/classes-courses"),
        request<AcademicRecord[]>("/api/admin/subjects"),
      ]);
      setAccounts(loadedAccounts);
      setClassCourses(loadedClassCourses);
      setSubjects(loadedSubjects);
      setSelectedAccountId((currentId) => currentId || loadedAccounts[0]?.id || "");
      setSelectedClassCourseId((currentId) => currentId || loadedClassCourses[0]?.id || "");
      setSelectedSubjectId((currentId) => currentId || loadedSubjects[0]?.id || "");
      setMessage("Academic setup is ready.");
    } catch (error) {
      if (error instanceof Error && /unauthor|forbidden/i.test(error.message)) {
        router.replace("/");
        return;
      }

      setMessage(error instanceof Error ? error.message : "The service is unavailable.");
    }
  }, [router]);

  useEffect(() => {
    const loadTimer = window.setTimeout(() => {
      void loadSetup();
    }, 0);

    return () => window.clearTimeout(loadTimer);
  }, [loadSetup]);

  async function submitAccount(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const form = event.currentTarget;
    const formData = new FormData(form);

    try {
      const createdAccount = await request<CreatedAccount>("/api/admin/users", {
        body: JSON.stringify({
          email: String(formData.get("email") ?? "") || null,
          fullName: String(formData.get("fullName") ?? ""),
          institutionalId: String(formData.get("institutionalId") ?? ""),
          role: String(formData.get("role") ?? ""),
        }),
        method: "POST",
      });
      setTemporaryCredential(createdAccount);
      form.reset();
      await loadSetup();
      setMessage("Account created. Share the temporary password privately, then remove it from view.");
    } catch (error) {
      setMessage(error instanceof Error ? error.message : "The account could not be created.");
    }
  }

  async function submitAccountUpdate(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!selectedAccount) {
      return;
    }

    const formData = new FormData(event.currentTarget);

    try {
      await request<Account>("/api/admin/users/" + encodeURIComponent(selectedAccount.institutionalId), {
        body: JSON.stringify({
          email: String(formData.get("email") ?? "") || null,
          fullName: String(formData.get("fullName") ?? ""),
          institutionalId: String(formData.get("institutionalId") ?? ""),
        }),
        method: "PUT",
      });
      await loadSetup();
      setMessage("Account profile updated. Its role remains unchanged.");
    } catch (error) {
      setMessage(error instanceof Error ? error.message : "The account could not be updated.");
    }
  }

  async function updateActivation(account: Account) {
    try {
      await request<void>("/api/admin/users/" + encodeURIComponent(account.institutionalId) + "/activation", {
        body: JSON.stringify({ isActive: !account.isActive }),
        method: "PUT",
      });
      await loadSetup();
      setMessage(account.isActive ? "Account deactivated." : "Account reactivated.");
    } catch (error) {
      setMessage(error instanceof Error ? error.message : "The account status could not be updated.");
    }
  }

  async function submitAcademicRecord(event: FormEvent<HTMLFormElement>, path: string, label: string) {
    event.preventDefault();
    const form = event.currentTarget;
    const formData = new FormData(form);

    try {
      await request<AcademicRecord>(path, {
        body: JSON.stringify({
          code: String(formData.get("code") ?? ""),
          name: String(formData.get("name") ?? ""),
        }),
        method: "POST",
      });
      form.reset();
      await loadSetup();
      setMessage(label + " created.");
    } catch (error) {
      setMessage(error instanceof Error ? error.message : label + " could not be created.");
    }
  }

  async function submitAcademicUpdate(event: FormEvent<HTMLFormElement>, path: string, label: string) {
    event.preventDefault();
    const form = new FormData(event.currentTarget);
    const recordId = String(form.get("recordId") ?? "");

    try {
      await request<AcademicRecord>(path + "/" + recordId, {
        body: JSON.stringify({
          code: String(form.get("code") ?? ""),
          name: String(form.get("name") ?? ""),
        }),
        method: "PUT",
      });
      await loadSetup();
      setMessage(label + " updated.");
    } catch (error) {
      setMessage(error instanceof Error ? error.message : label + " could not be updated.");
    }
  }

  async function archiveAcademicRecord(path: string, id: string, label: string) {
    try {
      await request<void>(path + "/" + id + "/archive", { method: "POST" });
      await loadSetup();
      setMessage(label + " archived. Historical records remain available.");
    } catch (error) {
      setMessage(error instanceof Error ? error.message : label + " could not be archived.");
    }
  }

  async function submitEnrollment(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const form = new FormData(event.currentTarget);

    try {
      await request("/api/admin/enrollments", {
        body: JSON.stringify({
          classCourseId: String(form.get("classCourseId") ?? ""),
          studentInstitutionalId: String(form.get("studentInstitutionalId") ?? ""),
        }),
        method: "POST",
      });
      setMessage("Student enrollment created.");
    } catch (error) {
      setMessage(error instanceof Error ? error.message : "The enrollment could not be created.");
    }
  }

  async function submitResponsibility(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const form = new FormData(event.currentTarget);

    try {
      await request("/api/admin/teacher-responsibilities", {
        body: JSON.stringify({
          classCourseId: String(form.get("classCourseId") ?? ""),
          subjectId: String(form.get("subjectId") ?? ""),
          teacherInstitutionalId: String(form.get("teacherInstitutionalId") ?? ""),
        }),
        method: "POST",
      });
      setMessage("Teacher responsibility created.");
    } catch (error) {
      setMessage(error instanceof Error ? error.message : "The teacher responsibility could not be created.");
    }
  }

  return (
    <main className="admin-console" data-account-action={accountAction} data-section={activeSection}>
      <header className="admin-console__header">
        <div>
          <p className="login-card__eyebrow">Administration</p>
          <h1>{activeSection === "overview" ? "Admin dashboard" : activeSection === "accounts" ? "Account management" : activeSection === "academic" ? "Academic structure" : activeSection === "enrollment" ? "Student enrollments" : "Teacher responsibilities"}</h1>
          <p>Use the navigation to focus on one administration workflow at a time.</p>
        </div>
        <button className="admin-console__refresh" onClick={() => void loadSetup()} type="button">Refresh</button>
      </header>

      <p aria-live="polite" className="admin-console__message">{message}</p>

      {temporaryCredential && (
        <section className="credential-notice" aria-live="assertive">
          <div>
            <p className="login-card__eyebrow">Show once</p>
            <strong>{temporaryCredential.institutionalId} · {temporaryCredential.role}</strong>
            <code>{temporaryCredential.temporaryPassword}</code>
            <p>Share this password through your institution’s private channel. It is not stored in this screen after dismissal.</p>
          </div>
          <button onClick={() => setTemporaryCredential(null)} type="button">Dismiss</button>
        </section>
      )}

      {activeSection === "accounts" && (
        <div className="account-actions-wrap">
          <p className="account-actions__intro">Choose an account task to continue.</p>
          <div aria-label="Account actions" className="account-actions">
            <button className={accountAction === "create" ? "is-active" : ""} onClick={() => setAccountAction("create")} type="button">Create account</button>
            <button className={accountAction === "manage" ? "is-active" : ""} onClick={() => setAccountAction("manage")} type="button">Manage accounts</button>
            <button className={accountAction === "update" ? "is-active" : ""} onClick={() => setAccountAction("update")} type="button">Update account</button>
          </div>
        </div>
      )}

      <section className="admin-console__grid">
        <article className="admin-panel admin-panel--overview">
          <h2>Institution overview</h2>
          <div className="overview-metrics">
            <p><strong>{accounts.length}</strong><span>Accounts</span></p>
            <p><strong>{availableClassCourses.length}</strong><span>Active classes/courses</span></p>
            <p><strong>{availableSubjects.length}</strong><span>Active subjects</span></p>
            <p><strong>{activeTeachers.length}</strong><span>Active teachers</span></p>
          </div>
          <p className="overview-note">Set up accounts first, then academic structure, enrollment, and teacher scope.</p>
        </article>
        <article className="admin-panel admin-panel--accounts account-panel--create">
          <h2>Create account</h2>
          <form onSubmit={submitAccount}>
            <label>Full name<input name="fullName" required /></label>
            <label>Institutional ID<input name="institutionalId" required /></label>
            <label>Contact email <span>optional</span><input name="email" type="email" /></label>
            <label>Role<select defaultValue="Student" name="role"><option>Student</option><option>Teacher</option></select></label>
            <button type="submit">Create account</button>
          </form>
        </article>

        <article className="admin-panel admin-panel--academic">
          <h2>Create class/course</h2>
          <form onSubmit={(event) => void submitAcademicRecord(event, "/api/admin/classes-courses", "Class/Course")}>
            <label>Name<input name="name" required /></label>
            <label>Code<input name="code" required /></label>
            <button type="submit">Add class/course</button>
          </form>
          <div className="admin-list">{classCourses.map((classCourse) => <p key={classCourse.id}><b>{classCourse.code}</b> {classCourse.name}{classCourse.isArchived ? " · archived" : ""}{!classCourse.isArchived && <button onClick={() => void archiveAcademicRecord("/api/admin/classes-courses", classCourse.id, "Class/Course")} type="button">Archive</button>}</p>)}</div>
          {selectedClassCourse && <form className="record-edit" key={selectedClassCourse.id} onSubmit={(event) => void submitAcademicUpdate(event, "/api/admin/classes-courses", "Class/Course")}>
            <input name="recordId" type="hidden" value={selectedClassCourse.id} />
            <label>Manage class/course<select onChange={(event) => setSelectedClassCourseId(event.target.value)} value={selectedClassCourseId}>{classCourses.map((classCourse) => <option key={classCourse.id} value={classCourse.id}>{classCourse.name} · {classCourse.code}</option>)}</select></label>
            <label>Name<input defaultValue={selectedClassCourse.name} name="name" required /></label>
            <label>Code<input defaultValue={selectedClassCourse.code} name="code" required /></label>
            <button disabled={selectedClassCourse.isArchived} type="submit">Save class/course</button>
          </form>}
        </article>

        <article className="admin-panel admin-panel--academic">
          <h2>Create subject</h2>
          <form onSubmit={(event) => void submitAcademicRecord(event, "/api/admin/subjects", "Subject")}>
            <label>Name<input name="name" required /></label>
            <label>Code<input name="code" required /></label>
            <button type="submit">Add subject</button>
          </form>
          <div className="admin-list">{subjects.map((subject) => <p key={subject.id}><b>{subject.code}</b> {subject.name}{subject.isArchived ? " · archived" : ""}{!subject.isArchived && <button onClick={() => void archiveAcademicRecord("/api/admin/subjects", subject.id, "Subject")} type="button">Archive</button>}</p>)}</div>
          {selectedSubject && <form className="record-edit" key={selectedSubject.id} onSubmit={(event) => void submitAcademicUpdate(event, "/api/admin/subjects", "Subject")}>
            <input name="recordId" type="hidden" value={selectedSubject.id} />
            <label>Manage subject<select onChange={(event) => setSelectedSubjectId(event.target.value)} value={selectedSubjectId}>{subjects.map((subject) => <option key={subject.id} value={subject.id}>{subject.name} · {subject.code}</option>)}</select></label>
            <label>Name<input defaultValue={selectedSubject.name} name="name" required /></label>
            <label>Code<input defaultValue={selectedSubject.code} name="code" required /></label>
            <button disabled={selectedSubject.isArchived} type="submit">Save subject</button>
          </form>}
        </article>

        <article className="admin-panel admin-panel--enrollment">
          <h2>Enroll student</h2>
          <form onSubmit={submitEnrollment}>
            <label>Student<select name="studentInstitutionalId" required>{activeStudents.map((student) => <option key={student.id} value={student.institutionalId}>{student.fullName} · {student.institutionalId}</option>)}</select></label>
            <label>Class/course<select name="classCourseId" required>{availableClassCourses.map((classCourse) => <option key={classCourse.id} value={classCourse.id}>{classCourse.name} · {classCourse.code}</option>)}</select></label>
            <button disabled={activeStudents.length === 0 || availableClassCourses.length === 0} type="submit">Enroll student</button>
          </form>
        </article>

        <article className="admin-panel admin-panel--responsibilities">
          <h2>Assign teacher</h2>
          <form onSubmit={submitResponsibility}>
            <label>Teacher<select name="teacherInstitutionalId" required>{activeTeachers.map((teacher) => <option key={teacher.id} value={teacher.institutionalId}>{teacher.fullName} · {teacher.institutionalId}</option>)}</select></label>
            <label>Class/course<select name="classCourseId" required>{availableClassCourses.map((classCourse) => <option key={classCourse.id} value={classCourse.id}>{classCourse.name} · {classCourse.code}</option>)}</select></label>
            <label>Subject<select name="subjectId" required>{availableSubjects.map((subject) => <option key={subject.id} value={subject.id}>{subject.name} · {subject.code}</option>)}</select></label>
            <button disabled={activeTeachers.length === 0 || availableClassCourses.length === 0 || availableSubjects.length === 0} type="submit">Assign teacher</button>
          </form>
        </article>

        <article className="admin-panel admin-panel--accounts admin-panel--wide account-panel--manage">
          <h2>Manage accounts</h2>
          <div className="account-table">{accounts.map((account) => <div className="account-row" key={account.id}><div><strong>{account.fullName}</strong><span>{account.institutionalId} · {account.role}</span></div><div><span className={account.isActive ? "status status--active" : "status"}>{account.isActive ? "Active" : "Inactive"}</span><button onClick={() => void updateActivation(account)} type="button">{account.isActive ? "Deactivate" : "Reactivate"}</button></div></div>)}</div>
        </article>

        <article className="admin-panel admin-panel--accounts admin-panel--wide account-panel--update">
          <h2>Update account</h2>
          <p className="account-panel__hint">Roles are fixed after account creation. An institutional ID must remain unique.</p>
          {selectedAccount && <form className="account-edit" key={selectedAccount.id} onSubmit={submitAccountUpdate}>
            <label>Account<select onChange={(event) => setSelectedAccountId(event.target.value)} value={selectedAccountId}>{accounts.map((account) => <option key={account.id} value={account.id}>{account.fullName} · {account.institutionalId}</option>)}</select></label>
            <label>Full name<input defaultValue={selectedAccount.fullName} name="fullName" required /></label>
            <label>Institutional ID<input defaultValue={selectedAccount.institutionalId} name="institutionalId" required /></label>
            <label>Contact email<input defaultValue={selectedAccount.email ?? ""} name="email" type="email" /></label>
            <button type="submit">Save profile</button>
          </form>}
        </article>
      </section>
    </main>
  );
}
