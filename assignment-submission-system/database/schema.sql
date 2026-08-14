-- =============================================================================
-- Assignment & Submission Management System - PostgreSQL schema
--
-- This script is the exact equivalent of the EF Core migration
-- 20260801000000_InitialCreate. You only need it if you prefer to create the
-- schema by hand; running the API is enough on its own, because it applies its
-- migrations at start-up.
--
-- Usage:
--   createdb assignment_db
--   psql -d assignment_db -f database/schema.sql
--   psql -d assignment_db -f database/seed.sql
--
-- The script is idempotent and finishes by recording the migration in
-- __EFMigrationsHistory, so the API will not try to create the tables again.
-- =============================================================================

BEGIN;

-- ----------------------------------------------------------------- settings
CREATE TABLE IF NOT EXISTS "AppSettings" (
    "Key"          character varying(100) NOT NULL,
    "Value"        character varying(500) NOT NULL,
    "Description"  character varying(300),
    "UpdatedAtUtc" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_AppSettings" PRIMARY KEY ("Key")
);

-- ------------------------------------------------------------------ classes
CREATE TABLE IF NOT EXISTS "ClassRooms" (
    "Id"           uuid NOT NULL,
    "Name"         character varying(120) NOT NULL,
    "Code"         character varying(30) NOT NULL,
    "AcademicYear" integer NOT NULL,
    "Description"  character varying(500),
    "IsActive"     boolean NOT NULL,
    "CreatedAtUtc" timestamp with time zone NOT NULL,
    "UpdatedAtUtc" timestamp with time zone,
    CONSTRAINT "PK_ClassRooms" PRIMARY KEY ("Id")
);

CREATE UNIQUE INDEX IF NOT EXISTS "IX_ClassRooms_Code" ON "ClassRooms" ("Code");

-- ----------------------------------------------------------------- subjects
CREATE TABLE IF NOT EXISTS "Subjects" (
    "Id"           uuid NOT NULL,
    "Name"         character varying(120) NOT NULL,
    "Code"         character varying(30) NOT NULL,
    "Description"  character varying(500),
    "IsActive"     boolean NOT NULL,
    "CreatedAtUtc" timestamp with time zone NOT NULL,
    "UpdatedAtUtc" timestamp with time zone,
    CONSTRAINT "PK_Subjects" PRIMARY KEY ("Id")
);

CREATE UNIQUE INDEX IF NOT EXISTS "IX_Subjects_Code" ON "Subjects" ("Code");

-- -------------------------------------------------------------------- users
-- One table for all three roles; "Role" is the discriminator (1 Admin, 2 Teacher, 3 Student).
-- "ClassRoomId" is only populated for students.
CREATE TABLE IF NOT EXISTS "Users" (
    "Id"           uuid NOT NULL,
    "FullName"     character varying(150) NOT NULL,
    "Email"        character varying(200) NOT NULL,
    "PasswordHash" character varying(400) NOT NULL,
    "Role"         integer NOT NULL,
    "IsActive"     boolean NOT NULL,
    "ClassRoomId"  uuid,
    "CreatedAtUtc" timestamp with time zone NOT NULL,
    "UpdatedAtUtc" timestamp with time zone,
    CONSTRAINT "PK_Users" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_Users_ClassRooms_ClassRoomId" FOREIGN KEY ("ClassRoomId")
        REFERENCES "ClassRooms" ("Id") ON DELETE RESTRICT
);

CREATE UNIQUE INDEX IF NOT EXISTS "IX_Users_Email" ON "Users" ("Email");
CREATE INDEX IF NOT EXISTS "IX_Users_Role" ON "Users" ("Role");
CREATE INDEX IF NOT EXISTS "IX_Users_ClassRoomId" ON "Users" ("ClassRoomId");

-- ------------------------------------------------------- teaching assignments
-- "Teacher T teaches subject S in class C". Every teacher permission check reads this table.
CREATE TABLE IF NOT EXISTS "ClassSubjectTeachers" (
    "Id"           uuid NOT NULL,
    "ClassRoomId"  uuid NOT NULL,
    "SubjectId"    uuid NOT NULL,
    "TeacherId"    uuid NOT NULL,
    "CreatedAtUtc" timestamp with time zone NOT NULL,
    "UpdatedAtUtc" timestamp with time zone,
    CONSTRAINT "PK_ClassSubjectTeachers" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_ClassSubjectTeachers_ClassRooms_ClassRoomId" FOREIGN KEY ("ClassRoomId")
        REFERENCES "ClassRooms" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_ClassSubjectTeachers_Subjects_SubjectId" FOREIGN KEY ("SubjectId")
        REFERENCES "Subjects" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_ClassSubjectTeachers_Users_TeacherId" FOREIGN KEY ("TeacherId")
        REFERENCES "Users" ("Id") ON DELETE CASCADE
);

CREATE UNIQUE INDEX IF NOT EXISTS "IX_ClassSubjectTeachers_Class_Subject_Teacher"
    ON "ClassSubjectTeachers" ("ClassRoomId", "SubjectId", "TeacherId");
CREATE INDEX IF NOT EXISTS "IX_ClassSubjectTeachers_SubjectId" ON "ClassSubjectTeachers" ("SubjectId");
CREATE INDEX IF NOT EXISTS "IX_ClassSubjectTeachers_TeacherId" ON "ClassSubjectTeachers" ("TeacherId");

-- -------------------------------------------------------------- assignments
-- "Status": 1 Draft, 2 Published, 3 Closed.
CREATE TABLE IF NOT EXISTS "Assignments" (
    "Id"                  uuid NOT NULL,
    "Title"               character varying(200) NOT NULL,
    "Description"         character varying(5000) NOT NULL,
    "ClassRoomId"         uuid NOT NULL,
    "SubjectId"           uuid NOT NULL,
    "TeacherId"           uuid NOT NULL,
    "MaxMarks"            numeric(6,2) NOT NULL,
    "DeadlineUtc"         timestamp with time zone NOT NULL,
    "Status"              integer NOT NULL,
    "AllowLateSubmission" boolean NOT NULL,
    "AllowResubmission"   boolean NOT NULL,
    "PublishedAtUtc"      timestamp with time zone,
    "CreatedAtUtc"        timestamp with time zone NOT NULL,
    "UpdatedAtUtc"        timestamp with time zone,
    CONSTRAINT "PK_Assignments" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_Assignments_ClassRooms_ClassRoomId" FOREIGN KEY ("ClassRoomId")
        REFERENCES "ClassRooms" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_Assignments_Subjects_SubjectId" FOREIGN KEY ("SubjectId")
        REFERENCES "Subjects" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_Assignments_Users_TeacherId" FOREIGN KEY ("TeacherId")
        REFERENCES "Users" ("Id") ON DELETE RESTRICT
);

CREATE INDEX IF NOT EXISTS "IX_Assignments_ClassRoom_Status" ON "Assignments" ("ClassRoomId", "Status");
CREATE INDEX IF NOT EXISTS "IX_Assignments_DeadlineUtc" ON "Assignments" ("DeadlineUtc");
CREATE INDEX IF NOT EXISTS "IX_Assignments_SubjectId" ON "Assignments" ("SubjectId");
CREATE INDEX IF NOT EXISTS "IX_Assignments_TeacherId" ON "Assignments" ("TeacherId");

-- -------------------------------------------------------------- submissions
-- "Status": 1 Submitted, 2 UnderReview, 3 Graded, 4 ReturnedForRevision, 5 Rejected.
CREATE TABLE IF NOT EXISTS "Submissions" (
    "Id"                uuid NOT NULL,
    "AssignmentId"      uuid NOT NULL,
    "StudentId"         uuid NOT NULL,
    "Content"           character varying(20000) NOT NULL,
    "AttachmentUrl"     character varying(500),
    "SubmittedAtUtc"    timestamp with time zone NOT NULL,
    "LastUpdatedAtUtc"  timestamp with time zone,
    "IsLate"            boolean NOT NULL,
    "RevisionCount"     integer NOT NULL,
    "Status"            integer NOT NULL,
    "Marks"             numeric(6,2),
    "Feedback"          character varying(2000),
    "GradedByUserId"    uuid,
    "GradedAtUtc"       timestamp with time zone,
    "CreatedAtUtc"      timestamp with time zone NOT NULL,
    "UpdatedAtUtc"      timestamp with time zone,
    CONSTRAINT "PK_Submissions" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_Submissions_Assignments_AssignmentId" FOREIGN KEY ("AssignmentId")
        REFERENCES "Assignments" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_Submissions_Users_GradedByUserId" FOREIGN KEY ("GradedByUserId")
        REFERENCES "Users" ("Id") ON DELETE SET NULL,
    CONSTRAINT "FK_Submissions_Users_StudentId" FOREIGN KEY ("StudentId")
        REFERENCES "Users" ("Id") ON DELETE RESTRICT
);

-- The integrity rule at the heart of the workflow: one answer per student per assignment.
CREATE UNIQUE INDEX IF NOT EXISTS "IX_Submissions_Assignment_Student"
    ON "Submissions" ("AssignmentId", "StudentId");
CREATE INDEX IF NOT EXISTS "IX_Submissions_GradedByUserId" ON "Submissions" ("GradedByUserId");
CREATE INDEX IF NOT EXISTS "IX_Submissions_Status" ON "Submissions" ("Status");
CREATE INDEX IF NOT EXISTS "IX_Submissions_StudentId" ON "Submissions" ("StudentId");

-- --------------------------------------------------------- migration history
CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId"    character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260801000000_InitialCreate', '8.0.8')
ON CONFLICT ("MigrationId") DO NOTHING;

COMMIT;
