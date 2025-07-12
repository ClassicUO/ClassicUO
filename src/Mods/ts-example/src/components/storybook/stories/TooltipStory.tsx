import React from "react";
import { Tooltip, IconButton } from "~/components/design-system";
import { StorybookSection } from "../StorybookSection";
import { ComponentShowcase } from "../ComponentShowcase";
import {
  ChildAlign,
  Colors,
  Float,
  LayoutAlignment,
  LayoutDirection,
  Sizing,
  TextStyle,
} from "~/ui";
import { View, Text } from "~/react";

/**
 * TooltipStory - Comprehensive showcase of Tooltip component
 *
 * Demonstrates various configurations and use cases of the Tooltip component
 * including different positions, styles, content types, and wrapped components.
 */
export const TooltipStory: React.FC = () => {
  return (
    <StorybookSection
      title="Tooltip"
      description="Hover information display component that wraps child elements"
      spacing={40}
    >
      <View
        backgroundColor={Colors.transparent}
        layout={{
          layoutDirection: LayoutDirection.LeftToRight,
          sizing: {
            width: Sizing.grow(),
            height: Sizing.fit(),
          },
          childAlignment: ChildAlign.topLeft,
        }}
      >
        {/* Basic Tooltip */}
        <ComponentShowcase
          name="Basic Tooltip"
          description="Hover to see tooltip"
          code="content='Basic tooltip text'"
        >
          <Tooltip content="This is a basic tooltip">
            <IconButton
              icon={{ type: "gump", id: 0x15a4 }}
              size={{ width: 44, height: 44 }}
            />
          </Tooltip>
        </ComponentShowcase>

        {/* Top Position Tooltip */}
        <ComponentShowcase
          name="Top Position"
          description="Tooltip positioned above"
          code="position='top'"
        >
          <Tooltip 
            content="Tooltip positioned on top" 
            position="top"
          >
            <IconButton
              icon={{ type: "gump", id: 0x15a4 }}
              size={{ width: 44, height: 44 }}
            />
          </Tooltip>
        </ComponentShowcase>

        {/* Bottom Position Tooltip */}
        <ComponentShowcase
          name="Bottom Position"
          description="Tooltip positioned below"
          code="position='bottom'"
        >
          <Tooltip 
            content="Tooltip positioned below" 
            position="bottom"
          >
            <IconButton
              icon={{ type: "gump", id: 0x15a4 }}
              size={{ width: 44, height: 44 }}
            />
          </Tooltip>
        </ComponentShowcase>

        {/* Long Text Tooltip */}
        <ComponentShowcase
          name="Long Text"
          description="Tooltip with longer content"
          code="maxWidth={200}"
        >
          <Tooltip 
            content="This is a much longer tooltip text that demonstrates how the component handles wrapping and sizing for extended content that goes beyond normal tooltip lengths."
            maxWidth={200}
          >
            <IconButton
              icon={{ type: "gump", id: 0x15a4 }}
              size={{ width: 44, height: 44 }}
            />
          </Tooltip>
        </ComponentShowcase>
      </View>

      {/* Second row with more variants */}
      <View
        backgroundColor={Colors.transparent}
        layout={{
          layoutDirection: LayoutDirection.LeftToRight,
          sizing: {
            width: Sizing.grow(),
            height: Sizing.fit(),
          },
          childAlignment: {
            x: LayoutAlignment.Left,
            y: LayoutAlignment.Top,
          },
        }}
      >
        {/* Left Position Tooltip */}
        <ComponentShowcase
          name="Left Position"
          description="Tooltip positioned to the left"
          code="position='left'"
        >
          <Tooltip 
            content="Left positioned tooltip" 
            position="left"
          >
            <IconButton
              icon={{ type: "gump", id: 0x15a4 }}
              size={{ width: 44, height: 44 }}
            />
          </Tooltip>
        </ComponentShowcase>

        {/* Right Position Tooltip */}
        <ComponentShowcase
          name="Right Position"
          description="Tooltip positioned to the right"
          code="position='right'"
        >
          <Tooltip 
            content="Right positioned tooltip" 
            position="right"
          >
            <IconButton
              icon={{ type: "gump", id: 0x15a4 }}
              size={{ width: 44, height: 44 }}
            />
          </Tooltip>
        </ComponentShowcase>

        {/* Custom Background Tooltip */}
        <ComponentShowcase
          name="Custom Background"
          description="Tooltip with custom background"
          code="backgroundGumpId={0x13be}"
        >
          <Tooltip 
            content="Custom background style" 
            backgroundGumpId={0x13be}
          >
            <IconButton
              icon={{ type: "gump", id: 0x15a4 }}
              size={{ width: 44, height: 44 }}
            />
          </Tooltip>
        </ComponentShowcase>

        {/* Tooltip with Arrow */}
        <ComponentShowcase
          name="With Arrow"
          description="Tooltip with arrow indicator"
          code="showArrow={true}"
        >
          <Tooltip 
            content="Tooltip with arrow" 
            showArrow={true}
          >
            <IconButton
              icon={{ type: "gump", id: 0x15a4 }}
              size={{ width: 44, height: 44 }}
            />
          </Tooltip>
        </ComponentShowcase>
      </View>

      {/* Third row with rich content examples */}
      <View
        backgroundColor={Colors.transparent}
        layout={{
          layoutDirection: LayoutDirection.LeftToRight,
          sizing: {
            width: Sizing.grow(),
            height: Sizing.fit(),
          },
          childAlignment: {
            x: LayoutAlignment.Left,
            y: LayoutAlignment.Top,
          },
        }}
      >
        {/* Rich Content Tooltip */}
        <ComponentShowcase
          name="Rich Content"
          description="Tooltip with React elements"
          code="content={<View>...</View>}"
          width={250}
        >
          <Tooltip 
            content={
              <View
                backgroundColor={Colors.transparent}
                layout={{
                  layoutDirection: LayoutDirection.TopToBottom,
                  sizing: { width: Sizing.fit(), height: Sizing.fit() },
                  childAlignment: ChildAlign.topLeft,
                }}
              >
                <Text style={{ ...TextStyle.medium, textColor: Colors.gold }}>
                  Rich Tooltip
                </Text>
                <Text style={{ ...TextStyle.small, textColor: Colors.lightGray }}>
                  With multiple elements
                </Text>
                <Text style={{ ...TextStyle.small, textColor: Colors.white }}>
                  • Feature 1
                </Text>
                <Text style={{ ...TextStyle.small, textColor: Colors.white }}>
                  • Feature 2
                </Text>
              </View>
            }
            maxWidth={220}
          >
            <IconButton
              icon={{ type: "gump", id: 0x15a4 }}
              size={{ width: 44, height: 44 }}
            />
          </Tooltip>
        </ComponentShowcase>

        {/* Text Element Tooltip */}
        <ComponentShowcase
          name="Text Element"
          description="Tooltip wrapping text"
          code="<Tooltip><Text>...</Text></Tooltip>"
        >
          <Tooltip content="This text has a tooltip!">
            <Text style={{ ...TextStyle.medium, textColor: Colors.link }}>
              Hover over me
            </Text>
          </Tooltip>
        </ComponentShowcase>

        {/* Fast Tooltip */}
        <ComponentShowcase
          name="Fast Show"
          description="Tooltip with reduced delay"
          code="delay={100}"
        >
          <Tooltip 
            content="Fast appearing tooltip" 
            delay={100}
          >
            <IconButton
              icon={{ type: "gump", id: 0x15a4 }}
              size={{ width: 44, height: 44 }}
            />
          </Tooltip>
        </ComponentShowcase>

        {/* No Delay Tooltip */}
        <ComponentShowcase
          name="Instant"
          description="Tooltip with no delay"
          code="delay={0}"
        >
          <Tooltip 
            content="Instant tooltip" 
            delay={0}
          >
            <IconButton
              icon={{ type: "gump", id: 0x15a4 }}
              size={{ width: 44, height: 44 }}
            />
          </Tooltip>
        </ComponentShowcase>
      </View>
    </StorybookSection>
  );
};