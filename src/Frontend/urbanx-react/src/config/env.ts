type EnvKey = keyof ImportMetaEnv;

const required: EnvKey[] = [
  'VITE_API_BASE_URL',
  'VITE_BFF_BASE_URL',
  'VITE_OIDC_CLIENT_ID',
  'VITE_OIDC_SCOPES',
];

function read(key: EnvKey): string {
  const value = import.meta.env[key];
  if (typeof value !== 'string') return '';
  return value;
}

function ensure(): void {
  const missing = required.filter((k) => !read(k));
  if (missing.length > 0) {
    throw new Error(
      `Missing required environment variables: ${missing.join(', ')}. ` +
        `Copy .env.example to .env.development.local (dev) or set them in your build pipeline (prod).`,
    );
  }
}

ensure();

export const env = {
  apiBaseUrl: read('VITE_API_BASE_URL'),
  bffBaseUrl: read('VITE_BFF_BASE_URL'),
  appVersion: read('VITE_APP_VERSION') || 'dev',
  sentryDsn: read('VITE_SENTRY_DSN'),
  oidc: {
    // Empty authority means "same origin" — relies on the Vite dev proxy / same-origin gateway.
    authority: read('VITE_OIDC_AUTHORITY') || (typeof window !== 'undefined' ? window.location.origin : ''),
    clientId: read('VITE_OIDC_CLIENT_ID'),
    scopes: read('VITE_OIDC_SCOPES'),
  },
} as const;
