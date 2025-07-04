import React from "react";
import type {
  ViewProps,
  GumpProps,
  ButtonProps,
  TextInputProps,
  TextProps,
  CheckboxProps,
  LabelProps,
  HSliderBarProps,
} from "./components";

export const View = "view" as unknown as React.ComponentType<ViewProps>;
export const Gump = "gump" as unknown as React.ComponentType<GumpProps>;
export const Button = "button" as unknown as React.ComponentType<ButtonProps>;
export const TextInput =
  "textinput" as unknown as React.ComponentType<TextInputProps>;
export const Text = "text" as unknown as React.ComponentType<TextProps>;
export const Checkbox = "checkbox" as unknown as React.ComponentType<CheckboxProps>;
export const Label = "label" as unknown as React.ComponentType<LabelProps>;
export const HSliderBar = "hsliderbar" as unknown as React.ComponentType<HSliderBarProps>;

export * from "./utils";

export type {
  ViewProps,
  GumpProps,
  ButtonProps,
  TextInputProps,
  TextProps,
  CheckboxProps,
  LabelProps,
  HSliderBarProps,
} from "./components";
export * from "./renderer";
