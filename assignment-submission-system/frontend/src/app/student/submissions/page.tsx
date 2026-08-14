'use client';

import Link from 'next/link';
import { useState } from 'react';
import { api, query } from '@/lib/api';
import { formatDateTime } from '@/lib/format';
import { useAsyncData } from '@/lib/hooks';
import type { PagedResult, Submission } from '@/lib/types';
import { Alert, Badge, Button, Card, EmptyState, Loading, PageHeader, Pagination } from '@/components/ui';

export default function StudentSubmissionsPage() {
  const [page, setPage] = useState(1);

  const submissions = useAsyncData<PagedResult<Submission>>(
    () => api.get<PagedResult<Submission>>(`/api/submissions/mine${query({ page, pageSize: 10 })}`),
    [page]
  );

  return (
    <>
      <PageHeader title="My submissions" description="What you handed in, and the marks and feedback you received." />

      {submissions.loading && <Loading />}
      {submissions.error && <Alert tone="error">{submissions.error}</Alert>}

      {submissions.data && submissions.data.items.length === 0 && (
        <EmptyState title="You have not submitted anything yet" description="Open an assignment to write your answer." />
      )}

      <div className="space-y-4">
        {submissions.data?.items.map((submission) => (
          <Card key={submission.id} className="p-5">
            <div className="flex flex-wrap items-start justify-between gap-4">
              <div className="min-w-0 flex-1">
                <div className="mb-1 flex flex-wrap items-center gap-2">
                  <p className="text-base font-semibold text-slate-900">{submission.assignmentTitle}</p>
                  <Badge value={submission.status} />
                  {submission.isLate && <Badge value="Late" />}
                </div>

                <p className="text-sm text-slate-500">
                  {submission.subjectName} &middot; submitted {formatDateTime(submission.submittedAtUtc)}
                  {submission.revisionCount > 0 && ` · revised ${submission.revisionCount}x`}
                </p>

                <p className="mt-3 line-clamp-3 whitespace-pre-wrap text-sm text-slate-600">{submission.content}</p>

                {submission.status === 'Graded' && (
                  <p className="mt-3 text-sm font-medium text-emerald-700">
                    Marks: {submission.marks} / {submission.assignmentMaxMarks}
                    {submission.gradedByName && (
                      <span className="ml-2 font-normal text-slate-500">marked by {submission.gradedByName}</span>
                    )}
                  </p>
                )}

                {submission.feedback && (
                  <div className="mt-3 rounded-lg border border-slate-200 bg-slate-50 p-3">
                    <p className="text-xs font-medium uppercase tracking-wide text-slate-500">Teacher feedback</p>
                    <p className="mt-1 text-sm text-slate-700">{submission.feedback}</p>
                  </div>
                )}
              </div>

              <Link href={`/student/assignments/${submission.assignmentId}`}>
                <Button variant="secondary">Open assignment</Button>
              </Link>
            </div>
          </Card>
        ))}
      </div>

      {submissions.data && submissions.data.items.length > 0 && (
        <Card className="mt-4">
          <Pagination
            page={submissions.data.page}
            totalPages={submissions.data.totalPages}
            totalCount={submissions.data.totalCount}
            onChange={setPage}
          />
        </Card>
      )}
    </>
  );
}
