import {
  ClaySizingType,
  ClayColor,
  ClayFloatingAttachToElement,
  ClayFloatingClipToElement,
  ClayTextAlignment,
  ClayLayoutAlignment,
  ClayLayoutDirection,
  ClaySizing,
  ClayText,
} from "~/types";

// Base64 encoding function (replacement for btoa)
export function base64Encode(data: Uint8Array): string {
  return Host.arrayBufferToBase64(data);
}

export function base64Decode(data: string): Uint8Array {
  return new Uint8Array(Host.base64ToArrayBuffer(data));
}

export function color(
  r: number,
  g: number,
  b: number,
  a: number = 1
): ClayColor {
  return { r, g, b, a };
}

export function sizing(
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
  transparent: color(0, 0, 0, 0),
  white: color(1, 1, 1, 1),
  black: color(0, 0, 0, 1),
  gray: color(0.5, 0.5, 0.5, 1),
  lightGray: color(0.8, 0.8, 0.8, 1),
  darkGray: color(0.2, 0.2, 0.2, 1),
  red: color(1, 0, 0, 1),
  green: color(0, 1, 0, 1),
  blue: color(0, 0, 1, 1),
  link: color(0.4, 0.6, 1, 1), // Light blue for links
};

export const Sizing = {
  grow: () => sizing(ClaySizingType.Grow),
  fit: (min = 0, max = 0) => sizing(ClaySizingType.Fit, undefined, min, max),
  fixed: (value: number) => sizing(ClaySizingType.Fixed, value),
  percent: (value: number) => sizing(ClaySizingType.Percent, value),
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
};

export const Float = {
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
  offsetParent: (x: number, y: number) => ({
    offset: { x, y },
    attachTo: ClayFloatingAttachToElement.Parent,
    clipTo: ClayFloatingClipToElement.AttachedParent,
  }),
};

export const TextAlign = {
  left: ClayTextAlignment.Left,
  center: ClayTextAlignment.Center,
  right: ClayTextAlignment.Right,
};

export const ChildAlign = {
  center: {
    x: ClayLayoutAlignment.Center,
    y: ClayLayoutAlignment.Center,
  },
  left: {
    x: ClayLayoutAlignment.Left,
    y: ClayLayoutAlignment.Center,
  },
  right: {
    x: ClayLayoutAlignment.Right,
    y: ClayLayoutAlignment.Center,
  },
  top: {
    x: ClayLayoutAlignment.Center,
    y: ClayLayoutAlignment.Top,
  },
  bottom: {
    x: ClayLayoutAlignment.Center,
    y: ClayLayoutAlignment.Bottom,
  },
  topLeft: {
    x: ClayLayoutAlignment.Left,
    y: ClayLayoutAlignment.Top,
  },
  topRight: {
    x: ClayLayoutAlignment.Right,
    y: ClayLayoutAlignment.Top,
  },
  bottomLeft: {
    x: ClayLayoutAlignment.Left,
    y: ClayLayoutAlignment.Bottom,
  },
  bottomRight: {
    x: ClayLayoutAlignment.Right,
    y: ClayLayoutAlignment.Bottom,
  },
};

export const CommonLayout = {
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
      attachTo: Float.attach.parent,
      clipTo: Float.clip.parent,
    },
  }),
};

export const TextStyle = {
  default: {
    fontId: 0,
    fontSize: 12,
    textColor: Colors.white,
    textAlignment: TextAlign.left,
  },
  xsmall: {
    fontId: 0,
    fontSize: 8,
    textColor: Colors.white,
    textAlignment: TextAlign.left,
  },

  small: {
    fontId: 0,
    fontSize: 10,
    textColor: Colors.white,
    textAlignment: TextAlign.left,
  },

  medium: {
    fontId: 0,
    fontSize: 14,
    textColor: Colors.white,
    textAlignment: TextAlign.left,
  },

  large: {
    fontId: 0,
    fontSize: 16,
    textColor: Colors.white,
    textAlignment: TextAlign.left,
  },

  xlarge: {
    fontId: 0,
    fontSize: 20,
    textColor: Colors.white,
    textAlignment: TextAlign.left,
  },

  xxlarge: {
    fontId: 0,
    fontSize: 24,
    textColor: Colors.white,
    textAlignment: TextAlign.left,
  },

  // common styles
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
    textColor: Colors.white,
    textAlignment: TextAlign.left,
  },

  link: {
    fontId: 3,
    fontSize: 12,
    textColor: Colors.link,
  },
} satisfies Record<string, ClayText>;
