import { CamelCase } from "type-fest";
import { EventType, HostWrapper, UIEvent } from "~/host";
import { DiffKeys } from "~/support";
import { ClayElement } from "./elements";

export type EventListenerMap = Map<
  number,
  Map<
    EventType,
    {
      id: number;
      listener: (event: UIEvent) => void;
    }
  >
>;
export type EventName = CamelCase<keyof typeof EventType>;

const eventMap = {
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
  } satisfies Record<EventName, EventType>),
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
};

const eventNames = Object.keys(eventMap) as EventName[];

export const hasEventProp = (props: object) => {
  return Object.keys(eventMap).some((event) => event in props)
    ? true
    : undefined;
};

export type EventHandlerMap = {
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

export type EventListener = (event: UIEvent) => void;

const EventListenerSymbol = Symbol("EventListener");
type BrandedEventListener = EventListener & {
  [EventListenerSymbol]: {
    eventId: number;
    entityId: number;
    eventType: EventType;
  };
};

export class EventManager {
  private eventListeners = new Map<number, BrandedEventListener>();
  private entityIdToEventId = new Map<number, Set<number>>();

  addListener(entityId: number, eventType: EventType, listener: EventListener) {
    const eventId = HostWrapper.addEventListener({ entityId, eventType });

    const brandedListener = Object.assign(listener, {
      [EventListenerSymbol]: { eventId, entityId, eventType },
    }) as BrandedEventListener;

    this.eventListeners.set(eventId, brandedListener);

    if (!this.entityIdToEventId.has(entityId)) {
      this.entityIdToEventId.set(entityId, new Set());
    }
    this.entityIdToEventId.get(entityId)?.add(eventId);
  }

  removeListener(listener: EventListener) {
    if (!listener[EventListenerSymbol]) return;

    const data = listener[EventListenerSymbol];
    HostWrapper.removeEventListener(data);
    this.eventListeners.delete(data.eventId);
    this.entityIdToEventId.get(data.entityId)?.delete(data.eventId);
  }

  clearEntityEvents(entityId: number) {
    const eventIds = this.entityIdToEventId.get(entityId);

    if (!eventIds) return;

    for (const eventId of eventIds) {
      this.eventListeners.delete(eventId);
    }

    this.entityIdToEventId.delete(entityId);
  }

  dispatch(event: UIEvent) {
    if (typeof event.eventId !== "number") return;
    this.eventListeners.get(event.entityId)?.(event);
  }
}

export const applyEvents = (
  events: EventManager,
  id: number,
  diff: DiffKeys,
  props: Record<string, unknown>,
  oldProps: Record<string, unknown> = {}
) => {
  const { added, deleted, updated } = diff;

  // new events being added
  if (added.length > 0) {
    for (const event of added) {
      if (!eventNames.includes(event as EventName)) {
        continue;
      }

      const fn = props[event] as (event: UIEvent) => void;

      if (typeof fn === "function") {
        console.log("applyEvents added", event, fn, eventMap[event]);

        events.addListener(id, eventMap[event], fn);
      }
    }
  }

  // events being removed
  if (deleted.length > 0) {
    for (const event of deleted) {
      if (!eventNames.includes(event as EventName)) {
        continue;
      }

      console.log("applyEvents deleted", event, eventMap[event]);

      const fn = props[event] as (event: UIEvent) => void;
      if (typeof fn === "function") {
        events.removeListener(fn);
      }
    }
  }

  // props updating which overwrites a previous handler
  if (updated.length > 0) {
    for (const event of updated) {
      if (!eventNames.includes(event as EventName)) {
        continue;
      }

      const fn = props[event] as (event: UIEvent) => void;
      if (typeof fn === "function") {
        console.log("applyEvents updated", event, fn, eventMap[event]);

        events.removeListener(oldProps[event] as EventListener);
        events.addListener(id, eventMap[event], fn);
      }
    }
  }
};
