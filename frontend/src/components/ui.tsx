'use client';

import type { ButtonHTMLAttributes, InputHTMLAttributes, ReactNode, SelectHTMLAttributes, TextareaHTMLAttributes } from 'react';

function cx(...classes: (string | false | undefined | null)[]): string {
  return classes.filter(Boolean).join(' ');
}

// --------------------------------------------------------------------- button

type ButtonProps = ButtonHTMLAttributes<HTMLButtonElement> & {
  variant?: 'primary' | 'secondary' | 'danger' | 'ghost';
  size?: 'sm' | 'md';
  loading?: boolean;
};

export function Button({
  variant = 'primary',
  size = 'md',
  loading = false,
  className,
  children,
  disabled,
  ...rest
}: ButtonProps) {
  const base =
    'inline-flex items-center justify-center gap-2 rounded-lg font-medium transition focus:outline-none focus-visible:ring-2 focus-visible:ring-brand-500 focus-visible:ring-offset-2 disabled:cursor-not-allowed disabled:opacity-60';

  const variants = {
    primary: 'bg-brand-600 text-white hover:bg-brand-700',
    secondary: 'border border-slate-300 bg-white text-slate-700 hover:bg-slate-50',
    danger: 'bg-rose-600 text-white hover:bg-rose-700',
    ghost: 'text-slate-600 hover:bg-slate-100'
  };

  const sizes = {
    sm: 'px-3 py-1.5 text-sm',
    md: 'px-4 py-2 text-sm'
  };

  return (
    <button className={cx(base, variants[variant], sizes[size], className)} disabled={disabled || loading} {...rest}>
      {loading && <Spinner className="h-4 w-4" />}
      {children}
    </button>
  );
}

// ---------------------------------------------------------------- form fields

export function Field({
  label,
  htmlFor,
  hint,
  error,
  required,
  children
}: {
  label: string;
  htmlFor?: string;
  hint?: string;
  error?: string;
  required?: boolean;
  children: ReactNode;
}) {
  return (
    <div className="space-y-1.5">
      <label htmlFor={htmlFor} className="block text-sm font-medium text-slate-700">
        {label}
        {required && <span className="ml-0.5 text-rose-600">*</span>}
      </label>
      {children}
      {hint && !error && <p className="text-xs text-slate-500">{hint}</p>}
      {error && <p className="text-xs font-medium text-rose-600">{error}</p>}
    </div>
  );
}

const controlClasses =
  'w-full rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm text-slate-900 placeholder:text-slate-400 focus:border-brand-500 focus:outline-none focus:ring-1 focus:ring-brand-500 disabled:bg-slate-100';

export function Input({ className, ...rest }: InputHTMLAttributes<HTMLInputElement>) {
  return <input className={cx(controlClasses, className)} {...rest} />;
}

export function Textarea({ className, ...rest }: TextareaHTMLAttributes<HTMLTextAreaElement>) {
  return <textarea className={cx(controlClasses, 'min-h-[120px]', className)} {...rest} />;
}

export function Select({ className, children, ...rest }: SelectHTMLAttributes<HTMLSelectElement>) {
  return (
    <select className={cx(controlClasses, className)} {...rest}>
      {children}
    </select>
  );
}

export function Checkbox({
  label,
  description,
  ...rest
}: InputHTMLAttributes<HTMLInputElement> & { label: string; description?: string }) {
  return (
    <label className="flex cursor-pointer items-start gap-3 rounded-lg border border-slate-200 p-3 hover:bg-slate-50">
      <input
        type="checkbox"
        className="mt-0.5 h-4 w-4 rounded border-slate-300 text-brand-600 focus:ring-brand-500"
        {...rest}
      />
      <span>
        <span className="block text-sm font-medium text-slate-700">{label}</span>
        {description && <span className="block text-xs text-slate-500">{description}</span>}
      </span>
    </label>
  );
}

// --------------------------------------------------------------- containers

export function Card({ className, children }: { className?: string; children: ReactNode }) {
  return <div className={cx('rounded-xl border border-slate-200 bg-white shadow-sm', className)}>{children}</div>;
}

export function PageHeader({
  title,
  description,
  action
}: {
  title: string;
  description?: string;
  action?: ReactNode;
}) {
  return (
    <div className="mb-6 flex flex-wrap items-start justify-between gap-4">
      <div>
        <h1 className="text-2xl font-semibold tracking-tight text-slate-900">{title}</h1>
        {description && <p className="mt-1 text-sm text-slate-500">{description}</p>}
      </div>
      {action}
    </div>
  );
}

export function StatCard({ label, value, tone = 'default' }: { label: string; value: ReactNode; tone?: 'default' | 'warning' | 'success' }) {
  const tones = {
    default: 'text-slate-900',
    warning: 'text-amber-600',
    success: 'text-emerald-600'
  };

  return (
    <Card className="p-4">
      <p className="text-xs font-medium uppercase tracking-wide text-slate-500">{label}</p>
      <p className={cx('mt-2 text-2xl font-semibold', tones[tone])}>{value}</p>
    </Card>
  );
}

// -------------------------------------------------------------- status chips

const badgeTones: Record<string, string> = {
  Draft: 'bg-slate-100 text-slate-700',
  Published: 'bg-emerald-100 text-emerald-800',
  Closed: 'bg-slate-200 text-slate-700',
  Submitted: 'bg-blue-100 text-blue-800',
  UnderReview: 'bg-amber-100 text-amber-800',
  Graded: 'bg-emerald-100 text-emerald-800',
  ReturnedForRevision: 'bg-orange-100 text-orange-800',
  Rejected: 'bg-rose-100 text-rose-800',
  Late: 'bg-rose-100 text-rose-800',
  Admin: 'bg-purple-100 text-purple-800',
  Teacher: 'bg-brand-100 text-brand-800',
  Student: 'bg-sky-100 text-sky-800',
  Active: 'bg-emerald-100 text-emerald-800',
  Inactive: 'bg-slate-200 text-slate-600'
};

/** Turns "ReturnedForRevision" into "Returned for revision". */
export function humanise(value: string): string {
  const spaced = value.replace(/([a-z])([A-Z])/g, '$1 $2');
  return spaced.charAt(0).toUpperCase() + spaced.slice(1).toLowerCase();
}

export function Badge({ value, label }: { value: string; label?: string }) {
  return (
    <span
      className={cx(
        'inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-medium',
        badgeTones[value] ?? 'bg-slate-100 text-slate-700'
      )}
    >
      {label ?? humanise(value)}
    </span>
  );
}

// ------------------------------------------------------------------ feedback

export function Alert({
  tone = 'error',
  children
}: {
  tone?: 'error' | 'success' | 'info' | 'warning';
  children: ReactNode;
}) {
  const tones = {
    error: 'border-rose-200 bg-rose-50 text-rose-800',
    success: 'border-emerald-200 bg-emerald-50 text-emerald-800',
    info: 'border-sky-200 bg-sky-50 text-sky-800',
    warning: 'border-amber-200 bg-amber-50 text-amber-800'
  };

  return <div className={cx('rounded-lg border px-4 py-3 text-sm', tones[tone])}>{children}</div>;
}

export function Spinner({ className }: { className?: string }) {
  return (
    <svg className={cx('animate-spin text-current', className ?? 'h-5 w-5')} viewBox="0 0 24 24" fill="none">
      <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" />
      <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8v4a4 4 0 00-4 4H4z" />
    </svg>
  );
}

export function Loading({ label = 'Loading...' }: { label?: string }) {
  return (
    <div className="flex items-center justify-center gap-3 py-12 text-sm text-slate-500">
      <Spinner className="h-5 w-5" />
      {label}
    </div>
  );
}

export function EmptyState({ title, description, action }: { title: string; description?: string; action?: ReactNode }) {
  return (
    <div className="flex flex-col items-center justify-center gap-2 rounded-lg border border-dashed border-slate-300 px-6 py-12 text-center">
      <p className="text-sm font-medium text-slate-700">{title}</p>
      {description && <p className="max-w-md text-sm text-slate-500">{description}</p>}
      {action && <div className="mt-2">{action}</div>}
    </div>
  );
}

// --------------------------------------------------------------------- modal

export function Modal({
  open,
  title,
  onClose,
  children,
  footer
}: {
  open: boolean;
  title: string;
  onClose: () => void;
  children: ReactNode;
  footer?: ReactNode;
}) {
  if (!open) return null;

  return (
    <div className="fixed inset-0 z-50 flex items-start justify-center overflow-y-auto bg-slate-900/40 p-4 sm:p-8">
      <div className="w-full max-w-2xl rounded-xl bg-white shadow-xl">
        <div className="flex items-center justify-between border-b border-slate-200 px-5 py-4">
          <h2 className="text-base font-semibold text-slate-900">{title}</h2>
          <button
            type="button"
            onClick={onClose}
            aria-label="Close"
            className="rounded-md p-1 text-slate-400 hover:bg-slate-100 hover:text-slate-600"
          >
            <svg className="h-5 w-5" viewBox="0 0 20 20" fill="currentColor">
              <path d="M6.3 6.3a1 1 0 011.4 0L10 8.6l2.3-2.3a1 1 0 111.4 1.4L11.4 10l2.3 2.3a1 1 0 01-1.4 1.4L10 11.4l-2.3 2.3a1 1 0 01-1.4-1.4L8.6 10 6.3 7.7a1 1 0 010-1.4z" />
            </svg>
          </button>
        </div>
        <div className="px-5 py-4">{children}</div>
        {footer && <div className="flex justify-end gap-2 border-t border-slate-200 px-5 py-4">{footer}</div>}
      </div>
    </div>
  );
}

// ---------------------------------------------------------------- pagination

export function Pagination({
  page,
  totalPages,
  totalCount,
  onChange
}: {
  page: number;
  totalPages: number;
  totalCount: number;
  onChange: (page: number) => void;
}) {
  if (totalPages <= 1) {
    return <p className="px-4 py-3 text-xs text-slate-500">{totalCount} record{totalCount === 1 ? '' : 's'}</p>;
  }

  return (
    <div className="flex items-center justify-between border-t border-slate-200 px-4 py-3">
      <p className="text-xs text-slate-500">
        Page {page} of {totalPages} &middot; {totalCount} records
      </p>
      <div className="flex gap-2">
        <Button variant="secondary" size="sm" disabled={page <= 1} onClick={() => onChange(page - 1)}>
          Previous
        </Button>
        <Button variant="secondary" size="sm" disabled={page >= totalPages} onClick={() => onChange(page + 1)}>
          Next
        </Button>
      </div>
    </div>
  );
}

// -------------------------------------------------------------------- table

export function Table({ head, children }: { head: ReactNode; children: ReactNode }) {
  return (
    <div className="overflow-x-auto">
      <table className="w-full min-w-[640px] text-left text-sm">
        <thead className="border-b border-slate-200 bg-slate-50 text-xs uppercase tracking-wide text-slate-500">
          {head}
        </thead>
        <tbody className="divide-y divide-slate-100">{children}</tbody>
      </table>
    </div>
  );
}

export const Th = ({ children, className }: { children?: ReactNode; className?: string }) => (
  <th className={cx('px-4 py-3 font-medium', className)}>{children}</th>
);

export const Td = ({ children, className }: { children?: ReactNode; className?: string }) => (
  <td className={cx('px-4 py-3 align-top text-slate-700', className)}>{children}</td>
);
