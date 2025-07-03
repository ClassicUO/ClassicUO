import {
  Keys,
  AssetType,
  CompressionType,
  TermOp,
  UIInteractionState,
  TimeProxy,
  HostMessages,
  UIMouseEvent,
} from "./types";
import { HostWrapper, Zlib } from "./host/hostWrapper";
import { base64Encode } from "./ui/utils";
import { createLoginScreenMenu } from "./scenes/login";

class State {
  public static time = 0;
  public static uiCallbacks: Record<number, () => void> = {};
}

// Main plugin functions
export function on_init(): I32 {
  console.log("plugin initialized");

  // Send packet to server
  // HostWrapper.sendPacketToServer(new Uint8Array([0x73, 0xff]));

  return 1;
}

export function on_update(): I32 {
  const json = HostWrapper.getInputString();
  const time: TimeProxy = JSON.parse(json);

  State.time = time.total;

  return 1;
}

export function on_event(): I32 {
  const json = HostWrapper.getInputString();
  const evList: HostMessages = JSON.parse(json);

  console.log("on_event", evList);

  for (const ev of evList.messages) {
    switch (ev.$type) {
      case "KeyPressed":
        break;

      case "KeyReleased":
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
            console.log(
              `found graphic: ${graphic.value.toString(16).toUpperCase()}`
            );
            break;
          }

          case Keys.F: {
            const playerSerial = HostWrapper.getPlayerSerial();
            HostWrapper.setEntityGraphic(playerSerial, { value: 0x12 });
            break;
          }

          case Keys.G:
            createLoginScreenMenu((id, callback) => {
              State.uiCallbacks[id] = callback;
            });
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

export function on_ui_mouse_event(): I32 {
  const json = HostWrapper.getInputString();
  const ev: UIMouseEvent = JSON.parse(json);

  console.log("on_ui_mouse_event callbacks", Object.keys(State.uiCallbacks));
  if (ev.state === UIInteractionState.Released && State.uiCallbacks[ev.id]) {
    State.uiCallbacks[ev.id]();
  }

  return 1;
}

export function on_ui_keyboard_event(): I32 {
  // Handle keyboard events
  return 1;
}

export function Handler_0x73(): I32 {
  console.log("warn", "0x73 handler");
  return 1;
}
