import { createClayColor, createClaySizingAxis } from "../ui/utils";
import { HostWrapper } from "../host/hostWrapper";
import {
  ClayLayoutDirection,
  ClayLayoutAlignmentY,
  ClaySizingType,
  UINodeProxy,
  ClayLayoutAlignmentX,
  UINodes,
  ClayFloatingClipToElement,
  ClayFloatingAttachToElement,
  ClayTextAlignment,
} from "../types";

export function createMenu(uiCallbacks: Record<number, () => void>): void {
  const rootEnt = HostWrapper.spawnEcsEntity();

  const root: UINodeProxy = {
    id: rootEnt,
    config: {
      backgroundColor: createClayColor(0.6, 0.6, 0.6, 1),
      layout: {
        sizing: {
          width: createClaySizingAxis(ClaySizingType.Fixed, {
            min: 700,
            max: 700,
          }),
          height: createClaySizingAxis(ClaySizingType.Fixed, {
            min: 700,
            max: 700,
          }),
        },
        layoutDirection: ClayLayoutDirection.TopToBottom,
        childAlignment: {
          x: ClayLayoutAlignmentX.Center,
          y: ClayLayoutAlignmentY.Center,
        },
      },
      floating: {
        clipTo: ClayFloatingClipToElement.AttachedParent,
        attachTo: ClayFloatingAttachToElement.Parent,
        offset: { x: 0, y: 0 },
      },
    },
    movable: true,
    acceptInputs: true,
  };

  const childEnt = HostWrapper.spawnEcsEntity();
  const child: UINodeProxy = {
    id: childEnt,
    config: {
      backgroundColor: createClayColor(1, 0, 0, 1),
      layout: {
        sizing: {
          width: createClaySizingAxis(ClaySizingType.Percent, undefined, 0.5),
          height: createClaySizingAxis(ClaySizingType.Percent, undefined, 0.5),
        },
      },
    },
    textConfig: {
      value: "ClassicUO is the best client ever made!",
      textConfig: {
        fontId: 4,
        fontSize: 36,
        textColor: createClayColor(0, 0, 1, 1),
        textAlignment: ClayTextAlignment.Center,
      },
    },
  };

  const childEnt2 = HostWrapper.spawnEcsEntity();
  const child2: UINodeProxy = {
    id: childEnt2,
    config: {
      backgroundColor: createClayColor(0, 1, 0, 1),
      layout: {
        sizing: {
          width: createClaySizingAxis(ClaySizingType.Percent, undefined, 0.5),
          height: createClaySizingAxis(ClaySizingType.Percent, undefined, 0.5),
        },
      },
    },
    textConfig: {
      value: "Hello from plugin!",
      textConfig: {
        fontId: 2,
        fontSize: 36,
        textColor: createClayColor(1, 0, 0, 1),
      },
    },
  };

  const relations: Record<number, number> = {
    [child.id]: root.id,
    [child2.id]: child.id,
  };

  const nodes: UINodes = {
    nodes: [root, child, child2],
    relations: relations,
  };

  HostWrapper.createUINodes(nodes);
}
