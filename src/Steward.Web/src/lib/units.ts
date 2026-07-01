const NM_TO_FT_LBS = 0.7376;
const LITRES_TO_QT = 1.0567;

export function nmToFtLbs(nm: number): number {
  return nm * NM_TO_FT_LBS;
}

export function ftLbsToNm(ftLbs: number): number {
  return ftLbs / NM_TO_FT_LBS;
}

export function litresToQt(l: number): number {
  return l * LITRES_TO_QT;
}

export function qtToLitres(qt: number): number {
  return qt / LITRES_TO_QT;
}

export function formatTorque(nm: number): string {
  return `${Math.round(nmToFtLbs(nm))} ft-lbs`;
}

export function formatVolume(litres: number): string {
  return `${litresToQt(litres).toFixed(1)} qt`;
}
