-- =============================================================================
-- Assignment & Submission Management System - demo data
--
-- Run after database/schema.sql. The API seeds exactly the same data on first
-- start-up, so this file is only needed for a manual setup or to reset the demo.
--
--   psql -d assignment_db -f database/seed.sql
--
-- Passwords are PBKDF2-HMAC-SHA256 hashes (100,000 iterations, 16-byte salt) in
-- the format PBKDF2$sha256$<iterations>$<base64 salt>$<base64 hash>.
--
--   admin@school.edu           Admin@123     (Admin)
--   teacher@school.edu         Teacher@123   (Teacher - Nusrat Jahan)
--   rakib.hasan@school.edu     Teacher@123   (Teacher - Rakib Hasan)
--   student@school.edu         Student@123   (Student - Tanvir Ahmed, Class 10-A)
--   sadia.islam@school.edu     Student@123   (Student - Class 10-A)
--   mahdi.hasan@school.edu     Student@123   (Student - Class 10-A)
--   farhana.akter@school.edu   Student@123   (Student - Class 10-A)
--   imran.kabir@school.edu     Student@123   (Student - Class 9-B)
--   nusaiba.rahman@school.edu  Student@123   (Student - Class 9-B)
--
-- Deadlines are relative to now(), so the demo always contains one open
-- assignment, one that has expired and one that has been closed.
-- Every statement is idempotent: re-running the file changes nothing.
-- =============================================================================

BEGIN;

-- ---------------------------------------------------------------- settings
INSERT INTO "AppSettings" ("Key", "Value", "Description", "UpdatedAtUtc") VALUES
  ('submissions.allow_late_globally', 'true',
   'Master switch. When false, late submissions are refused even if an assignment allows them.', now()),
  ('submissions.max_answer_length', '20000',
   'Maximum number of characters accepted in a submitted answer.', now()),
  ('app.institution_name', 'Green Valley School & College',
   'Institution name shown in the application header.', now()),
  ('app.student_portal_enabled', 'true',
   'When false, students cannot sign in.', now())
ON CONFLICT ("Key") DO NOTHING;

-- ----------------------------------------------------------------- classes
INSERT INTO "ClassRooms" ("Id", "Name", "Code", "AcademicYear", "Description", "IsActive", "CreatedAtUtc") VALUES
  ('44444444-4444-4444-4444-000000000001', 'Class 10 - Section A', 'C10A', EXTRACT(YEAR FROM now())::int,
   'Science group, morning shift.', true, now()),
  ('44444444-4444-4444-4444-000000000002', 'Class 9 - Section B', 'C09B', EXTRACT(YEAR FROM now())::int,
   'General group, day shift.', true, now())
ON CONFLICT ("Id") DO NOTHING;

-- ---------------------------------------------------------------- subjects
INSERT INTO "Subjects" ("Id", "Name", "Code", "Description", "IsActive", "CreatedAtUtc") VALUES
  ('55555555-5555-5555-5555-000000000001', 'Mathematics', 'MATH-101', 'Algebra, geometry and trigonometry.', true, now()),
  ('55555555-5555-5555-5555-000000000002', 'Physics', 'PHY-101', 'Mechanics and waves.', true, now()),
  ('55555555-5555-5555-5555-000000000003', 'Information & Communication Technology', 'ICT-101', 'Computing fundamentals and the web.', true, now()),
  ('55555555-5555-5555-5555-000000000004', 'English', 'ENG-101', 'Language and literature.', true, now())
ON CONFLICT ("Id") DO NOTHING;

-- ------------------------------------------------------------------- users
-- Role: 1 = Admin, 2 = Teacher, 3 = Student
INSERT INTO "Users" ("Id", "FullName", "Email", "PasswordHash", "Role", "IsActive", "ClassRoomId", "CreatedAtUtc") VALUES
  ('11111111-1111-1111-1111-111111111111', 'System Administrator', 'admin@school.edu',
   'PBKDF2$sha256$100000$QNwgshsU2brTaymmBCqDQA==$LyEJ8WWb/bCmEZDFzo7DdsnV4f1ynHZbscyU8xtH8VE=',
   1, true, NULL, now()),

  ('22222222-2222-2222-2222-000000000001', 'Nusrat Jahan', 'teacher@school.edu',
   'PBKDF2$sha256$100000$9CbySBOMWwFYQTAf54utEA==$/JPuOgGKjPK+JHbw01KYq5RGgUuy7SU0iz0aRNfaxBI=',
   2, true, NULL, now()),

  ('22222222-2222-2222-2222-000000000002', 'Rakib Hasan', 'rakib.hasan@school.edu',
   'PBKDF2$sha256$100000$cvtlrdh5Sap25LrLI5zvlA==$Ke9nLJ8Bk5JPf3QrTWAYvYB3Q41pYp294C/+j3IjLjw=',
   2, true, NULL, now()),

  ('33333333-3333-3333-3333-000000000001', 'Tanvir Ahmed', 'student@school.edu',
   'PBKDF2$sha256$100000$5P2XnUI5OoZtXdWdljsIkQ==$BmdThI+RFxOq6082z4NZw9GcdOR+pSZHSgSYvfb3tlo=',
   3, true, '44444444-4444-4444-4444-000000000001', now()),

  ('33333333-3333-3333-3333-000000000002', 'Sadia Islam', 'sadia.islam@school.edu',
   'PBKDF2$sha256$100000$GkFgQTVlsN/zfaSHKTWN+g==$NdjraQykVKEIMjp6F96f9UUje4WQK1yOegtDKSZQtPM=',
   3, true, '44444444-4444-4444-4444-000000000001', now()),

  ('33333333-3333-3333-3333-000000000003', 'Mahdi Hasan', 'mahdi.hasan@school.edu',
   'PBKDF2$sha256$100000$uRj3nX/UmTQOVwEAwi5KKA==$SRWRm9x2QJKh9LQ7ve0baCD54zxSL5r+37Z8rJpZ5RI=',
   3, true, '44444444-4444-4444-4444-000000000001', now()),

  ('33333333-3333-3333-3333-000000000004', 'Farhana Akter', 'farhana.akter@school.edu',
   'PBKDF2$sha256$100000$L/pHrAEjNjQuIlJF88r2cA==$sRY2R3JhjZoauQn7UsIZojFkr4TrpibOW1Rj/Q7P5ko=',
   3, true, '44444444-4444-4444-4444-000000000001', now()),

  ('33333333-3333-3333-3333-000000000005', 'Imran Kabir', 'imran.kabir@school.edu',
   'PBKDF2$sha256$100000$DlC90/khpLD8qu6euD8j7g==$fW6XaYWgNGTHbtbr9iIiaG8r1coVBA6DeGFk3/yEKM8=',
   3, true, '44444444-4444-4444-4444-000000000002', now()),

  ('33333333-3333-3333-3333-000000000006', 'Nusaiba Rahman', 'nusaiba.rahman@school.edu',
   'PBKDF2$sha256$100000$2cdCwfv9+tAGSDbmOqcRqw==$+2c8uaHjrBAV9kaDiECww8LmNGUys7zjoOPDLF8Eqbk=',
   3, true, '44444444-4444-4444-4444-000000000002', now())
ON CONFLICT ("Id") DO NOTHING;

-- --------------------------------------------------- teaching assignments
INSERT INTO "ClassSubjectTeachers" ("Id", "ClassRoomId", "SubjectId", "TeacherId", "CreatedAtUtc") VALUES
  ('66666666-6666-6666-6666-000000000001', '44444444-4444-4444-4444-000000000001', '55555555-5555-5555-5555-000000000001', '22222222-2222-2222-2222-000000000001', now()),
  ('66666666-6666-6666-6666-000000000002', '44444444-4444-4444-4444-000000000001', '55555555-5555-5555-5555-000000000002', '22222222-2222-2222-2222-000000000001', now()),
  ('66666666-6666-6666-6666-000000000003', '44444444-4444-4444-4444-000000000002', '55555555-5555-5555-5555-000000000001', '22222222-2222-2222-2222-000000000001', now()),
  ('66666666-6666-6666-6666-000000000004', '44444444-4444-4444-4444-000000000001', '55555555-5555-5555-5555-000000000003', '22222222-2222-2222-2222-000000000002', now()),
  ('66666666-6666-6666-6666-000000000005', '44444444-4444-4444-4444-000000000002', '55555555-5555-5555-5555-000000000004', '22222222-2222-2222-2222-000000000002', now())
ON CONFLICT ("Id") DO NOTHING;

-- -------------------------------------------------------------- assignments
-- Status: 1 = Draft, 2 = Published, 3 = Closed
INSERT INTO "Assignments" ("Id", "Title", "Description", "ClassRoomId", "SubjectId", "TeacherId",
                           "MaxMarks", "DeadlineUtc", "Status", "AllowLateSubmission", "AllowResubmission",
                           "PublishedAtUtc", "CreatedAtUtc") VALUES
  -- open, accepts late work
  ('77777777-7777-7777-7777-000000000001', 'Quadratic Equations - Problem Set 3',
   'Solve problems 1 to 12 from chapter 4. Show every step of your working. Answers without working will not receive full marks.',
   '44444444-4444-4444-4444-000000000001', '55555555-5555-5555-5555-000000000001', '22222222-2222-2222-2222-000000000001',
   50.00, now() + interval '7 days', 2, true, true, now() - interval '3 days', now() - interval '3 days'),

  -- open, no late work accepted
  ('77777777-7777-7777-7777-000000000002', 'Newton''s Laws - Lab Report',
   'Write a full lab report on the trolley and pulley experiment: aim, apparatus, procedure, observations, calculations and conclusion.',
   '44444444-4444-4444-4444-000000000001', '55555555-5555-5555-5555-000000000002', '22222222-2222-2222-2222-000000000001',
   100.00, now() + interval '14 days', 2, false, true, now() - interval '2 days', now() - interval '2 days'),

  -- draft: must stay invisible to students
  ('77777777-7777-7777-7777-000000000003', 'Trigonometry Revision Sheet',
   'Draft assignment - still being prepared. Students must not be able to see this one.',
   '44444444-4444-4444-4444-000000000001', '55555555-5555-5555-5555-000000000001', '22222222-2222-2222-2222-000000000001',
   30.00, now() + interval '21 days', 1, false, true, NULL, now() - interval '1 day'),

  -- deadline already passed, late work allowed
  ('77777777-7777-7777-7777-000000000004', 'Coordinate Geometry - Weekly Test',
   'Answer all five questions on straight lines and distance between two points.',
   '44444444-4444-4444-4444-000000000002', '55555555-5555-5555-5555-000000000001', '22222222-2222-2222-2222-000000000001',
   40.00, now() - interval '3 days', 2, true, true, now() - interval '12 days', now() - interval '12 days'),

  ('77777777-7777-7777-7777-000000000005', 'HTML & CSS Portfolio Page',
   'Build a single page portfolio with a header, an about section and a contact form. Submit the link to your hosted page.',
   '44444444-4444-4444-4444-000000000001', '55555555-5555-5555-5555-000000000003', '22222222-2222-2222-2222-000000000002',
   60.00, now() + interval '10 days', 2, true, true, now() - interval '5 days', now() - interval '5 days'),

  -- closed: visible but read only
  ('77777777-7777-7777-7777-000000000006', 'Essay: My Favourite Book',
   'Write 400 words about a book you enjoyed and explain what it taught you.',
   '44444444-4444-4444-4444-000000000002', '55555555-5555-5555-5555-000000000004', '22222222-2222-2222-2222-000000000002',
   25.00, now() - interval '10 days', 3, false, false, now() - interval '25 days', now() - interval '25 days')
ON CONFLICT ("Id") DO NOTHING;

-- -------------------------------------------------------------- submissions
-- Status: 1 = Submitted, 2 = UnderReview, 3 = Graded, 4 = ReturnedForRevision, 5 = Rejected
INSERT INTO "Submissions" ("Id", "AssignmentId", "StudentId", "Content", "AttachmentUrl", "SubmittedAtUtc",
                           "LastUpdatedAtUtc", "IsLate", "RevisionCount", "Status", "Marks", "Feedback",
                           "GradedByUserId", "GradedAtUtc", "CreatedAtUtc") VALUES
  -- graded
  ('88888888-8888-8888-8888-000000000001', '77777777-7777-7777-7777-000000000001', '33333333-3333-3333-3333-000000000001',
   'Question 1: x = 3 or x = -5 (factorised as (x-3)(x+5)=0). Question 2: discriminant = 49, so the roots are real and distinct... [full working attached]',
   'https://example.com/uploads/tanvir-problem-set-3.pdf', now() - interval '2 days', NULL, false, 0, 3, 45.00,
   'Very good work. Question 9 has a sign error in the second line, otherwise the method is correct throughout.',
   '22222222-2222-2222-2222-000000000001', now() - interval '1 day', now() - interval '2 days'),

  -- waiting to be marked, already revised once
  ('88888888-8888-8888-8888-000000000002', '77777777-7777-7777-7777-000000000001', '33333333-3333-3333-3333-000000000002',
   'Question 1: factorised form (x-3)(x+5). Question 2: used the quadratic formula, roots are 3.5 and -2. Question 3 onwards in the attached file.',
   NULL, now() - interval '1 day', now() - interval '8 hours', false, 1, 1, NULL, NULL, NULL, NULL, now() - interval '1 day'),

  -- under review
  ('88888888-8888-8888-8888-000000000003', '77777777-7777-7777-7777-000000000002', '33333333-3333-3333-3333-000000000003',
   'Aim: to verify Newton''s second law using a trolley and a pulley. Apparatus: trolley, ticker tape timer, slotted masses... Conclusion: acceleration was proportional to the applied force within experimental error.',
   NULL, now() - interval '30 hours', NULL, false, 0, 2, NULL, NULL, NULL, NULL, now() - interval '30 hours'),

  -- late submission accepted because the assignment allows it
  ('88888888-8888-8888-8888-000000000004', '77777777-7777-7777-7777-000000000004', '33333333-3333-3333-3333-000000000005',
   'Q1: distance = 13 units. Q2: the gradient is -2 so the line is decreasing. Q3: midpoint (4, 1). Sorry for the delay, I was unwell last week.',
   NULL, now() - interval '1 day', NULL, true, 0, 1, NULL, NULL, NULL, NULL, now() - interval '1 day'),

  -- returned for revision
  ('88888888-8888-8888-8888-000000000005', '77777777-7777-7777-7777-000000000005', '33333333-3333-3333-3333-000000000004',
   'My portfolio page is hosted at the attached link. It has a header, an about section and a contact form.',
   'https://example.com/farhana-portfolio', now() - interval '3 days', NULL, false, 0, 4, NULL,
   'Good start, but the contact form has no labels and the page is not responsive below 480px. Please fix both and submit again.',
   NULL, NULL, now() - interval '3 days'),

  -- graded, on a closed assignment
  ('88888888-8888-8888-8888-000000000006', '77777777-7777-7777-7777-000000000006', '33333333-3333-3333-3333-000000000006',
   'My favourite book is ''Aranyak'' by Bibhutibhushan Bandyopadhyay. It taught me that a place can change a person as much as people do...',
   NULL, now() - interval '12 days', NULL, false, 0, 3, 20.00,
   'Clear structure and honest reflection. Watch the tense changes in the third paragraph.',
   '22222222-2222-2222-2222-000000000002', now() - interval '9 days', now() - interval '12 days')
ON CONFLICT ("Id") DO NOTHING;

COMMIT;

-- Quick check
--   SELECT "Role", count(*) FROM "Users" GROUP BY "Role";
--   SELECT "Status", count(*) FROM "Assignments" GROUP BY "Status";
--   SELECT "Status", count(*) FROM "Submissions" GROUP BY "Status";
