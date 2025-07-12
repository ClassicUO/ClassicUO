import React from "react";
import { Button, Art, Gump, Hue } from "~/react";
import {
  FloatingAttachToElement,
  FloatingClipToElement,
  type ClayFloatingElementConfig,
  type ClaySize,
  type ClayColor,
  Vector3,
} from "~/host";
import { CommonLayout } from "~/ui";

export interface IconButtonProps {
  /**
   * Icon to display - can be either a Gump ID or Art ID
   */
  icon: {
    type: "gump" | "art";
    id: number;
  };

  /**
   * Size of the button
   */
  size: ClaySize;

  /**
   * Position of the button
   */
  floating?: ClayFloatingElementConfig;

  /**
   * Color hue for the button
   */
  color?: Partial<Hue>;

  /**
   * Click handler
   */
  onClick?: () => void;

  /**
   * Whether the button can be moved
   */
  movable?: boolean;

  /**
   * Disabled state
   */
  disabled?: boolean;

  /**
   * Icon offset from center
   */
  iconOffset?: { x: number; y: number };
}

/**
 * IconButton - A button with an icon overlay
 *
 * Combines a UO button gump with an icon (either gump or art) centered on top.
 * Supports all standard button interactions and customization.
 *
 * @example
 * ```tsx
 * // Basic icon button with gump icon
 * <IconButton
 *   icon={{ type: "gump", id: 0x15a4 }}
 *   buttonGumpIds={{ normal: 0x05ca, pressed: 0x05c9, over: 0x05c8 }}
 *   size={{ width: 44, height: 44 }}
 *   onClick={() => console.log("Clicked!")}
 * />
 *
 * // Icon button with art icon and custom positioning
 * <IconButton
 *   icon={{ type: "art", id: 0x1234 }}
 *   buttonGumpIds={{ normal: 0x05ca, pressed: 0x05c9, over: 0x05c8 }}
 *   size={{ width: 44, height: 44 }}
 *   iconOffset={{ x: 2, y: -1 }}
 *   floating={{ offset: { x: 100, y: 100 } }}
 *   onClick={() => console.log("Art icon clicked!")}
 * />
 * ```
 */
export const IconButton: React.FC<IconButtonProps> = ({
  icon,
  size,
  floating,
  color: hue,
  onClick,
  movable = false,
  disabled = false,
  iconOffset = { x: 0, y: 0 },
}) => {
  const handleClick = React.useCallback(() => {
    if (!disabled && onClick) {
      onClick();
    }
  }, [disabled, onClick]);

  // Calculate icon positioning - center by default with optional offset
  const iconFloating: ClayFloatingElementConfig = {
    ...floating,
    offset: {
      x: size.width / 2 - size.width / 2 / 2 + iconOffset.x,
      y: size.height / 2 - size.height / 2 / 2 + iconOffset.y,
    },
    attachTo: floating?.attachTo ?? FloatingAttachToElement.Parent,
    clipTo: floating?.clipTo ?? FloatingClipToElement.AttachedParent,
  };

  const Tag = icon.type === "gump" ? Gump : Art;

  return (
    <Tag
      id={icon.id}
      movable={movable}
      acceptInputs={!disabled}
      onClick={handleClick}
      size={size}
      floating={iconFloating}
      hue={disabled ? { ...hue, alpha: 0.5 } : hue}
    />
  );
};
