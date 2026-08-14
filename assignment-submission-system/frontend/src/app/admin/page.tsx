'use client';

import Link from 'next/link';
import { api } from '@/lib/api';
import { useAsyncData } from '@/lib/hooks';
import type { Dashboard } from '@/lib/types';
import { Alert, Card, Loading, PageHeader, StatCard } from '@/components/ui';

export default function AdminOverviewPage() {
  const { data, loading, error } = useAsyncData<Dashboard>(() => api.get<Dashboard>('/api/dashboard'), []);

  return (
    <>
      <PageHeader
        title="Overview"
        description="A snapshot of the institution: accounts, academic structure and marking workload."
      />

      {error && <Alert tone="error">{error}</Alert>}
      {loading && <Loading />}

      {data && (
        <div className="space-y-6">
          <section>
            <h2 className="mb-3 text-sm font-semibold uppercase tracking-wide text-slate-500">People</h2>
            <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
              <StatCard label="Total accounts" value={data.totalUsers} />
              <StatCard label="Teachers" value={data.totalTeachers} />
              <StatCard label="Students" value={data.totalStudents} />
              <StatCard label="Classes" value={data.totalClassRooms} />
            </div>
          </section>

          <section>
            <h2 className="mb-3 text-sm font-semibold uppercase tracking-wide text-slate-500">Academic work</h2>
            <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
              <StatCard label="Subjects" value={data.totalSubjects} />
              <StatCard label="Assignments" value={data.totalAssignments} />
              <StatCard label="Published" value={data.publishedAssignments} tone="success" />
              <StatCard label="Drafts" value={data.draftAssignments} />
            </div>
          </section>

          <section>
            <h2 className="mb-3 text-sm font-semibold uppercase tracking-wide text-slate-500">Submissions</h2>
            <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
              <StatCard label="Received" value={data.totalSubmissions} />
              <StatCard label="Waiting to be marked" value={data.pendingReviewSubmissions} tone="warning" />
              <StatCard label="Graded" value={data.gradedSubmissions} tone="success" />
              <StatCard label="Handed in late" value={data.lateSubmissions} tone="warning" />
            </div>
          </section>

          <Card className="p-5">
            <h2 className="text-sm font-semibold text-slate-900">Getting started</h2>
            <p className="mt-1 text-sm text-slate-500">
              The usual order is: create classes and subjects, add teacher and student accounts, then map each
              teacher to the class and subject they teach. Teachers can only create assignments for a mapping
              that exists.
            </p>
            <div className="mt-4 flex flex-wrap gap-2 text-sm">
              <Link href="/admin/classes" className="rounded-lg border border-slate-300 px-3 py-1.5 hover:bg-slate-50">
                Classes
              </Link>
              <Link href="/admin/subjects" className="rounded-lg border border-slate-300 px-3 py-1.5 hover:bg-slate-50">
                Subjects
              </Link>
              <Link href="/admin/users" className="rounded-lg border border-slate-300 px-3 py-1.5 hover:bg-slate-50">
                Users
              </Link>
              <Link
                href="/admin/teaching-assignments"
                className="rounded-lg border border-slate-300 px-3 py-1.5 hover:bg-slate-50"
              >
                Teacher mapping
              </Link>
            </div>
          </Card>
        </div>
      )}
    </>
  );
}
