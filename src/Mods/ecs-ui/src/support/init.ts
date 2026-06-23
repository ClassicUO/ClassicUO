// Global polyfills, applied at MODULE LOAD (before react-reconciler/scheduler
// evaluate). Must be the first import in the entry. StarlingMonkey's native
// timers + MessageChannel are bound to a host event loop that never turns in a
// synchronous component, so React's scheduled work would hang — force everything
// onto a frame-pumped timer.
import { buildTimeoutManager } from './polyfill/timeout';

const mgr = buildTimeoutManager();

// Force-override unconditionally (a native setTimeout would silently win otherwise).
(globalThis as any).setTimeout = mgr.setTimeout;
(globalThis as any).clearTimeout = mgr.clearTimeout;
(globalThis as any).requestAnimationFrame = mgr.requestAnimationFrame;
(globalThis as any).cancelAnimationFrame = mgr.cancelAnimationFrame;
(globalThis as any).queueMicrotask = (cb: () => void) => mgr.setTimeout(cb, 0);
// Push React's scheduler onto the setTimeout fallback instead of MessageChannel.
(globalThis as any).MessageChannel = undefined;

// Pump from the update system each frame so deferred React work runs.
export const pumpTimers = (tick: number) => mgr.processTimeouts(tick);
