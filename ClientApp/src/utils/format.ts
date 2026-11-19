const BYTE_UNITS = ['B', 'KB', 'MB', 'GB'] as const;

export function formatBytes(bytes: number): string {
  if (bytes <= 0) return '0 B';

  let value = bytes;
  let unitIndex = 0;

  while (value >= 1024 && unitIndex < BYTE_UNITS.length - 1) {
    value /= 1024;
    unitIndex++;
  }

  return unitIndex === 0 ? `${value} B` : `${value.toFixed(1)} ${BYTE_UNITS[unitIndex]}`;
}

export function formatDateTime(isoString: string): string {
  return new Date(isoString).toLocaleString();
}
