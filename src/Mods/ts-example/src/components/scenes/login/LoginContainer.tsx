import React from "react";
import { View, Gump } from "~/react";
import { Colors } from "~/ui";
import {
  ClayFloatingAttachToElement,
  ClayFloatingClipToElement,
  ClayLayoutAlignment,
  ClayLayoutDirection,
  ClaySizingType,
} from "~/types";

export const LoginContainer: React.FC<{ children: React.ReactNode }> = ({
  children,
}) => {
  // Note: In the original C# code, client version determines gump IDs
  // For now, we'll use the newer client gump IDs (>= CV_706400)
  const isNewClient = true; // This should be determined from client version

  const backgroundGumpId = isNewClient ? 0x014e : 0x2329;
  const uoFlagGumpId = isNewClient ? 0x0151 : 0x15a0;

  return (
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
            type: ClaySizingType.Grow,
            size: {},
          },
        },
        childAlignment: {
          x: ClayLayoutAlignment.Center,
          y: ClayLayoutAlignment.Center,
        },
      }}
    >
      {/* Main background gump */}
      <Gump
        gumpId={backgroundGumpId}
        hue={{ x: 0, y: 0, z: 1 }}
        size={{ width: 640, height: 480 }}
        floating={{
          offset: { x: 0, y: 0 },
          attachTo: ClayFloatingAttachToElement.Parent,
          clipTo: ClayFloatingClipToElement.AttachedParent,
        }}
      />

      {/* UO Flag */}
      <Gump
        gumpId={uoFlagGumpId}
        hue={{ x: 0, y: 0, z: 1 }}
        floating={{
          offset: { x: 0, y: 0 },
          attachTo: ClayFloatingAttachToElement.Parent,
          clipTo: ClayFloatingClipToElement.AttachedParent,
        }}
      />

      {/* For older clients, there would be additional gumps like: */}
      {/* - Tile gump (0x0150) at position 0,0 */}
      {/* - ResizePic (0x13BE) at position 136,95 size 408,342 */}

      {children}
    </View>
  );
};
