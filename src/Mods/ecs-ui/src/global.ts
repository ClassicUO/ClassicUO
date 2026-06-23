/**
 * This is the global namespace for the mod, used to polyfill global functions.
 * {@see ./support/polyfill}
 */
declare global {
  function setTimeout(callback: () => void, delay: number): number;
  function clearTimeout(timeoutId: number): void;
  function tickTimeout(tick: number): void;
  function requestAnimationFrame(callback: () => void): number;
  function cancelAnimationFrame(requestId: number): void;
}

export {};
