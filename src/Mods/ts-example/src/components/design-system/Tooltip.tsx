import React from "react";
import { View, Gump, Text } from "~/react";
import {
  FloatingAttachToElement,
  FloatingClipToElement,
  type ClayFloatingElementConfig,
  type ClaySize,
  type ClayColor,
  LayoutDirection,
  LayoutAlignment,
} from "~/host";
import { Colors, TextStyle, Sizing } from "~/ui";

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
  position?: "top" | "bottom" | "left" | "right";

  /**
   * Background color of the tooltip
   */
  backgroundColor?: ClayColor;

  /**
   * Maximum width of the tooltip
   */
  maxWidth?: number;

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
 *
 * @example
 * ```tsx
 * // Basic tooltip wrapping a button
 * <Tooltip content="This button does something important">
 *   <IconButton
 *     icon={{ type: "gump", id: 0x15a4 }}
 *     size={{ width: 44, height: 44 }}
 *   />
 * </Tooltip>
 *
 * // Tooltip with custom positioning
 * <Tooltip
 *   content="This tooltip appears below the button"
 *   position="bottom"
 *   maxWidth={200}
 * >
 *   <IconButton
 *     icon={{ type: "gump", id: 0x15a4 }}
 *     size={{ width: 44, height: 44 }}
 *   />
 * </Tooltip>
 *
 * // Tooltip with rich content
 * <Tooltip
 *   content={
 *     <View>
 *       <Text style={TextStyle.medium}>Rich Tooltip</Text>
 *       <Text style={TextStyle.small}>With multiple elements</Text>
 *     </View>
 *   }
 *   backgroundGumpId={0x13be}
 * >
 *   <Text>Hover over me</Text>
 * </Tooltip>
 * ```
 */
export const Tooltip: React.FC<TooltipProps> = ({
  content,
  children,
  position = "top",
  backgroundColor = Colors.black,
  maxWidth = 300,
  padding = 8,
  showArrow = false,
  backgroundGumpId = 0x0bb8, // Standard UO tooltip background
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
      setIsVisible(true);
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
  const tooltipSize: ClaySize = {
    width: maxWidth,
    height: 60, // Default height, will adjust based on content
  };

  // Calculate offset based on position
  const getPositionOffset = (): { x: number; y: number } => {
    const arrowOffset = showArrow ? 8 : 4;

    switch (position) {
      case "top":
        return {
          x: -tooltipSize.width / 2,
          y: -tooltipSize.height - arrowOffset,
        };
      case "bottom":
        return {
          x: -tooltipSize.width / 2,
          y: arrowOffset,
        };
      case "left":
        return {
          x: -tooltipSize.width - arrowOffset,
          y: -tooltipSize.height / 2,
        };
      case "right":
        return {
          x: arrowOffset,
          y: -tooltipSize.height / 2,
        };
      default:
        return {
          x: -tooltipSize.width / 2,
          y: -tooltipSize.height - arrowOffset,
        };
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
          backgroundColor={Colors.transparent}
          layout={{
            layoutDirection: LayoutDirection.TopToBottom,
            sizing: {
              width: Sizing.fixed(tooltipSize.width),
              height: Sizing.fit(),
            },
            childAlignment: {
              x: LayoutAlignment.Center,
              y: LayoutAlignment.Top,
            },
          }}
          floating={tooltipFloating}
          padding={{
            left: padding,
            right: padding,
            top: padding,
            bottom: padding,
          }}
        >
          {/* Tooltip background */}
          <Gump
            id={backgroundGumpId}
            ninePatch={ninePatch}
            size={tooltipSize}
            hue={{ alpha: 0.9 }}
          >
            {/* Tooltip content */}
            <View
              backgroundColor={Colors.transparent}
              layout={{
                layoutDirection: LayoutDirection.TopToBottom,
                sizing: {
                  width: Sizing.fixed(tooltipSize.width - padding * 2),
                  height: Sizing.fit(),
                },
                childAlignment: {
                  x: LayoutAlignment.Center,
                  y: LayoutAlignment.Top,
                },
              }}
              floating={{
                offset: { x: padding, y: padding },
                attachTo: FloatingAttachToElement.Parent,
                clipTo: FloatingClipToElement.AttachedParent,
              }}
            >
              {typeof content === "string" ? (
                <Text
                  style={{
                    ...TextStyle.small,
                    textColor: Colors.white,
                    fontSize: 12,
                  }}
                >
                  {content}
                </Text>
              ) : (
                content
              )}
            </View>
          </Gump>

          {/* Optional arrow indicator */}
          {showArrow && (
            <Gump
              id={0x15a4} // Small arrow or indicator gump
              size={{ width: 8, height: 8 }}
              floating={{
                offset:
                  position === "top"
                    ? { x: tooltipSize.width / 2 - 4, y: tooltipSize.height }
                    : position === "bottom"
                    ? { x: tooltipSize.width / 2 - 4, y: -8 }
                    : position === "left"
                    ? { x: tooltipSize.width, y: tooltipSize.height / 2 - 4 }
                    : { x: -8, y: tooltipSize.height / 2 - 4 },
                attachTo: FloatingAttachToElement.Parent,
                clipTo: FloatingClipToElement.AttachedParent,
              }}
            />
          )}
        </View>
      )}
    </View>
  );
};
