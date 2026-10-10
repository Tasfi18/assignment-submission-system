'use client';

import Link from 'next/link';
import { useParams, useRouter } from 'next/navigation';
import { useState } from 'react';
import { z } from 'zod';
import { AssignmentForm } from '@/components/AssignmentForm';
import { api, query } from '@/lib/api';
import { formatDateTime, relativeToNow } from '@/lib/format';
import { useAction, useAsyncData } from '@/lib/hooks';
import type { Assignment, PagedResult, Submission, SubmissionStatus } from '@/lib/types';
import {
  Alert,
  Badge,
  Button,
  Card,
  EmptyState,
  Field,
  Input,
  Loading,
  Modal,
  PageHeader,
  Select,
  Table,
  Td,
  Textarea,
  Th
} from '@/components/ui';

const gradeSchema = z.object({
  marks: z.number({ invalid_type_error: 'Enter the marks awarded.' }).min(0, 'Marks cannot be negative.'),
  feedback: z.string().max(2000).optional()
});

export default function TeacherAssignmentDetailPage() {
  const params = useParams<{ id: string }>();
  const router = useRouter();
  const assignmentId = params.id;

  const [editing, setEditing] = useState(false);
  const [grading, setGrading] = useState<Submission | null>(null);
  const [marks, setMarks] = useState('');
  const [feedback, setFeedback] = useState('');
  const [gradeError, setGradeError] = useState<string | null>(null);

  const action = useAction();

  const assignment = useAsyncData<Assignment>(
    () => api.get<Assignment>(`/api/assignments/${assignmentId}`),
    [assignmentId]
  );

  const submissions = useAsyncData<PagedResult<Submission>>(
    () =>
      api.get<PagedResult<Submission>>(
        `/api/assignments/${assignmentId}/submissions${query({ pageSize: 100 })}`
      ),
    [assignmentId]
  );

  function openGrading(submission: Submission) {
    setGrading(submission);
    setMarks(submission.marks === null || submission.marks === undefined ? '' : String(submission.marks));
    setFeedback(submission.feedback ?? '');
    setGradeError(null);
    action.setError(null);
  }

  async function saveGrade() {
    if (!grading) return;

    const parsed = gradeSchema.safeParse({ marks: Number(marks), feedback: feedback || undefined });

    if (!parsed.success) {
      setGradeError(parsed.error.flatten().fieldErrors.marks?.[0] ?? 'Check the marks you entered.');
      return;
    }

    if (assignment.data && parsed.data.marks > assignment.data.maxMarks) {
      setGradeError(`Marks cannot exceed the maximum of ${assignment.data.maxMarks}.`);
      return;
    }

    setGradeError(null);

    const ok = await action.run(async () => {
      await api.post(`/api/submissions/${grading.id}/grade`, {
        marks: parsed.data.marks,
        feedback: parsed.data.feedback ?? null
      });
    }, `Marks saved for ${grading.studentName}.`);

    if (ok) {
      setGrading(null);
      void submissions.reload();
      void assignment.reload();
    }
  }

  async function changeStatus(submission: Submission, status: SubmissionStatus) {
    const ok = await action.run(async () => {
      await api.patch(`/api/submissions/${submission.id}/status`, { status, feedback: null });
    }, `${submission.studentName}'s submission moved to ${status}.`);

    if (ok) void submissions.reload();
  }

  async function publish() {
    const ok = await action.run(async () => {
      await api.post(`/api/assignments/${assignmentId}/publish`);
    }, 'The assignment is now visible to the class.');

    if (ok) void assignment.reload();
  }

  async function close() {
    if (!window.confirm('Close this assignment? No further submissions will be accepted.')) return;

    const ok = await action.run(async () => {
      await api.post(`/api/assignments/${assignmentId}/close`);
    }, 'The assignment is closed.');

    if (ok) void assignment.reload();
  }

  async function remove() {
    if (!window.confirm('Delete this assignment?')) return;

    const ok = await action.run(async () => {
      await api.delete(`/api/assignments/${assignmentId}`);
    });

    if (ok) router.push('/teacher');
  }

  if (assignment.loading) return <Loading />;
  if (assignment.error) return <Alert tone="error">{assignment.error}</Alert>;
  if (!assignment.data) return null;

  const current = assignment.data;
  const items = submissions.data?.items ?? [];
  const pending = items.filter((s) => s.status === 'Submitted' || s.status === 'UnderReview').length;

  return (
    <>
      <div className="mb-4">
        <Link href="/teacher" className="text-sm text-brand-700 hover:underline">
          &larr; Back to my assignments
        </Link>
      </div>

      <PageHeader
        title={current.title}
        description={`${current.classRoomName} - ${current.subjectName}`}
        action={
          <div className="flex flex-wrap gap-2">
            {!editing && current.status !== 'Closed' && (
              <Button variant="secondary" onClick={() => setEditing(true)}>
                Edit
              </Button>
            )}
            {current.status === 'Draft' && <Button onClick={publish}>Publish</Button>}
            {current.status === 'Published' && (
              <Button variant="secondary" onClick={close}>
                Close
              </Button>
            )}
            {current.submissionCount === 0 && (
              <Button variant="danger" onClick={remove}>
                Delete
              </Button>
            )}
          </div>
        }
      />

      {action.error && (
        <div className="mb-4">
          <Alert tone="error">{action.error}</Alert>
        </div>
      )}
      {action.message && (
        <div className="mb-4">
          <Alert tone="success">{action.message}</Alert>
        </div>
      )}

      {editing ? (
        <Card className="mb-6 p-6">
          <h2 className="mb-4 text-sm font-semibold uppercase tracking-wide text-slate-500">Edit assignment</h2>
          <AssignmentForm
            mode="edit"
            assignment={current}
            onCancel={() => setEditing(false)}
            onSaved={() => {
              setEditing(false);
              void assignment.reload();
            }}
          />
        </Card>
      ) : (
        <div className="mb-6 grid gap-4 lg:grid-cols-3">
          <Card className="p-5 lg:col-span-2">
            <h2 className="text-sm font-semibold uppercase tracking-wide text-slate-500">Instructions</h2>
            <p className="mt-2 whitespace-pre-wrap text-sm text-slate-700">{current.description}</p>
          </Card>

          <Card className="space-y-3 p-5 text-sm">
            <div className="flex items-center justify-between">
              <span className="text-slate-500">Status</span>
              <Badge value={current.status} />
            </div>
            <div className="flex items-center justify-between">
              <span className="text-slate-500">Deadline</span>
              <span className="text-right">
                {formatDateTime(current.deadlineUtc)}
                <span className={`block text-xs ${current.isPastDeadline ? 'text-rose-600' : 'text-slate-500'}`}>
                  {relativeToNow(current.deadlineUtc)}
                </span>
              </span>
            </div>
            <div className="flex items-center justify-between">
              <span className="text-slate-500">Maximum marks</span>
              <span>{current.maxMarks}</span>
            </div>
            <div className="flex items-center justify-between">
              <span className="text-slate-500">Late submissions</span>
              <span>{current.allowLateSubmission ? 'Allowed' : 'Not allowed'}</span>
            </div>
            <div className="flex items-center justify-between">
              <span className="text-slate-500">Updates allowed</span>
              <span>{current.allowResubmission ? 'Yes' : 'No'}</span>
            </div>
            <div className="flex items-center justify-between border-t border-slate-100 pt-3">
              <span className="text-slate-500">Submitted</span>
              <span>
                {current.submissionCount} of {current.classStudentCount}
                {pending > 0 && <span className="ml-2 text-xs text-amber-600">({pending} to mark)</span>}
              </span>
            </div>
          </Card>
        </div>
      )}

      <Card>
        <div className="border-b border-slate-200 px-4 py-3">
          <h2 className="text-sm font-semibold text-slate-900">Submissions</h2>
        </div>

        {submissions.loading && <Loading />}
        {submissions.error && <div className="p-4"><Alert tone="error">{submissions.error}</Alert></div>}

        {submissions.data && items.length === 0 && (
          <div className="p-4">
            <EmptyState
              title="No submissions yet"
              description={
                current.status === 'Draft'
                  ? 'Publish the assignment so the class can see it.'
                  : 'Students have not handed anything in so far.'
              }
            />
          </div>
        )}

        {items.length > 0 && (
          <Table
            head={
              <tr>
                <Th>Student</Th>
                <Th>Submitted</Th>
                <Th>Status</Th>
                <Th>Marks</Th>
                <Th className="text-right">Actions</Th>
              </tr>
            }
          >
            {items.map((submission) => (
              <tr key={submission.id} className="align-top hover:bg-slate-50">
                <Td>
                  <p className="font-medium text-slate-900">{submission.studentName}</p>
                  <p className="text-xs text-slate-500">{submission.studentEmail}</p>
                </Td>
                <Td>
                  <p>{formatDateTime(submission.submittedAtUtc)}</p>
                  {submission.isLate && <Badge value="Late" />}
                  {submission.revisionCount > 0 && (
                    <p className="text-xs text-slate-500">Revised {submission.revisionCount}x</p>
                  )}
                </Td>
                <Td><Badge value={submission.status} /></Td>
                <Td>
                  {submission.marks === null || submission.marks === undefined
                    ? '-'
                    : `${submission.marks} / ${current.maxMarks}`}
                </Td>
                <Td className="text-right">
                  <div className="flex flex-wrap justify-end gap-2">
                    <Button size="sm" onClick={() => openGrading(submission)}>
                      {submission.status === 'Graded' ? 'Re-grade' : 'Grade'}
                    </Button>
                    {submission.status !== 'UnderReview' && submission.status !== 'Graded' && (
                      <Button variant="ghost" size="sm" onClick={() => changeStatus(submission, 'UnderReview')}>
                        Mark reviewing
                      </Button>
                    )}
                    {submission.status !== 'ReturnedForRevision' && (
                      <Button variant="ghost" size="sm" onClick={() => changeStatus(submission, 'ReturnedForRevision')}>
                        Return
                      </Button>
                    )}
                  </div>
                </Td>
              </tr>
            ))}
          </Table>
        )}
      </Card>

      <Modal
        open={grading !== null}
        title={grading ? `${grading.studentName}'s submission` : ''}
        onClose={() => setGrading(null)}
        footer={
          <>
            <Button variant="secondary" onClick={() => setGrading(null)}>
              Cancel
            </Button>
            <Button onClick={saveGrade} loading={action.busy}>
              Save marks
            </Button>
          </>
        }
      >
        {grading && (
          <div className="space-y-4">
            {gradeError && <Alert tone="error">{gradeError}</Alert>}
            {action.error && <Alert tone="error">{action.error}</Alert>}

            <div className="rounded-lg bg-slate-50 p-4">
              <p className="mb-2 text-xs font-medium uppercase tracking-wide text-slate-500">Answer</p>
              <p className="whitespace-pre-wrap text-sm text-slate-700">{grading.content}</p>
              {grading.attachmentUrl && (
                <a
                  href={grading.attachmentUrl}
                  target="_blank"
                  rel="noreferrer"
                  className="mt-3 inline-block text-sm text-brand-700 underline"
                >
                  Open attachment
                </a>
              )}
            </div>

            <div className="grid gap-4 sm:grid-cols-2">
              <Field label={`Marks (out of ${current.maxMarks})`} required>
                <Input
                  type="number"
                  min={0}
                  max={current.maxMarks}
                  step="0.5"
                  value={marks}
                  onChange={(event) => setMarks(event.target.value)}
                />
              </Field>

              <Field label="Current status">
                <Select value={grading.status} disabled>
                  <option value={grading.status}>{grading.statusName}</option>
                </Select>
              </Field>
            </div>

            <Field label="Feedback" hint="Shown to the student together with their marks.">
              <Textarea
                value={feedback}
                onChange={(event) => setFeedback(event.target.value)}
                placeholder="What was done well, and what to improve next time."
              />
            </Field>
          </div>
        )}
      </Modal>
    </>
  );
}
