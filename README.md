# Assignment Submission System

A role-based academic workflow application for administrators, teachers, and students. It supports institutional-ID sign-in, academic setup, assignment publishing, student submission, teacher review, grading, and post-deadline result disclosure.

## Evaluator quick start

Prerequisite: Docker Engine with Docker Compose.

```bash
docker compose up
```

Open [http://localhost:3000](http://localhost:3000). The frontend, API, PostgreSQL database, migrations, and staged demo data start automatically. First startup downloads container images and restores packages, so it can take a few minutes.

The API is available at `http://localhost:5112`; Swagger is at `http://localhost:5112/swagger` and readiness is at `http://localhost:5112/health`.

Stop the local application with:

```bash
docker compose down
```

Resetting deletes this project's local database volume and cannot be recovered:

```bash
docker compose down -v && docker compose up
```

## Demo accounts

These are public local-evaluation fixtures only. They are shown on the sign-in screen, and are not production credentials.

| Role | Institutional ID | Password |
| --- | --- | --- |
| Administrator | `ADM-001` | `Admin!nVPT7mB4lxeZBUq2XwI` |
| Teacher | `TCH-001` | `Teacher!XQt7fpdAg1jQJ7fGgSw` |
| Student | `STU-001` | `Student!v5lVJbrqfRO0m93zrKQ` |

## Demonstration path

The Development/demo seed is idempotent: it adds missing fixtures but never overwrites evaluator-created records.

1. Sign in as the Teacher to see a complete Draft, an open Published assignment, and submitted work ready for review.
2. Sign in as the Student to submit work for **Argument essay**.
3. Return as the Teacher to review and grade that submission.
4. Sign in as the Student to see the existing **Historical reading response** result. Its deadline is already past, so marks and feedback are visible.

Notifications are created for assignment publication and grading. They are stored in-app, can be marked read, and do not send email, SMS, or push messages.

## Technology

- Next.js, React, TypeScript, Tailwind CSS
- ASP.NET Core 10, C#, Swagger/OpenAPI
- PostgreSQL and Entity Framework Core migrations
- ASP.NET Identity, role authorization, HTTP-only JWT cookie
- Docker Compose

## Project structure

```text
apps/api/       ASP.NET Core API, authentication, endpoints, Swagger
apps/web/       Next.js frontend
src/            Domain, application, infrastructure, persistence
tests/          Domain and API integration tests
docker-compose.yml
```

## API and Swagger

Swagger is enabled in Development at `/swagger`. Use `POST /api/auth/login` with an institutional ID and password first. Swagger and the API share the same origin, so the returned HTTP-only cookie is used by subsequent authorized calls in that browser session.

Roles are enforced server-side:

- Admin: account and academic management, all submission records, admin dashboard.
- Teacher: own assignments, own teaching scope, review queues, and grading.
- Student: assignments from active enrollment, own submissions and results.

Unauthenticated requests receive `401`; authenticated users without the required role receive `403`. State-changing endpoints validate their request data and authorization at the API boundary.

## Configuration

`.env.example` contains only public local demo values. Copy it only when you want to customize local ports or demo fixture values:

```bash
cp .env.example .env
```

`.env`, `appsettings.Development.json`, uploads, build output, and other local data are ignored by Git. Never use the demo values in a deployed environment. For a non-demo environment, set `DEMO_DATA_ENABLED=false` and provide deployment-specific database and JWT configuration outside source control.

## Manual development commands

The Docker path is recommended. If the .NET SDK 10, Node.js, and PostgreSQL are already installed locally:

```bash
cd apps/web && npm ci && npm run dev
```

Configure the API with an ignored `apps/api/AssignmentSubmissionSystem.Api/appsettings.Development.json`, based on `appsettings.Development.example.json`, then run:

```bash
dotnet run --project apps/api/AssignmentSubmissionSystem.Api
```

## Verification

The complete backend test suite needs the test PostgreSQL service named `auth-test-db`, as used by the existing integration-test configuration. From the supplied evaluation environment, run:

```bash
dotnet test AssignmentSubmissionSystem.slnx
```

Frontend checks:

```bash
cd apps/web && npm run lint && npm run build
```

## Important rules and limitations

- Users sign in with their institutional ID. Administrators create and manage accounts; contact email is optional.
- The backend is the authorization boundary. UI visibility does not grant access.
- Deadlines are evaluated on the server in UTC. Late submission and updates are blocked.
- Marks and feedback are disclosed only when the submission is **Graded** and the assignment deadline has passed.
- A submission accepts one optional local attachment up to 10 MB: PDF, DOC, DOCX, TXT, PNG, JPG, or JPEG.
- Historical records are retained. This project intentionally does not include email/SMS, real-time delivery, reporting, exports, or deployment infrastructure.
