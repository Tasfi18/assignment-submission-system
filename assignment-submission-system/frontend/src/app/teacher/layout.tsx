'use client';

import type { ReactNode } from 'react';
import { Shell } from '@/components/Shell';

export default function TeacherLayout({ children }: { children: ReactNode }) {
  return <Shell role="Teacher">{children}</Shell>;
}
