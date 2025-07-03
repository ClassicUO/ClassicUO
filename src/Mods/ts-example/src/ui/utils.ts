import { ClaySizingType, ClayColor, Vector3, Vector2 } from "../types";

// Base64 encoding function (replacement for btoa)
export function base64Encode(data: Uint8Array): string {
  return Host.arrayBufferToBase64(data);
}

export function base64Decode(data: string): Uint8Array {
  return new Uint8Array(Host.base64ToArrayBuffer(data));
}

// Utility functions
export function createClaySizingAxis(
  type: ClaySizingType,
  minMax?: { min: number; max: number },
  percent?: number
) {
  if (type === ClaySizingType.Percent && percent !== undefined) {
    return { type, size: { percent } };
  }
  if (minMax !== undefined) {
    return {
      type,
      size: { minMax },
      ...(percent !== undefined && { percent }),
    };
  }
  return { type };
}

export function createClayColor(
  r: number,
  g: number,
  b: number,
  a: number = 1
): ClayColor {
  return { r, g, b, a };
}

export function createVector3(x: number, y: number, z: number): Vector3 {
  return { x, y, z };
}

export function createVector2(x: number, y: number): Vector2 {
  return { x, y };
}
