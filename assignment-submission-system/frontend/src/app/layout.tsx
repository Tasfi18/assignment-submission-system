import type { Metadata } from 'next';
import './globals.css';

export const metadata: Metadata = {
  title: 'Assignment & Submission Management System',
  description:
    'Role based assignment and submission management for schools and colleges: administrators manage the institution, teachers set and mark work, students submit answers.'
};

export default function RootLayout({ children }: { children: React.ReactNode }) {
  return (
    <html lang="en">
      <body>{children}</body>
    </html>
  );
}
