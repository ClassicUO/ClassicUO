import React from 'react';
import { State } from './state';

export const polyfill = () => {
  console.log('polyfill');

  const timeoutManager = TimeoutManager();
  Object.assign(globalThis, {
    React,
    ...timeoutManager,
  });

  return {
    timeoutManager,
  };
};

const TimeoutManager = () => {
  const timeouts = new Map<number, { callback: () => void; delay: number }>();

  return {
    setTimeout: (callback: () => void, delay: number) => {
      const id = Math.floor(Math.random() * 1000000);
      timeouts.set(id, { callback, delay: State.tick + delay });
      return id;
    },
    clearTimeout: (timeoutId: number) => {
      timeouts.delete(timeoutId);
    },
    processTimeouts: (tick: number) => {
      if (timeouts.size === 0) {
        return;
      }

      for (const [id, timeout] of timeouts) {
        if (tick >= timeout.delay) {
          try {
            timeout.callback();
          } catch (e) {
            console.error('set timeout callback error', e);
          }
          timeouts.delete(id);
        }
      }
    },
  };
};
