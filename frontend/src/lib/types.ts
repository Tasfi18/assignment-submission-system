// Mirrors the DTOs returned by the ASP.NET Core API. Enums are serialised as names
// (JsonStringEnumConverter), so they are modelled here as string unions.

export type UserRole = 'Admin' | 'Teacher' | 'Student';

export type AssignmentStatus = 'Draft' | 'Published' | 'Closed';

export type SubmissionStatus =
  | 'Submitted'
  | 'UnderReview'
  | 'Graded'
  | 'ReturnedForRevision'
  | 'Rejected';

export interface UserSummary {
  id: string;
  fullName: string;
  email: string;
  role: UserRole;
  roleName: string;
  classRoomId?: string | null;
  classRoomName?: string | null;
}

export interface LoginResponse {
  accessToken: string;
  tokenType: string;
  expiresAtUtc: string;
  user: UserSummary;
}

export interface UserResponse extends UserSummary {
  isActive: boolean;
  createdAtUtc: string;
}

export interface ClassRoom {
  id: string;
  name: string;
  code: string;
  academicYear: number;
  description?: string | null;
  isActive: boolean;
  studentCount: number;
}

export interface Subject {
  id: string;
  name: string;
  code: string;
  description?: string | null;
  isActive: boolean;
}

export interface TeachingAssignment {
  id: string;
  classRoomId: string;
  classRoomName: string;
  subjectId: string;
  subjectName: string;
  teacherId: string;
  teacherName: string;
  teacherEmail: string;
  createdAtUtc: string;
}

export interface StudentSubmissionSummary {
  id: string;
  status: SubmissionStatus;
  statusName: string;
  submittedAtUtc: string;
  isLate: boolean;
  marks?: number | null;
  feedback?: string | null;
}

export interface Assignment {
  id: string;
  title: string;
  description: string;
  classRoomId: string;
  classRoomName: string;
  subjectId: string;
  subjectName: string;
  teacherId: string;
  teacherName: string;
  maxMarks: number;
  deadlineUtc: string;
  status: AssignmentStatus;
  statusName: string;
  allowLateSubmission: boolean;
  allowResubmission: boolean;
  publishedAtUtc?: string | null;
  createdAtUtc: string;
  isPastDeadline: boolean;
  submissionCount: number;
  classStudentCount: number;
  mySubmission?: StudentSubmissionSummary | null;
}

export interface Submission {
  id: string;
  assignmentId: string;
  assignmentTitle: string;
  subjectName: string;
  classRoomName: string;
  assignmentMaxMarks: number;
  assignmentDeadlineUtc: string;
  studentId: string;
  studentName: string;
  studentEmail: string;
  content: string;
  attachmentUrl?: string | null;
  submittedAtUtc: string;
  lastUpdatedAtUtc?: string | null;
  isLate: boolean;
  revisionCount: number;
  status: SubmissionStatus;
  statusName: string;
  marks?: number | null;
  feedback?: string | null;
  gradedByName?: string | null;
  gradedAtUtc?: string | null;
}

export interface AppSetting {
  key: string;
  value: string;
  description?: string | null;
  updatedAtUtc: string;
}

export interface Dashboard {
  totalUsers: number;
  totalTeachers: number;
  totalStudents: number;
  totalClassRooms: number;
  totalSubjects: number;
  totalAssignments: number;
  publishedAssignments: number;
  draftAssignments: number;
  totalSubmissions: number;
  pendingReviewSubmissions: number;
  gradedSubmissions: number;
  lateSubmissions: number;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
  hasPrevious: boolean;
  hasNext: boolean;
}
