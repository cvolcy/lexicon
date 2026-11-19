export type HealthStatus = 'Healthy' | 'Degraded' | 'Unhealthy';

export interface HealthCheckEntryViewModel {
  name: string;
  status: HealthStatus;
  description: string | null;
  durationMs: number;
}

export interface HealthViewModel {
  status: HealthStatus;
  environment: string;
  timestampUtc: string;
  checks: HealthCheckEntryViewModel[];
}
