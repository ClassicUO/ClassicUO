import { EventType, HostWrapper, UIEvent } from '~/host';
import { DiffKeys } from '~/support';
import {
  BrandedEventListener,
  EventListener,
  EventListenerBrandSymbol,
  EventListenerData,
  EventName,
  ReactEventNames,
  ReactEventTypeMap,
} from './types';

const brandListener = (listener: EventListener | BrandedEventListener, data: EventListenerData) => {
  return Object.assign(listener, {
    [EventListenerBrandSymbol]: data,
  }) as BrandedEventListener;
};

export class EventManager {
  private eventListeners = new Map<number, BrandedEventListener>();
  private entityIdToEventId = new Map<number, Set<number>>();

  /**
   * Add a new event listener to the event manager.
   * @param entityId - The entity ID (EcsId) to add the listener to.
   * @param eventType - The event type to listen for.
   * @param listener - The listener function to call when the event is triggered.
   */
  addListener(entityId: number, eventType: EventType, listener: EventListener) {
    const eventId = HostWrapper.addEventListener({ entityId, eventType });

    const brandedListener = brandListener(listener, {
      eventId,
      entityId,
      eventType,
    });

    this.eventListeners.set(eventId, brandedListener);

    if (!this.entityIdToEventId.has(entityId)) {
      this.entityIdToEventId.set(entityId, new Set());
    }

    this.entityIdToEventId.get(entityId)?.add(eventId);
  }

  /**
   * Remove an event listener from the event manager.
   * @param listener - The listener function to remove, must have been added by addListener.
   */
  removeListener(listener: EventListener) {
    if (!listener[EventListenerBrandSymbol]) return;

    const data = listener[EventListenerBrandSymbol];

    HostWrapper.removeEventListener(data);

    this.eventListeners.delete(data.eventId);
    this.entityIdToEventId.get(data.entityId)?.delete(data.eventId);
  }

  /**
   * Clear all events for a given entity ID.
   * @param entityId - The entity ID to clear events for.
   */
  clearEntityEvents(entityId: number) {
    const eventIds = this.entityIdToEventId.get(entityId);

    if (!eventIds) return;

    for (const eventId of eventIds) {
      this.eventListeners.delete(eventId);
    }

    this.entityIdToEventId.delete(entityId);
  }

  /**
   * Dispatch an event to the event manager, typically from the host (CUO).
   * @param event - The event to dispatch.
   */
  dispatch(event: UIEvent) {
    if (typeof event.eventId !== 'number') return;
    this.eventListeners.get(event.eventId)?.(event);
  }
}

export const applyEvents = (
  events: EventManager,
  id: number,
  diff: DiffKeys,
  props: Record<string, unknown>,
  oldProps: Record<string, unknown> = {},
) => {
  const { added, deleted, updated } = diff;

  // new events being added
  if (added.length > 0) {
    for (const event of added) {
      if (!ReactEventNames.includes(event as EventName)) {
        continue;
      }

      const fn = props[event] as (event: UIEvent) => void;

      if (typeof fn === 'function') {
        events.addListener(id, ReactEventTypeMap[event], fn);
      }
    }
  }

  // events being removed
  if (deleted.length > 0) {
    for (const event of deleted) {
      if (!ReactEventNames.includes(event as EventName)) {
        continue;
      }

      const fn = props[event] as (event: UIEvent) => void;
      if (typeof fn === 'function') {
        events.removeListener(fn);
      }
    }
  }

  // props updating which overwrites a previous handler
  if (updated.length > 0) {
    for (const event of updated) {
      if (!ReactEventNames.includes(event as EventName)) {
        continue;
      }

      const fn = props[event] as (event: UIEvent) => void;
      if (typeof fn === 'function') {
        events.removeListener(oldProps[event] as EventListener);
        events.addListener(id, ReactEventTypeMap[event], fn);
      }
    }
  }
};
