import {
  SizingType,
  ClayColor,
  FloatingAttachToElement,
  FloatingClipToElement,
  TextAlignment,
  LayoutAlignment,
  LayoutDirection,
  ClaySizing,
  ClayText,
} from "~/host";

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
  type: SizingType,
  value?: number,
  min?: number,
  max?: number
): ClaySizing {
  if (type === SizingType.Percent) {
    return {
      type: SizingType.Percent,
      size: {
        minMax: { min: 0, max: 0 },
        percent: value,
      },
    };
  } else if (type === SizingType.Fit) {
    return {
      type: SizingType.Fit,
      size: {
        minMax: { min: min || 0, max: max || 0 },
      },
    };
  } else if (type === SizingType.Fixed) {
    return {
      type: SizingType.Fixed,
      size: {
        minMax: { min: value, max: value },
        percent: value,
      },
    };
  } else if (type === SizingType.Grow) {
    return {
      type: SizingType.Grow,
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
  lightBlue: color(0.4, 0.6, 1, 1),
  darkBlue: color(0.2, 0.4, 0.8, 1),
  lightGreen: color(0.4, 1, 0.4, 1),
  darkGreen: color(0.2, 0.8, 0.2, 1),
  lightRed: color(1, 0.4, 0.4, 1),
  darkRed: color(0.8, 0.2, 0.2, 1),
  lightYellow: color(1, 1, 0.4, 1),
  darkYellow: color(0.8, 0.8, 0.2, 1),
  lightPurple: color(0.4, 0.2, 0.8, 1),
  gold: color(1, 0.8, 0, 1),
  link: color(0.4, 0.6, 1, 1), // Light blue for links
};

export const Sizing = {
  grow: () => sizing(SizingType.Grow),
  fit: (min = 0, max = 0) => sizing(SizingType.Fit, undefined, min, max),
  fixed: (value: number) => sizing(SizingType.Fixed, value),
  percent: (value: number) => sizing(SizingType.Percent, value),
};

export const Layout = {
  direction: {
    horizontal: LayoutDirection.LeftToRight,
    vertical: LayoutDirection.TopToBottom,
  },
  align: {
    x: {
      left: LayoutAlignment.Left,
      center: LayoutAlignment.Center,
      right: LayoutAlignment.Right,
    },
    y: {
      top: LayoutAlignment.Top,
      center: LayoutAlignment.Center,
      bottom: LayoutAlignment.Bottom,
    },
  },
};

export const Float = {
  attach: {
    none: FloatingAttachToElement.None,
    parent: FloatingAttachToElement.Parent,
    element: FloatingAttachToElement.ElementWithId,
    root: FloatingAttachToElement.Root,
  },
  clip: {
    none: FloatingClipToElement.None,
    parent: FloatingClipToElement.AttachedParent,
  },
  offsetParent: (x: number, y: number) => ({
    offset: { x, y },
    attachTo: FloatingAttachToElement.Parent,
    clipTo: FloatingClipToElement.AttachedParent,
  }),
};

export const TextAlign = {
  left: TextAlignment.Left,
  center: TextAlignment.Center,
  right: TextAlignment.Right,
};

export const ChildAlign = {
  center: {
    x: LayoutAlignment.Center,
    y: LayoutAlignment.Center,
  },
  left: {
    x: LayoutAlignment.Left,
    y: LayoutAlignment.Center,
  },
  right: {
    x: LayoutAlignment.Right,
    y: LayoutAlignment.Center,
  },
  top: {
    x: LayoutAlignment.Center,
    y: LayoutAlignment.Top,
  },
  bottom: {
    x: LayoutAlignment.Center,
    y: LayoutAlignment.Bottom,
  },
  topLeft: {
    x: LayoutAlignment.Left,
    y: LayoutAlignment.Top,
  },
  topRight: {
    x: LayoutAlignment.Right,
    y: LayoutAlignment.Top,
  },
  bottomLeft: {
    x: LayoutAlignment.Left,
    y: LayoutAlignment.Bottom,
  },
  bottomRight: {
    x: LayoutAlignment.Right,
    y: LayoutAlignment.Bottom,
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
    fontSize: 10,
    textColor: Colors.white,
    textAlignment: TextAlign.left,
  },

  small: {
    fontId: 0,
    fontSize: 12,
    textColor: Colors.white,
    textAlignment: TextAlign.left,
  },

  medium: {
    fontId: 0,
    fontSize: 16,
    textColor: Colors.white,
    textAlignment: TextAlign.left,
  },

  large: {
    fontId: 0,
    fontSize: 18,
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
    fontSize: 22,
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
