import * as P from 'ts-pattern';
import { ClayUOCommandType, ClayWidgetType, LayoutAlignment, LayoutDirection, SizingType, UINode } from '~/host';
import { TextStyle } from '~/ui';
import { ClayElementPropTypes, defaultHue, hueToVector3 } from '../elements';
import { hasEventProp } from '../events';

export function createElement(type: string, props: unknown, id: number): UINode {
  return P.match<ClayElementPropTypes, UINode>({
    type: type.toLowerCase(),
    props,
  } as ClayElementPropTypes)
    .with({ type: 'view' }, (data) => {
      return {
        id,
        config: {
          layout: data.props.layout,
          backgroundColor: data.props.backgroundColor,
          floating: data.props.floating,
          cornerRadius: data.props.cornerRadius,
          border: data.props.border,
          clip: data.props.clip,
          padding: data.props.padding,
        },
        movable: data.props.movable || false,
      };
    })
    .with({ type: 'gump' }, (data) => {
      const size = data.props.size ?? { width: 50, height: 25 };

      return {
        id,
        movable: data.props.movable ?? false,
        uoConfig: {
          type: data.props.ninePatch ? ClayUOCommandType.GumpNinePatch : ClayUOCommandType.Gump,
          id: data.props.id,
          hue: hueToVector3(data.props.hue),
        },
        config: {
          layout: {
            sizing: {
              width: {
                type: SizingType.Fixed,
                size: {
                  minMax: { min: size.width, max: size.width },
                  percent: size.width,
                },
              },
              height: {
                type: SizingType.Fixed,
                size: {
                  minMax: { min: size.height, max: size.height },
                  percent: size.height,
                },
              },
            },
            layoutDirection: data.props.direction ?? LayoutDirection.TopToBottom,
            childAlignment: data.props.childAlignment ?? {
              x: LayoutAlignment.Center,
              y: LayoutAlignment.Center,
            },
            padding: data.props.padding,
          },
          floating: data.props.floating,
        },
      };
    })
    .with({ type: 'button' }, (data) => {
      const size = data.props.size ?? { width: 50, height: 25 };

      return {
        id,
        movable: data.props.movable ?? false,
        uoConfig: {
          type: ClayUOCommandType.Gump,
          id: data.props.gumpIds.normal,
          hue: hueToVector3(data.props.hue),
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
                type: SizingType.Fixed,
              },
              height: {
                size: {
                  minMax: { min: size.height, max: size.height },
                  percent: size.height,
                },
                type: SizingType.Fixed,
              },
            },
            padding: data.props.padding,
          },
          floating: data.props.floating,
        },
      };
    })
    .with({ type: 'textinput' }, (data) => {
      const size = data.props.size ?? { width: 50, height: 25 };
      return {
        id,
        textConfig: {
          value: data.props.placeholder || '',
          replacedChar: data.props.password ? '*' : undefined,
          textConfig: data.props.textStyle,
        },
        widgetType: ClayWidgetType.TextInput,
        acceptInputs: data.props.acceptInputs ?? hasEventProp(data.props),
        config: {
          layout: {
            sizing: {
              width: {
                size: {
                  minMax: { min: size.width, max: size.width },
                  percent: size.width,
                },
                type: SizingType.Fixed,
              },
              height: {
                size: {
                  minMax: { min: size.height, max: size.height },
                  percent: size.height,
                },
                type: SizingType.Fixed,
              },
            },
            padding: data.props.padding,
          },
          floating: data.props.floating,
        },
      };
    })
    .with({ type: 'text' }, (data) => {
      return {
        id,
        movable: data.props.movable ?? false,
        config: {
          floating: data.props.floating,
          padding: data.props.padding,
        },
        textConfig: {
          value: '',
          textConfig: data.props.style ?? TextStyle.default,
        },
        widgetType: ClayWidgetType.None,
        uoConfig: {
          type: ClayUOCommandType.Text,
          id: 0,
          hue: defaultHue,
        },
      };
    })
    .with({ type: 'checkbox' }, (data) => {
      return {
        id,
        config: { floating: data.props.floating, padding: data.props.padding },
        textConfig: {
          value: 'CHECKBOX',
          textConfig: TextStyle.default,
        },
        widgetType: ClayWidgetType.None,
        uoConfig: {
          id: 0,
          type: ClayUOCommandType.Text,
          hue: defaultHue,
        },
      };
    })
    .with({ type: 'label' }, (data) => {
      return {
        id,
        config: {
          floating: data.props.floating,
          padding: data.props.padding,
        },
        textConfig: { value: 'LABEL', textConfig: TextStyle.default },
        widgetType: ClayWidgetType.None,
        uoConfig: {
          id: 0,
          type: ClayUOCommandType.Text,
          hue: defaultHue,
        },
      };
    })
    .with({ type: 'hsliderbar' }, (data) => {
      return {
        id,
        config: {
          floating: data.props.floating,
          padding: data.props.padding,
        },
        textConfig: {
          value: 'HSLIDERBAR',
          textConfig: TextStyle.default,
        },
        widgetType: ClayWidgetType.None,
        uoConfig: {
          type: ClayUOCommandType.Text,
          id: 0,
          hue: defaultHue,
        },
      };
    })
    .with({ type: 'art' }, (data) => {
      const size = data.props.size;

      return {
        id,
        uoConfig: {
          type: ClayUOCommandType.Art,
          id: data.props.id,
          hue: hueToVector3(data.props.hue),
        },
        config: {
          layout: size
            ? {
                sizing: {
                  width: {
                    type: SizingType.Fixed,
                    size: {
                      minMax: { min: size.width, max: size.width },
                      percent: size.width,
                    },
                  },
                  height: {
                    type: SizingType.Fixed,
                    size: {
                      minMax: { min: size.height, max: size.height },
                      percent: size.height,
                    },
                  },
                },
              }
            : undefined,
          floating: data.props.floating,
          padding: data.props.padding,
        },
      };
    })
    .exhaustive();
}
