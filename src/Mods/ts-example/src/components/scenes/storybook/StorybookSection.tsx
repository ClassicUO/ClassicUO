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

export interface StorybookSectionProps {
  title: string;
  description?: string;
  children: React.ReactNode;
  spacing?: number;
}

/**
 * StorybookSection - A section within the storybook for grouping related components
 * 
 * Provides a titled section with optional description and proper spacing
 * for organizing component demonstrations.
 */
export const StorybookSection: React.FC<StorybookSectionProps> = ({
  title,
  description,
  children,
  spacing = 20,
}) => {
  return (
    <View
      backgroundColor={Colors.transparent}
      layout={{
        layoutDirection: ClayLayoutDirection.TopToBottom,
        sizing: {
          width: {
            type: ClaySizingType.Grow,
            size: {},
          },
          height: {
            type: ClaySizingType.Fit,
            size: {},
          },
        },
        childAlignment: {
          x: ClayLayoutAlignment.Left,
          y: ClayLayoutAlignment.Top,
        },
      }}
    >
      {/* Section background */}
      <Gump
        gumpId={0x0bb8}
        ninePatch={true}
        size={{ width: 720, height: 40 }}
        hue={{ x: 0, y: 0, z: 0.8 }}
        floating={{
          offset: { x: 0, y: 0 },
          attachTo: ClayFloatingAttachToElement.Parent,
          clipTo: ClayFloatingClipToElement.AttachedParent,
        }}
      />

      {/* Section title */}
      <Text
        style={TextStyle.medium}
        floating={Float.offsetParent(10, 12)}
      >
        {title}
      </Text>

      {/* Section description */}
      {description && (
        <Text
          style={TextStyle.small}
          floating={Float.offsetParent(10, 50)}
        >
          {description}
        </Text>
      )}

      {/* Content area with proper spacing */}
      <View
        backgroundColor={Colors.transparent}
        layout={{
          layoutDirection: ClayLayoutDirection.LeftToRight,
          sizing: {
            width: {
              type: ClaySizingType.Grow,
              size: {},
            },
            height: {
              type: ClaySizingType.Fit,
              size: {},
            },
          },
          childAlignment: {
            x: ClayLayoutAlignment.Left,
            y: ClayLayoutAlignment.Top,
          },
        }}
        floating={Float.offsetParent(10, description ? 80 : 50)}
      >
        {children}
      </View>
    </View>
  );
};