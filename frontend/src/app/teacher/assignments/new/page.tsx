'use client';

import { useRouter } from 'next/navigation';
import { AssignmentForm } from '@/components/AssignmentForm';
import { Card, PageHeader } from '@/components/ui';

export default function NewAssignmentPage() {
  const router = useRouter();

  return (
    <>
      <PageHeader
        title="New assignment"
        description="Saved as a draft unless you choose to publish it immediately. Students only ever see published work."
      />

      <Card className="p-6">
        <AssignmentForm
          mode="create"
          onSaved={(assignment) => router.push(`/teacher/assignments/${assignment.id}`)}
          onCancel={() => router.push('/teacher')}
        />
      </Card>
    </>
  );
}
