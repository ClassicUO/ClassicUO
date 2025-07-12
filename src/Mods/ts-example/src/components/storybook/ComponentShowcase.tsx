import React from "react";
import { ChildAlign, Colors, Float, Sizing, TextStyle } from "~/ui";
import {
  FloatingAttachToElement,
  FloatingClipToElement,
  LayoutAlignment,
  LayoutDirection,
  SizingType,
} from "~/host";
import { View, Gump, Text } from "~/react";

export interface ComponentShowcaseProps {
  name: string;
  description?: string;
  code?: string;
  children: React.ReactNode;
  variant?: string;
  width?: number;
  height?: number;
}

/**
 * ComponentShowcase - Individual component demonstration container
 *
 * Displays a single component with its name, description, and optional code example.
 * Provides consistent spacing and visual separation between different component examples.
 */
export const ComponentShowcase: React.FC<ComponentShowcaseProps> = ({
  name,
  description,
  code,
  children,
  variant,
  width = 200,
  height = 150,
}) => {
  return (
    <View
      backgroundColor={Colors.transparent}
      layout={{
        layoutDirection: LayoutDirection.TopToBottom,
        sizing: {
          width: Sizing.fixed(width),
          height: Sizing.fixed(height),
        },
        childAlignment: ChildAlign.center,
      }}
    >
      {/* Component showcase background */}
      <Gump
        id={0x13be}
        ninePatch={true}
        size={{ width: width - 10, height: height - 10 }}
      >
        {/* Component name */}
        <Text style={TextStyle.medium}>
          {variant ? `${name} - ${variant}` : name}
        </Text>

        {/* Component description */}
        {description && <Text style={TextStyle.small}>{description}</Text>}

        {/* Component demo area */}
        <View
          backgroundColor={Colors.transparent}
          layout={{
            layoutDirection: LayoutDirection.TopToBottom,
            sizing: {
              width: Sizing.fixed(width - 40),
              height: Sizing.fixed(height - 80),
            },
            childAlignment: ChildAlign.center,
          }}
        >
          {children}
        </View>

        {/* Code example (if provided) */}
        {code && <Text style={TextStyle.small}>{code}</Text>}
      </Gump>
    </View>
  );
};