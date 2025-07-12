import React from "react";
import { StorybookSection } from "./StorybookSection";
import { IconButtonStory, TooltipStory } from "./stories";
import {
  ChildAlign,
  Colors,
  Float,
  Sizing,
  TextStyle,
  LayoutDirection,
} from "~/ui";
import { Text, View } from "~/react";

export interface StorybookScreenProps {
  onBack?: () => void;
}

type StorybookSectionType = {
  title: string;
  description?: string;
  Stories: React.FC<{}>;
};

const sections = [
  {
    title: "Display" as const,
    description: "Basic display components",
    Stories: () => (
      <>
        <TooltipStory />
      </>
    ),
  },
  {
    title: "Action" as const,
    description: "Basic action components",
    Stories: () => (
      <>
        <IconButtonStory />
      </>
    ),
  },
  {
    title: "Input" as const,
    description: "Basic input components",
    Stories: () => <></>,
  },
  {
    title: "Game" as const,
    description: "Basic game components",
    Stories: () => <></>,
  },
] satisfies StorybookSectionType[];

type StorybookSectionTitle = (typeof sections)[number]["title"];

/**
 * StorybookScreen - Main screen for showcasing all design system components
 *
 * Provides a comprehensive view of all available design system components
 * with interactive examples and documentation.
 */
export const StorybookScreen: React.FC<StorybookScreenProps> = ({ onBack }) => {
  const [selectedSection, setSelectedSection] =
    React.useState<StorybookSectionTitle>("Display");

  const section = sections.find((section) => section.title === selectedSection);

  if (!section) {
    console.error(`StorybookScreen: Section ${selectedSection} not found`);
    return null;
  }
  const Stories = section.Stories;

  return (
    <View
      backgroundColor={Colors.transparent}
      layout={{
        childAlignment: ChildAlign.topLeft,
        layoutDirection: LayoutDirection.TopToBottom,
        sizing: { width: Sizing.grow(), height: Sizing.fit() },
      }}
    >
      <Text style={TextStyle.title}>ClassicUO Design System</Text>
      <View
        backgroundColor={Colors.transparent}
        layout={{
          childAlignment: ChildAlign.topLeft,
          layoutDirection: LayoutDirection.LeftToRight,
          sizing: { width: Sizing.grow(), height: Sizing.fit() },
        }}
      >
        <Text style={TextStyle.large}>Categories:</Text>
        {sections.map((section) => (
          <Text
            onClick={() => {
              console.log("clicked", selectedSection, section.title);
              return setSelectedSection(section.title);
            }}
            key={section.title}
            style={{
              ...TextStyle.large,
              textColor:
                selectedSection === section.title
                  ? Colors.gold
                  : Colors.lightGray,
            }}
          >
            {section.title}
          </Text>
        ))}
      </View>

      <StorybookSection
        title={section.title}
        description={section.description}
        spacing={20}
      >
        <Stories />
      </StorybookSection>
    </View>
  );
};