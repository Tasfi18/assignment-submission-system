'use client';

import Link from 'next/link';
import { useState } from 'react';
import { api, query } from '@/lib/api';
import { formatDateTime } from '@/lib/format';
import { useAsyncData } from '@/lib/hooks';
import type { PagedResult, Submission } from '@/lib/types';
import {
  Alert,
  Badge,
  Button,
  Card,
  EmptyState,
  Field,
  Input,
  Loading,
  PageHeader,
  Pagination,
  Select,
  Table,
  Td,
  Th
} from '@/components/ui';

/** Every answer handed in for the assignments this teacher owns. */
export default function TeacherSubmissionsPage() {
  const [page, setPage] = useState(1);
  const [status, setStatus] = useState('');
  const [search, setSearch] = useState('');
  const [searchTerm, setSearchTerm] = useState('');

  const submissions = useAsyncData<PagedResult<Submission>>(
    () =>
      api.get<PagedResult<Submission>>(
        `/api/submissions${query({ page, pageSize: 10, status, search: searchTerm })}`
      ),
    [page, status, searchTerm]
  );

  return (
    <>
      <PageHeader title="Submissions" description="Work waiting to be marked, and everything already graded." />

      <Card>
        <div className="flex flex-wrap items-end gap-3 border-b border-slate-200 p-4">
          <div className="w-full sm:w-56">
            <Field label="Search">
              <Input
                placeholder="Student or assignment"
                value={search}
                onChange={(event) => setSearch(event.target.value)}
                onKeyDown={(event) => {
                  if (event.key === 'Enter') {
                    setPage(1);
                    setSearchTerm(search);
                  }
                }}
              />
            </Field>
          </div>
          <div className="w-full sm:w-48">
            <Field label="Status">
              <Select
                value={status}
                onChange={(event) => {
                  setPage(1);
                  setStatus(event.target.value);
                }}
              >
                <option value="">All</option>
                <option value="Submitted">Submitted</option>
                <option value="UnderReview">Under review</option>
                <option value="Graded">Graded</option>
                <option value="ReturnedForRevision">Returned for revision</option>
                <option value="Rejected">Rejected</option>
              </Select>
            </Field>
          </div>
          <Button
            variant="secondary"
            onClick={() => {
              setPage(1);
              setSearchTerm(search);
            }}
          >
            Apply
          </Button>
        </div>

        {submissions.loading && <Loading />}
        {submissions.error && <div className="p-4"><Alert tone="error">{submissions.error}</Alert></div>}

        {submissions.data && submissions.data.items.length === 0 && (
          <div className="p-4">
            <EmptyState title="Nothing here yet" description="Submissions appear as soon as students hand in their work." />
          </div>
        )}

        {submissions.data && submissions.data.items.length > 0 && (
          <>
            <Table
              head={
                <tr>
                  <Th>Student</Th>
                  <Th>Assignment</Th>
                  <Th>Submitted</Th>
                  <Th>Status</Th>
                  <Th>Marks</Th>
                  <Th className="text-right">Actions</Th>
                </tr>
              }
            >
              {submissions.data.items.map((submission) => (
                <tr key={submission.id} className="hover:bg-slate-50">
                  <Td>
                    <p className="font-medium text-slate-900">{submission.studentName}</p>
                    <p className="text-xs text-slate-500">{submission.classRoomName}</p>
                  </Td>
                  <Td>
                    <p>{submission.assignmentTitle}</p>
                    <p className="text-xs text-slate-500">{submission.subjectName}</p>
                  </Td>
                  <Td>
                    <p>{formatDateTime(submission.submittedAtUtc)}</p>
                    {submission.isLate && <Badge value="Late" />}
                  </Td>
                  <Td><Badge value={submission.status} /></Td>
                  <Td>
                    {submission.marks === null || submission.marks === undefined
                      ? '-'
                      : `${submission.marks} / ${submission.assignmentMaxMarks}`}
                  </Td>
                  <Td className="text-right">
                    <Link href={`/teacher/assignments/${submission.assignmentId}`}>
                      <Button variant="secondary" size="sm">Open assignment</Button>
                    </Link>
                  </Td>
                </tr>
              ))}
            </Table>

            <Pagination
              page={submissions.data.page}
              totalPages={submissions.data.totalPages}
              totalCount={submissions.data.totalCount}
              onChange={setPage}
            />
          </>
        )}
      </Card>
    </>
  );
}
