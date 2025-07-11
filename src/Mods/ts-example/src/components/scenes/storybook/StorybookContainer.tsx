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

export interface StorybookContainerProps {
  children: React.ReactNode;
  title: string;
}

/**
 * StorybookContainer - A container for showcasing design system components
 *
 * Provides a clean, organized layout for displaying and testing UI components
 * with a dark background and proper spacing.
 */
export const StorybookContainer: React.FC<StorybookContainerProps> = ({
  children,
  title,
}) => {
  return (
    <View
      backgroundColor={Colors.darkGray}
      layout={{
        layoutDirection: ClayLayoutDirection.TopToBottom,
        sizing: {
          width: {
            type: ClaySizingType.Grow,
            size: {},
          },
          height: {
            type: ClaySizingType.Grow,
            size: {},
          },
        },
        childAlignment: {
          x: ClayLayoutAlignment.Center,
          y: ClayLayoutAlignment.Top,
        },
      }}
    >
      {/* Title */}
      <Text style={TextStyle.title} floating={Float.offsetParent(400, 30)}>
        {title}
      </Text>

      {/* Content area */}
      <View
        backgroundColor={Colors.transparent}
        layout={{
          layoutDirection: ClayLayoutDirection.TopToBottom,
          sizing: {
            width: {
              type: ClaySizingType.Fixed,
              size: {
                minMax: { min: 760, max: 760 },
                percent: 760,
              },
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
        floating={Float.offsetParent(20, 80)}
      >
        {children}
      </View>
    </View>
  );
};
