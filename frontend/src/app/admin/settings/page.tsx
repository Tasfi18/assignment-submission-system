'use client';

import { useEffect, useState } from 'react';
import { api } from '@/lib/api';
import { formatDateTime } from '@/lib/format';
import { useAction, useAsyncData } from '@/lib/hooks';
import type { AppSetting } from '@/lib/types';
import { Alert, Button, Card, Input, Loading, PageHeader, Select } from '@/components/ui';

const booleanKeys = ['submissions.allow_late_globally', 'app.student_portal_enabled'];

/**
 * Application-level switches. The two boolean settings are enforced by the API, not just the UI:
 * turning late submissions off blocks them even for assignments that allow late work, and turning
 * the student portal off stops students signing in.
 */
export default function AdminSettingsPage() {
  const settings = useAsyncData<AppSetting[]>(() => api.get<AppSetting[]>('/api/settings'), []);
  const action = useAction();
  const [draft, setDraft] = useState<Record<string, string>>({});

  useEffect(() => {
    if (settings.data) {
      setDraft(Object.fromEntries(settings.data.map((setting) => [setting.key, setting.value])));
    }
  }, [settings.data]);

  async function save(setting: AppSetting) {
    const value = draft[setting.key] ?? setting.value;

    const ok = await action.run(
      async () => {
        await api.put(`/api/settings/${encodeURIComponent(setting.key)}`, { value });
      },
      `Saved "${setting.key}".`
    );

    if (ok) void settings.reload();
  }

  return (
    <>
      <PageHeader title="Settings" description="Institution-wide switches applied by the backend on every request." />

      {action.error && (
        <div className="mb-4">
          <Alert tone="error">{action.error}</Alert>
        </div>
      )}
      {action.message && (
        <div className="mb-4">
          <Alert tone="success">{action.message}</Alert>
        </div>
      )}

      {settings.loading && <Loading />}
      {settings.error && <Alert tone="error">{settings.error}</Alert>}

      <div className="space-y-4">
        {settings.data?.map((setting) => (
          <Card key={setting.key} className="p-5">
            <div className="flex flex-wrap items-start justify-between gap-4">
              <div className="min-w-0 flex-1">
                <p className="font-mono text-sm font-medium text-slate-900">{setting.key}</p>
                {setting.description && <p className="mt-1 text-sm text-slate-500">{setting.description}</p>}
                <p className="mt-1 text-xs text-slate-400">Last updated {formatDateTime(setting.updatedAtUtc)}</p>
              </div>

              <div className="flex w-full items-end gap-2 sm:w-auto">
                <div className="w-full sm:w-56">
                  {booleanKeys.includes(setting.key) ? (
                    <Select
                      value={draft[setting.key] ?? setting.value}
                      onChange={(event) => setDraft({ ...draft, [setting.key]: event.target.value })}
                    >
                      <option value="true">Enabled (true)</option>
                      <option value="false">Disabled (false)</option>
                    </Select>
                  ) : (
                    <Input
                      value={draft[setting.key] ?? setting.value}
                      onChange={(event) => setDraft({ ...draft, [setting.key]: event.target.value })}
                    />
                  )}
                </div>
                <Button onClick={() => save(setting)} loading={action.busy}>
                  Save
                </Button>
              </div>
            </div>
          </Card>
        ))}
      </div>
    </>
  );
}
