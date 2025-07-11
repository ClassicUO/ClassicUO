import React from "react";
import { Colors, Float, TextStyle } from "~/ui";
import {
  ClayFloatingAttachToElement,
  ClayFloatingClipToElement,
  ClayLayoutAlignment,
  ClayLayoutDirection,
  ClaySizingType,
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
        layoutDirection: ClayLayoutDirection.TopToBottom,
        sizing: {
          width: {
            type: ClaySizingType.Fixed,
            size: {
              minMax: { min: width, max: width },
              percent: width,
            },
          },
          height: {
            type: ClaySizingType.Fixed,
            size: {
              minMax: { min: height, max: height },
              percent: height,
            },
          },
        },
        childAlignment: {
          x: ClayLayoutAlignment.Center,
          y: ClayLayoutAlignment.Top,
        },
      }}
    >
      {/* Component showcase background */}
      <Gump
        gumpId={0x13be}
        ninePatch={true}
        size={{ width: width - 10, height: height - 10 }}
        hue={{ x: 0, y: 1, z: 0.5 }}
        floating={{
          offset: { x: 5, y: 5 },
          attachTo: ClayFloatingAttachToElement.Parent,
          clipTo: ClayFloatingClipToElement.AttachedParent,
        }}
      />

      {/* Component name */}
      <Text style={TextStyle.medium} floating={Float.offsetParent(10, 10)}>
        {variant ? `${name} - ${variant}` : name}
      </Text>

      {/* Component description */}
      {description && (
        <Text style={TextStyle.small} floating={Float.offsetParent(10, 25)}>
          {description}
        </Text>
      )}

      {/* Component demo area */}
      <View
        backgroundColor={Colors.transparent}
        layout={{
          layoutDirection: ClayLayoutDirection.TopToBottom,
          sizing: {
            width: {
              type: ClaySizingType.Fixed,
              size: {
                minMax: { min: width - 40, max: width - 40 },
                percent: width - 40,
              },
            },
            height: {
              type: ClaySizingType.Fixed,
              size: {
                minMax: { min: height - 80, max: height - 80 },
                percent: height - 80,
              },
            },
          },
          childAlignment: {
            x: ClayLayoutAlignment.Center,
            y: ClayLayoutAlignment.Center,
          },
        }}
        floating={Float.offsetParent(20, description ? 45 : 30)}
      >
        {children}
      </View>

      {/* Code example (if provided) */}
      {code && (
        <Text
          style={TextStyle.small}
          floating={Float.offsetParent(10, height - 35)}
        >
          {code}
        </Text>
      )}
    </View>
  );
};
