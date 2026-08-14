'use client';

import { useState } from 'react';
import { api, query } from '@/lib/api';
import { formatDate } from '@/lib/format';
import { useAction, useAsyncData } from '@/lib/hooks';
import type { ClassRoom, PagedResult, Subject, TeachingAssignment, UserResponse } from '@/lib/types';
import {
  Alert,
  Button,
  Card,
  EmptyState,
  Field,
  Loading,
  Modal,
  PageHeader,
  Pagination,
  Select,
  Table,
  Td,
  Th
} from '@/components/ui';

/**
 * This mapping is the backbone of teacher authorisation: the API refuses to let a teacher create
 * or edit an assignment for a class/subject pair that is not listed here.
 */
export default function AdminTeachingAssignmentsPage() {
  const [page, setPage] = useState(1);
  const [modalOpen, setModalOpen] = useState(false);
  const [classRoomId, setClassRoomId] = useState('');
  const [subjectId, setSubjectId] = useState('');
  const [teacherId, setTeacherId] = useState('');
  const [validationError, setValidationError] = useState<string | null>(null);

  const action = useAction();

  const mappings = useAsyncData<PagedResult<TeachingAssignment>>(
    () => api.get<PagedResult<TeachingAssignment>>(`/api/teaching-assignments${query({ page, pageSize: 10 })}`),
    [page]
  );

  const classes = useAsyncData<PagedResult<ClassRoom>>(
    () => api.get<PagedResult<ClassRoom>>('/api/classes?pageSize=100'),
    []
  );

  const subjects = useAsyncData<PagedResult<Subject>>(
    () => api.get<PagedResult<Subject>>('/api/subjects?pageSize=100'),
    []
  );

  const teachers = useAsyncData<PagedResult<UserResponse>>(
    () => api.get<PagedResult<UserResponse>>('/api/users?role=Teacher&pageSize=100'),
    []
  );

  function openCreate() {
    setClassRoomId('');
    setSubjectId('');
    setTeacherId('');
    setValidationError(null);
    action.setError(null);
    setModalOpen(true);
  }

  async function save() {
    if (!classRoomId || !subjectId || !teacherId) {
      setValidationError('Select a class, a subject and a teacher.');
      return;
    }

    setValidationError(null);

    const ok = await action.run(async () => {
      await api.post('/api/teaching-assignments', { classRoomId, subjectId, teacherId });
    });

    if (ok) {
      setModalOpen(false);
      void mappings.reload();
    }
  }

  async function remove(mapping: TeachingAssignment) {
    if (!window.confirm(`Remove ${mapping.teacherName} from ${mapping.subjectName} in ${mapping.classRoomName}?`)) {
      return;
    }

    const ok = await action.run(async () => {
      await api.delete(`/api/teaching-assignments/${mapping.id}`);
    });

    if (ok) void mappings.reload();
  }

  return (
    <>
      <PageHeader
        title="Teacher mapping"
        description="Decide which teacher is responsible for a subject in a class. Teachers can only create assignments for their own mappings."
        action={<Button onClick={openCreate}>Assign teacher</Button>}
      />

      {action.error && !modalOpen && (
        <div className="mb-4">
          <Alert tone="error">{action.error}</Alert>
        </div>
      )}

      <Card>
        {mappings.loading && <Loading />}
        {mappings.error && <div className="p-4"><Alert tone="error">{mappings.error}</Alert></div>}

        {mappings.data && mappings.data.items.length === 0 && (
          <div className="p-4">
            <EmptyState
              title="No teachers assigned yet"
              description="Until a teacher is mapped to a class and subject, they cannot create any assignment."
              action={<Button onClick={openCreate}>Assign teacher</Button>}
            />
          </div>
        )}

        {mappings.data && mappings.data.items.length > 0 && (
          <>
            <Table
              head={
                <tr>
                  <Th>Teacher</Th>
                  <Th>Class</Th>
                  <Th>Subject</Th>
                  <Th>Assigned</Th>
                  <Th className="text-right">Actions</Th>
                </tr>
              }
            >
              {mappings.data.items.map((mapping) => (
                <tr key={mapping.id} className="hover:bg-slate-50">
                  <Td>
                    <p className="font-medium text-slate-900">{mapping.teacherName}</p>
                    <p className="text-xs text-slate-500">{mapping.teacherEmail}</p>
                  </Td>
                  <Td>{mapping.classRoomName}</Td>
                  <Td>{mapping.subjectName}</Td>
                  <Td>{formatDate(mapping.createdAtUtc)}</Td>
                  <Td className="text-right">
                    <Button variant="ghost" size="sm" className="text-rose-600" onClick={() => remove(mapping)}>
                      Remove
                    </Button>
                  </Td>
                </tr>
              ))}
            </Table>

            <Pagination
              page={mappings.data.page}
              totalPages={mappings.data.totalPages}
              totalCount={mappings.data.totalCount}
              onChange={setPage}
            />
          </>
        )}
      </Card>

      <Modal
        open={modalOpen}
        title="Assign a teacher"
        onClose={() => setModalOpen(false)}
        footer={
          <>
            <Button variant="secondary" onClick={() => setModalOpen(false)}>
              Cancel
            </Button>
            <Button onClick={save} loading={action.busy}>
              Assign
            </Button>
          </>
        }
      >
        <div className="space-y-4">
          {action.error && <Alert tone="error">{action.error}</Alert>}
          {validationError && <Alert tone="error">{validationError}</Alert>}

          <Field label="Teacher" required>
            <Select value={teacherId} onChange={(event) => setTeacherId(event.target.value)}>
              <option value="">Select a teacher</option>
              {teachers.data?.items.map((teacher) => (
                <option key={teacher.id} value={teacher.id}>
                  {teacher.fullName} ({teacher.email})
                </option>
              ))}
            </Select>
          </Field>

          <Field label="Class" required>
            <Select value={classRoomId} onChange={(event) => setClassRoomId(event.target.value)}>
              <option value="">Select a class</option>
              {classes.data?.items.map((classRoom) => (
                <option key={classRoom.id} value={classRoom.id}>
                  {classRoom.name} ({classRoom.code})
                </option>
              ))}
            </Select>
          </Field>

          <Field label="Subject" required>
            <Select value={subjectId} onChange={(event) => setSubjectId(event.target.value)}>
              <option value="">Select a subject</option>
              {subjects.data?.items.map((subject) => (
                <option key={subject.id} value={subject.id}>
                  {subject.name} ({subject.code})
                </option>
              ))}
            </Select>
          </Field>
        </div>
      </Modal>
    </>
  );
}
