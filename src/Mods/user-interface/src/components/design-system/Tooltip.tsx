import React from 'react';
import {
  FloatingAttachToElement,
  FloatingClipToElement,
  LayoutAlignment,
  LayoutDirection,
  type ClayColor,
  type ClayFloatingElementConfig,
  type ClaySize,
} from '~/host';
import { Gump, Text, View } from '~/react';
import { Colors, Sizing, TextStyle } from '~/ui';

export interface TooltipProps {
  /**
   * The content to show in the tooltip
   */
  content: React.ReactNode;

  /**
   * The child component to wrap with tooltip functionality
   */
  children: React.ReactNode;

  /**
   * Position relative to the target element
   */
  position?: 'top' | 'bottom' | 'left' | 'right';

  /**
   * Background color of the tooltip
   */
  backgroundColor?: ClayColor;

  /**
   * Width of the tooltip
   */
  width?: number;

  /**
   * Height of the tooltip
   */
  height?: number;

  /**
   * Padding inside the tooltip
   */
  padding?: number;

  /**
   * Whether to show an arrow pointing to the target
   */
  showArrow?: boolean;

  /**
   * Custom gump ID for the tooltip background (defaults to standard UO tooltip style)
   */
  backgroundGumpId?: number;

  /**
   * Whether to use nine-patch rendering for the background
   */
  ninePatch?: boolean;

  /**
   * Delay before showing tooltip in milliseconds
   */
  delay?: number;
}

/**
 * Tooltip - Hover information display component
 *
 * Wraps a child component and displays contextual information in a floating container
 * when the user hovers over the child. Follows UO's traditional tooltip styling with
 * support for custom positioning and styling.
 */
export const Tooltip: React.FC<TooltipProps> = ({
  content,
  children,
  position = 'top',
  backgroundColor = Colors.transparent,
  width: width = 300,
  height: height = 60,
  padding = 8,
  backgroundGumpId = 0x0bb8,
  ninePatch = true,
  delay = 500,
}) => {
  const [isVisible, setIsVisible] = React.useState(false);
  const timeoutId = React.useRef<number | null>(null);

  const handleMouseEnter = React.useCallback(() => {
    // Clear any existing timeout
    if (timeoutId.current) {
      clearTimeout(timeoutId.current);
    }

    // Set a timeout to show the tooltip after the delay
    const id = setTimeout(() => {
      setIsVisible(() => true);
    }, delay);

    timeoutId.current = id;
  }, [delay, timeoutId]);

  const handleMouseLeave = React.useCallback(() => {
    // Clear the timeout and hide the tooltip immediately
    if (timeoutId.current) {
      clearTimeout(timeoutId.current);
      timeoutId.current = null;
    }
    setIsVisible(false);
  }, []);

  // Cleanup timeout on unmount
  React.useEffect(() => {
    return () => {
      if (timeoutId.current) {
        clearTimeout(timeoutId.current);
      }
    };
  }, []);

  // Calculate tooltip size - we'll use a reasonable default since we can't measure ReactNode
  const tooltipSize: ClaySize = { width: width, height: height };

  // Calculate offset based on position
  const getPositionOffset = (): { x: number; y: number } => {
    switch (position) {
      case 'top':
        return { x: -tooltipSize.width / 2, y: -tooltipSize.height };
      case 'bottom':
        return { x: -tooltipSize.width / 2, y: 0 };
      case 'left':
        return { x: -tooltipSize.width, y: -tooltipSize.height / 2 };
      case 'right':
        return { x: 0, y: -tooltipSize.height / 2 };
      default:
        return { x: -tooltipSize.width / 2, y: -tooltipSize.height };
    }
  };

  const tooltipFloating: ClayFloatingElementConfig = {
    offset: getPositionOffset(),
    attachTo: FloatingAttachToElement.Parent,
    clipTo: FloatingClipToElement.AttachedParent,
  };

  return (
    <View
      backgroundColor={Colors.transparent}
      layout={{
        layoutDirection: LayoutDirection.TopToBottom,
        sizing: {
          width: Sizing.fit(),
          height: Sizing.fit(),
        },
        childAlignment: {
          x: LayoutAlignment.Center,
          y: LayoutAlignment.Center,
        },
      }}
      onMouseEnter={handleMouseEnter}
      onMouseLeave={handleMouseLeave}
    >
      {/* Child component */}
      {children}

      {/* Tooltip overlay */}
      {isVisible && (
        <View
          backgroundColor={backgroundColor}
          layout={{
            layoutDirection: LayoutDirection.TopToBottom,
            sizing: { width: Sizing.fixed(tooltipSize.width), height: Sizing.fit() },
            childAlignment: { x: LayoutAlignment.Center, y: LayoutAlignment.Top },
          }}
          floating={tooltipFloating}
          padding={{ left: padding, right: padding, top: padding, bottom: padding }}
        >
          {/* Tooltip background */}
          <Gump id={backgroundGumpId} ninePatch={ninePatch} size={tooltipSize} hue={{ alpha: 0.9 }}>
            {/* Tooltip content */}
            <View
              backgroundColor={backgroundColor}
              layout={{
                layoutDirection: LayoutDirection.TopToBottom,
                sizing: { width: Sizing.fixed(tooltipSize.width - padding * 2), height: Sizing.fit() },
                childAlignment: { x: LayoutAlignment.Center, y: LayoutAlignment.Top },
              }}
              floating={{
                offset: { x: padding, y: padding },
                attachTo: FloatingAttachToElement.Parent,
                clipTo: FloatingClipToElement.AttachedParent,
              }}
            >
              {typeof content === 'string' ? (
                <Text style={{ ...TextStyle.small, textColor: Colors.white, fontSize: 12 }}>{content}</Text>
              ) : (
                content
              )}
            </View>
          </Gump>
        </View>
      )}
    </View>
  );
};
