import type { Metadata } from 'next';
import type { ReactNode } from 'react';
import './globals.css';

export const metadata: Metadata = {
  title: 'Auto Veritas',
  description:
    'Porównywarka ofert samochodów i finansowania w Hiszpanii — każda wartość z datą ostatniej weryfikacji.',
};

export default function RootLayout({ children }: { children: ReactNode }) {
  return (
    <html lang="pl">
      <body>{children}</body>
    </html>
  );
}
