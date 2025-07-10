import {
  UINode,
  ClayWidgetType,
  ClayUOCommandType,
  ClaySizingType,
  ClayLayoutDirection,
  ClayLayoutAlignment,
} from "~/host";
import { TextStyle } from "~/ui";
import { ClayElementPropTypes } from "./elements";
import * as P from "ts-pattern";

export function createElement(type: string, props: any, id: number): UINode {
  return P.match<ClayElementPropTypes, UINode>({
    type: type.toLowerCase(),
    props,
  } as ClayElementPropTypes)
    .with({ type: "view" }, (data) => {
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
        movable: data.props.movable || false,
        acceptInputs: data.props.acceptInputs || false,
      };
    })
    .with({ type: "gump" }, (data) => {
      const size = data.props.size ?? { width: 50, height: 25 };

      return {
        id,
        uoConfig: {
          type: data.props.ninePatch
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
    })
    .with({ type: "button" }, (data) => {
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
    })
    .with({ type: "textinput" }, (data) => {
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
    })
    .with({ type: "text" }, (data) => {
      return {
        id,
        config: {
          floating: data.props.floating,
        },
        textConfig: {
          value: "",
          textConfig: data.props.style ?? TextStyle.default,
        },
        widgetType: ClayWidgetType.None,
        uoConfig: {
          type: ClayUOCommandType.Text,
          id: 0,
          hue: { x: 0, y: 0, z: 1 },
        },
      };
    })
    .with({ type: "checkbox" }, (data) => {
      return {
        id,
        config: { floating: data.props.floating },
        textConfig: {
          value: `CHECKBOX`,
          textConfig: TextStyle.default,
        },
        widgetType: ClayWidgetType.None,
        uoConfig: {
          id: 0,
          type: ClayUOCommandType.Text,
          hue: { x: 0, y: 0, z: 1 },
        },
      };
    })
    .with({ type: "label" }, (data) => {
      return {
        id,
        config: { floating: data.props.floating },
        textConfig: { value: `LABEL`, textConfig: TextStyle.default },
        widgetType: ClayWidgetType.None,
        uoConfig: {
          id: 0,
          type: ClayUOCommandType.Text,
          hue: { x: 0, y: 0, z: 1 },
        },
      };
    })
    .with({ type: "hsliderbar" }, (data) => {
      return {
        id,
        config: { floating: data.props.floating },
        textConfig: {
          value: `HSLIDERBAR`,
          textConfig: TextStyle.default,
        },
        widgetType: ClayWidgetType.None,
        uoConfig: {
          type: ClayUOCommandType.Text,
          id: 0,
          hue: { x: 0, y: 0, z: 1 },
        },
      };
    })
    .exhaustive();
}
