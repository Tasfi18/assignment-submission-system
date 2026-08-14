'use client';

import Link from 'next/link';
import { usePathname, useRouter } from 'next/navigation';
import { useEffect, useState, type ReactNode } from 'react';
import { clearSession, getUser, homeFor } from '@/lib/auth';
import type { UserRole, UserSummary } from '@/lib/types';
import { Badge, Loading } from './ui';

type NavItem = { href: string; label: string };

const navigation: Record<UserRole, NavItem[]> = {
  Admin: [
    { href: '/admin', label: 'Overview' },
    { href: '/admin/users', label: 'Users' },
    { href: '/admin/classes', label: 'Classes' },
    { href: '/admin/subjects', label: 'Subjects' },
    { href: '/admin/teaching-assignments', label: 'Teacher mapping' },
    { href: '/admin/assignments', label: 'Assignments' },
    { href: '/admin/submissions', label: 'Submissions' },
    { href: '/admin/settings', label: 'Settings' }
  ],
  Teacher: [
    { href: '/teacher', label: 'My assignments' },
    { href: '/teacher/assignments/new', label: 'Create assignment' },
    { href: '/teacher/submissions', label: 'Submissions' }
  ],
  Student: [
    { href: '/student', label: 'Assignments' },
    { href: '/student/submissions', label: 'My submissions' }
  ]
};

/**
 * Shared chrome for the three portals. The server-side route guard lives in middleware.ts;
 * this component additionally makes sure the signed in role matches the section being rendered,
 * which also covers client-side navigation.
 */
export function Shell({ role, children }: { role: UserRole; children: ReactNode }) {
  const router = useRouter();
  const pathname = usePathname();
  const [user, setUser] = useState<UserSummary | null>(null);
  const [checked, setChecked] = useState(false);
  const [menuOpen, setMenuOpen] = useState(false);

  useEffect(() => {
    const current = getUser();

    if (!current) {
      router.replace('/login');
      return;
    }

    if (current.role !== role) {
      router.replace(homeFor(current.role));
      return;
    }

    setUser(current);
    setChecked(true);
  }, [role, router]);

  if (!checked || !user) {
    return <Loading label="Checking your session..." />;
  }

  const items = navigation[role];

  function signOut() {
    clearSession();
    router.replace('/login');
  }

  return (
    <div className="min-h-screen bg-slate-50">
      <header className="sticky top-0 z-40 border-b border-slate-200 bg-white">
        <div className="mx-auto flex max-w-7xl items-center justify-between gap-4 px-4 py-3 sm:px-6">
          <div className="flex items-center gap-3">
            <button
              type="button"
              className="rounded-md p-2 text-slate-500 hover:bg-slate-100 lg:hidden"
              onClick={() => setMenuOpen((open) => !open)}
              aria-label="Toggle navigation"
            >
              <svg className="h-5 w-5" viewBox="0 0 20 20" fill="currentColor">
                <path d="M3 5h14v2H3V5zm0 4h14v2H3V9zm0 4h14v2H3v-2z" />
              </svg>
            </button>
            <Link href={homeFor(role)} className="flex items-center gap-2">
              <span className="flex h-8 w-8 items-center justify-center rounded-lg bg-brand-600 text-sm font-bold text-white">
                AS
              </span>
              <span className="hidden text-sm font-semibold text-slate-900 sm:block">
                Assignment &amp; Submission System
              </span>
            </Link>
          </div>

          <div className="flex items-center gap-3">
            <div className="hidden text-right sm:block">
              <p className="text-sm font-medium text-slate-900">{user.fullName}</p>
              <p className="text-xs text-slate-500">
                {user.classRoomName ? `${user.roleName} - ${user.classRoomName}` : user.roleName}
              </p>
            </div>
            <Badge value={user.role} />
            <button
              type="button"
              onClick={signOut}
              className="rounded-lg border border-slate-300 px-3 py-1.5 text-sm font-medium text-slate-700 hover:bg-slate-50"
            >
              Sign out
            </button>
          </div>
        </div>
      </header>

      <div className="mx-auto flex max-w-7xl gap-6 px-4 py-6 sm:px-6">
        <aside className={`${menuOpen ? 'block' : 'hidden'} w-full shrink-0 lg:block lg:w-56`}>
          <nav className="space-y-1">
            {items.map((item) => {
              const active = pathname === item.href;
              return (
                <Link
                  key={item.href}
                  href={item.href}
                  onClick={() => setMenuOpen(false)}
                  className={`block rounded-lg px-3 py-2 text-sm font-medium transition ${
                    active ? 'bg-brand-600 text-white' : 'text-slate-600 hover:bg-white hover:text-slate-900'
                  }`}
                >
                  {item.label}
                </Link>
              );
            })}
          </nav>
        </aside>

        <main className={`${menuOpen ? 'hidden' : 'block'} min-w-0 flex-1 lg:block`}>{children}</main>
      </div>
    </div>
  );
}
