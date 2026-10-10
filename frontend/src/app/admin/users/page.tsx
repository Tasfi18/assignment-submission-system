'use client';

import { useState } from 'react';
import { z } from 'zod';
import { api, query } from '@/lib/api';
import { formatDate } from '@/lib/format';
import { useAction, useAsyncData } from '@/lib/hooks';
import type { ClassRoom, PagedResult, UserResponse, UserRole } from '@/lib/types';
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
  Select,
  Table,
  Td,
  Th
} from '@/components/ui';

const baseSchema = {
  fullName: z.string().min(2, 'Full name is required.').max(150),
  email: z.string().min(1, 'Email is required.').email('Enter a valid email address.')
};

const createSchema = z
  .object({
    ...baseSchema,
    password: z.string().min(6, 'Password must be at least 6 characters.'),
    role: z.enum(['Admin', 'Teacher', 'Student']),
    classRoomId: z.string().optional()
  })
  .refine((value) => value.role !== 'Student' || Boolean(value.classRoomId), {
    message: 'A student must be assigned to a class.',
    path: ['classRoomId']
  });

const updateSchema = z
  .object({
    ...baseSchema,
    isActive: z.boolean(),
    classRoomId: z.string().optional(),
    newPassword: z.string().max(128).optional()
  })
  .refine((value) => !value.newPassword || value.newPassword.length >= 6, {
    message: 'Password must be at least 6 characters.',
    path: ['newPassword']
  });

type FormState = {
  fullName: string;
  email: string;
  password: string;
  role: UserRole;
  classRoomId: string;
  isActive: boolean;
};

const emptyForm: FormState = {
  fullName: '',
  email: '',
  password: '',
  role: 'Student',
  classRoomId: '',
  isActive: true
};

export default function AdminUsersPage() {
  const [page, setPage] = useState(1);
  const [roleFilter, setRoleFilter] = useState('');
  const [search, setSearch] = useState('');
  const [searchTerm, setSearchTerm] = useState('');

  const [modalOpen, setModalOpen] = useState(false);
  const [editing, setEditing] = useState<UserResponse | null>(null);
  const [form, setForm] = useState<FormState>(emptyForm);
  const [fieldErrors, setFieldErrors] = useState<Record<string, string>>({});

  const action = useAction();

  const users = useAsyncData<PagedResult<UserResponse>>(
    () =>
      api.get<PagedResult<UserResponse>>(
        `/api/users${query({ page, pageSize: 10, role: roleFilter, search: searchTerm })}`
      ),
    [page, roleFilter, searchTerm]
  );

  const classes = useAsyncData<PagedResult<ClassRoom>>(
    () => api.get<PagedResult<ClassRoom>>('/api/classes?pageSize=100'),
    []
  );

  function openCreate() {
    setEditing(null);
    setForm(emptyForm);
    setFieldErrors({});
    action.setError(null);
    setModalOpen(true);
  }

  function openEdit(user: UserResponse) {
    setEditing(user);
    setForm({
      fullName: user.fullName,
      email: user.email,
      password: '',
      role: user.role,
      classRoomId: user.classRoomId ?? '',
      isActive: user.isActive
    });
    setFieldErrors({});
    action.setError(null);
    setModalOpen(true);
  }

  async function save() {
    setFieldErrors({});

    if (editing) {
      const parsed = updateSchema.safeParse({
        fullName: form.fullName,
        email: form.email,
        isActive: form.isActive,
        classRoomId: form.classRoomId || undefined,
        newPassword: form.password || undefined
      });

      if (!parsed.success) {
        setFieldErrors(collectErrors(parsed.error));
        return;
      }

      const ok = await action.run(async () => {
        await api.put(`/api/users/${editing.id}`, {
          fullName: parsed.data.fullName,
          email: parsed.data.email,
          isActive: parsed.data.isActive,
          classRoomId: form.role === 'Student' ? parsed.data.classRoomId ?? null : null,
          newPassword: parsed.data.newPassword ?? null
        });
      });

      if (ok) {
        setModalOpen(false);
        void users.reload();
      }

      return;
    }

    const parsed = createSchema.safeParse({
      fullName: form.fullName,
      email: form.email,
      password: form.password,
      role: form.role,
      classRoomId: form.classRoomId || undefined
    });

    if (!parsed.success) {
      setFieldErrors(collectErrors(parsed.error));
      return;
    }

    const ok = await action.run(async () => {
      await api.post('/api/users', {
        fullName: parsed.data.fullName,
        email: parsed.data.email,
        password: parsed.data.password,
        role: parsed.data.role,
        classRoomId: parsed.data.role === 'Student' ? parsed.data.classRoomId : null
      });
    });

    if (ok) {
      setModalOpen(false);
      void users.reload();
    }
  }

  async function toggleStatus(user: UserResponse) {
    const ok = await action.run(async () => {
      await api.patch(`/api/users/${user.id}/status${query({ isActive: !user.isActive })}`);
    });

    if (ok) void users.reload();
  }

  async function remove(user: UserResponse) {
    if (!window.confirm(`Delete ${user.fullName}? This cannot be undone.`)) return;

    const ok = await action.run(async () => {
      await api.delete(`/api/users/${user.id}`);
    });

    if (ok) void users.reload();
  }

  return (
    <>
      <PageHeader
        title="Users"
        description="Create and maintain admin, teacher and student accounts."
        action={<Button onClick={openCreate}>Add user</Button>}
      />

      {action.error && !modalOpen && (
        <div className="mb-4">
          <Alert tone="error">{action.error}</Alert>
        </div>
      )}

      <Card>
        <div className="flex flex-wrap items-end gap-3 border-b border-slate-200 p-4">
          <div className="w-full sm:w-64">
            <Field label="Search">
              <Input
                placeholder="Name or email"
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
            <Field label="Role">
              <Select
                value={roleFilter}
                onChange={(event) => {
                  setPage(1);
                  setRoleFilter(event.target.value);
                }}
              >
                <option value="">All roles</option>
                <option value="Admin">Admin</option>
                <option value="Teacher">Teacher</option>
                <option value="Student">Student</option>
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

        {users.loading && <Loading />}
        {users.error && <div className="p-4"><Alert tone="error">{users.error}</Alert></div>}

        {users.data && users.data.items.length === 0 && (
          <div className="p-4">
            <EmptyState title="No accounts found" description="Try a different search or filter." />
          </div>
        )}

        {users.data && users.data.items.length > 0 && (
          <>
            <Table
              head={
                <tr>
                  <Th>Name</Th>
                  <Th>Role</Th>
                  <Th>Class</Th>
                  <Th>Status</Th>
                  <Th>Created</Th>
                  <Th className="text-right">Actions</Th>
                </tr>
              }
            >
              {users.data.items.map((user) => (
                <tr key={user.id} className="hover:bg-slate-50">
                  <Td>
                    <p className="font-medium text-slate-900">{user.fullName}</p>
                    <p className="text-xs text-slate-500">{user.email}</p>
                  </Td>
                  <Td><Badge value={user.role} /></Td>
                  <Td>{user.classRoomName ?? '-'}</Td>
                  <Td><Badge value={user.isActive ? 'Active' : 'Inactive'} /></Td>
                  <Td>{formatDate(user.createdAtUtc)}</Td>
                  <Td className="text-right">
                    <div className="flex justify-end gap-2">
                      <Button variant="secondary" size="sm" onClick={() => openEdit(user)}>
                        Edit
                      </Button>
                      <Button variant="ghost" size="sm" onClick={() => toggleStatus(user)}>
                        {user.isActive ? 'Deactivate' : 'Activate'}
                      </Button>
                      <Button variant="ghost" size="sm" className="text-rose-600" onClick={() => remove(user)}>
                        Delete
                      </Button>
                    </div>
                  </Td>
                </tr>
              ))}
            </Table>

            <Pagination
              page={users.data.page}
              totalPages={users.data.totalPages}
              totalCount={users.data.totalCount}
              onChange={setPage}
            />
          </>
        )}
      </Card>

      <Modal
        open={modalOpen}
        title={editing ? `Edit ${editing.fullName}` : 'Add user'}
        onClose={() => setModalOpen(false)}
        footer={
          <>
            <Button variant="secondary" onClick={() => setModalOpen(false)}>
              Cancel
            </Button>
            <Button onClick={save} loading={action.busy}>
              {editing ? 'Save changes' : 'Create user'}
            </Button>
          </>
        }
      >
        <div className="space-y-4">
          {action.error && <Alert tone="error">{action.error}</Alert>}

          <Field label="Full name" required error={fieldErrors.fullName}>
            <Input
              value={form.fullName}
              onChange={(event) => setForm({ ...form, fullName: event.target.value })}
              placeholder="Nusrat Jahan"
            />
          </Field>

          <Field label="Email" required error={fieldErrors.email}>
            <Input
              type="email"
              value={form.email}
              onChange={(event) => setForm({ ...form, email: event.target.value })}
              placeholder="name@school.edu"
            />
          </Field>

          {!editing && (
            <Field label="Role" required>
              <Select
                value={form.role}
                onChange={(event) => setForm({ ...form, role: event.target.value as UserRole, classRoomId: '' })}
              >
                <option value="Student">Student</option>
                <option value="Teacher">Teacher</option>
                <option value="Admin">Admin</option>
              </Select>
            </Field>
          )}

          {form.role === 'Student' && (
            <Field label="Class" required error={fieldErrors.classRoomId} hint="Students see the assignments of this class only.">
              <Select value={form.classRoomId} onChange={(event) => setForm({ ...form, classRoomId: event.target.value })}>
                <option value="">Select a class</option>
                {classes.data?.items.map((classRoom) => (
                  <option key={classRoom.id} value={classRoom.id}>
                    {classRoom.name} ({classRoom.code})
                  </option>
                ))}
              </Select>
            </Field>
          )}

          <Field
            label={editing ? 'New password' : 'Password'}
            required={!editing}
            error={fieldErrors.password ?? fieldErrors.newPassword}
            hint={editing ? 'Leave blank to keep the current password.' : 'At least 6 characters.'}
          >
            <Input
              type="password"
              value={form.password}
              onChange={(event) => setForm({ ...form, password: event.target.value })}
              placeholder={editing ? 'Unchanged' : 'Minimum 6 characters'}
            />
          </Field>

          {editing && (
            <Checkbox
              label="Account is active"
              description="Deactivated accounts keep their history but cannot sign in."
              checked={form.isActive}
              onChange={(event) => setForm({ ...form, isActive: event.target.checked })}
            />
          )}
        </div>
      </Modal>
    </>
  );
}

function collectErrors(error: z.ZodError): Record<string, string> {
  const flattened = error.flatten().fieldErrors;

  return Object.entries(flattened).reduce<Record<string, string>>((accumulator, [key, messages]) => {
    if (messages && messages.length > 0) accumulator[key] = messages[0];
    return accumulator;
  }, {});
}
