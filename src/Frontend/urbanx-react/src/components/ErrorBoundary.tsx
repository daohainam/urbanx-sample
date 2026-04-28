import React from 'react';
import { logger } from '../lib/logger';

interface Props {
  children: React.ReactNode;
  fallback?: (error: Error, reset: () => void) => React.ReactNode;
}

interface State {
  error: Error | null;
}

export class ErrorBoundary extends React.Component<Props, State> {
  state: State = { error: null };

  static getDerivedStateFromError(error: Error): State {
    return { error };
  }

  componentDidCatch(error: Error, info: React.ErrorInfo) {
    logger.error('ErrorBoundary caught an error', error, { componentStack: info.componentStack });
  }

  reset = () => this.setState({ error: null });

  render() {
    if (this.state.error) {
      if (this.props.fallback) return this.props.fallback(this.state.error, this.reset);
      return (
        <div role="alert" className="min-h-[40vh] flex items-center justify-center p-8">
          <div className="max-w-md text-center space-y-4">
            <h1 className="text-2xl font-semibold">Something went wrong</h1>
            <p className="text-sm text-gray-600">
              The page failed to render. The error has been reported.
            </p>
            <div className="flex gap-2 justify-center">
              <button
                type="button"
                onClick={this.reset}
                className="px-4 py-2 rounded bg-gray-200 hover:bg-gray-300"
              >
                Try again
              </button>
              <button
                type="button"
                onClick={() => window.location.assign('/')}
                className="px-4 py-2 rounded bg-black text-white hover:bg-gray-800"
              >
                Go home
              </button>
            </div>
          </div>
        </div>
      );
    }
    return this.props.children;
  }
}
