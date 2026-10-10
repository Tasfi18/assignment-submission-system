'use client';

import { useMemo, useState } from 'react';
import { z } from 'zod';
import { api } from '@/lib/api';
import { localInputToUtcIso, utcIsoToLocalInput } from '@/lib/format';
import { useAction, useAsyncData } from '@/lib/hooks';
import type { Assignment, TeachingAssignment } from '@/lib/types';
import { Alert, Button, Checkbox, Field, Input, Loading, Select, Textarea } from './ui';

const schema = z.object({
  title: z.string().min(3, 'Title must be at least 3 characters.').max(200),
  description: z.string().min(1, 'A description is required.').max(5000),
  pair: z.string().min(1, 'Select the class and subject.'),
  maxMarks: z
    .number({ invalid_type_error: 'Maximum marks must be a number.' })
    .min(1, 'Maximum marks must be at least 1.')
    .max(1000, 'Maximum marks cannot exceed 1000.'),
  deadline: z.string().min(1, 'A deadline is required.')
});

type Props = {
  mode: 'create' | 'edit';
  assignment?: Assignment;
  onSaved: (assignment: Assignment) => void;
  onCancel?: () => void;
};

/**
 * The class/subject picker is driven by /api/teaching-assignments/mine, so a teacher can only
 * choose a pair an administrator granted them. The API enforces the same rule again.
 */
export function AssignmentForm({ mode, assignment, onSaved, onCancel }: Props) {
  const mine = useAsyncData<TeachingAssignment[]>(
    () => api.get<TeachingAssignment[]>('/api/teaching-assignments/mine'),
    []
  );

  const [title, setTitle] = useState(assignment?.title ?? '');
  const [description, setDescription] = useState(assignment?.description ?? '');
  const [pair, setPair] = useState(
    assignment ? `${assignment.classRoomId}|${assignment.subjectId}` : ''
  );
  const [maxMarks, setMaxMarks] = useState(String(assignment?.maxMarks ?? 100));
  const [deadline, setDeadline] = useState(utcIsoToLocalInput(assignment?.deadlineUtc));
  const [allowLateSubmission, setAllowLateSubmission] = useState(assignment?.allowLateSubmission ?? false);
  const [allowResubmission, setAllowResubmission] = useState(assignment?.allowResubmission ?? true);
  const [publishImmediately, setPublishImmediately] = useState(false);
  const [errors, setErrors] = useState<Record<string, string>>({});

  const action = useAction();

  const pairs = useMemo(
    () =>
      (mine.data ?? []).map((link) => ({
        value: `${link.classRoomId}|${link.subjectId}`,
        label: `${link.classRoomName} - ${link.subjectName}`
      })),
    [mine.data]
  );

  async function submit(event: React.FormEvent) {
    event.preventDefault();

    const parsed = schema.safeParse({
      title,
      description,
      pair,
      maxMarks: Number(maxMarks),
      deadline
    });

    if (!parsed.success) {
      const flattened = parsed.error.flatten().fieldErrors;
      setErrors({
        title: flattened.title?.[0] ?? '',
        description: flattened.description?.[0] ?? '',
        pair: flattened.pair?.[0] ?? '',
        maxMarks: flattened.maxMarks?.[0] ?? '',
        deadline: flattened.deadline?.[0] ?? ''
      });
      return;
    }

    setErrors({});

    const [classRoomId, subjectId] = parsed.data.pair.split('|');

    const body = {
      title: parsed.data.title,
      description: parsed.data.description,
      classRoomId,
      subjectId,
      maxMarks: parsed.data.maxMarks,
      deadlineUtc: localInputToUtcIso(parsed.data.deadline),
      allowLateSubmission,
      allowResubmission,
      ...(mode === 'create' ? { publishImmediately } : {})
    };

    await action.run(async () => {
      const saved =
        mode === 'create'
          ? await api.post<Assignment>('/api/assignments', body)
          : await api.put<Assignment>(`/api/assignments/${assignment!.id}`, body);

      onSaved(saved);
    });
  }

  if (mine.loading) return <Loading label="Loading your classes..." />;

  if (mine.data && mine.data.length === 0) {
    return (
      <Alert tone="warning">
        You have not been assigned to any class and subject yet, so you cannot create assignments. Ask an
        administrator to add a teacher mapping for you.
      </Alert>
    );
  }

  return (
    <form onSubmit={submit} className="space-y-5" noValidate>
      {action.error && <Alert tone="error">{action.error}</Alert>}
      {mine.error && <Alert tone="error">{mine.error}</Alert>}

      <Field label="Title" required error={errors.title}>
        <Input
          value={title}
          onChange={(event) => setTitle(event.target.value)}
          placeholder="Quadratic Equations - Problem Set 3"
        />
      </Field>

      <Field label="Instructions" required error={errors.description}>
        <Textarea
          className="min-h-[160px]"
          value={description}
          onChange={(event) => setDescription(event.target.value)}
          placeholder="Explain what the students have to do, how to present their answer and how it will be marked."
        />
      </Field>

      <Field
        label="Class and subject"
        required
        error={errors.pair}
        hint={
          mode === 'edit'
            ? 'Cannot be changed once a student has submitted.'
            : 'Only the pairs an administrator assigned to you are listed.'
        }
      >
        <Select value={pair} onChange={(event) => setPair(event.target.value)}>
          <option value="">Select a class and subject</option>
          {pairs.map((option) => (
            <option key={option.value} value={option.value}>
              {option.label}
            </option>
          ))}
        </Select>
      </Field>

      <div className="grid gap-4 sm:grid-cols-2">
        <Field label="Maximum marks" required error={errors.maxMarks}>
          <Input
            type="number"
            min={1}
            max={1000}
            step="0.5"
            value={maxMarks}
            onChange={(event) => setMaxMarks(event.target.value)}
          />
        </Field>

        <Field label="Deadline" required error={errors.deadline} hint="Entered in your local time and stored as UTC.">
          <Input type="datetime-local" value={deadline} onChange={(event) => setDeadline(event.target.value)} />
        </Field>
      </div>

      <div className="grid gap-3 sm:grid-cols-2">
        <Checkbox
          label="Accept late submissions"
          description="Work handed in after the deadline is accepted and flagged as late."
          checked={allowLateSubmission}
          onChange={(event) => setAllowLateSubmission(event.target.checked)}
        />
        <Checkbox
          label="Allow students to update their answer"
          description="Until it is graded or the assignment is closed."
          checked={allowResubmission}
          onChange={(event) => setAllowResubmission(event.target.checked)}
        />
      </div>

      {mode === 'create' && (
        <Checkbox
          label="Publish straight away"
          description="Otherwise the assignment is saved as a draft that students cannot see."
          checked={publishImmediately}
          onChange={(event) => setPublishImmediately(event.target.checked)}
        />
      )}

      <div className="flex justify-end gap-2">
        {onCancel && (
          <Button type="button" variant="secondary" onClick={onCancel}>
            Cancel
          </Button>
        )}
        <Button type="submit" loading={action.busy}>
          {mode === 'create' ? 'Create assignment' : 'Save changes'}
        </Button>
      </div>
    </form>
  );
}
