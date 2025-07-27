/**
 * We don't have a real delay/timeout system, so we add our own using the ticks from on_update()
 */
export type TimeoutManager = {
  setTimeout: (callback: () => void, delay: number) => number;
  clearTimeout: (timeoutId: number) => void;
  processTimeouts: (tick: number) => void;
  requestAnimationFrame: (callback: () => void) => number;
  cancelAnimationFrame: (requestId: number) => void;
};

export const buildTimeoutManager = (): TimeoutManager => {
  const timeouts = new Map<number, { callback: () => void; delay: number }>();
  let lastTick = 0;

  return {
    setTimeout: (callback: () => void, delay: number): number => {
      const id = Math.floor(Math.random() * 1000000);
      timeouts.set(id, { callback, delay: lastTick + delay });
      return id;
    },

    clearTimeout: (timeoutId: number): void => {
      timeouts.delete(timeoutId);
    },

    processTimeouts: (tick: number): void => {
      lastTick = tick;

      if (timeouts.size === 0) return;

      for (const [id, timeout] of timeouts) {
        if (tick < timeout.delay) continue;

        try {
          timeout.callback();
        } catch (e) {
          console.error('timeout callback exception', id, e);
        } finally {
          timeouts.delete(id);
        }
      }
    },

    requestAnimationFrame: (callback: () => void): number => {
      const id = Math.floor(Math.random() * 1000000);
      timeouts.set(id, { callback, delay: lastTick + 1 });
      return id;
    },

    cancelAnimationFrame: (requestId: number): void => {
      timeouts.delete(requestId);
    },
  };
};
