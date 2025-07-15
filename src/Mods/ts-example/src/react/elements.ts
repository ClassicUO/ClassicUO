import * as React from "react";
import type {
  ClayLayoutConfig,
  ClayFloatingElementConfig,
  ClayColor,
  Vector2,
  ClayText,
  UINode,
  ClaySize,
  LayoutDirection,
  ClayChildAlignment,
  Vector3,
  ClayPadding,
} from "~/host";
import { EventHandlerMap } from "./events";

export const defaultHue: Vector3 = { x: 0, y: 0, z: 1.0 };

export const hueToVector3 = (hue: Hue = {}): Vector3 => {
  return {
    x: hue.hue ?? 0,
    y: hue.hueMode ?? 0,
    z: hue.alpha ?? 1.0,
  };
};

export interface ClayElement {
  type: string;
  props: any;
  children: ClayElement[];
  instanceId: number;
  node?: UINode;
}

export type Hue = {
  hue?: number; // 0 - 65535 hue based on hues.mul
  hueMode?: number; // 0 - 255 hue mode
  alpha?: number; // 0.0 - 1.0 alpha
};

// Base props that all components can accept
export interface BaseElementProps extends EventHandlerMap {
  children?: React.ReactNode;
  movable?: boolean;
  padding?: ClayPadding;
}

// View component props
export interface ViewProps extends BaseElementProps {
  backgroundColor?: ClayColor;
  layout: ClayLayoutConfig;
  floating?: ClayFloatingElementConfig;
  padding?: ClayPadding;
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
  id: number;
  direction?: LayoutDirection;
  floating?: ClayFloatingElementConfig;
  hue?: Partial<Hue>;
  ninePatch?: boolean;
  size?: ClaySize;
  padding?: ClayPadding;
  childAlignment?: ClayChildAlignment;
}

// Button component props
export interface ButtonProps extends BaseElementProps {
  gumpIds: { normal: number; pressed: number; over: number }; // [normal, pressed, over]
  movable?: boolean;
  acceptInputs?: boolean;
  size: ClaySize;
  floating?: ClayFloatingElementConfig;
  padding?: ClayPadding;
  hue?: Partial<Hue>;
}

// Text component props
export interface TextProps extends BaseElementProps {
  style?: ClayText;
  floating?: ClayFloatingElementConfig;
  padding?: ClayPadding;
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
  padding?: ClayPadding;
  size?: ClaySize;
  acceptInputs?: boolean;
  onChange?: (value: string) => void;
}

// Checkbox component props
export interface CheckboxProps extends BaseElementProps {
  checked?: boolean;
  onChange?: (checked: boolean) => void;
  floating?: ClayFloatingElementConfig;
  padding?: ClayPadding;
  size?: ClaySize;
}

// HSliderBar component props
export interface HSliderBarProps extends BaseElementProps {
  min?: number;
  max?: number;
  value?: number;
  onChange?: (value: number) => void;
  floating?: ClayFloatingElementConfig;
  padding?: ClayPadding;
  size?: ClaySize;
  barGumpId?: number;
  handleGumpId?: number;
}

// Art component props
export interface ArtProps extends BaseElementProps {
  id: number;
  floating?: ClayFloatingElementConfig;
  hue?: Partial<Hue>;
  padding?: ClayPadding;
  size?: ClaySize;
}

export type ClayElementMap = {
  View: ViewProps;
  Gump: GumpProps;
  Button: ButtonProps;
  Text: TextProps;
  TextInput: TextInputProps;
  Checkbox: CheckboxProps;
  Label: LabelProps;
  HSliderBar: HSliderBarProps;
  Art: ArtProps;
};

export type ClayElementNames = keyof ClayElementMap;

export type ClayElementPropTypes = {
  [K in keyof ClayElementMap]: {
    type: Lowercase<K>;
    props: ClayElementMap[K];
  };
}[keyof ClayElementMap];

// primitive built-in components, these resolve to Clay elements inside CUO
export const View = "view" as unknown as React.FC<ViewProps>;
export const Gump = "gump" as unknown as React.FC<GumpProps>;
export const Button = "button" as unknown as React.FC<ButtonProps>;
export const Text = "text" as unknown as React.FC<TextProps>;
export const TextInput = "textinput" as unknown as React.FC<TextInputProps>;
export const Checkbox = "checkbox" as unknown as React.FC<CheckboxProps>;
export const Label = "label" as unknown as React.FC<LabelProps>;
export const HSliderBar = "hsliderbar" as unknown as React.FC<HSliderBarProps>;
export const Art = "art" as unknown as React.FC<ArtProps>;
