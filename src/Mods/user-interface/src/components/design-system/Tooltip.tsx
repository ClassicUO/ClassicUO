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
  art?: number;

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
  width = 300,
  height = 60,
  padding = 8,
  art = 0x0bb8,
  ninePatch = true,
  delay = 500,
}) => {
  const [isVisible, setIsVisible] = React.useState(false);
  const timeoutId = React.useRef<number | null>(null);
  const hideId = React.useRef<number | null>(null);
  // Mirror visibility into a ref so the handlers can read it WITHOUT depending on
  // it. A handler whose identity changes (useCallback dep on isVisible) makes the
  // reconciler re-register the host listener on every show/hide; an enter/leave
  // arriving mid-swap is then dropped, so the tooltip "appears once" and never
  // re-arms. Stable handlers (empty deps) keep one listener for the View's life.
  const visibleRef = React.useRef(false);
  visibleRef.current = isVisible;

  // Hover over overlapping interactive elements (icon + this wrapper) makes the
  // host's hovered target flip, firing rapid enter/leave. Debounce the hide so
  // a re-enter within the grace window cancels it; the show timer survives the
  // jitter and the tooltip actually appears.
  const HIDE_GRACE = 120;

  const handleMouseEnter = React.useCallback(() => {
    if (hideId.current) {
      clearTimeout(hideId.current);
      hideId.current = null;
    }
    if (timeoutId.current || visibleRef.current) return; // show timer already pending / shown
    timeoutId.current = setTimeout(() => {
      timeoutId.current = null;
      setIsVisible(true);
    }, delay);
  }, [delay]);

  const handleMouseLeave = React.useCallback(() => {
    // Defer the WHOLE hide — including cancelling a still-pending show — by the
    // grace window. The host flips its hovered target between the icon and this
    // wrapper mid-hover, firing leave+enter within a frame. Cancelling the
    // pending show timer immediately (as before) is what killed delayed
    // tooltips: a spurious leave during the `delay` wait aborted the show, so
    // only delay:0 (instant) tooltips ever appeared. A re-enter within grace
    // clears this, so the show timer rides through the blip.
    if (hideId.current) clearTimeout(hideId.current);
    hideId.current = setTimeout(() => {
      hideId.current = null;
      if (timeoutId.current) {
        clearTimeout(timeoutId.current);
        timeoutId.current = null;
      }
      setIsVisible(false);
    }, HIDE_GRACE);
  }, []);

  // Cleanup timers on unmount
  React.useEffect(() => {
    return () => {
      if (timeoutId.current) clearTimeout(timeoutId.current);
      if (hideId.current) clearTimeout(hideId.current);
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

      {/* Tooltip overlay: ninePatch gump fits its content (auto height); the
          inner View carries the fixed width so text wraps against it. The text
          must sit in a regular element, not directly in the custom-render gump
          (a gump's children aren't width-constrained for text wrapping). */}
      {isVisible && (
        <Gump
          id={art}
          ninePatch={ninePatch}
          hue={{ alpha: 0.9 }}
          // Make the tooltip itself hoverable. The host's hover is topmost-
          // INTERACTIVE only; a non-interactive tooltip is invisible to it, so
          // moving the cursor onto the tooltip leaves the trigger's subtree and
          // hides it (flicker). With an event prop the Gump becomes the hovered
          // entity — its ancestor chain includes this wrapper, so HoverBoundary
          // fires no leave. onMouseEnter also cancels a stray pending hide.
          // Deliberately NO onMouseLeave here: moving tooltip->icon must not
          // schedule a hide (the wrapper's own leave governs the real exit).
          onMouseEnter={handleMouseEnter}
          floating={tooltipFloating}
          padding={{ left: padding, right: padding, top: padding, bottom: padding }}
          layout={{
            layoutDirection: LayoutDirection.TopToBottom,
            sizing: { width: Sizing.fit(), height: Sizing.fit() },
            childAlignment: { x: LayoutAlignment.Center, y: LayoutAlignment.Top },
          }}
        >
          <View
            backgroundColor={backgroundColor}
            layout={{
              layoutDirection: LayoutDirection.TopToBottom,
              sizing: { width: Sizing.fixed(tooltipSize.width - padding * 2), height: Sizing.fit() },
              childAlignment: { x: LayoutAlignment.Left, y: LayoutAlignment.Top },
            }}
          >
            {typeof content === 'string' ? (
              <Text style={{ ...TextStyle.small, textColor: Colors.white, fontSize: 12 }}>{content}</Text>
            ) : (
              content
            )}
          </View>
        </Gump>
      )}
    </View>
  );
};
