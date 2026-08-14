# Assignment Submission System

A role-based academic workflow application for administrators, teachers, and students. It supports institutional-ID sign-in, academic setup, assignment publishing, student submission, teacher review, grading, and post-deadline result disclosure.

## Run locally

The Docker setup is the recommended and complete local setup. It starts the Next.js frontend, ASP.NET Core API, PostgreSQL database, migrations, and demo data together. No `.env` file is needed for the default demo.

### Prerequisites

- Git
- Docker Engine with Docker Compose

### Start the application

```bash
git clone https://github.com/thetakibkhan/assignment-submission-system.git
cd assignment-submission-system
docker compose up
```

The first start downloads container images and restores packages, so it can take a few minutes. Keep this terminal open while using the application.

When the services are ready, open:

- Application: [http://localhost:3000](http://localhost:3000)
- API health check: [http://localhost:5112/health](http://localhost:5112/health)
- Swagger/OpenAPI: [http://localhost:5112/swagger](http://localhost:5112/swagger)

Use one of the [demo accounts](#demo-accounts) to sign in.

### Stop or reset

Stop the local application:

```bash
docker compose down
```

Resetting deletes this project’s local database volume and cannot be recovered:

```bash
docker compose down -v && docker compose up
```

## Demo accounts

These public evaluation fixtures are shown on the sign-in screen and are available in both the local setup and the hosted recruiter demo. Do not use this demo configuration for real institutional data.

| Role | Institutional ID | Password |
| --- | --- | --- |
| Administrator | `DEMO-ADM-001` | `Admin!nVPT7mB4lxeZBUq2XwI` |
| Teacher | `DEMO-TCH-001` | `Teacher!XQt7fpdAg1jQJ7fGgSw` |
| Student | `DEMO-STU-001` | `Student!v5lVJbrqfRO0m93zrKQ` |

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

`.env`, `appsettings.Development.json`, uploads, build output, and other local data are ignored by Git. The public demo values are intentionally used by the hosted recruiter demo. For a private or real-data deployment, disable demo data and provide deployment-specific database, administrator, and JWT configuration outside source control.

## Deploying to Render

The repository includes a free demonstration [Render Blueprint](render.yaml). It creates separate web and API services plus a free managed PostgreSQL database without requiring a paid persistent disk.

1. Push the committed code to GitHub, then create a new Blueprint in Render from this repository.
2. During Blueprint setup, provide these requested values:
   - NEXT_PUBLIC_API_BASE_URL: the public HTTPS URL of the API service, for example https://assignment-submission-system-api.onrender.com
   - Cors__AllowedOrigins__0: the public HTTPS URL of the web service, for example https://assignment-submission-system-web.onrender.com
   - Jwt__Issuer: the API URL or another stable production issuer identifier
   - Jwt__Audience: a stable identifier for the web client, such as assignment-submission-system-web
3. The Blueprint enables the public recruiter-demo accounts listed above and keeps bootstrap administration disabled. For a private deployment, set DemoData__Enabled=false, enable BootstrapAdmin temporarily, and provide its values only through Render environment variables.
4. After the API is healthy at /health, verify demo sign-in, upload/download, and role access through the deployed web URL.

The web service proxies browser requests under /api to the API service, so the HTTP-only SameSite=Strict authentication cookie remains on the web origin even though Render uses separate service domains. Render provides HTTPS for web services. On the free tier, attachments and ASP.NET Data Protection keys use the API service ephemeral filesystem and can disappear whenever Render restarts or redeploys that service. Free Render PostgreSQL databases also expire after 30 days. Use this configuration only for evaluation; switch the API to a paid persistent disk and use paid PostgreSQL before storing real institutional data.

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

Run the complete backend unit and integration suite with its isolated PostgreSQL database:

```bash
docker compose --profile test run --rm test
```

The test database is not exposed to the host. Remove only that temporary database after testing if you do not need it again:

```bash
docker compose --profile test rm -sf test-database
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
- A submission accepts up to five local attachments: 10 MB per file and 25 MB combined. Accepted types are PDF, DOC, DOCX, TXT, PNG, JPG, and JPEG.
- Historical records are retained. This project intentionally does not include email/SMS, real-time delivery, reporting, or exports.
