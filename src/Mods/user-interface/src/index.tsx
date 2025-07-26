import React from 'react';
import { LoginScreen, StorybookScreen } from './components';
import {
  AssetType,
  CompressionType,
  EventType,
  HostMessages,
  Keys,
  TermOp,
  TimeProxy,
  UIEvent,
  UIInteractionState,
  UIMouseEvent,
} from './host';
import { HostWrapper, Zlib } from './host/wrapper';
import { polyfill } from './polyfill';
import { ClayReactRenderer } from './react';
import { State } from './state';
import { base64Encode } from './ui/theme';

// Main plugin functions
export function on_init(): I32 {
  console.log('plugin initialized');
  const { timeoutManager } = polyfill();
  State.processTimeouts = timeoutManager.processTimeouts;

  // HostWrapper.sendPacketToServer(new Uint8Array([0x73, 0xff]));

  return 1;
}

export function on_update(): I32 {
  try {
    const json = HostWrapper.getInputString();
    const time: TimeProxy = JSON.parse(json);

    State.tick = time.total;

    State.processTimeouts(State.tick);
  } catch (e) {
    console.error('error during on_update', e);
  }

  return 1;
}

export function on_event(): I32 {
  const json = HostWrapper.getInputString();
  const evList: HostMessages = JSON.parse(json);

  // console.log("on_event", evList);

  for (const ev of evList.messages) {
    switch (ev.$type) {
      case 'KeyPressed':
        break;

      case 'KeyReleased':
        switch (ev.key) {
          case Keys.A: {
            // This would need the actual image data in a real implementation
            const imageData = new Uint8Array(0); // Placeholder
            const compressedData = Zlib.compress(imageData);
            console.log(`compressed: ${compressedData.length}`);
            HostWrapper.setSprite({
              assetType: AssetType.Gump,
              idx: 0x014e,
              width: 497,
              height: 376,
              base64Data: base64Encode(compressedData),
              compression: CompressionType.Zlib,
            });
            break;
          }

          case Keys.S: {
            const descOut = HostWrapper.getSprite({
              assetType: AssetType.Gump,
              idx: 0x014e,
            });
            break;
          }

          case Keys.D: {
            const playerSerial = HostWrapper.getPlayerSerial();
            const graphic = HostWrapper.getEntityGraphic(playerSerial);
            console.log(`found graphic: ${graphic.value.toString(16).toUpperCase()}`);
            break;
          }

          case Keys.F: {
            const playerSerial = HostWrapper.getPlayerSerial();
            HostWrapper.setEntityGraphic(playerSerial, { value: 0x12 });
            break;
          }

          case Keys.G:
            break;

          case Keys.R:
            console.log('Keys.R => creating renderer');
            State.renderer?.unmount();
            State.renderer = new ClayReactRenderer();

            State.renderer.render(
              <LoginScreen
                onQuit={() => {
                  console.log('React: Quit button clicked');
                }}
                onCredits={() => {
                  console.log('React: Credits button clicked');
                }}
                onLogin={() => {
                  console.log('React: Login button clicked');
                }}
              />,
            );
            break;

          case Keys.T:
            console.log('Keys.T => creating renderer');
            State.renderer?.unmount();
            State.renderer = new ClayReactRenderer();

            State.renderer.render(<StorybookScreen />);
            break;

          case Keys.H:
            const response = HostWrapper.query({
              terms: [{ ids: 1, op: TermOp.Optional }],
            });
            for (const result of response.results) {
              // Process results
            }
            break;
        }
        break;
    }
  }

  return 1;
}

export function on_ui_event(): I32 {
  const json = HostWrapper.getInputString();
  const ev: UIEvent = JSON.parse(json);

  State.renderer?.handleUIEvent(ev);
  return 1;
}

export function Handler_0x73(): I32 {
  console.log('warn', '0x73 handler');
  return 1;
}
