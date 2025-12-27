import { ReactNode } from 'react';
import Navigation from '../components/Navigation';

interface LayoutProps {
  children: ReactNode;
}

export default function Layout({ children }: LayoutProps) {
  return (
    <div className="flex h-screen">
      <Navigation />
      <main className="flex-1 overflow-auto bg-neutral-50">
        <div className="container mx-auto p-8">
          {children}
        </div>
      </main>
    </div>
  );
}
