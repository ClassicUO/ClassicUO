import React from "react";
import { State } from "./state";

export const polyfill = () => {
  console.log("polyfill");

  Object.assign(globalThis, {
    React,
    setTimeout: (callback: () => void, delay: number) => {
      const id = Math.floor(Math.random() * 1000000);
      State.timeouts.set(id, { callback, delay });
      return id;
    },
    clearTimeout: (timeoutId: number) => {
      State.timeouts.delete(timeoutId);
    },
  });
};
