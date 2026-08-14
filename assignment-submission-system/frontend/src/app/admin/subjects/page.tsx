'use client';

import { useState } from 'react';
import { z } from 'zod';
import { api, query } from '@/lib/api';
import { useAction, useAsyncData } from '@/lib/hooks';
import type { PagedResult, Subject } from '@/lib/types';
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
  name: z.string().min(2, 'Subject name is required.').max(120),
  code: z.string().min(2, 'Subject code is required.').max(30),
  description: z.string().max(500).optional()
});

type FormState = { name: string; code: string; description: string; isActive: boolean };

const emptyForm: FormState = { name: '', code: '', description: '', isActive: true };

export default function AdminSubjectsPage() {
  const [page, setPage] = useState(1);
  const [modalOpen, setModalOpen] = useState(false);
  const [editing, setEditing] = useState<Subject | null>(null);
  const [form, setForm] = useState<FormState>(emptyForm);
  const [errors, setErrors] = useState<Record<string, string>>({});

  const action = useAction();

  const subjects = useAsyncData<PagedResult<Subject>>(
    () => api.get<PagedResult<Subject>>(`/api/subjects${query({ page, pageSize: 10 })}`),
    [page]
  );

  function openCreate() {
    setEditing(null);
    setForm(emptyForm);
    setErrors({});
    action.setError(null);
    setModalOpen(true);
  }

  function openEdit(subject: Subject) {
    setEditing(subject);
    setForm({
      name: subject.name,
      code: subject.code,
      description: subject.description ?? '',
      isActive: subject.isActive
    });
    setErrors({});
    action.setError(null);
    setModalOpen(true);
  }

  async function save() {
    const parsed = schema.safeParse({
      name: form.name,
      code: form.code,
      description: form.description || undefined
    });

    if (!parsed.success) {
      const flattened = parsed.error.flatten().fieldErrors;
      setErrors({
        name: flattened.name?.[0] ?? '',
        code: flattened.code?.[0] ?? '',
        description: flattened.description?.[0] ?? ''
      });
      return;
    }

    setErrors({});

    const body = { ...parsed.data, isActive: form.isActive };

    const ok = await action.run(async () => {
      if (editing) {
        await api.put(`/api/subjects/${editing.id}`, body);
      } else {
        await api.post('/api/subjects', body);
      }
    });

    if (ok) {
      setModalOpen(false);
      void subjects.reload();
    }
  }

  async function remove(subject: Subject) {
    if (!window.confirm(`Delete ${subject.name}?`)) return;

    const ok = await action.run(async () => {
      await api.delete(`/api/subjects/${subject.id}`);
    });

    if (ok) void subjects.reload();
  }

  return (
    <>
      <PageHeader
        title="Subjects"
        description="Subjects are combined with a class and a teacher before assignments can be created."
        action={<Button onClick={openCreate}>Add subject</Button>}
      />

      {action.error && !modalOpen && (
        <div className="mb-4">
          <Alert tone="error">{action.error}</Alert>
        </div>
      )}

      <Card>
        {subjects.loading && <Loading />}
        {subjects.error && <div className="p-4"><Alert tone="error">{subjects.error}</Alert></div>}

        {subjects.data && subjects.data.items.length === 0 && (
          <div className="p-4">
            <EmptyState title="No subjects yet" action={<Button onClick={openCreate}>Add subject</Button>} />
          </div>
        )}

        {subjects.data && subjects.data.items.length > 0 && (
          <>
            <Table
              head={
                <tr>
                  <Th>Subject</Th>
                  <Th>Code</Th>
                  <Th>Status</Th>
                  <Th className="text-right">Actions</Th>
                </tr>
              }
            >
              {subjects.data.items.map((subject) => (
                <tr key={subject.id} className="hover:bg-slate-50">
                  <Td>
                    <p className="font-medium text-slate-900">{subject.name}</p>
                    {subject.description && <p className="text-xs text-slate-500">{subject.description}</p>}
                  </Td>
                  <Td><span className="font-mono text-xs">{subject.code}</span></Td>
                  <Td><Badge value={subject.isActive ? 'Active' : 'Inactive'} /></Td>
                  <Td className="text-right">
                    <div className="flex justify-end gap-2">
                      <Button variant="secondary" size="sm" onClick={() => openEdit(subject)}>
                        Edit
                      </Button>
                      <Button variant="ghost" size="sm" className="text-rose-600" onClick={() => remove(subject)}>
                        Delete
                      </Button>
                    </div>
                  </Td>
                </tr>
              ))}
            </Table>

            <Pagination
              page={subjects.data.page}
              totalPages={subjects.data.totalPages}
              totalCount={subjects.data.totalCount}
              onChange={setPage}
            />
          </>
        )}
      </Card>

      <Modal
        open={modalOpen}
        title={editing ? `Edit ${editing.name}` : 'Add subject'}
        onClose={() => setModalOpen(false)}
        footer={
          <>
            <Button variant="secondary" onClick={() => setModalOpen(false)}>
              Cancel
            </Button>
            <Button onClick={save} loading={action.busy}>
              {editing ? 'Save changes' : 'Create subject'}
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
              placeholder="Mathematics"
            />
          </Field>

          <Field label="Code" required error={errors.code} hint="Stored in upper case and unique.">
            <Input
              value={form.code}
              onChange={(event) => setForm({ ...form, code: event.target.value })}
              placeholder="MATH-101"
            />
          </Field>

          <Field label="Description" error={errors.description}>
            <Textarea
              value={form.description}
              onChange={(event) => setForm({ ...form, description: event.target.value })}
              placeholder="Algebra, geometry and trigonometry."
            />
          </Field>

          {editing && (
            <Checkbox
              label="Subject is active"
              checked={form.isActive}
              onChange={(event) => setForm({ ...form, isActive: event.target.checked })}
            />
          )}
        </div>
      </Modal>
    </>
  );
}
