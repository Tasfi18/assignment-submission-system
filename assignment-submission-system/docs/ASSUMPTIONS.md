# Assumptions and design decisions

The brief left a number of details open. This file records what was assumed, and why.

## Domain

**One account table for all three roles.** `Users.Role` is the discriminator rather than three
separate tables. A school has a few thousand accounts at most and every role shares the same
fields, so separate tables would only add joins.

**A student belongs to exactly one class.** `Users.ClassRoomId` is populated for students and left
null for admins and teachers. This is what makes "students see the assignments of their class"
a single, cheap filter. A student who changes class is edited by an administrator; their existing
submissions stay attached to the assignments they were made for.

**A teacher is authorised per class *and* subject.** The `ClassSubjectTeachers` table is the single
source of truth for what a teacher may do. Without a row there, a teacher cannot create an
assignment for that pair, no matter what the request body says. The same teacher can be mapped to
several pairs, and two teachers can share a pair (co-teaching).

**Assignments target one class and one subject.** The brief says a teacher creates assignments
"for a specific class/subject", so a single pair per assignment. Setting the same work for two
classes means creating it twice.

**One submission per student per assignment.** Enforced by a unique index on
`(AssignmentId, StudentId)`. Updating an answer replaces the content and increments
`RevisionCount` rather than creating a second row, so a teacher always has one thing to mark and
the marking history stays attached to it.

## Workflow

**Draft → Published → Closed.** Students never see drafts; a draft that a student asks for by id
returns 404 rather than 403, so the endpoint cannot be used to discover unpublished work.
Publishing requires a future deadline and at least one student in the target class — both are
mistakes that are much cheaper to catch before the class sees the assignment. Closing keeps the
assignment visible but stops accepting answers.

**Late submissions need two switches.** An assignment's `AllowLateSubmission` flag *and* the
institution-wide `submissions.allow_late_globally` setting must both allow it. Accepted late work
is flagged `IsLate` rather than being silently treated as on time; the teacher decides what that
is worth.

**Changing an answer is allowed until it is graded.** A student may replace their answer while the
assignment is open, unless `AllowResubmission` is off. A submission returned for revision can
always be updated, even when `AllowResubmission` is off, because the teacher explicitly asked for
it — otherwise "return for revision" would be a dead end.

**Grading is restricted to the teacher who owns the assignment,** not to any teacher of that
subject. Administrators can read every submission but cannot award marks; marking is an academic
judgement, not an administrative one.

**Destructive operations are refused once history exists.** An assignment with submissions cannot
be deleted, a user with assignments or submissions cannot be deleted, a class with students cannot
be deleted, and an assignment's class/subject cannot be changed after the first submission.
`MaxMarks` cannot be lowered below marks already awarded. In every case deactivation is offered
instead of deletion.

## Technical

**No file upload.** Submissions carry a text answer plus an optional `AttachmentUrl`. Real file
storage means a storage backend, virus scanning, quotas and signed URLs — well outside the scope of
a recruitment task, and a URL demonstrates the same workflow.

**PBKDF2 instead of BCrypt.** Passwords use PBKDF2-HMAC-SHA256, 100,000 iterations, a 16-byte
random salt per password, stored as `PBKDF2$sha256$<iterations>$<salt>$<hash>`. It is part of
.NET itself (`Rfc2898DeriveBytes.Pbkdf2`), so the project has no third-party crypto dependency and
the seed script can contain hashes that were generated the same way. BCrypt or Argon2 would be a
reasonable swap: only `IPasswordHasher` would change.

**Access tokens only, no refresh tokens.** The token carries id, email, role and (for students)
class id, and lasts 8 hours by default. Refresh tokens need a revocation store and rotation logic,
which is a system of its own.

**Marks are `decimal(6,2)`,** because half marks are normal in schools and floating point is the
wrong tool for anything that gets added up.

**UTC everywhere.** The API stores and returns UTC; the browser converts for display and converts
back before sending. Anything else eventually breaks when the clocks change.

**Class and subject reads are open to all signed-in users.** Teachers and students need names for
dropdowns and headings. Writes remain admin-only.

**Seed data is idempotent and relative to now.** The seeder checks before inserting, and deadlines
are expressed as offsets from the current time, so the demo always contains one open assignment,
one that has expired and one that is closed — whenever it is run.
