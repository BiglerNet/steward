function base64UrlDecode(segment: string): string {
  const padded = segment.replace(/-/g, "+").replace(/_/g, "/").padEnd(Math.ceil(segment.length / 4) * 4, "=");
  return atob(padded);
}

/**
 * Decodes a JWT's claims payload without verifying its signature — the token was already
 * validated server-side; this is purely for reading claims client-side (e.g. role checks
 * that gate UI, not actual authorization, which the API enforces independently).
 */
export function decodeJwtClaims(token: string): Record<string, unknown> | null {
  try {
    const [, payload] = token.split(".");
    if (!payload) return null;
    return JSON.parse(base64UrlDecode(payload)) as Record<string, unknown>;
  } catch {
    return null;
  }
}

export function jwtHasRole(token: string, role: string): boolean {
  const claims = decodeJwtClaims(token);
  if (!claims) return false;
  const roleClaim = claims["role"];
  if (Array.isArray(roleClaim)) {
    return roleClaim.includes(role);
  }
  return roleClaim === role;
}
