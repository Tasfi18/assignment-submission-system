'use client';

import { clearSession, getToken } from './auth';

export const API_BASE_URL =
  process.env.NEXT_PUBLIC_API_BASE_URL?.replace(/\/$/, '') ?? 'http://localhost:5080';

/** Error carrying the HTTP status and the RFC 7807 payload the API returns. */
export class ApiError extends Error {
  status: number;
  errors?: Record<string, string[]>;

  constructor(message: string, status: number, errors?: Record<string, string[]>) {
    super(message);
    this.name = 'ApiError';
    this.status = status;
    this.errors = errors;
  }

  /** Flattens field errors into a single readable string for a form-level alert. */
  get detail(): string {
    if (!this.errors) return this.message;

    const fieldMessages = Object.values(this.errors).flat();
    return fieldMessages.length > 0 ? fieldMessages.join(' ') : this.message;
  }
}

type ProblemDetails = {
  title?: string;
  detail?: string;
  status?: number;
  errors?: Record<string, string[]>;
};

async function request<T>(path: string, init: RequestInit = {}): Promise<T> {
  const token = getToken();

  const headers = new Headers(init.headers);
  headers.set('Accept', 'application/json');

  if (init.body && !headers.has('Content-Type')) {
    headers.set('Content-Type', 'application/json');
  }

  if (token) {
    headers.set('Authorization', `Bearer ${token}`);
  }

  let response: Response;

  try {
    response = await fetch(`${API_BASE_URL}${path}`, { ...init, headers, cache: 'no-store' });
  } catch {
    throw new ApiError(
      `Could not reach the API at ${API_BASE_URL}. Is the backend running?`,
      0
    );
  }

  if (response.status === 401) {
    // The token expired or was revoked: drop it and send the user back to the sign in page.
    clearSession();
    if (typeof window !== 'undefined' && !window.location.pathname.startsWith('/login')) {
      window.location.href = '/login?expired=1';
    }
    throw new ApiError('Your session has expired. Please sign in again.', 401);
  }

  if (response.status === 204) {
    return undefined as T;
  }

  const text = await response.text();
  const payload = text ? (JSON.parse(text) as unknown) : null;

  if (!response.ok) {
    const problem = (payload ?? {}) as ProblemDetails;
    throw new ApiError(
      problem.detail || problem.title || `Request failed with status ${response.status}.`,
      response.status,
      problem.errors
    );
  }

  return payload as T;
}

export const api = {
  get: <T>(path: string) => request<T>(path),
  post: <T>(path: string, body?: unknown) =>
    request<T>(path, { method: 'POST', body: body === undefined ? undefined : JSON.stringify(body) }),
  put: <T>(path: string, body?: unknown) =>
    request<T>(path, { method: 'PUT', body: body === undefined ? undefined : JSON.stringify(body) }),
  patch: <T>(path: string, body?: unknown) =>
    request<T>(path, { method: 'PATCH', body: body === undefined ? undefined : JSON.stringify(body) }),
  delete: <T>(path: string) => request<T>(path, { method: 'DELETE' })
};

/** Builds a query string, skipping empty values. */
export function query(params: Record<string, string | number | boolean | undefined | null>): string {
  const search = new URLSearchParams();

  Object.entries(params).forEach(([key, value]) => {
    if (value !== undefined && value !== null && value !== '') {
      search.set(key, String(value));
    }
  });

  const result = search.toString();
  return result ? `?${result}` : '';
}
