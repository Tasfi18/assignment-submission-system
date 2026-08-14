'use client';

import Link from 'next/link';
import { useState } from 'react';
import { api, query } from '@/lib/api';
import { formatDateTime, relativeToNow } from '@/lib/format';
import { useAction, useAsyncData } from '@/lib/hooks';
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
  Select,
  Table,
  Td,
  Th
} from '@/components/ui';

export default function TeacherAssignmentsPage() {
  const [page, setPage] = useState(1);
  const [status, setStatus] = useState('');
  const action = useAction();

  const assignments = useAsyncData<PagedResult<Assignment>>(
    () => api.get<PagedResult<Assignment>>(`/api/assignments${query({ page, pageSize: 10, status })}`),
    [page, status]
  );

  async function publish(assignment: Assignment) {
    const ok = await action.run(async () => {
      await api.post(`/api/assignments/${assignment.id}/publish`);
    }, `"${assignment.title}" is now visible to the class.`);

    if (ok) void assignments.reload();
  }

  async function close(assignment: Assignment) {
    if (!window.confirm(`Close "${assignment.title}"? No further submissions will be accepted.`)) return;

    const ok = await action.run(async () => {
      await api.post(`/api/assignments/${assignment.id}/close`);
    }, `"${assignment.title}" is closed.`);

    if (ok) void assignments.reload();
  }

  async function remove(assignment: Assignment) {
    if (!window.confirm(`Delete "${assignment.title}"?`)) return;

    const ok = await action.run(async () => {
      await api.delete(`/api/assignments/${assignment.id}`);
    }, 'Assignment deleted.');

    if (ok) void assignments.reload();
  }

  return (
    <>
      <PageHeader
        title="My assignments"
        description="Everything you created, across every class and subject assigned to you."
        action={
          <Link href="/teacher/assignments/new">
            <Button>New assignment</Button>
          </Link>
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

      <Card>
        <div className="flex flex-wrap items-end gap-3 border-b border-slate-200 p-4">
          <div className="w-full sm:w-44">
            <Field label="Status">
              <Select
                value={status}
                onChange={(event) => {
                  setPage(1);
                  setStatus(event.target.value);
                }}
              >
                <option value="">All</option>
                <option value="Draft">Draft</option>
                <option value="Published">Published</option>
                <option value="Closed">Closed</option>
              </Select>
            </Field>
          </div>
        </div>

        {assignments.loading && <Loading />}
        {assignments.error && <div className="p-4"><Alert tone="error">{assignments.error}</Alert></div>}

        {assignments.data && assignments.data.items.length === 0 && (
          <div className="p-4">
            <EmptyState
              title="No assignments yet"
              description="Create your first assignment and publish it when it is ready."
              action={
                <Link href="/teacher/assignments/new">
                  <Button>New assignment</Button>
                </Link>
              }
            />
          </div>
        )}

        {assignments.data && assignments.data.items.length > 0 && (
          <>
            <Table
              head={
                <tr>
                  <Th>Assignment</Th>
                  <Th>Class / subject</Th>
                  <Th>Deadline</Th>
                  <Th>Status</Th>
                  <Th>Submitted</Th>
                  <Th className="text-right">Actions</Th>
                </tr>
              }
            >
              {assignments.data.items.map((assignment) => (
                <tr key={assignment.id} className="hover:bg-slate-50">
                  <Td>
                    <Link href={`/teacher/assignments/${assignment.id}`} className="font-medium text-brand-700 hover:underline">
                      {assignment.title}
                    </Link>
                    <p className="text-xs text-slate-500">Max marks: {assignment.maxMarks}</p>
                  </Td>
                  <Td>
                    <p>{assignment.classRoomName}</p>
                    <p className="text-xs text-slate-500">{assignment.subjectName}</p>
                  </Td>
                  <Td>
                    <p>{formatDateTime(assignment.deadlineUtc)}</p>
                    <p className={`text-xs ${assignment.isPastDeadline ? 'text-rose-600' : 'text-slate-500'}`}>
                      {relativeToNow(assignment.deadlineUtc)}
                    </p>
                  </Td>
                  <Td><Badge value={assignment.status} /></Td>
                  <Td>
                    {assignment.submissionCount} / {assignment.classStudentCount}
                  </Td>
                  <Td className="text-right">
                    <div className="flex flex-wrap justify-end gap-2">
                      <Link href={`/teacher/assignments/${assignment.id}`}>
                        <Button variant="secondary" size="sm">Open</Button>
                      </Link>
                      {assignment.status === 'Draft' && (
                        <Button size="sm" onClick={() => publish(assignment)}>Publish</Button>
                      )}
                      {assignment.status === 'Published' && (
                        <Button variant="ghost" size="sm" onClick={() => close(assignment)}>Close</Button>
                      )}
                      {assignment.submissionCount === 0 && (
                        <Button variant="ghost" size="sm" className="text-rose-600" onClick={() => remove(assignment)}>
                          Delete
                        </Button>
                      )}
                    </div>
                  </Td>
                </tr>
              ))}
            </Table>

            <Pagination
              page={assignments.data.page}
              totalPages={assignments.data.totalPages}
              totalCount={assignments.data.totalCount}
              onChange={setPage}
            />
          </>
        )}
      </Card>
    </>
  );
}
