const STATUS_CLASS: Record<string, string> = {
  Scheduled: 'badge-success',
  Cancelled: 'badge-danger',
  Completed: 'badge-muted'
};

const STATUS_LABEL: Record<string, string> = {
  Scheduled: 'מתוכנן',
  Cancelled: 'בוטל',
  Completed: 'הושלם'
};

export function statusClass(status: string): string {
  return STATUS_CLASS[status] ?? '';
}

export function statusLabel(status: string): string {
  return STATUS_LABEL[status] ?? status;
}
