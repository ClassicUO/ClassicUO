import {
  UINodes,
  UINode,
  UIEvent,
  EventType,
  Keys,
  MouseButtonType,
} from "~/host";
import { HostWrapper } from "~/host";
import { ClayElement } from "./elements";
import { EventCallbackMap } from "./events";

export class ClayContainer {
  public elements: ClayElement[] = [];
  public synced: boolean = true;
  private entities: Set<number> = new Set();

  constructor(private eventCallbacks: EventCallbackMap = new Map()) {}

  appendChild(child: ClayElement): void {
    this.elements.push(child);
    this.trackEntity(child);
  }

  removeChild(child: ClayElement): void {
    const index = this.elements.indexOf(child);
    if (index !== -1) {
      this.elements.splice(index, 1);
      this.cleanupEntity(child);
    }
  }

  insertBefore(child: ClayElement, beforeChild: ClayElement): void {
    const index = this.elements.indexOf(beforeChild);
    if (index !== -1) {
      this.elements.splice(index, 0, child);
    } else {
      this.elements.push(child);
    }
    this.trackEntity(child);
  }

  clear(): void {
    // Clean up all entities before clearing
    for (const element of this.elements) {
      this.cleanupEntity(element);
    }
    this.elements = [];
    // ClayContainer.uiCallbacks.clear(); // TODO: necessary?
    this.entities.clear();
    this.synced = false;
  }

  private trackEntity(element: ClayElement): void {
    this.entities.add(element.instanceId);
    for (const child of element.children) {
      this.trackEntity(child);
    }
  }

  private cleanupEntity(element: ClayElement): void {
    this.entities.delete(element.instanceId);
    this.eventCallbacks.delete(element.instanceId);

    HostWrapper.deleteEntity(element.instanceId);
  }

  render(): void {
    if (this.synced) return;

    // Convert element tree to UINodes format
    const nodes: UINode[] = [];
    const relations: Record<number, number> = {};

    this.collectNodes(this.elements, nodes, relations);

    const uiNodes: UINodes = {
      nodes,
      relations,
    };

    // Send to host
    HostWrapper.setNode(uiNodes);
    this.synced = true;

    // console.log("ClayContainer render", uiNodes);
  }

  private collectNodes(
    elements: ClayElement[],
    nodes: UINode[],
    relations: Record<number, number>,
    parentId?: number
  ): void {
    for (const element of elements) {
      if (element.node) {
        nodes.push(element.node);

        // Set parent relationship
        if (parentId !== undefined) {
          relations[element.instanceId] = parentId;
        }

        // Recursively collect children
        this.collectNodes(
          element.children,
          nodes,
          relations,
          element.instanceId
        );
      }
    }
  }

  // Called from plugin's UI event handler
  handleUIEvent(event: UIEvent): void {
    // stale event handler?
    if (!this.entities.has(event.entityId)) {
      console.warn("event handler called for untracked entity", event);
      return;
    }

    const callback = this.eventCallbacks
      .get(event.entityId)
      ?.get(event.eventType);

    if (typeof callback === "function") {
      callback(event);
    }
  }

  addEvent(event: UIEvent, callback: (event: UIEvent) => void): void {
    if (!this.eventCallbacks.has(event.entityId)) {
      this.eventCallbacks.set(event.entityId, new Map());
    }

    const map = this.eventCallbacks.get(event.entityId);

    if (!map?.has(event.eventType)) {
      HostWrapper.addEventListener(event);
    }
    map.set(event.eventType, callback);
  }

  removeEvent(event: UIEvent): void {
    this.eventCallbacks.get(event.entityId)?.delete(event.eventType);
    HostWrapper.removeEventListener(event);
  }
}
