import React from 'react';
import { Gump, View } from '~/react';
import { ChildAlign, Colors, Float, LayoutDirection, Sizing } from '~/ui';

export const LoginContainer: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  // Note: In the original C# code, client version determines gump IDs
  // For now, we'll use the newer client gump IDs (>= CV_706400)
  const isNewClient = true; // This should be determined from client version

  const backgroundGumpId = isNewClient ? 0x014e : 0x2329;
  const uoFlagGumpId = isNewClient ? 0x0151 : 0x15a0;

  return (
    <View
      backgroundColor={Colors.transparent}
      layout={{
        layoutDirection: LayoutDirection.TopToBottom,
        sizing: {
          width: Sizing.grow(),
          height: Sizing.grow(),
        },
        childAlignment: ChildAlign.center,
      }}
    >
      {/* Main background gump */}
      <Gump id={backgroundGumpId} size={{ width: 640, height: 480 }} floating={Float.offsetParent(0, 0)} />

      {/* UO Flag */}
      <Gump id={uoFlagGumpId} floating={Float.offsetParent(0, 0)} />

      {/* For older clients, there would be additional gumps like: */}
      {/* - Tile gump (0x0150) at position 0,0 */}
      {/* - ResizePic (0x13BE) at position 136,95 size 408,342 */}

      {children}
    </View>
  );
};
