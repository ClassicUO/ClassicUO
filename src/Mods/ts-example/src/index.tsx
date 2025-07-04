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
import React from "react";
import { HostWrapper, Zlib } from "./host/wrapper";
import { base64Encode } from "./ui/utils";
import { createLoginScreenMenu } from "./scenes/login";
import { ClayReactRenderer } from "./react";
import { LoginScreen } from "./components";

class State {
  public static tick = 0;
  public static uiCallbacks: Record<number, () => void> = {};
  public static renderer: ClayReactRenderer | null = null;
  public static timeouts: Map<number, { callback: () => void; delay: number }> =
    new Map();
}

Object.assign(globalThis, {
  React,
  setTimeout: (callback: () => void, delay: number) => {
    console.log("setTimeout", callback, delay);
    State.timeouts.set(State.tick + delay, { callback, delay });
    // random integer id
    return Math.floor(Math.random() * 1000000);
  },
  clearTimeout: (timeoutId: number) => {
    console.log("clearTimeout", timeoutId);
    State.timeouts.delete(timeoutId);
  },
});

// Main plugin functions
export function on_init(): I32 {
  console.log("plugin initialized");

  // HostWrapper.sendPacketToServer(new Uint8Array([0x73, 0xff]));

  return 1;
}

export function on_update(): I32 {
  const json = HostWrapper.getInputString();
  const time: TimeProxy = JSON.parse(json);

  State.tick = time.total;

  if (State.timeouts.size > 0) {
    for (const [id, timeout] of State.timeouts) {
      if (State.tick >= timeout.delay) {
        timeout.callback();
        State.timeouts.delete(id);
      }
    }
  }

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

          case Keys.R:
            console.log("Keys.R => creating renderer");
            State.renderer?.unmount();
            State.renderer = new ClayReactRenderer();

            State.renderer.render(
              <LoginScreen
                onQuit={() => {
                  console.log("React: Quit button clicked");
                }}
                onCredits={() => {
                  console.log("React: Credits button clicked");
                }}
                onLogin={() => {
                  console.log("React: Login button clicked");
                }}
              />
            );
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

  if (ev.state === UIInteractionState.Released) {
    State.renderer?.handleUIEvent(ev.id, "click");

    // old render method
    if (State.uiCallbacks[ev.id]) {
      State.uiCallbacks[ev.id]();
    }
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
