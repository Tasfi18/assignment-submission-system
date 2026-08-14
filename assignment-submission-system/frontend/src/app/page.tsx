'use client';

import { useEffect } from 'react';
import { useRouter } from 'next/navigation';
import { getToken, getUser, homeFor } from '@/lib/auth';
import { Loading } from '@/components/ui';

/** Sends the visitor to their own portal, or to the sign in page when there is no session. */
export default function HomePage() {
  const router = useRouter();

  useEffect(() => {
    const token = getToken();
    const user = getUser();

    router.replace(token && user ? homeFor(user.role) : '/login');
  }, [router]);

  return <Loading label="Taking you to your dashboard..." />;
}
