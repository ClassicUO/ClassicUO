import { UINodes, UINode } from "~/host";
import { HostWrapper } from "~/host";
import { ClayElement } from "./elements";
import { EventManager } from "./events";

export class ClayContainer {
  public children: ClayElement[] = [];
  public synced: boolean = true;
  private entities: Set<number> = new Set();

  constructor(private events: EventManager) {}

  appendChild(child: ClayElement): void {
    this.children.push(child);
    this.trackEntity(child);
  }

  removeChild(child: ClayElement): void {
    const index = this.children.indexOf(child);
    if (index !== -1) {
      this.children.splice(index, 1);
      this.cleanupEntity(child);
    }
  }

  insertBefore(child: ClayElement, beforeChild: ClayElement): void {
    const index = this.children.indexOf(beforeChild);
    if (index !== -1) {
      this.children.splice(index, 0, child);
    } else {
      this.children.push(child);
    }
    this.trackEntity(child);
  }

  clear(): void {
    // Clean up all entities before clearing
    for (const element of this.children) {
      this.cleanupEntity(element);
    }
    this.children = [];
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
    this.events.clearEntityEvents(element.instanceId);

    HostWrapper.deleteEntity(element.instanceId);
  }

  render(): void {
    if (this.synced) return;

    // Convert element tree to UINodes format
    const nodes: UINode[] = [];
    const relations: Record<number, number> = {};

    this.collectNodes(this.children, nodes, relations);

    const uiNodes: UINodes = {
      nodes,
      relations,
    };

    // Send to host
    // HostWrapper.setNode(uiNodes);
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
}
