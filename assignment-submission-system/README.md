# Assignment &amp; Submission Management System

A role-based web application for a school or college. Administrators manage the institution,
teachers set and mark work, students submit answers and see their marks.

Built for the OnnoRokom Projukti Ltd. Assistant Software Engineer recruitment project.

**Repository:** `<add your Git repository URL here before submitting>`

---

## Contents

- [Project description](#project-description)
- [Features by role](#features-by-role)
- [Tech stack](#tech-stack)
- [Project structure](#project-structure)
- [Prerequisites](#prerequisites)
- [Quick start with Docker](#quick-start-with-docker)
- [Local setup](#local-setup)
- [Database setup](#database-setup)
- [Running the tests](#running-the-tests)
- [Demo credentials](#demo-credentials)
- [API summary](#api-summary)
- [Business rules](#business-rules)
- [Configuration](#configuration)
- [Assumptions](#assumptions)
- [Known limitations](#known-limitations)

---

## Project description

A role-based assignment and submission system for a school or college, built as one full-stack
application: a Next.js frontend, an ASP.NET Core Web API and a PostgreSQL database.

It follows the everyday flow of coursework:

1. An **administrator** sets up classes and subjects, creates accounts, and maps each teacher to
   the subjects they teach in each class.
2. A **teacher** creates an assignment for one of their class/subject pairs, saves it as a draft,
   then publishes it when it is ready. They see what comes in, award marks and write feedback.
3. A **student** sees the published assignments for their own class, submits an answer, updates it
   while that is still allowed, and reads their marks and feedback.

### Approach

The backend is layered: `Domain` (entities only, no dependencies) → `Application` (business rules,
DTOs, interfaces) → `Infrastructure` (EF Core, JWT, password hashing) → `Api` (controllers,
middleware). Each rule lives in exactly one place in the Application layer, which is what makes it
possible to unit test the rules without a web server or a real database.

Authorisation is decided by the API, never by the caller. Roles come from JWT claims and are
checked by policy on every endpoint, then each service scopes its own queries: a student's list is
filtered to their class, a teacher's to the work they created, an administrator sees everything but
cannot mark. A teacher's right to set work comes from a `ClassSubjectTeacher` row mapping them to a
class *and* a subject — without that row the request is refused, whatever the request body says.
The frontend mirrors the same rules so the UI does not offer actions the server would reject, but
that is a convenience only; nothing from the browser is trusted.

State changes are guarded rather than free-form. An assignment moves Draft → Published → Closed,
and each transition has preconditions (publishing needs a future deadline and a class with students
in it). Destructive operations are refused once academic history exists: an assignment with
submissions cannot be deleted or re-targeted, marks cannot be lowered below what was already
awarded, and an account with work attached can only be deactivated. Failures come back as RFC 7807
problem details with a status that matches the reason (403 for "not yours", 404 for a draft a
student must not know about, 422 for a business rule), so the frontend can say something useful
instead of "something went wrong".

### Features at a glance

| Role | What they can do |
| --- | --- |
| **Admin** | Manage users, classes and subjects; map teachers to class/subject pairs; read-only oversight of every assignment and submission; institution-wide settings; dashboard |
| **Teacher** | Create, edit, publish, close and delete assignments; see the submissions for their own work; award marks and feedback; move submissions through the review workflow |
| **Student** | See the published assignments for their class; submit an answer; update it while that is allowed; read status, marks and feedback |

Shared across the three portals: JWT sign in with role-based routing, paging, filtering and search
on every list, form validation on both client and server, a responsive layout, Swagger
documentation, 70+ unit tests, and a one-command Docker Compose setup.

### Setup in brief

```bash
cp .env.example .env      # then set JWT_KEY
docker compose up --build
```

Frontend on http://localhost:3000, API and Swagger on http://localhost:5080. The API creates the
schema and seeds the demo data on first start, so there is nothing to run by hand — sign in with
`admin@school.edu` / `Admin@123`.

Without Docker: run `dotnet run --project src/AssignmentSystem.Api` from `backend/`, and
`npm install && npm run dev` from `frontend/` in a second terminal, against a local PostgreSQL
database called `assignment_db`. Step by step instructions are in [Local setup](#local-setup).

## Features by role

### Administrator

- Create, edit, activate/deactivate and delete accounts for all three roles
- Manage classes (name, code, academic year) and subjects
- Map a teacher to a subject in a class — the mapping that grants a teacher permission to set work
- Read-only oversight of every assignment and every submission, with filters and search
- Institution-wide switches: allow late submissions globally, maximum answer length, institution
  name, and a master switch for student sign in
- Dashboard with counts of accounts, assignments and marking workload

### Teacher

- Create assignments for the class/subject pairs assigned to them: title, instructions, deadline,
  maximum marks, whether late work is accepted, whether answers may be updated
- Save as draft, publish when ready, close when the work is finished
- Edit an assignment while it is still open; delete it while nobody has submitted
- See every submission for their assignments, including who is late and who has revised
- Award marks with feedback, or move a submission through the review workflow
  (under review, returned for revision, rejected)

### Student

- See the published assignments for their own class, with deadlines and a countdown
- Read the full instructions, marks available, and whether late work is accepted
- Submit an answer with an optional attachment link
- Update the answer while the assignment allows it and it has not been graded
- See status, marks and teacher feedback for everything they submitted

## Tech stack

| Layer | Choice |
| --- | --- |
| Frontend | Next.js 14 (App Router), React 18, TypeScript, Tailwind CSS, Zod |
| Backend | ASP.NET Core 8 Web API, C# 12 |
| Data | PostgreSQL 16, Entity Framework Core 8 (Npgsql) |
| Auth | JWT bearer tokens, role-based policies |
| Docs | Swagger / OpenAPI with a bearer-token button |
| Tests | xUnit, FluentAssertions, EF Core in-memory provider |
| Container | Docker, Docker Compose |

No paid or restrictively licensed packages: FluentAssertions is pinned to 6.x (Apache-2.0), and
password hashing uses the .NET base class library rather than a third-party crypto package.

## Project structure

```
assignment-submission-system/
├── backend/
│   ├── AssignmentSystem.sln
│   ├── src/
│   │   ├── AssignmentSystem.Domain/          entities, enums, no dependencies
│   │   ├── AssignmentSystem.Application/     services, DTOs, business rules
│   │   │   ├── Common/                       exceptions, paging, interfaces
│   │   │   ├── Contracts/                    request and response records
│   │   │   └── Services/                     Auth, User, Academic, Assignment, Submission, Settings
│   │   ├── AssignmentSystem.Infrastructure/  EF Core, JWT, PBKDF2 hashing
│   │   │   ├── Persistence/                  DbContext, configurations, migrations, seeder
│   │   │   └── Security/                     token generation, current user, hashing
│   │   └── AssignmentSystem.Api/             controllers, middleware, Swagger, DI
│   └── tests/
│       └── AssignmentSystem.UnitTests/       70+ tests over the business rules
├── frontend/
│   ├── src/
│   │   ├── app/                              App Router pages
│   │   │   ├── login/                        sign in
│   │   │   ├── admin/                        overview, users, classes, subjects,
│   │   │   │                                 teacher mapping, assignments, submissions, settings
│   │   │   ├── teacher/                      assignments, create, detail + grading, submissions
│   │   │   └── student/                      assignments, assignment detail + submit, submissions
│   │   ├── components/                       UI primitives, app shell, assignment form
│   │   ├── lib/                              API client, auth, formatting, hooks, types
│   │   └── middleware.ts                     route guard
│   └── Dockerfile
├── database/
│   ├── schema.sql                            full DDL, equivalent to the EF migration
│   ├── seed.sql                              demo data
│   └── README.md
├── docs/
│   ├── API.md                                endpoint reference
│   ├── ASSUMPTIONS.md                        design decisions
│   └── er-diagram.md                         ER diagram and constraints
├── docker-compose.yml
├── .env.example
└── README.md
```

## Prerequisites

For the Docker route: Docker 24+ with Compose v2. Nothing else.

For a local setup:

- [.NET SDK 8.0](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Node.js 18.17+](https://nodejs.org) (20 LTS recommended) and npm
- [PostgreSQL 14+](https://www.postgresql.org/download/) running locally

## Quick start with Docker

```bash
git clone <your-repo-url>
cd assignment-submission-system

cp .env.example .env
# edit .env and set JWT_KEY to a long random string

docker compose up --build
```

| | URL |
| --- | --- |
| Frontend | http://localhost:3000 |
| API | http://localhost:5080 |
| Swagger | http://localhost:5080/swagger |
| Health | http://localhost:5080/health |
| PostgreSQL | localhost:5432 |

The API waits for PostgreSQL, applies its migrations and seeds the demo data on first start, so
there is nothing else to run. Sign in with any of the [demo credentials](#demo-credentials).

To stop and wipe the database volume: `docker compose down -v`.

## Local setup

### 1. Database

```bash
createdb assignment_db
```

The default development connection string is in
`backend/src/AssignmentSystem.Api/appsettings.Development.json`:

```
Host=localhost;Port=5432;Database=assignment_db;Username=postgres;Password=postgres
```

Change it there if your local PostgreSQL uses different credentials, or override it with an
environment variable:

```bash
export ConnectionStrings__DefaultConnection="Host=localhost;Port=5432;Database=assignment_db;Username=me;Password=secret"
```

### 2. Backend

```bash
cd backend
dotnet restore
dotnet run --project src/AssignmentSystem.Api
```

The API starts on **http://localhost:5080**, creates the schema, seeds the demo data and opens
Swagger at http://localhost:5080/swagger.

### 3. Frontend

In a second terminal:

```bash
cd frontend
cp .env.example .env.local
npm install
npm run dev
```

The app starts on **http://localhost:3000**. `.env.local` only needs
`NEXT_PUBLIC_API_BASE_URL=http://localhost:5080`.

## Database setup

Nothing manual is required: the API applies migrations and seeds on start-up.

If you would rather do it by hand, see [`database/README.md`](database/README.md). In short:

```bash
psql -d assignment_db -f database/schema.sql
psql -d assignment_db -f database/seed.sql
```

Or use the EF tooling:

```bash
cd backend
dotnet ef database update \
  --project src/AssignmentSystem.Infrastructure \
  --startup-project src/AssignmentSystem.Api
```

To turn the automatic behaviour off, set `Database:ApplyMigrationsOnStartup` or
`Database:SeedDemoData` to `false` in `appsettings.json`.

## Running the tests

```bash
cd backend
dotnet test
```

For a readable list of what each test asserts:

```bash
dotnet test --logger "console;verbosity=detailed"
```

With coverage:

```bash
dotnet test --collect:"XPlat Code Coverage"
```

The suite covers the parts that would be expensive to get wrong:

| Area | Examples |
| --- | --- |
| Visibility | a student sees only published work for their own class; a draft returns 404, not 403; a teacher sees only their own assignments; an admin sees everything |
| Assignment lifecycle | only a draft can be published; publishing needs a future deadline and a class with students; only a published assignment can be closed |
| Ownership | a teacher cannot edit, delete or grade another teacher's work |
| Data integrity | an assignment with submissions cannot be deleted or re-targeted; max marks cannot fall below marks already awarded |
| Submission workflow | one submission per student; late work refused or flagged; the global switch overrides the per-assignment flag; graded work is frozen; returned-for-revision can always be resubmitted |
| Grading | marks are bounded by the assignment maximum; moving to Graded without marks is refused; returning for revision clears provisional marks |
| Authentication | wrong password and unknown email fail identically; deactivated accounts and the student portal switch block sign in; password changes require the current password |
| Security primitives | PBKDF2 hashes are salted per password, verify correctly and reject malformed input; JWTs carry the right identity, role and class claims |

Frontend type checking:

```bash
cd frontend
npm run type-check
```

## Demo credentials

| Role | Email | Password |
| --- | --- | --- |
| **Admin** | `admin@school.edu` | `Admin@123` |
| **Teacher** | `teacher@school.edu` | `Teacher@123` |
| **Student** | `student@school.edu` | `Student@123` |

Additional accounts, all seeded with the same demo data:

| Role | Email | Password | Notes |
| --- | --- | --- | --- |
| Teacher | `rakib.hasan@school.edu` | `Teacher@123` | ICT in 10-A, English in 9-B |
| Student | `sadia.islam@school.edu` | `Student@123` | Class 10-A, one submission awaiting marking |
| Student | `mahdi.hasan@school.edu` | `Student@123` | Class 10-A, one submission under review |
| Student | `farhana.akter@school.edu` | `Student@123` | Class 10-A, one returned for revision |
| Student | `imran.kabir@school.edu` | `Student@123` | Class 9-B, one late submission |
| Student | `nusaiba.rahman@school.edu` | `Student@123` | Class 9-B, graded on a closed assignment |

The sign-in page has one-tap buttons for the three main accounts.

The seeded data deliberately contains an open assignment, a draft (invisible to students), an
assignment whose deadline has passed but which accepts late work, and a closed one — so every
branch of the workflow can be seen without creating anything first.

## API summary

Full reference: [`docs/API.md`](docs/API.md). Interactive: http://localhost:5080/swagger

| Area | Endpoints |
| --- | --- |
| Auth | `POST /api/auth/login`, `GET /api/auth/me`, `POST /api/auth/change-password` |
| Users (admin) | `GET/POST /api/users`, `GET/PUT/DELETE /api/users/{id}`, `PATCH /api/users/{id}/status` |
| Classes | `GET/POST /api/classes`, `GET/PUT/DELETE /api/classes/{id}` |
| Subjects | `GET/POST /api/subjects`, `GET/PUT/DELETE /api/subjects/{id}` |
| Teacher mapping | `GET/POST /api/teaching-assignments`, `GET /api/teaching-assignments/mine`, `DELETE /api/teaching-assignments/{id}` |
| Assignments | `GET/POST /api/assignments`, `GET/PUT/DELETE /api/assignments/{id}`, `POST /api/assignments/{id}/publish`, `POST /api/assignments/{id}/close` |
| Submissions | `GET/POST /api/assignments/{id}/submissions`, `GET /api/submissions`, `GET /api/submissions/mine`, `GET/PUT /api/submissions/{id}`, `POST /api/submissions/{id}/grade`, `PATCH /api/submissions/{id}/status` |
| Settings (admin) | `GET /api/settings`, `PUT /api/settings/{key}`, `GET /api/dashboard` |
| Health | `GET /health` |

All list endpoints support `page` and `pageSize` plus filters and search. Errors are RFC 7807
problem details. Enums are serialised as names.

Signing in from the command line:

```bash
curl -X POST http://localhost:5080/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"teacher@school.edu","password":"Teacher@123"}'
```

## Business rules

These are enforced by the API and covered by unit tests.

**Visibility**

- Students see `Published` and `Closed` assignments for their own class only. A draft returns 404,
  so the endpoint cannot be used to discover unpublished work.
- Teachers see the assignments they created and the submissions handed in for them.
- Administrators read everything but cannot create or grade assignments.

**Creating and changing assignments**

- A teacher can only target a class/subject pair an administrator mapped to them.
- Publishing requires a draft, a deadline in the future, and at least one student in the class.
- Closing requires a published assignment.
- An assignment cannot be deleted, nor its class/subject changed, once a student has submitted.
- Maximum marks cannot be lowered below marks already awarded.
- A closed assignment can no longer be edited.

**Submitting**

- One submission per student per assignment, enforced by a unique index.
- Late work is only accepted when the assignment allows it *and* the institution-wide setting
  allows it; accepted late work is flagged rather than silently treated as on time.
- An answer can be replaced while the assignment is open and the work has not been graded or
  rejected — unless the assignment forbids updates.
- A submission returned for revision can always be updated, even when updates are otherwise off.

**Grading**

- Only the teacher who owns the assignment may award marks.
- Marks must be between 0 and the assignment maximum.
- Moving a submission to `Graded` without marks is refused.
- Returning a submission for revision clears any marks already given.

**Accounts**

- Email addresses are unique and normalised to lower case.
- Students must belong to a class; teachers and admins never do.
- An administrator cannot deactivate their own account.
- An account with assignments or submissions cannot be deleted, only deactivated.
- Deactivated accounts keep their history but cannot sign in.
- When `app.student_portal_enabled` is `false`, students cannot sign in; staff are unaffected.

## Configuration

Backend, via `appsettings.json` or environment variables (double underscore for nesting):

| Setting | Environment variable | Default |
| --- | --- | --- |
| Connection string | `ConnectionStrings__DefaultConnection` | dev value in `appsettings.Development.json` |
| JWT signing key | `Jwt__Key` | empty — must be set outside development |
| JWT issuer / audience | `Jwt__Issuer`, `Jwt__Audience` | `AssignmentSystem`, `AssignmentSystemClient` |
| Token lifetime | `Jwt__AccessTokenMinutes` | 480 |
| Allowed CORS origin | `Cors__AllowedOrigins__0` | `http://localhost:3000` |
| Apply migrations at start | `Database__ApplyMigrationsOnStartup` | `true` |
| Seed demo data | `Database__SeedDemoData` | `true` |
| Expose Swagger | `Swagger__Enabled` | `true` |

Frontend: `NEXT_PUBLIC_API_BASE_URL` (see `frontend/.env.example`). It is baked into the client
bundle at build time, so it must be the URL the *browser* can reach.

No real secret is committed. `.env.example` and `appsettings.json` contain placeholders only, and
the dev-only JWT key in `appsettings.Development.json` is clearly labelled as such.

## Assumptions

The full list, with reasoning, is in [`docs/ASSUMPTIONS.md`](docs/ASSUMPTIONS.md). The main ones:

- One `Users` table for all three roles, with `Role` as the discriminator.
- A student belongs to exactly one class; that is what scopes their assignment list.
- A teacher's permission comes from the `ClassSubjectTeachers` mapping, per class *and* subject.
- An assignment targets one class and one subject.
- One submission per student per assignment; updating replaces the content and increments a
  revision counter rather than creating a second row.
- Marks are `decimal(6,2)` because half marks are normal in schools.
- All timestamps are UTC; the browser converts for display.

## Known limitations

Honest list of what this does not do, and why.

- **The code has not been compiled or run.** It was written in an environment without the .NET SDK,
  npm registry access or a database, so nothing could be built or executed end to end. Everything
  was written carefully against the framework APIs, but before submitting please run
  `dotnet build`, `dotnet test` and `npm install && npm run build` once, or start
  `docker compose up --build`, and fix anything the compiler flags.
- **The JWT is stored in a JavaScript-readable cookie**, not an httpOnly one, so that Next.js
  middleware can guard routes before rendering. A production build should have the API set an
  httpOnly, secure, SameSite cookie instead. This is the single biggest security trade-off in the
  project and it was made knowingly.
- **No refresh tokens.** The access token lasts 8 hours; when it expires the user signs in again.
- **No file upload.** A submission carries text plus an optional attachment *URL*. Real uploads
  need storage, virus scanning and quotas.
- **No notifications.** No email or in-app alerts when an assignment is published or marked. The
  brief lists this as optional.
- **PBKDF2 rather than BCrypt or Argon2**, to keep the project free of third-party crypto
  dependencies and to let the SQL seed contain matching hashes. Swapping it means changing one
  class behind `IPasswordHasher`.
- **Administrators are deliberately read-only over assignments and submissions.** They can see
  everything and manage accounts and structure, but marking is left to teachers.
- **Search is `ILIKE`-based**, which is fine at school scale but is not full-text search.
- **No rate limiting or account lockout** on the login endpoint.
- **Deleting a class or subject that is in use is blocked rather than cascaded**, on purpose —
  the alternative silently destroys academic records.
- **Timezone display uses the browser's locale.** There is no per-institution timezone setting.
