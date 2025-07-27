export type DiffKeys = {
  updated: string[];
  unchanged: string[];
  added: string[];
  deleted: string[];
};

/**
 * @param base - The base object to compare against
 * @param compared - The object to compare to the base
 * @returns An object containing the updated, unchanged, added, and deleted keys
 */
export function shallowDiffKeys(base: Record<string, unknown>, compared: Record<string, unknown>): DiffKeys {
  const unchanged: string[] = [];
  const updated: string[] = [];
  const deleted: string[] = [];
  const added: string[] = [];

  // Loop through the compared object
  Object.keys(compared).forEach((key) => {
    if (!(key in base)) {
      // added
      added.push(key);
    } else if (compared[key] !== base[key]) {
      // updated
      updated.push(key);
    } else if (compared[key] === base[key]) {
      // unchanged
      unchanged.push(key);
    }
  });

  // Loop through the before object
  Object.keys(base).forEach((key) => {
    if (!(key in compared)) {
      // deleted
      deleted.push(key);
    }
  });

  return {
    updated: updated,
    unchanged: unchanged,
    added: added,
    deleted: deleted,
  };
}
