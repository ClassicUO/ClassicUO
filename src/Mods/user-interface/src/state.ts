import type { TimeProxy } from './host/clay';
import type { ClayReactRenderer } from './react';
import type { GlobalPolyfills } from './support/polyfill';

/**
 * This is the global state of the mod, used to persist data across host calls.
 */
export class State {
  public static tick = 0;
  public static frame = 0;
  public static renderer: ClayReactRenderer | null = null;
  public static processTimeouts: (tick: number) => void;

  public static init(polyfills: GlobalPolyfills) {
    this.tick = 0;
    this.frame = 0;
    this.renderer = null;
    this.processTimeouts = polyfills.processTimeouts;
  }

  public static update(time: TimeProxy) {
    this.tick = time.total;
    this.frame = time.frame;
    this.processTimeouts(time.total);
  }
}
