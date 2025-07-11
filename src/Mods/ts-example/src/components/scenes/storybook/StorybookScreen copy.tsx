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
      {/* Navigation/Filter Section */}
      <StorybookSection
        title="Component Categories"
        description="Browse design system components by category"
      >
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
        >
          <Text style={TextStyle.small} floating={Float.offsetParent(10, 10)}>
            Categories: All | Foundation | Input | Action | Display | Game |
            Feedback
          </Text>
        </View>
      </StorybookSection>

      {/* Component Stories Section */}
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
        {/* High Priority Components */}
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
            {/* Action Components */}
            <IconButtonStory />

            {/* Placeholder for other high priority components */}
            <StorybookSection
              title="Coming Soon"
              description="More components will be added as they are implemented"
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
                <Text
                  style={TextStyle.small}
                  floating={Float.offsetParent(10, 10)}
                >
                  Foundation: Panel, Card, ScrollView, Flex, Stack, Modal,
                  Tooltip
                </Text>
                <Text
                  style={TextStyle.small}
                  floating={Float.offsetParent(10, 25)}
                >
                  Input: Input, PasswordInput, Button
                </Text>
                <Text
                  style={TextStyle.small}
                  floating={Float.offsetParent(10, 40)}
                >
                  Display: Progress, Spinner, Badge, Avatar
                </Text>
                <Text
                  style={TextStyle.small}
                  floating={Float.offsetParent(10, 55)}
                >
                  Game: HealthBar, ItemIcon, Paperdoll
                </Text>
              </View>
            </StorybookSection>
          </View>
        </StorybookSection>

        {/* Development Guidelines */}
        <StorybookSection
          title="Development Guidelines"
          description="Best practices for using and extending the design system"
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
            <Text style={TextStyle.small} floating={Float.offsetParent(10, 10)}>
              • Use only available reconciler primitives (Button, Gump, Art,
              View, Text)
            </Text>
            <Text style={TextStyle.small} floating={Float.offsetParent(10, 25)}>
              • Follow UO authentic design patterns and gump IDs
            </Text>
            <Text style={TextStyle.small} floating={Float.offsetParent(10, 40)}>
              • Implement proper TypeScript types and documentation
            </Text>
            <Text style={TextStyle.small} floating={Float.offsetParent(10, 55)}>
              • Add component stories to this storybook for testing
            </Text>
            <Text style={TextStyle.small} floating={Float.offsetParent(10, 70)}>
              • Consider accessibility, performance, and customization
            </Text>
          </View>
        </StorybookSection>
      </View>
    </StorybookContainer>
  );
};
