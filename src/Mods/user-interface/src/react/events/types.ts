import { CamelCase } from 'type-fest';
import { EventType, UIEvent } from '~/host';

export type EventName = CamelCase<keyof typeof EventType>;
export type EventListener = (event: UIEvent) => void;
export type EventListenerData = {
  eventId: number;
  entityId: number;
  eventType: EventType;
};

export const EventListenerBrandSymbol = Symbol('EventListener');
export type BrandedEventListener = EventListener & {
  [EventListenerBrandSymbol]: EventListenerData;
};

export const ReactEventTypeMap = {
  ...({
    onMouseMove: EventType.OnMouseMove,
    onMouseWheel: EventType.OnMouseWheel,
    onMouseOver: EventType.OnMouseOver,
    onMousePressed: EventType.OnMousePressed,
    onMouseReleased: EventType.OnMouseReleased,
    onMouseDoubleClick: EventType.OnMouseDoubleClick,
    onMouseEnter: EventType.OnMouseEnter,
    onMouseLeave: EventType.OnMouseLeave,
    onDragging: EventType.OnDragging,
    onKeyPressed: EventType.OnKeyPressed,
    onKeyReleased: EventType.OnKeyReleased,
    onTextChanged: EventType.OnTextChanged,
  } satisfies Record<EventName, EventType>),
  // Aliases, based on browser names
  onMouseDown: EventType.OnMousePressed,
  onMouseUp: EventType.OnMouseReleased,
  onMouseClick: EventType.OnMouseReleased,
  onClick: EventType.OnMouseReleased,
  onDoubleClick: EventType.OnMouseDoubleClick,
  onKeyDown: EventType.OnKeyPressed,
  onKeyUp: EventType.OnKeyReleased,
  onKeyPress: EventType.OnKeyPressed,
  onFocus: EventType.OnMouseEnter,
  onBlur: EventType.OnMouseLeave,
  onMouseDoubleClick: EventType.OnMouseDoubleClick,
  // Editable text field value changes (textinput). Dispatched with the new
  // string in UIEvent.value; EventManager hands it to the handler directly.
  onChange: EventType.OnTextChanged,
};

export const ReactEventNames = Object.keys(ReactEventTypeMap) as EventName[];

export const hasEventProp = (props: object) => {
  return ReactEventNames.some((event) => event in props) ? true : undefined;
};

// TODO: using the base UIEvent type as the param, but each handler should have a specific type
export type ReactEventHandlerProps = {
  onMouseMove?: (event: UIEvent) => void;
  onMouseWheel?: (event: UIEvent) => void;
  onMouseOver?: (event: UIEvent) => void;
  onMousePressed?: (event: UIEvent) => void;
  onMouseReleased?: (event: UIEvent) => void;
  onMouseDoubleClick?: (event: UIEvent) => void;
  onMouseEnter?: (event: UIEvent) => void;
  onMouseLeave?: (event: UIEvent) => void;
  onDragging?: (event: UIEvent) => void;
  onKeyPressed?: (event: UIEvent) => void;
  onKeyReleased?: (event: UIEvent) => void;
  // Editable text fields: receives the new value string (not a UIEvent).
  onTextChanged?: (value: string) => void;
  onFocus?: (event: UIEvent) => void;
  onBlur?: (event: UIEvent) => void;
  onClick?: (event: UIEvent) => void;
  onDoubleClick?: (event: UIEvent) => void;
  onKeyDown?: (event: UIEvent) => void;
  onKeyUp?: (event: UIEvent) => void;
  onKeyPress?: (event: UIEvent) => void;
  onMouseDown?: (event: UIEvent) => void;
  onMouseUp?: (event: UIEvent) => void;
  onMouseClick?: (event: UIEvent) => void;
};
