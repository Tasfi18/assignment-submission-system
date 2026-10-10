'use client';

import { useCallback, useEffect, useState, type DependencyList } from 'react';
import { ApiError } from './api';

/**
 * Small data-loading helper used by every page: keeps the loading / error / reload
 * plumbing in one place instead of repeating a useEffect in each component.
 */
export function useAsyncData<T>(loader: () => Promise<T>, deps: DependencyList = []) {
  const [data, setData] = useState<T | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const memoisedLoader = useCallback(loader, deps);

  const reload = useCallback(async () => {
    setLoading(true);
    setError(null);

    try {
      setData(await memoisedLoader());
    } catch (caught) {
      setError(caught instanceof ApiError ? caught.detail : 'Unable to load data. Please try again.');
    } finally {
      setLoading(false);
    }
  }, [memoisedLoader]);

  useEffect(() => {
    void reload();
  }, [reload]);

  return { data, loading, error, reload, setData };
}

/** Tracks a submit action: disables the button and surfaces the API error message. */
export function useAction() {
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [message, setMessage] = useState<string | null>(null);

  async function run(action: () => Promise<void>, successMessage?: string) {
    setBusy(true);
    setError(null);
    setMessage(null);

    try {
      await action();
      if (successMessage) setMessage(successMessage);
      return true;
    } catch (caught) {
      setError(caught instanceof ApiError ? caught.detail : 'The action could not be completed.');
      return false;
    } finally {
      setBusy(false);
    }
  }

  return { busy, error, message, setError, setMessage, run };
}
