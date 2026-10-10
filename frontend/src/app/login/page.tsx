'use client';

import { Suspense, useState } from 'react';
import { useRouter, useSearchParams } from 'next/navigation';
import { z } from 'zod';
import { api, ApiError } from '@/lib/api';
import { homeFor, saveSession } from '@/lib/auth';
import type { LoginResponse } from '@/lib/types';
import { Alert, Button, Card, Field, Input, Loading } from '@/components/ui';

const loginSchema = z.object({
  email: z.string().min(1, 'Email is required.').email('Enter a valid email address.'),
  password: z.string().min(6, 'Password must be at least 6 characters.')
});

const demoAccounts = [
  { role: 'Admin', email: 'admin@school.edu', password: 'Admin@123' },
  { role: 'Teacher', email: 'teacher@school.edu', password: 'Teacher@123' },
  { role: 'Student', email: 'student@school.edu', password: 'Student@123' }
];

function LoginForm() {
  const router = useRouter();
  const searchParams = useSearchParams();

  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [fieldErrors, setFieldErrors] = useState<{ email?: string; password?: string }>({});
  const [formError, setFormError] = useState<string | null>(
    searchParams.get('expired') ? 'Your session expired. Please sign in again.' : null
  );
  const [submitting, setSubmitting] = useState(false);

  async function handleSubmit(event: React.FormEvent) {
    event.preventDefault();
    setFormError(null);

    const parsed = loginSchema.safeParse({ email, password });

    if (!parsed.success) {
      const flattened = parsed.error.flatten().fieldErrors;
      setFieldErrors({ email: flattened.email?.[0], password: flattened.password?.[0] });
      return;
    }

    setFieldErrors({});
    setSubmitting(true);

    try {
      const response = await api.post<LoginResponse>('/api/auth/login', parsed.data);
      saveSession(response.accessToken, response.user, response.expiresAtUtc);

      const next = searchParams.get('next');
      router.replace(next && next.startsWith('/') ? next : homeFor(response.user.role));
    } catch (error) {
      setFormError(error instanceof ApiError ? error.detail : 'Something went wrong. Please try again.');
    } finally {
      setSubmitting(false);
    }
  }

  function useDemoAccount(account: (typeof demoAccounts)[number]) {
    setEmail(account.email);
    setPassword(account.password);
    setFieldErrors({});
  }

  return (
    <div className="flex min-h-screen items-center justify-center bg-slate-100 px-4 py-10">
      <div className="w-full max-w-md space-y-6">
        <div className="text-center">
          <span className="mx-auto mb-3 flex h-12 w-12 items-center justify-center rounded-xl bg-brand-600 text-lg font-bold text-white">
            AS
          </span>
          <h1 className="text-2xl font-semibold tracking-tight text-slate-900">
            Assignment &amp; Submission System
          </h1>
          <p className="mt-1 text-sm text-slate-500">Sign in to continue to your portal.</p>
        </div>

        <Card className="p-6">
          <form onSubmit={handleSubmit} className="space-y-4" noValidate>
            {formError && <Alert tone="error">{formError}</Alert>}

            <Field label="Email" htmlFor="email" required error={fieldErrors.email}>
              <Input
                id="email"
                name="email"
                type="email"
                autoComplete="username"
                placeholder="you@school.edu"
                value={email}
                onChange={(event) => setEmail(event.target.value)}
                aria-invalid={Boolean(fieldErrors.email)}
              />
            </Field>

            <Field label="Password" htmlFor="password" required error={fieldErrors.password}>
              <Input
                id="password"
                name="password"
                type="password"
                autoComplete="current-password"
                placeholder="Your password"
                value={password}
                onChange={(event) => setPassword(event.target.value)}
                aria-invalid={Boolean(fieldErrors.password)}
              />
            </Field>

            <Button type="submit" className="w-full" loading={submitting}>
              Sign in
            </Button>
          </form>
        </Card>

        <Card className="p-4">
          <p className="mb-3 text-xs font-medium uppercase tracking-wide text-slate-500">Demo accounts</p>
          <div className="space-y-2">
            {demoAccounts.map((account) => (
              <button
                key={account.email}
                type="button"
                onClick={() => useDemoAccount(account)}
                className="flex w-full items-center justify-between rounded-lg border border-slate-200 px-3 py-2 text-left text-sm hover:border-brand-300 hover:bg-brand-50"
              >
                <span>
                  <span className="block font-medium text-slate-800">{account.role}</span>
                  <span className="block text-xs text-slate-500">{account.email}</span>
                </span>
                <span className="text-xs font-medium text-brand-600">Use</span>
              </button>
            ))}
          </div>
        </Card>
      </div>
    </div>
  );
}

/**
 * useSearchParams() opts a component into client-side rendering, so Next.js requires it to sit
 * behind a Suspense boundary. Wrapping here keeps the shell renderable while the form hydrates.
 */
export default function LoginPage() {
  return (
    <Suspense fallback={<Loading label="Loading sign in..." />}>
      <LoginForm />
    </Suspense>
  );
}
