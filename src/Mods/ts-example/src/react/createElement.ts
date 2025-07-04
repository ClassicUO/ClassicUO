import {
  UINode,
  ClayWidgetType,
  ClayUOCommandType,
  ClayElementDecl,
  ClaySizingType,
  ClayLayoutDirection,
  ClayLayoutAlignment,
} from "../types";
import {
  ClayElement,
  ClayElementNames,
  ClayElementPropTypes,
} from "./components";

export function createElement(type: string, props: any, id: number): UINode {
  const data = { type, props } as ClayElementPropTypes;

  switch (data.type) {
    case "view":
      return {
        id,
        config: {
          layout: data.props.layout,
          backgroundColor: data.props.backgroundColor,
          floating: data.props.floating,
          cornerRadius: data.props.cornerRadius,
          border: data.props.border,
          clip: data.props.clip,
        },
        movable: props.movable || false,
        acceptInputs: props.acceptInputs || false,
      };

    case "gump": {
      const size = data.props.size ?? { width: 50, height: 25 };

      return {
        id,
        uoConfig: {
          type: props.ninePatch
            ? ClayUOCommandType.GumpNinePatch
            : ClayUOCommandType.Gump,
          id: data.props.gumpId,
          hue: data.props.hue ?? { x: 0, y: 0, z: 1 },
        },
        config: {
          layout: {
            sizing: {
              width: {
                type: ClaySizingType.Fixed,
                size: {
                  minMax: { min: size.width, max: size.width },
                  percent: size.width,
                },
              },
              height: {
                type: ClaySizingType.Fixed,
                size: {
                  minMax: { min: size.height, max: size.height },
                  percent: size.height,
                },
              },
            },
            layoutDirection:
              data.props.direction ?? ClayLayoutDirection.TopToBottom,
            childAlignment: data.props.childAlignment ?? {
              x: ClayLayoutAlignment.Center,
              y: ClayLayoutAlignment.Center,
            },
          },
          floating: data.props.floating,
        },
      };
    }
    case "button": {
      const size = data.props.size ?? { width: 50, height: 25 };

      return {
        id,
        movable: data.props.movable ?? false,
        acceptInputs: data.props.acceptInputs ?? true,
        uoConfig: {
          type: ClayUOCommandType.Gump,
          id: data.props.gumpIds.normal,
          hue: data.props.hue ?? { x: 0, y: 0, z: 1 },
        },
        uoButton: data.props.gumpIds,
        widgetType: ClayWidgetType.Button,
        config: {
          layout: {
            sizing: {
              width: {
                size: {
                  minMax: { min: size.width, max: size.width },
                  percent: size.width,
                },
                type: ClaySizingType.Fixed,
              },
              height: {
                size: {
                  minMax: { min: size.height, max: size.height },
                  percent: size.height,
                },
                type: ClaySizingType.Fixed,
              },
            },
          },
          floating: data.props.floating,
        },
      };
    }

    case "textinput": {
      const size = data.props.size ?? { width: 50, height: 25 };
      return {
        id,
        textConfig: {
          value: data.props.placeholder || "",
          replacedChar: data.props.password ? "*" : undefined,
          textConfig: data.props.textStyle,
        },
        widgetType: ClayWidgetType.TextInput,
        acceptInputs: data.props.acceptInputs ?? true,
        config: {
          layout: {
            sizing: {
              width: {
                size: {
                  minMax: { min: size.width, max: size.width },
                  percent: size.width,
                },
                type: ClaySizingType.Fixed,
              },
              height: {
                size: {
                  minMax: { min: size.height, max: size.height },
                  percent: size.height,
                },
                type: ClaySizingType.Fixed,
              },
            },
          },
          floating: data.props.floating,
        },
      };
    }

    case "text":
      const size = data.props.size ?? { width: 50, height: 25 };
      return {
        id,
        config: {
          floating: data.props.floating,
        },
        textConfig: {
          value: data.props.children || "",
          textConfig: data.props.style,
        },
        widgetType: ClayWidgetType.None,
      };

    default:
      console.warn(`Unknown element type: ${type}`);
      throw new Error(`Unknown element type: ${type}`);
  }
}
