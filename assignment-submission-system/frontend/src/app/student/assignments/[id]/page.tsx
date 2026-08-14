'use client';

import Link from 'next/link';
import { useParams } from 'next/navigation';
import { useEffect, useState } from 'react';
import { z } from 'zod';
import { api, query } from '@/lib/api';
import { formatDateTime, relativeToNow } from '@/lib/format';
import { useAction, useAsyncData } from '@/lib/hooks';
import type { Assignment, PagedResult, Submission } from '@/lib/types';
import { Alert, Badge, Button, Card, Field, Input, Loading, PageHeader, Textarea } from '@/components/ui';

const schema = z.object({
  content: z.string().min(1, 'Write your answer before submitting.').max(20000, 'The answer is too long.'),
  attachmentUrl: z.string().url('Enter a valid URL, or leave the field empty.').optional().or(z.literal(''))
});

export default function StudentAssignmentDetailPage() {
  const params = useParams<{ id: string }>();
  const assignmentId = params.id;

  const [content, setContent] = useState('');
  const [attachmentUrl, setAttachmentUrl] = useState('');
  const [errors, setErrors] = useState<Record<string, string>>({});

  const action = useAction();

  const assignment = useAsyncData<Assignment>(
    () => api.get<Assignment>(`/api/assignments/${assignmentId}`),
    [assignmentId]
  );

  const mySubmissions = useAsyncData<PagedResult<Submission>>(
    () => api.get<PagedResult<Submission>>(`/api/submissions/mine${query({ assignmentId, pageSize: 1 })}`),
    [assignmentId]
  );

  const submission = mySubmissions.data?.items[0] ?? null;

  useEffect(() => {
    if (submission) {
      setContent(submission.content);
      setAttachmentUrl(submission.attachmentUrl ?? '');
    }
  }, [submission]);

  async function submit(event: React.FormEvent) {
    event.preventDefault();

    const parsed = schema.safeParse({ content, attachmentUrl: attachmentUrl || '' });

    if (!parsed.success) {
      const flattened = parsed.error.flatten().fieldErrors;
      setErrors({
        content: flattened.content?.[0] ?? '',
        attachmentUrl: flattened.attachmentUrl?.[0] ?? ''
      });
      return;
    }

    setErrors({});

    const body = {
      content: parsed.data.content,
      attachmentUrl: parsed.data.attachmentUrl ? parsed.data.attachmentUrl : null
    };

    const ok = await action.run(async () => {
      if (submission) {
        await api.put(`/api/submissions/${submission.id}`, body);
      } else {
        await api.post(`/api/assignments/${assignmentId}/submissions`, body);
      }
    }, submission ? 'Your answer has been updated.' : 'Your answer has been submitted.');

    if (ok) {
      void mySubmissions.reload();
      void assignment.reload();
    }
  }

  if (assignment.loading) return <Loading />;
  if (assignment.error) return <Alert tone="error">{assignment.error}</Alert>;
  if (!assignment.data) return null;

  const current = assignment.data;

  const isClosed = current.status === 'Closed';
  const isGraded = submission?.status === 'Graded';
  const isRejected = submission?.status === 'Rejected';
  const returnedForRevision = submission?.status === 'ReturnedForRevision';

  // Mirrors the server-side rules so the form is not offered when the API would refuse it.
  const deadlinePassed = current.isPastDeadline;
  const lateBlocked = deadlinePassed && !current.allowLateSubmission;
  const updateBlocked = Boolean(submission) && !current.allowResubmission && !returnedForRevision;
  const canWrite = !isClosed && !isGraded && !isRejected && !lateBlocked && !updateBlocked;

  return (
    <>
      <div className="mb-4">
        <Link href="/student" className="text-sm text-brand-700 hover:underline">
          &larr; Back to assignments
        </Link>
      </div>

      <PageHeader title={current.title} description={`${current.subjectName} - set by ${current.teacherName}`} />

      <div className="grid gap-4 lg:grid-cols-3">
        <Card className="p-5 lg:col-span-2">
          <h2 className="text-sm font-semibold uppercase tracking-wide text-slate-500">Instructions</h2>
          <p className="mt-2 whitespace-pre-wrap text-sm text-slate-700">{current.description}</p>
        </Card>

        <Card className="space-y-3 p-5 text-sm">
          <div className="flex items-center justify-between">
            <span className="text-slate-500">Status</span>
            <Badge value={current.status} />
          </div>
          <div className="flex items-start justify-between">
            <span className="text-slate-500">Deadline</span>
            <span className="text-right">
              {formatDateTime(current.deadlineUtc)}
              <span className={`block text-xs ${deadlinePassed ? 'text-rose-600' : 'text-emerald-600'}`}>
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
            <span>{current.allowLateSubmission ? 'Accepted' : 'Not accepted'}</span>
          </div>
          <div className="flex items-center justify-between">
            <span className="text-slate-500">Can be updated</span>
            <span>{current.allowResubmission ? 'Yes, until graded' : 'No'}</span>
          </div>
        </Card>
      </div>

      {submission && (
        <Card className="mt-4 p-5">
          <div className="flex flex-wrap items-center justify-between gap-2">
            <h2 className="text-sm font-semibold uppercase tracking-wide text-slate-500">Your submission</h2>
            <div className="flex items-center gap-2">
              <Badge value={submission.status} />
              {submission.isLate && <Badge value="Late" />}
            </div>
          </div>

          <p className="mt-2 text-xs text-slate-500">
            Submitted {formatDateTime(submission.submittedAtUtc)}
            {submission.lastUpdatedAtUtc && ` · last updated ${formatDateTime(submission.lastUpdatedAtUtc)}`}
            {submission.revisionCount > 0 && ` · revised ${submission.revisionCount}x`}
          </p>

          {submission.status === 'Graded' && (
            <p className="mt-3 text-base font-semibold text-emerald-700">
              {submission.marks} / {current.maxMarks}
              {submission.gradedByName && (
                <span className="ml-2 text-sm font-normal text-slate-500">marked by {submission.gradedByName}</span>
              )}
            </p>
          )}

          {submission.feedback && (
            <div className="mt-3 rounded-lg border border-slate-200 bg-slate-50 p-3">
              <p className="text-xs font-medium uppercase tracking-wide text-slate-500">Teacher feedback</p>
              <p className="mt-1 text-sm text-slate-700">{submission.feedback}</p>
            </div>
          )}
        </Card>
      )}

      <Card className="mt-4 p-5">
        <h2 className="mb-4 text-sm font-semibold uppercase tracking-wide text-slate-500">
          {submission ? 'Update your answer' : 'Write your answer'}
        </h2>

        {action.message && (
          <div className="mb-4">
            <Alert tone="success">{action.message}</Alert>
          </div>
        )}
        {action.error && (
          <div className="mb-4">
            <Alert tone="error">{action.error}</Alert>
          </div>
        )}

        {!canWrite && (
          <Alert tone="info">
            {isClosed && 'This assignment is closed, so it no longer accepts answers.'}
            {!isClosed && isGraded && 'Your submission has been graded and can no longer be changed.'}
            {!isClosed && isRejected && 'Your submission was rejected. Please speak to your teacher.'}
            {!isClosed && !isGraded && !isRejected && lateBlocked && 'The deadline has passed and this assignment does not accept late submissions.'}
            {!isClosed && !isGraded && !isRejected && !lateBlocked && updateBlocked && 'This assignment does not allow a submitted answer to be changed.'}
          </Alert>
        )}

        {canWrite && (
          <form onSubmit={submit} className="space-y-4" noValidate>
            {deadlinePassed && current.allowLateSubmission && (
              <Alert tone="warning">
                The deadline has passed. Your work will still be accepted but will be recorded as late.
              </Alert>
            )}

            {returnedForRevision && (
              <Alert tone="warning">
                Your teacher asked for a revision. Update your answer and submit it again.
              </Alert>
            )}

            <Field label="Answer" required error={errors.content}>
              <Textarea
                className="min-h-[220px]"
                value={content}
                onChange={(event) => setContent(event.target.value)}
                placeholder="Type your full answer here."
              />
            </Field>

            <Field
              label="Attachment link"
              error={errors.attachmentUrl}
              hint="Optional. Paste a link to a document, repository or hosted page."
            >
              <Input
                type="url"
                value={attachmentUrl}
                onChange={(event) => setAttachmentUrl(event.target.value)}
                placeholder="https://drive.google.com/..."
              />
            </Field>

            <div className="flex justify-end">
              <Button type="submit" loading={action.busy}>
                {submission ? 'Update submission' : 'Submit answer'}
              </Button>
            </div>
          </form>
        )}
      </Card>
    </>
  );
}
