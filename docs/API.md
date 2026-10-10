# API reference

Base URL: `http://localhost:5080` · interactive docs: `http://localhost:5080/swagger`

Every endpoint except `POST /api/auth/login` and `GET /health` requires
`Authorization: Bearer <token>`.

Enums are sent and received as **names** (`"Published"`, `"ReturnedForRevision"`), not numbers.
All timestamps are UTC ISO-8601.

---

## Conventions

### Paging

List endpoints accept `page` (default 1) and `pageSize` (default 20, maximum 100) and return:

```json
{
  "items": [],
  "totalCount": 42,
  "page": 1,
  "pageSize": 20,
  "totalPages": 3,
  "hasPrevious": false,
  "hasNext": true
}
```

### Errors

Failures use RFC 7807 problem details:

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.5",
  "title": "Business rule violated",
  "status": 422,
  "detail": "The deadline for this assignment has passed and late submissions are not allowed.",
  "traceId": "00-8f2c...-01"
}
```

| Status | Meaning in this API |
| --- | --- |
| 400 | Model validation failed (`errors` holds the field messages) |
| 401 | Missing, expired or invalid token |
| 403 | Signed in, but not allowed to touch this resource |
| 404 | Not found, or deliberately hidden (a student asking for a draft) |
| 409 | Conflict: duplicate email/code, or a second submission |
| 422 | A business rule refused the operation |
| 500 | Unhandled error (details are logged, not returned) |

---

## Authentication

| Method | Path | Roles | Description |
| --- | --- | --- | --- |
| POST | `/api/auth/login` | anonymous | Returns the JWT and the profile |
| GET | `/api/auth/me` | any | The signed in user |
| POST | `/api/auth/change-password` | any | Requires the current password |

```http
POST /api/auth/login
{ "email": "teacher@school.edu", "password": "Teacher@123" }
```

```json
{
  "accessToken": "eyJhbGciOi...",
  "tokenType": "Bearer",
  "expiresAtUtc": "2026-08-14T18:30:00Z",
  "user": {
    "id": "2222...", "fullName": "Nusrat Jahan", "email": "teacher@school.edu",
    "role": "Teacher", "roleName": "Teacher", "classRoomId": null, "classRoomName": null
  }
}
```

## Users — admin only

| Method | Path | Description |
| --- | --- | --- |
| GET | `/api/users` | Filters: `role`, `classRoomId`, `isActive`, `search` |
| GET | `/api/users/{id}` | Single account |
| POST | `/api/users` | `role` is `Admin`/`Teacher`/`Student`; students need `classRoomId` |
| PUT | `/api/users/{id}` | `newPassword` optional |
| PATCH | `/api/users/{id}/status?isActive=false` | Deactivate without deleting |
| DELETE | `/api/users/{id}` | 422 if the account owns assignments or submissions |

## Classes and subjects

| Method | Path | Roles |
| --- | --- | --- |
| GET | `/api/classes`, `/api/classes/{id}` | any signed in user |
| POST / PUT / DELETE | `/api/classes`, `/api/classes/{id}` | Admin |
| GET | `/api/subjects`, `/api/subjects/{id}` | any signed in user |
| POST / PUT / DELETE | `/api/subjects`, `/api/subjects/{id}` | Admin |

Reading is open to everyone because teachers and students need class and subject names for
dropdowns and headings.

## Teacher mapping

| Method | Path | Roles | Description |
| --- | --- | --- | --- |
| GET | `/api/teaching-assignments` | Admin | Filters: `classRoomId`, `subjectId`, `teacherId` |
| GET | `/api/teaching-assignments/mine` | Teacher | The pairs this teacher may use |
| POST | `/api/teaching-assignments` | Admin | `{ classRoomId, subjectId, teacherId }` |
| DELETE | `/api/teaching-assignments/{id}` | Admin | 422 if assignments already exist for that pair |

## Assignments

| Method | Path | Roles | Description |
| --- | --- | --- | --- |
| GET | `/api/assignments` | any | Scoped by role. Filters: `status`, `classRoomId`, `subjectId`, `teacherId`, `search`, `upcomingOnly` |
| GET | `/api/assignments/{id}` | any | Includes `mySubmission` for students |
| POST | `/api/assignments` | Teacher | Optional `publishImmediately` |
| PUT | `/api/assignments/{id}` | Teacher (owner) | |
| POST | `/api/assignments/{id}/publish` | Teacher (owner) | Draft → Published |
| POST | `/api/assignments/{id}/close` | Teacher (owner) | Published → Closed |
| DELETE | `/api/assignments/{id}` | Teacher (owner) | 422 once submissions exist |
| GET | `/api/assignments/{id}/submissions` | Teacher (owner) / Admin | |
| POST | `/api/assignments/{id}/submissions` | Student | Submit an answer |

Visibility: admins see everything; teachers see the assignments they created; students see
`Published` and `Closed` assignments for their own class only.

```http
POST /api/assignments
{
  "title": "Quadratic Equations - Problem Set 3",
  "description": "Solve problems 1 to 12 from chapter 4.",
  "classRoomId": "4444...", "subjectId": "5555...",
  "maxMarks": 50,
  "deadlineUtc": "2026-08-30T17:00:00Z",
  "allowLateSubmission": true,
  "allowResubmission": true,
  "publishImmediately": false
}
```

## Submissions

| Method | Path | Roles | Description |
| --- | --- | --- | --- |
| GET | `/api/submissions` | any | Scoped by role. Filters: `assignmentId`, `studentId`, `classRoomId`, `status`, `isLate`, `search` |
| GET | `/api/submissions/mine` | Student | The student's own work |
| GET | `/api/submissions/{id}` | any (scoped) | |
| PUT | `/api/submissions/{id}` | Student (owner) | Replace the answer |
| POST | `/api/submissions/{id}/grade` | Teacher (owner) | `{ marks, feedback }` |
| PATCH | `/api/submissions/{id}/status` | Teacher (owner) | `{ status, feedback }` |

`status` accepts `Submitted`, `UnderReview`, `Graded`, `ReturnedForRevision`, `Rejected`.
Moving to `Graded` without marks returns 422; `ReturnedForRevision` clears any marks already
awarded.

## Settings and dashboard — admin only

| Method | Path | Description |
| --- | --- | --- |
| GET | `/api/settings` | All switches |
| PUT | `/api/settings/{key}` | `{ "value": "false" }` |
| GET | `/api/dashboard` | Aggregated counts |

| Key | Effect |
| --- | --- |
| `submissions.allow_late_globally` | When `false`, late work is refused even if the assignment allows it |
| `submissions.max_answer_length` | Maximum characters in an answer |
| `app.institution_name` | Display only |
| `app.student_portal_enabled` | When `false`, students cannot sign in |

## Health

`GET /health` returns `{ "status": "healthy", "timeUtc": "..." }` and needs no token.
