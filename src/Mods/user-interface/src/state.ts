import type { ClayReactRenderer } from './react';

export class State {
  public static tick = 0;
  public static renderer: ClayReactRenderer | null = null;
  public static processTimeouts: (tick: number) => void;
}
