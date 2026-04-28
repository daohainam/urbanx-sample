import React from 'react';
import { AlertTriangle, RefreshCcw } from 'lucide-react';
import { ApiError } from '../../services/http';

interface ErrorStateProps {
    error: unknown;
    onRetry?: () => void;
    title?: string;
    /** Override the default user-facing message. */
    message?: string;
}

/**
 * Standard "something went wrong" panel for inline page errors. Uses an `ApiError`'s
 * `kind`/`status` to pick a sensible default message, but always lets the caller override.
 */
export const ErrorState: React.FC<ErrorStateProps> = ({ error, onRetry, title = 'Something went wrong', message }) => {
    const resolved = message ?? defaultMessageFor(error);
    const correlationId = error instanceof ApiError ? error.correlationId : undefined;

    return (
        <div role="alert" className="border border-red-200 bg-red-50 rounded-lg p-6 max-w-xl mx-auto text-center">
            <div className="flex justify-center mb-3 text-red-500">
                <AlertTriangle size={32} />
            </div>
            <h2 className="text-lg font-semibold text-red-900 mb-1">{title}</h2>
            <p className="text-sm text-red-800">{resolved}</p>
            {correlationId && (
                <p className="text-xs text-red-700/70 mt-2 font-mono">Reference: {correlationId}</p>
            )}
            {onRetry && (
                <div className="mt-4">
                    <button
                        type="button"
                        onClick={onRetry}
                        className="inline-flex items-center gap-2 bg-red-600 text-white px-4 py-2 rounded-md text-sm font-medium hover:bg-red-700 transition-colors"
                    >
                        <RefreshCcw size={16} /> Try again
                    </button>
                </div>
            )}
        </div>
    );
};

function defaultMessageFor(error: unknown): string {
    if (error instanceof ApiError) {
        switch (error.kind) {
            case 'timeout':
                return "The request took too long. Check your connection and try again.";
            case 'network':
                return "We couldn't reach the server. Check your connection and try again.";
            case 'parse':
                return 'The server returned an unexpected response.';
            case 'http':
                if (error.status === 401) return 'Your session has expired. Please sign in again.';
                if (error.status === 403) return "You don't have permission to view this.";
                if (error.status === 404) return "We couldn't find what you were looking for.";
                if (error.status >= 500) return 'The server is having trouble. Please try again in a moment.';
                return error.message || 'Request failed.';
        }
    }
    if (error instanceof Error) return error.message;
    return 'An unexpected error occurred.';
}
