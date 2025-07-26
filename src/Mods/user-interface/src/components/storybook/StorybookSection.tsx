import React from 'react';
import { Gump, Text, View } from '~/react';
import { ChildAlign, Colors, Float, LayoutDirection, Sizing, TextStyle } from '~/ui';

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
export const StorybookSection: React.FC<StorybookSectionProps> = ({ title, description, children, spacing = 20 }) => (
  <View
    backgroundColor={Colors.transparent}
    padding={{ left: spacing }}
    layout={{
      layoutDirection: LayoutDirection.TopToBottom,
      sizing: { width: Sizing.grow(), height: Sizing.fit() },
      childAlignment: ChildAlign.topLeft,
    }}
  >
    {/* Section background */}
    <Gump id={0x0bb8} ninePatch={true} size={{ width: 720, height: 40 }} hue={{ alpha: 0.8 }}>
      {/* Section title */}
      <Text style={TextStyle.medium}>{title}</Text>

      {/* Section description */}
      {description && <Text style={TextStyle.small}>{description}</Text>}
    </Gump>

    {/* Content area with proper spacing */}
    {children}
  </View>
);
