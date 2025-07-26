import React from 'react';
import { IconButton } from '~/components/design-system';
import { View } from '~/react';
import { ChildAlign, Colors, Float, LayoutAlignment, LayoutDirection, Sizing } from '~/ui';
import { ComponentShowcase } from '../ComponentShowcase';
import { StorybookSection } from '../StorybookSection';

/**
 * IconButtonStory - Comprehensive showcase of IconButton component
 *
 * Demonstrates various configurations and use cases of the IconButton component
 * including different icons, sizes, states, and interactions.
 */
export const IconButtonStory: React.FC = () => {
  const [clickCount, setClickCount] = React.useState(0);

  const handleClick = React.useCallback(() => {
    console.log('IconButtonStory: handleClick');
    setClickCount((prev) => prev + 1);
  }, []);

  return (
    <StorybookSection
      title="IconButton"
      description="Button with icon overlay supporting both gump and art icons"
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
        {/* Basic IconButton with Gump */}
        <ComponentShowcase
          name="Basic Gump Icon"
          description="Standard button with gump icon"
          code="icon={{ type: 'gump', id: 0x15a4 }}"
        >
          <IconButton icon={{ type: 'gump', id: 0x15a4 }} size={{ width: 44, height: 44 }} onClick={handleClick} />
        </ComponentShowcase>

        {/* IconButton with Art */}
        <ComponentShowcase
          name="Art Icon"
          description="Button with art sprite icon"
          code="icon={{ type: 'art', id: 0x1234 }}"
        >
          <IconButton icon={{ type: 'art', id: 0x15a4 }} size={{ width: 44, height: 44 }} onClick={handleClick} />
        </ComponentShowcase>

        {/* Large IconButton */}
        <ComponentShowcase
          name="Large Size"
          description="Larger button with scaled icon"
          code="size={{ width: 66, height: 66 }}"
        >
          <IconButton icon={{ type: 'gump', id: 0x15a4 }} size={{ width: 66, height: 66 }} onClick={handleClick} />
        </ComponentShowcase>

        {/* Disabled IconButton */}
        <ComponentShowcase name="Disabled State" description="Button in disabled state" code="disabled={true}">
          <IconButton
            icon={{ type: 'gump', id: 0x15a4 }}
            size={{ width: 44, height: 44 }}
            disabled={true}
            onClick={handleClick}
          />
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
        {/* IconButton with offset */}
        <ComponentShowcase
          name="Custom Offset"
          description="Icon positioned with custom offset"
          code="iconOffset={{ x: 50, y: 50 }}"
        >
          <IconButton
            icon={{ type: 'gump', id: 0x15a4 }}
            size={{ width: 44, height: 44 }}
            iconOffset={{ x: 50, y: 50 }}
            onClick={handleClick}
          />
        </ComponentShowcase>

        {/* IconButton with custom hue */}
        <ComponentShowcase name="Custom Hue" description="Button with custom color hue" code="color={{ hue: 0x44 }}">
          <IconButton
            icon={{ type: 'gump', id: 0x15a4 }}
            size={{ width: 44, height: 44 }}
            color={{ hue: 0x44 }}
            onClick={handleClick}
          />
        </ComponentShowcase>

        {/* Interactive counter */}
        <ComponentShowcase name="Interactive" description={`Clicked ${clickCount} times`} code="onClick={handleClick}">
          <IconButton icon={{ type: 'gump', id: 0x15a4 }} size={{ width: 44, height: 44 }} onClick={handleClick} />
        </ComponentShowcase>

        {/* Small IconButton */}
        <ComponentShowcase
          name="Small Size"
          description="Compact button for tight spaces"
          code="size={{ width: 32, height: 32 }}"
        >
          <IconButton icon={{ type: 'gump', id: 0x15a4 }} size={{ width: 32, height: 32 }} onClick={handleClick} />
        </ComponentShowcase>
      </View>
    </StorybookSection>
  );
};
