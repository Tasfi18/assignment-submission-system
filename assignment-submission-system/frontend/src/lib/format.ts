/** Formatting helpers shared by the pages. All timestamps from the API are UTC. */

export function formatDateTime(value?: string | null): string {
  if (!value) return '-';

  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return '-';

  return date.toLocaleString(undefined, {
    day: '2-digit',
    month: 'short',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit'
  });
}

export function formatDate(value?: string | null): string {
  if (!value) return '-';

  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return '-';

  return date.toLocaleDateString(undefined, { day: '2-digit', month: 'short', year: 'numeric' });
}

/** "in 3 days" / "2 hours ago", used for deadlines. */
export function relativeToNow(value?: string | null): string {
  if (!value) return '';

  const target = new Date(value).getTime();
  if (Number.isNaN(target)) return '';

  const diffMs = target - Date.now();
  const past = diffMs < 0;
  const minutes = Math.round(Math.abs(diffMs) / 60000);

  let text: string;
  if (minutes < 60) text = `${minutes} minute${minutes === 1 ? '' : 's'}`;
  else if (minutes < 60 * 24) {
    const hours = Math.round(minutes / 60);
    text = `${hours} hour${hours === 1 ? '' : 's'}`;
  } else {
    const days = Math.round(minutes / (60 * 24));
    text = `${days} day${days === 1 ? '' : 's'}`;
  }

  return past ? `${text} ago` : `in ${text}`;
}

/** Converts a value from <input type="datetime-local"> into an ISO UTC string. */
export function localInputToUtcIso(value: string): string {
  return new Date(value).toISOString();
}

/** Converts an ISO UTC string into the format <input type="datetime-local"> expects. */
export function utcIsoToLocalInput(value?: string | null): string {
  if (!value) return '';

  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return '';

  const pad = (n: number) => String(n).padStart(2, '0');

  return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}T${pad(date.getHours())}:${pad(date.getMinutes())}`;
}
