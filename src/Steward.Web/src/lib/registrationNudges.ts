import type { AssetTypeDefinition, RegistrationKind, RegistrationResponse } from "@/api/types";

const KIND_NOUN: Record<RegistrationKind, string> = {
  Registration: "a registration",
  TrailPass: "a trail pass",
  Permit: "a permit",
};

/// A record with no expiresOn is treated as current (open-ended), matching the backend's
/// list ordering, which puts undated records alongside the most-current ones.
function isCurrent(registration: RegistrationResponse, today: string): boolean {
  return registration.expiresOn === null || registration.expiresOn >= today;
}

/// Kinds the asset category typically needs (per the type registry) that have no current
/// record on file. Pure client-side derivation; no persistence of dismissals.
export function computePermitNudges(
  definition: AssetTypeDefinition | undefined,
  registrations: RegistrationResponse[],
  today: string = new Date().toISOString().slice(0, 10)
): RegistrationKind[] {
  if (!definition) {
    return [];
  }

  const currentKinds = new Set(
    registrations.filter((registration) => isCurrent(registration, today)).map((registration) => registration.kind)
  );

  return definition.typicalPermitKinds
    .map((kind) => kind as RegistrationKind)
    .filter((kind) => !currentKinds.has(kind));
}

export function permitNudgeMessage(displayLabel: string, kind: RegistrationKind): string {
  return `${displayLabel}s usually need ${KIND_NOUN[kind]} — none current.`;
}
