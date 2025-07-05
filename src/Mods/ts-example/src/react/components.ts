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
} from "~/types";

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
  movable?: boolean;
  acceptInputs?: boolean;
  textConfig?: ClayText;
}

// View component props
export interface ViewProps extends BaseElementProps {
  backgroundColor?: ClayColor;
  layout: ClayLayoutConfig;
  floating?: ClayFloatingElementConfig;
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
  hue?: Partial<Vector3>;
}

// Text component props
export interface TextProps extends BaseElementProps {
  children?: string;
  style?: ClayText;
  floating?: ClayFloatingElementConfig;
  size?: ClaySize;
}

export type LabelProps = TextProps;

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

// Checkbox component props
export interface CheckboxProps extends BaseElementProps {
  checked?: boolean;
  onChange?: (checked: boolean) => void;
  floating?: ClayFloatingElementConfig;
  size?: ClaySize;
}

// HSliderBar component props
export interface HSliderBarProps extends BaseElementProps {
  min?: number;
  max?: number;
  value?: number;
  onChange?: (value: number) => void;
  floating?: ClayFloatingElementConfig;
  size?: ClaySize;
  barGumpId?: number;
  handleGumpId?: number;
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
      Checkbox: CheckboxProps;
      Label: LabelProps;
      HSliderBar: HSliderBarProps;
    }
  }
}

export type ClayElementNames =
  | "View"
  | "Gump"
  | "Button"
  | "Text"
  | "TextInput"
  | "Checkbox"
  | "Label"
  | "HSliderBar";

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
    }
  | {
      type: Lowercase<"checkbox">;
      props: CheckboxProps;
    }
  | {
      type: Lowercase<"label">;
      props: LabelProps;
    }
  | {
      type: Lowercase<"hsliderbar">;
      props: HSliderBarProps;
    };
