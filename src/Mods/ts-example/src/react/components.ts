import * as React from "react";
import type {
  ClayLayoutConfig,
  ClayFloatingElementConfig,
  ClayColor,
  Vector3,
  Vector2,
  ClayText,
  UINode,
  ClaySize,
  ClayLayoutDirection,
  ClayChildAlignment,
} from "../types";

export interface ClayElement {
  type: string;
  props: any;
  children: ClayElement[];
  instanceId: number;
  node?: UINode;
}

// Base props that all components can accept
export interface BaseElementProps {
  children?: React.ReactNode;
  onClick?: () => void;
}

// View component props
export interface ViewProps extends BaseElementProps {
  backgroundColor?: ClayColor;
  layout: ClayLayoutConfig;
  floating?: ClayFloatingElementConfig;
  movable?: boolean;
  cornerRadius?: {
    topLeft: number;
    topRight: number;
    bottomLeft: number;
    bottomRight: number;
  };
  border?: {
    color: ClayColor;
    width: {
      left: number;
      right: number;
      top: number;
      bottom: number;
      betweenChildren: number;
    };
  };
  clip?: {
    horizontal: boolean;
    vertical: boolean;
    childOffset: Vector2;
  };
}

// Gump component props
export interface GumpProps extends BaseElementProps {
  gumpId: number;
  direction?: ClayLayoutDirection;
  floating?: ClayFloatingElementConfig;
  hue?: Partial<Vector3>;
  ninePatch?: boolean;
  size?: ClaySize;
  childAlignment?: ClayChildAlignment;
}

// Button component props
export interface ButtonProps extends BaseElementProps {
  gumpIds: { normal: number; pressed: number; over: number }; // [normal, pressed, over]
  movable?: boolean;
  acceptInputs?: boolean;
  size: ClaySize;
  floating?: ClayFloatingElementConfig;
  hue?: Vector3;
}

// Text component props
export interface TextProps extends BaseElementProps {
  children?: string;
  style?: ClayText;
  floating?: ClayFloatingElementConfig;
  size?: ClaySize;
  layout: ClayLayoutConfig;
}

// TextInput component props
export interface TextInputProps extends BaseElementProps {
  placeholder?: string;
  password?: boolean;
  floating?: ClayFloatingElementConfig;
  backgroundColor?: ClayColor;
  textStyle?: ClayText;
  value?: string;
  size?: ClaySize;
  acceptInputs?: boolean;
  onChange?: (value: string) => void;
}

// JSX Intrinsic Elements
declare global {
  namespace JSX {
    interface IntrinsicElements {
      View: ViewProps;
      Gump: GumpProps;
      Button: ButtonProps;
      Text: TextProps;
      TextInput: TextInputProps;
    }
  }
}

export type ClayElementNames =
  | "View"
  | "Gump"
  | "Button"
  | "Text"
  | "TextInput";

export type ClayElementPropTypes =
  | {
      type: Lowercase<"View">;
      props: ViewProps;
    }
  | {
      type: Lowercase<"gump">;
      props: GumpProps;
    }
  | {
      type: Lowercase<"button">;
      props: ButtonProps;
    }
  | {
      type: Lowercase<"text">;
      props: TextProps;
    }
  | {
      type: Lowercase<"textinput">;
      props: TextInputProps;
    };
