# UrbanX Frontend (urbanx-react)

React 19 + Vite SPA for the UrbanX microservices sample. Talks to the API Gateway at
`/api/*` and (in the BFF design) `/bff/*`.

## Quick start

```bash
cp .env.example .env.development.local   # copy and edit if you want non-default values
npm ci
npm run dev                                # http://localhost:5173, proxies /api → :5000
```

The dev server proxies `/api`, `/bff`, `/signin-oidc`, `/signout-callback-oidc`,
`/connect`, and `/.well-known` to whatever `VITE_DEV_PROXY_TARGET` (default
`http://localhost:5000`) points at — the API Gateway.

## Scripts

| Script | What it does |
| --- | --- |
| `npm run dev` | Start Vite dev server with HMR. |
| `npm run build` | TypeScript project build + production Vite bundle. |
| `npm run preview` | Serve the production build locally. |
| `npm run lint` | ESLint (TS, React Hooks, React Refresh, jsx-a11y, Prettier-aware). |
| `npm run typecheck` | `tsc -b --noEmit` — fail on any type error. |
| `npm test` | Vitest (unit + component) once. |
| `npm run test:watch` | Vitest in watch mode. |
| `npm run test:coverage` | Vitest with V8 coverage (`coverage/index.html`). |
| `npm run test:e2e` | Playwright smoke suite against `vite preview`. |
| `npm run size` | Enforce bundle-size budgets (see `size-limit` in package.json). |
| `npm run format` | Prettier-write the whole tree. |
| `npm run format:check` | CI-friendly Prettier check (no writes). |

## Project layout

```
src/
├── components/      Layout primitives, forms, UI states (Skeleton/Empty/Error), ErrorBoundary
├── config/          env.ts — required-env validator
├── context/         AuthContext, CartContext + zod-validated localStorage
├── lib/             logger, queryClient, queryKeys
├── pages/           One file per route. Lazy-loaded in App.tsx.
├── schemas/         zod schemas (auth, address, cart, common primitives)
├── services/        http.ts (typed ApiError, timeout, X-Request-Id), api.ts service modules
├── test/            Vitest setup, MSW handlers, render helper
└── types/           Domain TypeScript interfaces
```

## Environment variables

Required (validated at boot — the app will refuse to render without them):

| Var | Example | Purpose |
| --- | --- | --- |
| `VITE_API_BASE_URL` | `/api` | Base URL for service API calls. Same-origin in prod. |
| `VITE_BFF_BASE_URL` | `/bff` | Base URL for BFF endpoints (`/bff/login`, `/bff/user`). |
| `VITE_OIDC_CLIENT_ID` | `urbanx-spa` | OIDC client id (transitional — removed once BFF lands). |
| `VITE_OIDC_SCOPES` | `openid profile email …` | Requested scopes. |

Optional:

| Var | Purpose |
| --- | --- |
| `VITE_OIDC_AUTHORITY` | OIDC issuer. Empty falls back to `window.location.origin`. |
| `VITE_SENTRY_DSN` | Enables Sentry reporting in production builds. |
| `VITE_APP_VERSION` | Release tag forwarded to Sentry. CI sets it to the commit SHA. |
| `VITE_DEV_PROXY_TARGET` | Override the dev proxy target (default `http://localhost:5000`). |

## Routing & data layer

- **Routes are lazy-loaded** (`React.lazy`) so the initial bundle ships only Home + nav
  + auth context. Each page is its own chunk under `dist/assets/<PageName>-*.js`.
- **TanStack Query** is the source of truth for server state; configured in
  `src/lib/queryClient.ts` with retry only on retriable `ApiError`s (network/timeout/5xx).
- **`http.ts`** wraps `fetch` with: env-aware base URL, `X-Request-Id`, 15 s timeout,
  typed `ApiError { kind, status, correlationId }`, and ASP.NET ProblemDetails parsing.
- **Forms** use `react-hook-form` + `zod` resolvers. Schemas live in `src/schemas/`.
- **Cart** persists to localStorage with zod schema validation — corrupted data is
  dropped silently rather than crashing.

## Tests

- **Unit + component**: Vitest + jsdom + React Testing Library. MSW intercepts
  `fetch` so tests never hit a real network. Setup in `src/test/setup.ts`.
- **End-to-end**: Playwright (`e2e/*.spec.ts`) runs against `vite preview` (the prod
  build). API calls stubbed via `page.route()`; no backend required.
- CI runs both jobs in `.github/workflows/frontend.yml`. Playwright browsers are
  cached between runs.

## Production deployment

The recommended deployment serves the static `dist/` from the same origin as the
API Gateway (e.g. nginx → `/` static + `/api/*` reverse proxy). Same-origin avoids
CORS and third-party-cookie issues entirely, and is required for the cookie-based
BFF auth model.

### Required security headers

The frontend serves no headers itself — the gateway / hosting layer must. Bake these
into the reverse proxy or nginx config:

```
Content-Security-Policy:
  default-src 'self';
  script-src  'self';
  style-src   'self' 'unsafe-inline';      # Vite emits one inline <style> for critical CSS
  img-src     'self' https: data:;          # product images come from a CDN/Unsplash today
  connect-src 'self' https://o0.ingest.sentry.io;   # Sentry DSN host if used
  font-src    'self' data:;
  frame-ancestors 'none';
  base-uri    'self';
  form-action 'self';
Strict-Transport-Security: max-age=63072000; includeSubDomains; preload
X-Content-Type-Options: nosniff
Referrer-Policy: strict-origin-when-cross-origin
Permissions-Policy: camera=(), geolocation=(), microphone=()
Cross-Origin-Opener-Policy: same-origin
```

Notes:
- The `<title>` and `<meta>` tags rendered by React 19 are not inline scripts —
  CSP without `'unsafe-inline'` for `script-src` is fine.
- All page-level `backgroundImage` inline styles have been replaced with `<img>`
  elements (see L4 in the plan), so `style-src 'unsafe-inline'` is needed only for
  Vite's tooling-injected critical CSS. If you eliminate that (e.g. via `vite-plugin-cssnano`
  + nonce injection), you can tighten `style-src` to `'self'`.
- `connect-src` must include the gateway origin (when not same-origin) and any
  observability endpoints (Sentry, OTEL collector).
- `frame-ancestors 'none'` is essential — UrbanX is not designed to be embedded.

### Pre-commit (optional)

`lint-staged` is wired up in `package.json`. To run Prettier + ESLint on staged
files automatically before each commit, install [husky](https://typicode.github.io/husky/)
in this directory:

```bash
npx husky init
echo "cd src/Frontend/urbanx-react && npx lint-staged" > .husky/pre-commit
```

This is opt-in — CI enforces lint and formatting independently via the frontend workflow.

## Bundle size

`npm run size` enforces budgets defined in `package.json` under `size-limit`. Current
state (brotli):

| Bundle | Limit | Actual |
| --- | --- | --- |
| Initial (main JS + CSS) | 150 KB | ~113 KB |
| Heaviest route (CheckoutPage) | 10 KB | ~4 KB |

If you add a heavy dependency, the size-limit check will fail in CI before merge.
