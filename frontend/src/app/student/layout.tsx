'use client';

import type { ReactNode } from 'react';
import { Shell } from '@/components/Shell';

export default function StudentLayout({ children }: { children: ReactNode }) {
  return <Shell role="Student">{children}</Shell>;
}
