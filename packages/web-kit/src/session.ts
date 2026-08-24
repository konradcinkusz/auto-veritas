import { createRemoteJWKSet, decodeJwt, jwtVerify, type JWTPayload } from 'jose';

/**
 * Token verification shared by the edge middleware and the session route: full
 * signature verification with issuer and audience against the authservice JWKS.
 * Decode-only exp checks accept forged tokens — a recorded production failure
 * in the estate this repo's standards were extracted from.
 */

const ROLE_CLAIM = 'http://schemas.microsoft.com/ws/2008/06/identity/claims/role';

let jwks: ReturnType<typeof createRemoteJWKSet> | null = null;
let jwksUrl: string | null = null;

function remoteJwks() {
  const url = process.env.AUTH_JWKS_URL ?? 'http://localhost:8081/.well-known/jwks.json';
  if (!jwks || jwksUrl !== url) {
    jwks = createRemoteJWKSet(new URL(url));
    jwksUrl = url;
  }
  return jwks;
}

export interface VerifiedSession {
  subject: string;
  email: string | null;
  roles: string[];
  expiresAt: number;
}

export function isExpired(token: string): boolean {
  try {
    const { exp } = decodeJwt(token);
    return !exp || exp * 1000 < Date.now();
  } catch {
    return true;
  }
}

export async function verifyAccessToken(token: string): Promise<VerifiedSession | null> {
  let payload: JWTPayload;
  try {
    ({ payload } = await jwtVerify(token, remoteJwks(), {
      issuer: process.env.AUTH_ISSUER,
      audience: process.env.AUTH_AUDIENCE,
    }));
  } catch {
    return null;
  }

  const rawRoles = payload[ROLE_CLAIM];
  const roles = Array.isArray(rawRoles)
    ? rawRoles.filter((role): role is string => typeof role === 'string')
    : typeof rawRoles === 'string'
      ? [rawRoles]
      : [];

  return {
    subject: typeof payload.sub === 'string' ? payload.sub : '',
    email: typeof payload.email === 'string' ? payload.email : null,
    roles,
    expiresAt: payload.exp ?? 0,
  };
}
