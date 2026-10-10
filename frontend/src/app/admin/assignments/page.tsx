'use client';

import { useState } from 'react';
import { api, query } from '@/lib/api';
import { formatDateTime, relativeToNow } from '@/lib/format';
import { useAsyncData } from '@/lib/hooks';
import type { Assignment, ClassRoom, PagedResult } from '@/lib/types';
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

/** Read-only oversight: administrators can see every assignment but only teachers may change them. */
export default function AdminAssignmentsPage() {
  const [page, setPage] = useState(1);
  const [status, setStatus] = useState('');
  const [classRoomId, setClassRoomId] = useState('');
  const [search, setSearch] = useState('');
  const [searchTerm, setSearchTerm] = useState('');

  const assignments = useAsyncData<PagedResult<Assignment>>(
    () =>
      api.get<PagedResult<Assignment>>(
        `/api/assignments${query({ page, pageSize: 10, status, classRoomId, search: searchTerm })}`
      ),
    [page, status, classRoomId, searchTerm]
  );

  const classes = useAsyncData<PagedResult<ClassRoom>>(
    () => api.get<PagedResult<ClassRoom>>('/api/classes?pageSize=100'),
    []
  );

  return (
    <>
      <PageHeader title="Assignments" description="Every assignment in the institution, across all teachers." />

      <Card>
        <div className="flex flex-wrap items-end gap-3 border-b border-slate-200 p-4">
          <div className="w-full sm:w-56">
            <Field label="Search">
              <Input
                placeholder="Title or description"
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
          <div className="w-full sm:w-40">
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
          <div className="w-full sm:w-56">
            <Field label="Class">
              <Select
                value={classRoomId}
                onChange={(event) => {
                  setPage(1);
                  setClassRoomId(event.target.value);
                }}
              >
                <option value="">All classes</option>
                {classes.data?.items.map((classRoom) => (
                  <option key={classRoom.id} value={classRoom.id}>
                    {classRoom.name}
                  </option>
                ))}
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

        {assignments.loading && <Loading />}
        {assignments.error && <div className="p-4"><Alert tone="error">{assignments.error}</Alert></div>}

        {assignments.data && assignments.data.items.length === 0 && (
          <div className="p-4">
            <EmptyState title="No assignments found" description="Adjust the filters to widen the search." />
          </div>
        )}

        {assignments.data && assignments.data.items.length > 0 && (
          <>
            <Table
              head={
                <tr>
                  <Th>Assignment</Th>
                  <Th>Class / subject</Th>
                  <Th>Teacher</Th>
                  <Th>Deadline</Th>
                  <Th>Status</Th>
                  <Th>Submissions</Th>
                </tr>
              }
            >
              {assignments.data.items.map((assignment) => (
                <tr key={assignment.id} className="hover:bg-slate-50">
                  <Td>
                    <p className="font-medium text-slate-900">{assignment.title}</p>
                    <p className="text-xs text-slate-500">Max marks: {assignment.maxMarks}</p>
                  </Td>
                  <Td>
                    <p>{assignment.classRoomName}</p>
                    <p className="text-xs text-slate-500">{assignment.subjectName}</p>
                  </Td>
                  <Td>{assignment.teacherName}</Td>
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
