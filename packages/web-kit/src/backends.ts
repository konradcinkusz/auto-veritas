/**
 * Backend address resolution: one ordered candidate ladder that works on a laptop,
 * under Aspire, and on Fly with zero per-environment code. A 403 from a rung means
 * "wrong ingress, try the next candidate"; only when every rung fails is the call
 * itself failed.
 */

const DEFAULT_PORTS: Record<string, number> = {
  offers: 8082,
  auth: 8081,
};

export function candidates(service: string): string[] {
  return [
    process.env[`${service.toUpperCase()}_API_URL`],
    process.env[`services__${service}__https__0`],
    process.env[`services__${service}__http__0`],
    `http://localhost:${DEFAULT_PORTS[service] ?? 8080}`,
  ].filter((value): value is string => Boolean(value));
}

/** The authservice base URL; AUTH_URL is set in every composed environment. */
export function authBaseUrl(): string {
  return process.env.AUTH_URL ?? `http://localhost:${DEFAULT_PORTS.auth}`;
}
