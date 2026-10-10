import { NextResponse } from 'next/server';
import type { NextRequest } from 'next/server';

/**
 * First line of route protection: it runs before a protected page is rendered and keeps a
 * signed out (or wrong-role) visitor away from the three portals.
 *
 * It is a convenience layer only. Every rule is enforced again by the API, which is the single
 * source of truth for authorisation.
 */
const sections: Array<{ prefix: string; role: string }> = [
  { prefix: '/admin', role: 'Admin' },
  { prefix: '/teacher', role: 'Teacher' },
  { prefix: '/student', role: 'Student' }
];

function homeFor(role: string | undefined): string {
  const match = sections.find((section) => section.role === role);
  return match ? match.prefix : '/login';
}

export function middleware(request: NextRequest) {
  const { pathname } = request.nextUrl;

  const token = request.cookies.get('as_token')?.value;
  const role = request.cookies.get('as_role')?.value;

  const section = sections.find((entry) => pathname.startsWith(entry.prefix));

  if (!section) {
    return NextResponse.next();
  }

  if (!token) {
    const login = new URL('/login', request.url);
    login.searchParams.set('next', pathname);
    return NextResponse.redirect(login);
  }

  if (role !== section.role) {
    return NextResponse.redirect(new URL(homeFor(role), request.url));
  }

  return NextResponse.next();
}

export const config = {
  matcher: ['/admin/:path*', '/teacher/:path*', '/student/:path*']
};
