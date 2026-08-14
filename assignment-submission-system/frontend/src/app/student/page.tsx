'use client';

import Link from 'next/link';
import { useState } from 'react';
import { api, query } from '@/lib/api';
import { formatDateTime, relativeToNow } from '@/lib/format';
import { useAsyncData } from '@/lib/hooks';
import type { Assignment, PagedResult } from '@/lib/types';
import {
  Alert,
  Badge,
  Button,
  Card,
  EmptyState,
  Field,
  Loading,
  PageHeader,
  Pagination,
  Select
} from '@/components/ui';

/** Students only ever receive published (or closed) assignments for their own class from the API. */
export default function StudentAssignmentsPage() {
  const [page, setPage] = useState(1);
  const [upcomingOnly, setUpcomingOnly] = useState('');

  const assignments = useAsyncData<PagedResult<Assignment>>(
    () => api.get<PagedResult<Assignment>>(`/api/assignments${query({ page, pageSize: 10, upcomingOnly })}`),
    [page, upcomingOnly]
  );

  return (
    <>
      <PageHeader title="Assignments" description="Everything your teachers have set for your class." />

      <div className="mb-4 w-full sm:w-56">
        <Field label="Show">
          <Select
            value={upcomingOnly}
            onChange={(event) => {
              setPage(1);
              setUpcomingOnly(event.target.value);
            }}
          >
            <option value="">All assignments</option>
            <option value="true">Deadline not passed</option>
          </Select>
        </Field>
      </div>

      {assignments.loading && <Loading />}
      {assignments.error && <Alert tone="error">{assignments.error}</Alert>}

      {assignments.data && assignments.data.items.length === 0 && (
        <EmptyState title="Nothing to do right now" description="New assignments will appear here as soon as a teacher publishes them." />
      )}

      <div className="space-y-4">
        {assignments.data?.items.map((assignment) => {
          const submission = assignment.mySubmission;
          const canSubmit = assignment.status === 'Published' && !submission;

          return (
            <Card key={assignment.id} className="p-5">
              <div className="flex flex-wrap items-start justify-between gap-4">
                <div className="min-w-0 flex-1">
                  <div className="mb-1 flex flex-wrap items-center gap-2">
                    <Link
                      href={`/student/assignments/${assignment.id}`}
                      className="text-base font-semibold text-slate-900 hover:text-brand-700"
                    >
                      {assignment.title}
                    </Link>
                    <Badge value={assignment.status} />
                    {submission && <Badge value={submission.status} />}
                    {submission?.isLate && <Badge value="Late" />}
                  </div>

                  <p className="text-sm text-slate-500">
                    {assignment.subjectName} &middot; {assignment.teacherName} &middot; {assignment.maxMarks} marks
                  </p>

                  <p className="mt-2 line-clamp-2 text-sm text-slate-600">{assignment.description}</p>

                  <p className="mt-3 text-sm">
                    <span className="text-slate-500">Deadline: </span>
                    {formatDateTime(assignment.deadlineUtc)}
                    <span className={`ml-2 text-xs ${assignment.isPastDeadline ? 'text-rose-600' : 'text-emerald-600'}`}>
                      {relativeToNow(assignment.deadlineUtc)}
                    </span>
                  </p>

                  {submission?.status === 'Graded' && (
                    <p className="mt-2 text-sm font-medium text-emerald-700">
                      Marks: {submission.marks} / {assignment.maxMarks}
                    </p>
                  )}
                </div>

                <Link href={`/student/assignments/${assignment.id}`}>
                  <Button variant={canSubmit ? 'primary' : 'secondary'}>
                    {canSubmit ? 'Submit answer' : 'View details'}
                  </Button>
                </Link>
              </div>
            </Card>
          );
        })}
      </div>

      {assignments.data && assignments.data.items.length > 0 && (
        <Card className="mt-4">
          <Pagination
            page={assignments.data.page}
            totalPages={assignments.data.totalPages}
            totalCount={assignments.data.totalCount}
            onChange={setPage}
          />
        </Card>
      )}
    </>
  );
}
