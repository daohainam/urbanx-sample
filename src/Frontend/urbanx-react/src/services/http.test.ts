import { describe, expect, it, vi } from 'vitest';
import { http, HttpResponse, delay } from 'msw';
import { server } from '../test/msw/server';
import { ApiError, request } from './http';

// Override the auth helper so http.ts doesn't try to read OIDC state in tests.
vi.mock('./auth', () => ({
    userManager: {
        getUser: () => Promise.resolve(null),
    },
}));

describe('http.request', () => {
    it('parses a JSON response on 2xx', async () => {
        server.use(http.get('/api/ping', () => HttpResponse.json({ ok: true })));

        const result = await request<{ ok: boolean }>('/ping', { anonymous: true });
        expect(result).toEqual({ ok: true });
    });

    it('sends X-Request-Id and Accept headers', async () => {
        const seen: { id: string | null; accept: string | null } = { id: null, accept: null };
        server.use(
            http.get('/api/headers', ({ request: req }) => {
                seen.id = req.headers.get('x-request-id');
                seen.accept = req.headers.get('accept');
                return HttpResponse.json({});
            }),
        );

        await request('/headers', { anonymous: true });
        expect(seen.id).toBeTruthy();
        expect(seen.accept).toContain('application/json');
    });

    it('serialises `json` option and sets Content-Type', async () => {
        let receivedBody: unknown = null;
        let contentType: string | null = null;
        server.use(
            http.post('/api/echo', async ({ request: req }) => {
                contentType = req.headers.get('content-type');
                receivedBody = await req.json();
                return HttpResponse.json({ echoed: receivedBody });
            }),
        );

        await request('/echo', { method: 'POST', anonymous: true, json: { hello: 'world' } });
        expect(contentType).toContain('application/json');
        expect(receivedBody).toEqual({ hello: 'world' });
    });

    it('throws an ApiError with status, kind=http, and parsed message on 4xx', async () => {
        server.use(
            http.get('/api/forbidden', () =>
                HttpResponse.json({ message: 'Not allowed for you' }, { status: 403 }),
            ),
        );

        await expect(request('/forbidden', { anonymous: true })).rejects.toMatchObject({
            name: 'ApiError',
            kind: 'http',
            status: 403,
            message: 'Not allowed for you',
        });
    });

    it('marks 401 responses as auth errors', async () => {
        server.use(http.get('/api/secret', () => new HttpResponse(null, { status: 401 })));

        try {
            await request('/secret', { anonymous: true });
            expect.fail('expected ApiError');
        } catch (err) {
            expect(err).toBeInstanceOf(ApiError);
            expect((err as ApiError).isAuthError).toBe(true);
        }
    });

    it('extracts ASP.NET ProblemDetails `title` when no `message` is present', async () => {
        server.use(
            http.get('/api/problem', () =>
                HttpResponse.json({ title: 'Validation failed', status: 400 }, { status: 400 }),
            ),
        );

        await expect(request('/problem', { anonymous: true })).rejects.toMatchObject({
            kind: 'http',
            status: 400,
            message: 'Validation failed',
        });
    });

    it('classifies network failures as kind=network and isRetriable', async () => {
        server.use(http.get('/api/down', () => HttpResponse.error()));

        try {
            await request('/down', { anonymous: true });
            expect.fail('expected ApiError');
        } catch (err) {
            expect(err).toBeInstanceOf(ApiError);
            expect((err as ApiError).kind).toBe('network');
            expect((err as ApiError).isRetriable).toBe(true);
        }
    });

    it('classifies slow responses as kind=timeout when timeoutMs is exceeded', async () => {
        server.use(
            http.get('/api/slow', async () => {
                await delay(50);
                return HttpResponse.json({});
            }),
        );

        try {
            await request('/slow', { anonymous: true, timeoutMs: 5 });
            expect.fail('expected ApiError');
        } catch (err) {
            expect(err).toBeInstanceOf(ApiError);
            expect((err as ApiError).kind).toBe('timeout');
            expect((err as ApiError).isRetriable).toBe(true);
        }
    });

    it('returns undefined for 204 No Content', async () => {
        server.use(http.delete('/api/thing/1', () => new HttpResponse(null, { status: 204 })));

        const result = await request<void>('/thing/1', { method: 'DELETE', anonymous: true });
        expect(result).toBeUndefined();
    });

    it('considers 5xx responses retriable', async () => {
        server.use(http.get('/api/boom', () => new HttpResponse(null, { status: 503 })));

        try {
            await request('/boom', { anonymous: true });
            expect.fail('expected ApiError');
        } catch (err) {
            expect(err).toBeInstanceOf(ApiError);
            expect((err as ApiError).status).toBe(503);
            expect((err as ApiError).isRetriable).toBe(true);
        }
    });
});
