import { UINodes, UINode, UIEvent, EventType, Keys, MouseButtonType } from "~/types";
import { HostWrapper } from "~/host";
import { ClayElement } from "./components";

export class ClayContainer {
  public elements: ClayElement[] = [];
  public synced: boolean = true;
  public uiCallbacks: Map<number, Map<number, () => void>> = new Map();
  private allEntities: Set<number> = new Set(); // Track all created entities

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
    this.uiCallbacks.clear();
    this.allEntities.clear();
    this.synced = false;
  }

  private trackEntity(element: ClayElement): void {
    this.allEntities.add(element.instanceId);
    // Recursively track children
    for (const child of element.children) {
      this.trackEntity(child);
    }
  }

  private cleanupEntity(element: ClayElement): void {
    // Remove from tracking
    this.allEntities.delete(element.instanceId);

    // Remove UI callback
    this.uiCallbacks.delete(element.instanceId);

    // Use the new UI-specific delete function which handles recursive cleanup
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

        // Register callbacks
        if (element.props.onClick) {
          const eventId = HostWrapper.addEventListener({
            eventType: EventType.OnMouseReleased,
            entityId: element.instanceId,
            mouseButton: MouseButtonType.Left
          });

          const eventId2 = HostWrapper.addEventListener({
            eventType: EventType.OnMouseOver,
            entityId: element.instanceId,
          });

          const eventId3 = HostWrapper.addEventListener({
             eventType: EventType.OnMouseLeave,
             entityId: element.instanceId,
          });


          if (!this.uiCallbacks.has(element.instanceId)) {
            this.uiCallbacks.set(element.instanceId, new Map());
          }

          const events = this.uiCallbacks.get(element.instanceId);

          if (eventId !== 0) {
            events.set(eventId, element.props.onClick);
          }

          if (eventId2 !== 0) {
            events.set(eventId2, () => console.log("on mouse over the element"));
          }

          if (eventId3 !== 0) {
            events.set(eventId3, () => console.log("on mouse leave the element"));
          }
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
  handleUIEvent(entityId: number, eventId: number): void {
    const events = this.uiCallbacks.get(entityId);
    if (events) {
        events.get(eventId)?.();
    }
  }
}
