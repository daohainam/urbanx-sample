import { env } from '../config/env';

type LogContext = Record<string, unknown>;

const isProd = import.meta.env.PROD;

function emit(level: 'info' | 'warn' | 'error', message: string, context?: LogContext) {
  if (isProd && env.sentryDsn) {
    // Sentry will be initialised in main.tsx when a DSN is configured.
    // We avoid a hard dependency here so the bundle stays small when reporting is off.
    const sentry = (globalThis as { Sentry?: { captureMessage: (m: string, c?: unknown) => void } }).Sentry;
    if (sentry) {
      sentry.captureMessage(message, { level, extra: context });
      return;
    }
  }
  console[level](message, context ?? '');
}

export const logger = {
  info: (message: string, context?: LogContext) => emit('info', message, context),
  warn: (message: string, context?: LogContext) => emit('warn', message, context),
  error: (message: string, error?: unknown, context?: LogContext) => {
    if (isProd && env.sentryDsn) {
      const sentry = (globalThis as { Sentry?: { captureException: (e: unknown, c?: unknown) => void } }).Sentry;
      if (sentry) {
        sentry.captureException(error ?? new Error(message), { extra: { message, ...context } });
        return;
      }
    }
    console.error(message, error, context ?? '');
  },
};
