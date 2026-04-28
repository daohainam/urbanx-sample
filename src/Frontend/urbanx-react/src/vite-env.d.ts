/// <reference types="vite/client" />

interface ImportMetaEnv {
  readonly VITE_API_BASE_URL: string;
  readonly VITE_BFF_BASE_URL: string;
  readonly VITE_APP_VERSION: string;
  readonly VITE_SENTRY_DSN?: string;
  readonly VITE_OIDC_AUTHORITY: string;
  readonly VITE_OIDC_CLIENT_ID: string;
  readonly VITE_OIDC_SCOPES: string;
}

interface ImportMeta {
  readonly env: ImportMetaEnv;
}
