import React from "react";
import type {
  ViewProps,
  GumpProps,
  ButtonProps,
  TextInputProps,
  TextProps,
} from "./components";

export const View = "view" as unknown as React.ComponentType<ViewProps>;
export const Gump = "gump" as unknown as React.ComponentType<GumpProps>;
export const Button = "button" as unknown as React.ComponentType<ButtonProps>;
export const TextInput =
  "textinput" as unknown as React.ComponentType<TextInputProps>;
export const Text = "text" as unknown as React.ComponentType<TextProps>;

export * from "./utils";

export type {
  ViewProps,
  GumpProps,
  ButtonProps,
  TextInputProps,
  TextProps,
} from "./components";
export * from "./renderer";
