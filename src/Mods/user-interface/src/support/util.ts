export type DiffKeys = {
  updated: string[];
  unchanged: string[];
  added: string[];
  deleted: string[];
};

export function shallowDiff(base: Record<string, unknown>, compared: Record<string, unknown>): DiffKeys {
  const unchanged: string[] = [];
  const updated: string[] = [];
  const deleted: string[] = [];
  const added: string[] = [];

  // Loop through the compared object
  Object.keys(compared).forEach((key) => {
    // To get the added items
    if (!(key in base)) {
      added.push(key);

      // The updated items
    } else if (compared[key] !== base[key]) {
      updated.push(key);

      // And the unchanged
    } else if (compared[key] === base[key]) {
      unchanged.push(key);
    }
  });

  // Loop through the before object
  Object.keys(base).forEach((key) => {
    // To get the deleted items
    if (!(key in compared)) {
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
