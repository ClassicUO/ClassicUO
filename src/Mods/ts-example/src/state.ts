import type { ClayReactRenderer } from "./react";

export class State {
  public static tick = 0;
  public static renderer: ClayReactRenderer | null = null;
  public static timeouts: Map<number, { callback: () => void; delay: number }> =
    new Map();
}
