'use client';

import { useState } from 'react';
import { z } from 'zod';
import { api, query } from '@/lib/api';
import { useAction, useAsyncData } from '@/lib/hooks';
import type { ClassRoom, PagedResult } from '@/lib/types';
import {
  Alert,
  Badge,
  Button,
  Card,
  Checkbox,
  EmptyState,
  Field,
  Input,
  Loading,
  Modal,
  PageHeader,
  Pagination,
  Table,
  Td,
  Textarea,
  Th
} from '@/components/ui';

const schema = z.object({
  name: z.string().min(2, 'Class name is required.').max(120),
  code: z.string().min(2, 'Class code is required.').max(30),
  academicYear: z
    .number({ invalid_type_error: 'Academic year must be a number.' })
    .int()
    .min(2000, 'Academic year must be 2000 or later.')
    .max(2100),
  description: z.string().max(500).optional()
});

type FormState = { name: string; code: string; academicYear: string; description: string; isActive: boolean };

const emptyForm: FormState = {
  name: '',
  code: '',
  academicYear: String(new Date().getFullYear()),
  description: '',
  isActive: true
};

export default function AdminClassesPage() {
  const [page, setPage] = useState(1);
  const [modalOpen, setModalOpen] = useState(false);
  const [editing, setEditing] = useState<ClassRoom | null>(null);
  const [form, setForm] = useState<FormState>(emptyForm);
  const [errors, setErrors] = useState<Record<string, string>>({});

  const action = useAction();

  const classes = useAsyncData<PagedResult<ClassRoom>>(
    () => api.get<PagedResult<ClassRoom>>(`/api/classes${query({ page, pageSize: 10 })}`),
    [page]
  );

  function openCreate() {
    setEditing(null);
    setForm(emptyForm);
    setErrors({});
    action.setError(null);
    setModalOpen(true);
  }

  function openEdit(classRoom: ClassRoom) {
    setEditing(classRoom);
    setForm({
      name: classRoom.name,
      code: classRoom.code,
      academicYear: String(classRoom.academicYear),
      description: classRoom.description ?? '',
      isActive: classRoom.isActive
    });
    setErrors({});
    action.setError(null);
    setModalOpen(true);
  }

  async function save() {
    const parsed = schema.safeParse({
      name: form.name,
      code: form.code,
      academicYear: Number(form.academicYear),
      description: form.description || undefined
    });

    if (!parsed.success) {
      const flattened = parsed.error.flatten().fieldErrors;
      setErrors({
        name: flattened.name?.[0] ?? '',
        code: flattened.code?.[0] ?? '',
        academicYear: flattened.academicYear?.[0] ?? '',
        description: flattened.description?.[0] ?? ''
      });
      return;
    }

    setErrors({});

    const body = { ...parsed.data, isActive: form.isActive };

    const ok = await action.run(async () => {
      if (editing) {
        await api.put(`/api/classes/${editing.id}`, body);
      } else {
        await api.post('/api/classes', body);
      }
    });

    if (ok) {
      setModalOpen(false);
      void classes.reload();
    }
  }

  async function remove(classRoom: ClassRoom) {
    if (!window.confirm(`Delete ${classRoom.name}?`)) return;

    const ok = await action.run(async () => {
      await api.delete(`/api/classes/${classRoom.id}`);
    });

    if (ok) void classes.reload();
  }

  return (
    <>
      <PageHeader
        title="Classes"
        description="A class groups the students who receive the same assignments."
        action={<Button onClick={openCreate}>Add class</Button>}
      />

      {action.error && !modalOpen && (
        <div className="mb-4">
          <Alert tone="error">{action.error}</Alert>
        </div>
      )}

      <Card>
        {classes.loading && <Loading />}
        {classes.error && <div className="p-4"><Alert tone="error">{classes.error}</Alert></div>}

        {classes.data && classes.data.items.length === 0 && (
          <div className="p-4">
            <EmptyState
              title="No classes yet"
              description="Create a class before adding students or assignments."
              action={<Button onClick={openCreate}>Add class</Button>}
            />
          </div>
        )}

        {classes.data && classes.data.items.length > 0 && (
          <>
            <Table
              head={
                <tr>
                  <Th>Class</Th>
                  <Th>Code</Th>
                  <Th>Year</Th>
                  <Th>Students</Th>
                  <Th>Status</Th>
                  <Th className="text-right">Actions</Th>
                </tr>
              }
            >
              {classes.data.items.map((classRoom) => (
                <tr key={classRoom.id} className="hover:bg-slate-50">
                  <Td>
                    <p className="font-medium text-slate-900">{classRoom.name}</p>
                    {classRoom.description && <p className="text-xs text-slate-500">{classRoom.description}</p>}
                  </Td>
                  <Td><span className="font-mono text-xs">{classRoom.code}</span></Td>
                  <Td>{classRoom.academicYear}</Td>
                  <Td>{classRoom.studentCount}</Td>
                  <Td><Badge value={classRoom.isActive ? 'Active' : 'Inactive'} /></Td>
                  <Td className="text-right">
                    <div className="flex justify-end gap-2">
                      <Button variant="secondary" size="sm" onClick={() => openEdit(classRoom)}>
                        Edit
                      </Button>
                      <Button variant="ghost" size="sm" className="text-rose-600" onClick={() => remove(classRoom)}>
                        Delete
                      </Button>
                    </div>
                  </Td>
                </tr>
              ))}
            </Table>

            <Pagination
              page={classes.data.page}
              totalPages={classes.data.totalPages}
              totalCount={classes.data.totalCount}
              onChange={setPage}
            />
          </>
        )}
      </Card>

      <Modal
        open={modalOpen}
        title={editing ? `Edit ${editing.name}` : 'Add class'}
        onClose={() => setModalOpen(false)}
        footer={
          <>
            <Button variant="secondary" onClick={() => setModalOpen(false)}>
              Cancel
            </Button>
            <Button onClick={save} loading={action.busy}>
              {editing ? 'Save changes' : 'Create class'}
            </Button>
          </>
        }
      >
        <div className="space-y-4">
          {action.error && <Alert tone="error">{action.error}</Alert>}

          <Field label="Name" required error={errors.name}>
            <Input
              value={form.name}
              onChange={(event) => setForm({ ...form, name: event.target.value })}
              placeholder="Class 10 - Section A"
            />
          </Field>

          <div className="grid gap-4 sm:grid-cols-2">
            <Field label="Code" required error={errors.code} hint="Stored in upper case and unique.">
              <Input
                value={form.code}
                onChange={(event) => setForm({ ...form, code: event.target.value })}
                placeholder="C10A"
              />
            </Field>

            <Field label="Academic year" required error={errors.academicYear}>
              <Input
                type="number"
                value={form.academicYear}
                onChange={(event) => setForm({ ...form, academicYear: event.target.value })}
              />
            </Field>
          </div>

          <Field label="Description" error={errors.description}>
            <Textarea
              value={form.description}
              onChange={(event) => setForm({ ...form, description: event.target.value })}
              placeholder="Science group, morning shift."
            />
          </Field>

          {editing && (
            <Checkbox
              label="Class is active"
              checked={form.isActive}
              onChange={(event) => setForm({ ...form, isActive: event.target.checked })}
            />
          )}
        </div>
      </Modal>
    </>
  );
}
