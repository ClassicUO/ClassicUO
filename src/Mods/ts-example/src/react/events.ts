import { CamelCase } from "type-fest";
import { EventType } from "~/host";
import { ClayContainer } from "./container";
import { DiffKeys } from "~/support";

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

export const applyEvents = (
  container: ClayContainer,
  id: number,
  diff: DiffKeys,
  props: Record<string, unknown>
) => {
  const { added, deleted, updated } = diff;

  // new events being added
  if (added.length > 0) {
    for (const event of added) {
      const fn = props[event] as (event: UIEvent) => void;
      if (typeof fn === "function") {
        container.addEvent({ eventType: eventMap[event], entityId: id }, fn);
      }
    }
  }

  // events being removed
  if (deleted.length > 0) {
    for (const event of deleted) {
      container.removeEvent({ eventType: eventMap[event], entityId: id });
    }
  }

  // props updating which overwrites a previous handler
  if (updated.length > 0) {
    for (const event of updated) {
      const fn = props[event] as (event: UIEvent) => void;
      if (typeof fn === "function") {
        container.addEvent({ eventType: eventMap[event], entityId: id }, fn);
      }
    }
  }
};
