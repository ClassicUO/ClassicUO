import { UINodes, UINode } from "../types";
import { HostWrapper } from "../host/wrapper";
import { ClayElement } from "./components";

export class ClayContainer {
  public elements: ClayElement[] = [];
  public synced: boolean = true;
  public uiCallbacks: Map<number, () => void> = new Map();
  private allEntities: Set<number> = new Set(); // Track all created entities

  appendChild(child: ClayElement): void {
    this.elements.push(child);
    this.trackEntity(child);
    this.synced = false;
  }

  removeChild(child: ClayElement): void {
    const index = this.elements.indexOf(child);
    if (index !== -1) {
      this.elements.splice(index, 1);
      this.cleanupEntity(child);
      this.synced = false;
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
    this.synced = false;
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
    HostWrapper.deleteUINode(element.instanceId);
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
    HostWrapper.createUINodes(uiNodes);
    this.synced = true;

    console.log("ClayContainer render", uiNodes);
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
          this.uiCallbacks.set(element.instanceId, element.props.onClick);
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
  handleUIEvent(entityId: number, eventType: string): void {
    this.uiCallbacks.get(entityId)?.();
  }
}
