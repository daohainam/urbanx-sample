import { env } from '../config/env';
import { userManager } from './auth';

/**
 * Typed error thrown by `request()` for non-2xx responses, network failures, and
 * timeouts. Pages and react-query consumers can branch on `status` / `kind`.
 */
export class ApiError extends Error {
    readonly kind: 'http' | 'network' | 'timeout' | 'parse';
    readonly status: number;
    readonly correlationId?: string;
    readonly payload?: unknown;

    constructor(
        message: string,
        opts: {
            kind: 'http' | 'network' | 'timeout' | 'parse';
            status?: number;
            correlationId?: string;
            payload?: unknown;
            cause?: unknown;
        },
    ) {
        super(message, opts.cause ? { cause: opts.cause } : undefined);
        this.name = 'ApiError';
        this.kind = opts.kind;
        this.status = opts.status ?? 0;
        this.correlationId = opts.correlationId;
        this.payload = opts.payload;
    }

    get isAuthError(): boolean {
        return this.status === 401;
    }

    get isForbidden(): boolean {
        return this.status === 403;
    }

    get isRetriable(): boolean {
        return this.kind === 'network' || this.kind === 'timeout' || (this.status >= 500 && this.status < 600);
    }
}

export interface RequestOptions extends Omit<RequestInit, 'body' | 'signal'> {
    /** JSON-serialisable body. Sets `Content-Type: application/json` automatically. */
    json?: unknown;
    /** Raw body (FormData, Blob, string). Mutually exclusive with `json`. */
    body?: BodyInit | null;
    /** Skip attaching `Authorization: Bearer …`. Public endpoints set this to true. */
    anonymous?: boolean;
    /** Override the default request timeout (ms). Defaults to 15 s. */
    timeoutMs?: number;
    /** Cancellation signal from the caller; combined with the internal timeout. */
    signal?: AbortSignal;
}

const DEFAULT_TIMEOUT_MS = 15_000;

function newCorrelationId(): string {
    if (typeof crypto !== 'undefined' && 'randomUUID' in crypto) return crypto.randomUUID();
    return `cid-${Math.random().toString(36).slice(2)}-${Date.now()}`;
}

async function getAuthHeader(): Promise<Record<string, string>> {
    try {
        const user = await userManager.getUser();
        if (user?.access_token && !user.expired) {
            return { Authorization: `Bearer ${user.access_token}` };
        }
    } catch {
        // Treat any failure to read the user as "unauthenticated"; let the caller's 401 path handle it.
    }
    return {};
}

function combineSignals(...signals: (AbortSignal | undefined)[]): AbortSignal {
    // Prefer native AbortSignal.any when available (modern browsers / Node 20+).
    const filtered = signals.filter((s): s is AbortSignal => Boolean(s));
    if (filtered.length === 0) return new AbortController().signal;
    if (filtered.length === 1) return filtered[0];
    if (typeof AbortSignal !== 'undefined' && 'any' in AbortSignal) {
        return (AbortSignal as unknown as { any: (s: AbortSignal[]) => AbortSignal }).any(filtered);
    }
    const ctrl = new AbortController();
    for (const s of filtered) {
        if (s.aborted) {
            ctrl.abort(s.reason);
            break;
        }
        s.addEventListener('abort', () => ctrl.abort(s.reason), { once: true });
    }
    return ctrl.signal;
}

/**
 * Low-level typed fetch wrapper. Prefer using the service modules in `services/api.ts`
 * directly; reach for `request()` only when adding a new endpoint.
 *
 * - Prepends `env.apiBaseUrl` if `path` starts with '/'.
 * - Sends `X-Request-Id` for correlation with backend logs.
 * - Attaches `Authorization: Bearer` unless `anonymous: true`.
 * - Aborts after `timeoutMs` (default 15s) or when the caller signal aborts.
 * - Throws `ApiError` for non-2xx, network failures, timeouts, parse errors.
 */
export async function request<T>(path: string, opts: RequestOptions = {}): Promise<T> {
    const { json, anonymous, timeoutMs = DEFAULT_TIMEOUT_MS, signal: callerSignal, headers, body, ...init } = opts;

    const url = path.startsWith('http') ? path : `${env.apiBaseUrl}${path}`;
    const correlationId = newCorrelationId();

    const headerBag = new Headers(headers);
    headerBag.set('Accept', 'application/json');
    headerBag.set('X-Request-Id', correlationId);

    let finalBody = body;
    if (json !== undefined) {
        headerBag.set('Content-Type', 'application/json');
        finalBody = JSON.stringify(json);
    }

    if (!anonymous) {
        const auth = await getAuthHeader();
        for (const [k, v] of Object.entries(auth)) headerBag.set(k, v);
    }

    const timeoutCtrl = new AbortController();
    const timer = setTimeout(() => timeoutCtrl.abort(new DOMException('Request timed out', 'TimeoutError')), timeoutMs);

    let response: Response;
    try {
        response = await fetch(url, {
            ...init,
            headers: headerBag,
            body: finalBody ?? null,
            signal: combineSignals(timeoutCtrl.signal, callerSignal),
        });
    } catch (err) {
        clearTimeout(timer);
        if (err instanceof DOMException && err.name === 'AbortError') {
            // Distinguish a timeout abort from a caller abort.
            if (timeoutCtrl.signal.aborted) {
                throw new ApiError(`Request to ${path} timed out after ${timeoutMs}ms`, {
                    kind: 'timeout',
                    correlationId,
                    cause: err,
                });
            }
            throw err; // Caller-initiated abort — propagate as-is so react-query treats it correctly.
        }
        throw new ApiError(`Network error calling ${path}`, {
            kind: 'network',
            correlationId,
            cause: err,
        });
    }
    clearTimeout(timer);

    if (response.status === 204) {
        return undefined as T;
    }

    let payload: unknown;
    const contentType = response.headers.get('content-type') ?? '';
    if (contentType.includes('application/json')) {
        try {
            payload = await response.json();
        } catch (err) {
            if (response.ok) {
                throw new ApiError(`Failed to parse JSON response from ${path}`, {
                    kind: 'parse',
                    status: response.status,
                    correlationId,
                    cause: err,
                });
            }
            payload = undefined;
        }
    } else if (response.ok) {
        return (await response.text()) as unknown as T;
    }

    if (!response.ok) {
        const message = extractMessage(payload, response.statusText);
        throw new ApiError(message, {
            kind: 'http',
            status: response.status,
            correlationId,
            payload,
        });
    }

    return payload as T;
}

function extractMessage(payload: unknown, fallback: string): string {
    if (payload && typeof payload === 'object') {
        const obj = payload as Record<string, unknown>;
        if (typeof obj.message === 'string') return obj.message;
        if (typeof obj.title === 'string') return obj.title; // ASP.NET ProblemDetails
        if (typeof obj.error === 'string') return obj.error;
    }
    return fallback || 'Request failed';
}
