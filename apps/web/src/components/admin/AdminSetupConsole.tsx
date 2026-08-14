"use client";

import type { DashboardSection } from "@/components/app-shell";
import {
  academicStructureActions,
  type AcademicStructureAction,
  getAcademicStructurePanelVisibility,
} from "@/components/admin/academic-structure-actions";
import { useRouter } from "next/navigation";
import { type FormEvent, useCallback, useEffect, useMemo, useState } from "react";
import { browserApiBaseUrl } from "@/lib/api-routing";

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
type Submission = { id: string; status: string; studentName: string; submittedAt: string; };

type CreatedAccount = {
  institutionalId: string;
  role: string;
  temporaryPassword: string;
};

async function request<T>(path: string, options?: RequestInit): Promise<T> {
  const response = await fetch(browserApiBaseUrl + path, {
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
  const [submissions, setSubmissions] = useState<Submission[]>([]);
  const [accountSearch, setAccountSearch] = useState("");
  const [academicSearch, setAcademicSearch] = useState("");
  const [submissionSearch, setSubmissionSearch] = useState("");
  const [academicClasses, setAcademicClasses] = useState<AcademicRecord[]>([]);
  const [subjects, setSubjects] = useState<AcademicRecord[]>([]);
  const [message, setMessage] = useState("Loading academic setup…");
  const [temporaryCredential, setTemporaryCredential] = useState<CreatedAccount | null>(null);
  const [accountAction, setAccountAction] = useState<AccountAction>("manage");
  const [academicAction, setAcademicAction] = useState<AcademicStructureAction | null>(null);
  const [selectedAccountId, setSelectedAccountId] = useState("");
  const [selectedAcademicClassId, setSelectedAcademicClassId] = useState("");
  const [selectedSubjectId, setSelectedSubjectId] = useState("");

  const selectedAccount = useMemo(
    () => accounts.find((account) => account.id === selectedAccountId),
    [accounts, selectedAccountId],
  );
  const selectedAcademicClass = useMemo(
    () => academicClasses.find((academicClass) => academicClass.id === selectedAcademicClassId),
    [academicClasses, selectedAcademicClassId],
  );
  const selectedSubject = useMemo(
    () => subjects.find((subject) => subject.id === selectedSubjectId),
    [subjects, selectedSubjectId],
  );

  const normalizedAccountSearch = accountSearch.trim().toLocaleLowerCase();
  const normalizedAcademicSearch = academicSearch.trim().toLocaleLowerCase();
  const normalizedSubmissionSearch = submissionSearch.trim().toLocaleLowerCase();
  const filteredAccounts = accounts.filter((account) => (account.fullName + " " + account.institutionalId + " " + account.role).toLocaleLowerCase().includes(normalizedAccountSearch));
  const filteredAcademicClasses = academicClasses.filter((record) => (record.name + " " + record.code).toLocaleLowerCase().includes(normalizedAcademicSearch));
  const filteredSubjects = subjects.filter((record) => (record.name + " " + record.code).toLocaleLowerCase().includes(normalizedAcademicSearch));
  const filteredSubmissions = submissions.filter((submission) => (submission.studentName + " " + submission.status).toLocaleLowerCase().includes(normalizedSubmissionSearch));
  const activeStudents = accounts.filter((account) => account.isActive && account.role === "Student");
  const activeTeachers = accounts.filter((account) => account.isActive && account.role === "Teacher");
  const availableAcademicClasses = academicClasses.filter((academicClass) => !academicClass.isArchived);
  const availableSubjects = subjects.filter((subject) => !subject.isArchived);
  const academicPanelVisibility = getAcademicStructurePanelVisibility(academicAction);

  const loadSetup = useCallback(async () => {
    try {
      const [loadedAccounts, loadedAcademicClasses, loadedSubjects, loadedSubmissions] = await Promise.all([
        request<Account[]>("/api/admin/users"),
        request<AcademicRecord[]>("/api/admin/classes"),
        request<AcademicRecord[]>("/api/admin/subjects"),
        request<Submission[]>("/api/admin/submissions"),
      ]);
      setAccounts(loadedAccounts);
      setAcademicClasses(loadedAcademicClasses);
      setSubjects(loadedSubjects);
      setSubmissions(loadedSubmissions);
      setSelectedAccountId((currentId) => currentId || loadedAccounts[0]?.id || "");
      setSelectedAcademicClassId((currentId) => currentId || loadedAcademicClasses[0]?.id || "");
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
          academicClassId: String(form.get("academicClassId") ?? ""),
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
          academicClassId: String(form.get("academicClassId") ?? ""),
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
    <main className="admin-console" data-section={activeSection}>
      <header className="admin-console__header">
        <div>
          <p className="login-card__eyebrow">Administration</p>
          <h1>{activeSection === "overview" ? "Admin dashboard" : activeSection === "accounts" ? "Account management" : activeSection === "academic" ? "Academic structure" : activeSection === "enrollment" ? "Student enrollments" : activeSection === "responsibilities" ? "Teacher responsibilities" : "Submission records"}</h1>
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
        <div className="workflow-actions">
          <p className="workflow-actions__intro">Choose an account task to continue.</p>
          <div aria-label="Account actions" className="workflow-actions__buttons">
            <button className={accountAction === "create" ? "is-active" : ""} onClick={() => setAccountAction("create")} type="button">Create account</button>
            <button className={accountAction === "manage" ? "is-active" : ""} onClick={() => setAccountAction("manage")} type="button">Manage accounts</button>
            <button className={accountAction === "update" ? "is-active" : ""} onClick={() => setAccountAction("update")} type="button">Update account</button>
          </div>
        </div>
      )}

      {activeSection === "academic" && (
        <div className="workflow-actions">
          <p className="workflow-actions__intro">Choose an academic structure task to continue.</p>
          <div aria-label="Academic structure actions" className="workflow-actions__buttons">
            {academicStructureActions.map((action) => (
              <button
                className={academicAction === action.id ? "is-active" : ""}
                key={action.id}
                onClick={() => setAcademicAction(action.id)}
                type="button"
              >
                {action.label}
              </button>
            ))}
          </div>
        </div>
      )}

      <section className="admin-console__grid">
        <article className="admin-panel admin-panel--overview">
          <h2>Institution overview</h2>
          <div className="overview-metrics">
            <p><strong>{accounts.length}</strong><span>Accounts</span></p>
            <p><strong>{availableAcademicClasses.length}</strong><span>Active classes</span></p>
            <p><strong>{availableSubjects.length}</strong><span>Active subjects</span></p>
            <p><strong>{activeTeachers.length}</strong><span>Active teachers</span></p>
          </div>
          <p className="overview-note">Set up accounts first, then academic structure, enrollment, and teacher scope.</p>
        </article>
        {accountAction === "create" && <article className="admin-panel admin-panel--accounts">
          <h2>Create account</h2>
          <form onSubmit={submitAccount}>
            <label>Full name<input name="fullName" required /></label>
            <label>Institutional ID<input name="institutionalId" required /></label>
            <label>Contact email <span>optional</span><input name="email" type="email" /></label>
            <label>Role<select defaultValue="Student" name="role"><option>Student</option><option>Teacher</option></select></label>
            <button type="submit">Create account</button>
          </form>
        </article>}

        {academicPanelVisibility.create && <article className="admin-panel admin-panel--academic">
          <h2>Create class</h2>
          <form onSubmit={(event) => void submitAcademicRecord(event, "/api/admin/classes", "Class")}>
            <label>Name<input name="name" required /></label>
            <label>Code<input name="code" required /></label>
            <button type="submit">Add class</button>
          </form>
        </article>}

        {academicPanelVisibility.create && <article className="admin-panel admin-panel--academic">
          <h2>Create subject</h2>
          <form onSubmit={(event) => void submitAcademicRecord(event, "/api/admin/subjects", "Subject")}>
            <label>Name<input name="name" required /></label>
            <label>Code<input name="code" required /></label>
            <button type="submit">Add subject</button>
          </form>
        </article>}

        {academicPanelVisibility.manage && <article className="admin-panel admin-panel--academic">
          <h2>Manage classes</h2>
          <label className="workspace-filter">Search academic records<input aria-label="Search academic records" onChange={(event) => setAcademicSearch(event.target.value)} placeholder="Search name or code" value={academicSearch} /></label><div className="admin-list">{filteredAcademicClasses.map((academicClass) => <p key={academicClass.id}><b>{academicClass.code}</b> {academicClass.name}{academicClass.isArchived ? " · archived" : ""}{!academicClass.isArchived && <button onClick={() => void archiveAcademicRecord("/api/admin/classes", academicClass.id, "Class")} type="button">Archive</button>}</p>)}</div>
        </article>}

        {academicPanelVisibility.manage && <article className="admin-panel admin-panel--academic">
          <h2>Manage subjects</h2>
          <div className="admin-list">{filteredSubjects.map((subject) => <p key={subject.id}><b>{subject.code}</b> {subject.name}{subject.isArchived ? " · archived" : ""}{!subject.isArchived && <button onClick={() => void archiveAcademicRecord("/api/admin/subjects", subject.id, "Subject")} type="button">Archive</button>}</p>)}</div>
        </article>}

        {academicPanelVisibility.update && <article className="admin-panel admin-panel--academic">
          <h2>Update class</h2>
          {selectedAcademicClass && <form key={selectedAcademicClass.id} onSubmit={(event) => void submitAcademicUpdate(event, "/api/admin/classes", "Class")}>
            <input name="recordId" type="hidden" value={selectedAcademicClass.id} />
            <label>Class<select onChange={(event) => setSelectedAcademicClassId(event.target.value)} value={selectedAcademicClassId}>{academicClasses.map((academicClass) => <option key={academicClass.id} value={academicClass.id}>{academicClass.name} · {academicClass.code}</option>)}</select></label>
            <label>Name<input defaultValue={selectedAcademicClass.name} name="name" required /></label>
            <label>Code<input defaultValue={selectedAcademicClass.code} name="code" required /></label>
            <button disabled={selectedAcademicClass.isArchived} type="submit">Save class</button>
          </form>}
        </article>}

        {academicPanelVisibility.update && <article className="admin-panel admin-panel--academic">
          <h2>Update subject</h2>
          {selectedSubject && <form key={selectedSubject.id} onSubmit={(event) => void submitAcademicUpdate(event, "/api/admin/subjects", "Subject")}>
            <input name="recordId" type="hidden" value={selectedSubject.id} />
            <label>Subject<select onChange={(event) => setSelectedSubjectId(event.target.value)} value={selectedSubjectId}>{subjects.map((subject) => <option key={subject.id} value={subject.id}>{subject.name} · {subject.code}</option>)}</select></label>
            <label>Name<input defaultValue={selectedSubject.name} name="name" required /></label>
            <label>Code<input defaultValue={selectedSubject.code} name="code" required /></label>
            <button disabled={selectedSubject.isArchived} type="submit">Save subject</button>
          </form>}
        </article>}

        <article className="admin-panel admin-panel--enrollment">
          <h2>Enroll student</h2>
          <form onSubmit={submitEnrollment}>
            <label>Student<select name="studentInstitutionalId" required>{activeStudents.map((student) => <option key={student.id} value={student.institutionalId}>{student.fullName} · {student.institutionalId}</option>)}</select></label>
            <label>Class<select name="academicClassId" required>{availableAcademicClasses.map((academicClass) => <option key={academicClass.id} value={academicClass.id}>{academicClass.name} · {academicClass.code}</option>)}</select></label>
            <button disabled={activeStudents.length === 0 || availableAcademicClasses.length === 0} type="submit">Enroll student</button>
          </form>
        </article>

        <article className="admin-panel admin-panel--responsibilities">
          <h2>Assign teacher</h2>
          <form onSubmit={submitResponsibility}>
            <label>Teacher<select name="teacherInstitutionalId" required>{activeTeachers.map((teacher) => <option key={teacher.id} value={teacher.institutionalId}>{teacher.fullName} · {teacher.institutionalId}</option>)}</select></label>
            <label>Class<select name="academicClassId" required>{availableAcademicClasses.map((academicClass) => <option key={academicClass.id} value={academicClass.id}>{academicClass.name} · {academicClass.code}</option>)}</select></label>
            <label>Subject<select name="subjectId" required>{availableSubjects.map((subject) => <option key={subject.id} value={subject.id}>{subject.name} · {subject.code}</option>)}</select></label>
            <button disabled={activeTeachers.length === 0 || availableAcademicClasses.length === 0 || availableSubjects.length === 0} type="submit">Assign teacher</button>
          </form>
        </article>

        {accountAction === "manage" && <article className="admin-panel admin-panel--accounts admin-panel--wide">
          <h2>Manage accounts</h2>
          <label className="workspace-filter">Search accounts<input aria-label="Search accounts" onChange={(event) => setAccountSearch(event.target.value)} placeholder="Search name, ID, or role" value={accountSearch} /></label><div className="account-table">{filteredAccounts.map((account) => <div className="account-row" key={account.id}><div><strong>{account.fullName}</strong><span>{account.institutionalId} · {account.role}</span></div><div><span className={account.isActive ? "status status--active" : "status"}>{account.isActive ? "Active" : "Inactive"}</span><button onClick={() => void updateActivation(account)} type="button">{account.isActive ? "Deactivate" : "Reactivate"}</button></div></div>)}</div>
        </article>}

        {activeSection === "submissions" && <article className="admin-panel admin-panel--submissions admin-panel--wide"><h2>Submission records</h2><p className="account-panel__hint">Search the records already available to administrators.</p><label className="workspace-filter">Search submissions<input aria-label="Search submissions" onChange={(event) => setSubmissionSearch(event.target.value)} placeholder="Search student or status" value={submissionSearch} /></label><div className="admin-list admin-submission-list">{filteredSubmissions.length === 0 ? <p>No matching submissions.</p> : filteredSubmissions.map((submission) => <article className="admin-submission-record" key={submission.id}><div className="admin-submission-record__identity"><strong>{submission.studentName}</strong><span>{submission.status}</span></div><time dateTime={submission.submittedAt}>Submitted {new Date(submission.submittedAt).toLocaleString()}</time></article>)}</div></article>}

        {accountAction === "update" && <article className="admin-panel admin-panel--accounts admin-panel--wide">
          <h2>Update account</h2>
          <p className="account-panel__hint">Roles are fixed after account creation. An institutional ID must remain unique.</p>
          {selectedAccount && <form className="account-edit" key={selectedAccount.id} onSubmit={submitAccountUpdate}>
            <label>Account<select onChange={(event) => setSelectedAccountId(event.target.value)} value={selectedAccountId}>{accounts.map((account) => <option key={account.id} value={account.id}>{account.fullName} · {account.institutionalId}</option>)}</select></label>
            <label>Full name<input defaultValue={selectedAccount.fullName} name="fullName" required /></label>
            <label>Institutional ID<input defaultValue={selectedAccount.institutionalId} name="institutionalId" required /></label>
            <label>Contact email<input defaultValue={selectedAccount.email ?? ""} name="email" type="email" /></label>
            <button type="submit">Save profile</button>
          </form>}
        </article>}
      </section>
    </main>
  );
}
