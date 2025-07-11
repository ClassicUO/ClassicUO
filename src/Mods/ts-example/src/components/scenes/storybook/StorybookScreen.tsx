import React from "react";
import { StorybookContainer } from "./StorybookContainer";
import { StorybookSection } from "./StorybookSection";
import { IconButtonStory } from "./stories";
import { Colors, Float, TextStyle } from "~/ui";
import { Text, View } from "~/react";
import {
  ClayLayoutAlignment,
  ClayLayoutDirection,
  ClaySizingType,
} from "~/host";

export interface StorybookScreenProps {
  onBack?: () => void;
}

/**
 * StorybookScreen - Main screen for showcasing all design system components
 *
 * Provides a comprehensive view of all available design system components
 * with interactive examples and documentation.
 */
export const StorybookScreen: React.FC<StorybookScreenProps> = ({ onBack }) => {
  const [selectedSection, setSelectedSection] = React.useState<string>("all");

  return (
    <StorybookContainer title="ClassicUO Design System">
      <Text style={TextStyle.large} floating={Float.offsetParent(10, 10)}>
        Categories: All | Foundation | Input | Action | Display | Game
      </Text>
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
        floating={Float.offsetParent(0, 100)}
      >
        <StorybookSection
          title="High Priority Components"
          description="Essential components needed for basic UIs"
        >
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
            <IconButtonStory />
          </View>
        </StorybookSection>
      </View>
    </StorybookContainer>
  );
};
