import {
  UINodeProxy,
  Vector2,
  Vector3,
  AssetType,
  ClayUOCommandType,
  ClayWidgetType,
  ClaySizingType,
  ClayFloatingAttachToElement,
  ClayFloatingClipToElement,
} from "../types";
import { HostWrapper } from "../host/hostWrapper";
import { createClaySizingAxis, createClayColor } from "./utils";

// Gump builder class
export class GumpBuilder {
  addLabel(text: string, position?: Vector2, size?: Vector2): UINodeProxy {
    const node: UINodeProxy = {
      id: HostWrapper.spawnEcsEntity(),
      config: {
        layout: {
          sizing: {
            width: createClaySizingAxis(ClaySizingType.Fixed, {
              min: size?.x || 0,
              max: size?.x || 0,
            }),
            height: createClaySizingAxis(ClaySizingType.Fixed, {
              min: size?.y || 0,
              max: size?.y || 0,
            }),
          },
        },
        floating: {
          clipTo: ClayFloatingClipToElement.AttachedParent,
          attachTo: position
            ? ClayFloatingAttachToElement.Parent
            : ClayFloatingAttachToElement.None,
          offset: position || { x: 0, y: 0 },
        },
      },
      textConfig: {
        value: text,
        textConfig: {
          fontId: 0,
          fontSize: 12,
          textColor: createClayColor(1, 1, 1, 1),
        },
      },
    };
    return node;
  }

  addButton(
    ids: [number, number, number],
    hue: Vector3,
    position?: Vector2
  ): UINodeProxy {
    const node = this.addGump(ids[0], hue, position, false, true);
    node.widgetType = ClayWidgetType.Button;
    node.uoButton = { normal: ids[0], pressed: ids[1], over: ids[2] };
    return node;
  }

  addGump(
    id: number,
    hue: Vector3,
    position?: Vector2,
    movable: boolean = false,
    acceptInputs: boolean = false
  ): UINodeProxy {
    const spriteInfo = HostWrapper.getSprite({
      assetType: AssetType.Gump,
      idx: id,
    });

    const node: UINodeProxy = {
      id: HostWrapper.spawnEcsEntity(),
      config: {
        layout: {
          sizing: {
            width: createClaySizingAxis(ClaySizingType.Fixed, {
              min: spriteInfo.width || 0,
              max: spriteInfo.width || 0,
            }),
            height: createClaySizingAxis(ClaySizingType.Fixed, {
              min: spriteInfo.height || 0,
              max: spriteInfo.height || 0,
            }),
          },
        },
        floating: {
          clipTo: ClayFloatingClipToElement.AttachedParent,
          attachTo: position
            ? ClayFloatingAttachToElement.Parent
            : ClayFloatingAttachToElement.None,
          offset: position || { x: 0, y: 0 },
        },
      },
      uoConfig: {
        type: ClayUOCommandType.Gump,
        id: id,
        hue: hue,
      },
      movable: movable,
      acceptInputs: acceptInputs,
    };

    return node;
  }

  addGumpNinePatch(
    id: number,
    hue: Vector3,
    position?: Vector2,
    size?: Vector2
  ): UINodeProxy {
    const spriteInfo = HostWrapper.getSprite({
      assetType: AssetType.Gump,
      idx: id,
    });

    const node: UINodeProxy = {
      id: HostWrapper.spawnEcsEntity(),
      config: {
        layout: {
          sizing: {
            width: createClaySizingAxis(ClaySizingType.Fixed, {
              min: size?.x || spriteInfo.width || 0,
              max: size?.x || spriteInfo.width || 0,
            }),
            height: createClaySizingAxis(ClaySizingType.Fixed, {
              min: size?.y || spriteInfo.height || 0,
              max: size?.y || spriteInfo.height || 0,
            }),
          },
        },
        floating: {
          clipTo: ClayFloatingClipToElement.AttachedParent,
          attachTo: position
            ? ClayFloatingAttachToElement.Parent
            : ClayFloatingAttachToElement.None,
          offset: position || { x: 0, y: 0 },
        },
      },
      uoConfig: {
        type: ClayUOCommandType.GumpNinePatch,
        id: id,
        hue: hue,
      },
    };

    return node;
  }
}
