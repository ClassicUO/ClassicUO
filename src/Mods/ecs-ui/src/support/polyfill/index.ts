import React from 'react';
import { buildTimeoutManager } from './timeout';

export type GlobalPolyfills = ReturnType<typeof polyfillGlobals>;

export const polyfillGlobals = () => {
  const extra = {
    React,
    ...buildTimeoutManager(),
  };

  Object.assign(globalThis, extra);

  return extra;
};
