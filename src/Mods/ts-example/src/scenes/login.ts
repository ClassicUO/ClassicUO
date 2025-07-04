import { createClayColor, createClaySizingAxis } from "../ui/utils";
import { HostWrapper } from "../host/wrapper";
import {
  ClayLayoutDirection,
  ClayLayoutAlignment,
  ClaySizingType,
  UINode,
  ClayLayoutAlignment,
  ClayWidgetType,
  UINodes,
} from "../types";
import { GumpBuilder } from "../ui/gumpBuilder";

export function createLoginScreenMenu(
  addCallback: (id: number, callback: () => void) => void
): void {
  const root: UINode = {
    id: HostWrapper.spawnEcsEntity(),
    config: {
      backgroundColor: createClayColor(0.2, 0.2, 0.2, 1),
      layout: {
        sizing: {
          width: createClaySizingAxis(ClaySizingType.Grow),
          height: createClaySizingAxis(ClaySizingType.Grow),
        },
        childAlignment: {
          x: ClayLayoutAlignment.Center,
          y: ClayLayoutAlignment.Center,
        },
        layoutDirection: ClayLayoutDirection.TopToBottom,
      },
    },
  };

  const mainMenu: UINode = {
    id: HostWrapper.spawnEcsEntity(),
    config: {
      backgroundColor: createClayColor(0.2, 0.2, 0.2, 1),
      layout: {
        sizing: {
          width: createClaySizingAxis(ClaySizingType.Fit, { min: 0, max: 0 }),
          height: createClaySizingAxis(ClaySizingType.Fit, { min: 0, max: 0 }),
        },
        layoutDirection: ClayLayoutDirection.TopToBottom,
      },
    },
  };

  const builder = new GumpBuilder();
  const unitZ = { x: 0, y: 0, z: 1 };

  const background = builder.addGump(0x014e, unitZ);
  const quitButton = builder.addButton([0x05ca, 0x05c9, 0x05c8], unitZ, {
    x: 25,
    y: 240,
  });
  const creditButton = builder.addButton([0x05d0, 0x05cf, 0x5ce], unitZ, {
    x: 530,
    y: 125,
  });
  const arrowButton = builder.addButton([0x5cd, 0x5cc, 0x5cb], unitZ, {
    x: 280,
    y: 365,
  });

  const usernameBackground = builder.addGumpNinePatch(
    0x0bb8,
    unitZ,
    { x: 218, y: 283 },
    { x: 210, y: 30 }
  );
  usernameBackground.textConfig = {
    value: "your username",
    textConfig: {
      fontId: 0,
      fontSize: 24,
      textColor: createClayColor(0.2, 0.2, 0.2, 1),
    },
  };
  usernameBackground.acceptInputs = true;
  usernameBackground.widgetType = ClayWidgetType.TextInput;

  const passwordBackground = builder.addGumpNinePatch(
    0x0bb8,
    unitZ,
    { x: 218, y: 283 + 50 },
    { x: 210, y: 30 }
  );
  passwordBackground.textConfig = {
    value: "your password",
    replacedChar: "*",
    textConfig: {
      fontId: 0,
      fontSize: 24,
      textColor: createClayColor(1, 1, 1, 1),
    },
  };
  passwordBackground.acceptInputs = true;
  passwordBackground.widgetType = ClayWidgetType.TextInput;

  const relations: Record<number, number> = {
    [mainMenu.id]: root.id,
    [background.id]: mainMenu.id,
    [quitButton.id]: mainMenu.id,
    [creditButton.id]: mainMenu.id,
    [arrowButton.id]: mainMenu.id,
    [usernameBackground.id]: mainMenu.id,
    [passwordBackground.id]: mainMenu.id,
  };

  const nodes: UINodes = {
    nodes: [
      root,
      mainMenu,
      background,
      quitButton,
      creditButton,
      arrowButton,
      usernameBackground,
      passwordBackground,
    ],
    relations: relations,
  };

  addCallback(quitButton.id, () => {
    console.log(`[${quitButton.id}] Quit button clicked`);
  });

  addCallback(creditButton.id, () => {
    console.log(`[${creditButton.id}] Credit button clicked`);
  });

  addCallback(arrowButton.id, () => {
    console.log(`[${arrowButton.id}] Login button clicked`);
  });

  HostWrapper.createUINodes(nodes);
}
