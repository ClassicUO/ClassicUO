import { HostWrapper } from '~/host';
import { ClayElement } from '../elements';
import { EventManager } from '../events';

// Root host instance the reconciler mounts into. Nodes are pushed to the host
// per-instance (createInstance/commitUpdate -> HostWrapper.setNode); the
// container just tracks the top-level children and tears their entities down on
// removal/clear.
export class ClayContainer {
  public children: ClayElement[] = [];

  constructor(private events: EventManager) {}

  appendChild(child: ClayElement): void {
    this.children.push(child);
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
    this.children.splice(index === -1 ? this.children.length : index, 0, child);
  }

  clear(): void {
    for (const element of this.children) this.cleanupEntity(element);
    this.children = [];
  }

  private cleanupEntity(element: ClayElement): void {
    this.events.clearEntityEvents(element.instanceId);
    HostWrapper.deleteEntity(element.instanceId);
    for (const child of element.children) this.cleanupEntity(child);
  }
}
