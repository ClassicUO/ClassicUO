declare global {
  function setTimeout(callback: () => void, delay: number): number;
  function clearTimeout(timeoutId: number): void;
}

export {};
