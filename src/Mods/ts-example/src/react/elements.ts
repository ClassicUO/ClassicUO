import * as React from "react";
import type {
  ClayLayoutConfig,
  ClayFloatingElementConfig,
  ClayColor,
  Vector2,
  ClayText,
  UINode,
  ClaySize,
  ClayLayoutDirection,
  ClayChildAlignment,
  Vector3,
} from "~/host";
import { EventHandlerMap } from "./events";

export interface ClayElement {
  type: string;
  props: any;
  children: ClayElement[];
  instanceId: number;
  node?: UINode;
}

// Base props that all components can accept
export interface BaseElementProps extends EventHandlerMap {
  children?: React.ReactNode;
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

// Art component props
export interface ArtProps extends BaseElementProps {
  artId: number;
  floating?: ClayFloatingElementConfig;
  hue?: Partial<Vector3>;
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
