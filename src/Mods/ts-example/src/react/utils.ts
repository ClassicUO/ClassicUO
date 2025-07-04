import {
  ClayColor,
  Vector2,
  Vector3,
  ClaySizingAxis,
  ClaySizingType,
  ClayLayoutDirection,
  ClayLayoutAlignment,
  ClayLayoutAlignment,
  ClayFloatingAttachToElement,
  ClayFloatingClipToElement,
  ClayTextAlignment,
  ClaySizing,
} from "../types";

// Utility functions for creating Clay UI structures

export function createColor(
  r: number,
  g: number,
  b: number,
  a: number = 1
): ClayColor {
  return { r, g, b, a };
}

export function createSizing(
  type: ClaySizingType,
  value?: number,
  min?: number,
  max?: number
): ClaySizing {
  if (type === ClaySizingType.Percent) {
    return {
      type: ClaySizingType.Percent,
      size: {
        minMax: { min: 0, max: 0 },
        percent: value,
      },
    };
  } else if (type === ClaySizingType.Fit) {
    return {
      type: ClaySizingType.Fit,
      size: {
        minMax: { min: min || 0, max: max || 0 },
      },
    };
  } else if (type === ClaySizingType.Fixed) {
    return {
      type: ClaySizingType.Fixed,
      size: {
        minMax: { min: value, max: value },
        percent: value,
      },
    };
  } else if (type === ClaySizingType.Grow) {
    return {
      type: ClaySizingType.Grow,
      size: {},
    };
  }

  throw new Error(`Invalid sizing type: ${type}`);
}

export const Colors = {
  transparent: createColor(0, 0, 0, 0),
  white: createColor(1, 1, 1, 1),
  black: createColor(0, 0, 0, 1),
  gray: createColor(0.5, 0.5, 0.5, 1),
  lightGray: createColor(0.8, 0.8, 0.8, 1),
  darkGray: createColor(0.2, 0.2, 0.2, 1),
  red: createColor(1, 0, 0, 1),
  green: createColor(0, 1, 0, 1),
  blue: createColor(0, 0, 1, 1),
  link: createColor(0.4, 0.6, 1, 1), // Light blue for links
};

export const Sizing = {
  grow: () => createSizing(ClaySizingType.Grow),
  fit: (min = 0, max = 0) =>
    createSizing(ClaySizingType.Fit, undefined, min, max),
  fixed: (value: number) => createSizing(ClaySizingType.Fixed, value),
  percent: (value: number) => createSizing(ClaySizingType.Percent, value),
};

export const Layout = {
  direction: {
    horizontal: ClayLayoutDirection.LeftToRight,
    vertical: ClayLayoutDirection.TopToBottom,
  },
  align: {
    x: {
      left: ClayLayoutAlignment.Left,
      center: ClayLayoutAlignment.Center,
      right: ClayLayoutAlignment.Right,
    },
    y: {
      top: ClayLayoutAlignment.Top,
      center: ClayLayoutAlignment.Center,
      bottom: ClayLayoutAlignment.Bottom,
    },
  },
  floating: {
    attachTo: {
      none: ClayFloatingAttachToElement.None,
      parent: ClayFloatingAttachToElement.Parent,
      element: ClayFloatingAttachToElement.ElementWithId,
    },
    clipTo: {
      none: ClayFloatingClipToElement.None,
      parent: ClayFloatingClipToElement.AttachedParent,
    },
  },
};

export const Floating = {
  attach: {
    none: ClayFloatingAttachToElement.None,
    parent: ClayFloatingAttachToElement.Parent,
    element: ClayFloatingAttachToElement.ElementWithId,
    root: ClayFloatingAttachToElement.Root,
  },
  clip: {
    none: ClayFloatingClipToElement.None,
    parent: ClayFloatingClipToElement.AttachedParent,
  },
};

export const TextAlign = {
  left: ClayTextAlignment.Left,
  center: ClayTextAlignment.Center,
  right: ClayTextAlignment.Right,
};

export const CommonLayouts = {
  fillParent: {
    sizing: {
      width: Sizing.grow(),
      height: Sizing.grow(),
    },
  },

  center: {
    sizing: {
      width: Sizing.fit(),
      height: Sizing.fit(),
    },
    childAlignment: {
      x: Layout.align.x.center,
      y: Layout.align.y.center,
    },
  },

  column: {
    layoutDirection: Layout.direction.vertical,
    sizing: {
      width: Sizing.fit(),
      height: Sizing.fit(),
    },
  },

  row: {
    layoutDirection: Layout.direction.horizontal,
    sizing: {
      width: Sizing.fit(),
      height: Sizing.fit(),
    },
  },

  absolutePosition: (x: number, y: number) => ({
    floating: {
      offset: { x, y },
      attachTo: Floating.attach.parent,
      clipTo: Floating.clip.parent,
    },
  }),
};

export const TextStyles = {
  default: {
    fontId: 0,
    fontSize: 12,
    textColor: Colors.white,
    textAlignment: TextAlign.left,
  },

  title: {
    fontId: 4,
    fontSize: 24,
    textColor: Colors.white,
    textAlignment: TextAlign.center,
  },

  button: {
    fontId: 2,
    fontSize: 16,
    textColor: Colors.white,
    textAlignment: TextAlign.center,
  },

  input: {
    fontId: 0,
    fontSize: 14,
    textColor: Colors.black,
    textAlignment: TextAlign.left,
  },
};
