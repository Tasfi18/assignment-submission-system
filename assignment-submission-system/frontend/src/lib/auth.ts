'use client';

import type { UserSummary } from './types';

// The token lives in a readable cookie rather than localStorage so that Next.js middleware
// can guard the /admin, /teacher and /student routes before a page is ever rendered.
// A production deployment would move to an httpOnly cookie set by the API itself; see the
// "Known limitations" section of the README.

const TOKEN_COOKIE = 'as_token';
const ROLE_COOKIE = 'as_role';
const USER_COOKIE = 'as_user';

function writeCookie(name: string, value: string, expiresAtUtc?: string) {
  const expires = expiresAtUtc ? new Date(expiresAtUtc) : new Date(Date.now() + 8 * 60 * 60 * 1000);
  document.cookie = `${name}=${encodeURIComponent(value)}; path=/; expires=${expires.toUTCString()}; SameSite=Lax`;
}

function readCookie(name: string): string | null {
  if (typeof document === 'undefined') return null;

  const match = document.cookie
    .split('; ')
    .find((row) => row.startsWith(`${name}=`));

  return match ? decodeURIComponent(match.slice(name.length + 1)) : null;
}

function deleteCookie(name: string) {
  document.cookie = `${name}=; path=/; expires=Thu, 01 Jan 1970 00:00:00 GMT; SameSite=Lax`;
}

export function saveSession(token: string, user: UserSummary, expiresAtUtc: string) {
  writeCookie(TOKEN_COOKIE, token, expiresAtUtc);
  writeCookie(ROLE_COOKIE, user.role, expiresAtUtc);
  writeCookie(USER_COOKIE, JSON.stringify(user), expiresAtUtc);
}

export function getToken(): string | null {
  return readCookie(TOKEN_COOKIE);
}

export function getUser(): UserSummary | null {
  const raw = readCookie(USER_COOKIE);
  if (!raw) return null;

  try {
    return JSON.parse(raw) as UserSummary;
  } catch {
    return null;
  }
}

export function clearSession() {
  deleteCookie(TOKEN_COOKIE);
  deleteCookie(ROLE_COOKIE);
  deleteCookie(USER_COOKIE);
}

/** Landing page for each role, used after sign in and by the route guards. */
export function homeFor(role: string | undefined | null): string {
  switch (role) {
    case 'Admin':
      return '/admin';
    case 'Teacher':
      return '/teacher';
    case 'Student':
      return '/student';
    default:
      return '/login';
  }
}
