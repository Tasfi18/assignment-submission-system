# Database design

## Entity relationship diagram

```mermaid
erDiagram
    CLASSROOM ||--o{ USER : "enrolls (students only)"
    CLASSROOM ||--o{ CLASS_SUBJECT_TEACHER : "is taught in"
    SUBJECT   ||--o{ CLASS_SUBJECT_TEACHER : "is taught as"
    USER      ||--o{ CLASS_SUBJECT_TEACHER : "teaches"

    CLASSROOM ||--o{ ASSIGNMENT : "receives"
    SUBJECT   ||--o{ ASSIGNMENT : "categorises"
    USER      ||--o{ ASSIGNMENT : "is set by (teacher)"

    ASSIGNMENT ||--o{ SUBMISSION : "collects"
    USER       ||--o{ SUBMISSION : "is handed in by (student)"
    USER       ||--o{ SUBMISSION : "is marked by (teacher)"

    CLASSROOM {
        uuid Id PK
        string Name
        string Code UK
        int AcademicYear
        string Description
        bool IsActive
    }

    SUBJECT {
        uuid Id PK
        string Name
        string Code UK
        string Description
        bool IsActive
    }

    USER {
        uuid Id PK
        string FullName
        string Email UK
        string PasswordHash
        int Role "1 Admin, 2 Teacher, 3 Student"
        bool IsActive
        uuid ClassRoomId FK "students only"
    }

    CLASS_SUBJECT_TEACHER {
        uuid Id PK
        uuid ClassRoomId FK
        uuid SubjectId FK
        uuid TeacherId FK
    }

    ASSIGNMENT {
        uuid Id PK
        string Title
        string Description
        uuid ClassRoomId FK
        uuid SubjectId FK
        uuid TeacherId FK
        decimal MaxMarks
        datetime DeadlineUtc
        int Status "1 Draft, 2 Published, 3 Closed"
        bool AllowLateSubmission
        bool AllowResubmission
        datetime PublishedAtUtc
    }

    SUBMISSION {
        uuid Id PK
        uuid AssignmentId FK
        uuid StudentId FK
        string Content
        string AttachmentUrl
        datetime SubmittedAtUtc
        datetime LastUpdatedAtUtc
        bool IsLate
        int RevisionCount
        int Status "1 Submitted, 2 UnderReview, 3 Graded, 4 ReturnedForRevision, 5 Rejected"
        decimal Marks
        string Feedback
        uuid GradedByUserId FK
        datetime GradedAtUtc
    }

    APP_SETTING {
        string Key PK
        string Value
        string Description
        datetime UpdatedAtUtc
    }
```

`APP_SETTING` has no relationships: it is a small key/value table for institution-wide switches.

## Keys and constraints

| Constraint | Table | Why |
| --- | --- | --- |
| Unique `Email` | Users | Sign in is by email |
| Unique `Code` | ClassRooms, Subjects | Human-readable identifiers stay unambiguous |
| Unique `(ClassRoomId, SubjectId, TeacherId)` | ClassSubjectTeachers | A teacher is mapped to a pair once |
| Unique `(AssignmentId, StudentId)` | Submissions | One submission per student per assignment |
| Index `(ClassRoomId, Status)` | Assignments | The student list query filters on exactly this |
| Index `Status`, `StudentId` | Submissions | Marking queues and per-student history |
| Index `DeadlineUtc` | Assignments | "Upcoming only" filtering |

## Delete behaviour

| Relationship | Behaviour | Reasoning |
| --- | --- | --- |
| Assignment → Submissions | Cascade | Submissions have no meaning without the assignment, and an assignment with submissions cannot be deleted anyway |
| ClassRoom / Subject / Teacher → Assignment | Restrict | Deleting a class must never quietly remove academic work |
| ClassRoom / Subject / Teacher → ClassSubjectTeacher | Cascade | The mapping is pure metadata |
| Student → Submission | Restrict | A student with submitted work cannot be deleted; deactivate instead |
| GradedByUser → Submission | Set null | Marks survive even if the marker's account is later removed |

Application-level rules go further than the foreign keys: the services refuse to delete an
assignment that has submissions, a user with academic history, or a class that still has students.
