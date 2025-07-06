import { OpaqueRoot } from "react-reconciler";
import { getClayReconciler, ClayReconciler } from "./reconciler";
import { ClayContainer } from "./container";
import React from "react";

export class ClayReactRenderer {
  private container: ClayContainer;
  private reconciler: ClayReconciler;
  private fiberRoot: OpaqueRoot;

  constructor() {
    this.reconciler = getClayReconciler();
    this.container = new ClayContainer();
    this.fiberRoot = this.reconciler.createContainer(
      this.container,
      0, // ConcurrentRoot
      null,
      false,
      null,
      "",
      (error: any) => {
        console.error("onRecoverableError", error);
      },
      null
    );
  }

  render(element: React.ReactElement): void {
    console.log("ClayReactRenderer render");
    this.reconciler.updateContainer(element, this.fiberRoot, null, () => {});
  }

  clear(): void {
    this.container.clear();
  }

  unmount(): void {
    this.container.clear();
    this.reconciler.updateContainer(null, this.fiberRoot, null, () => {});
  }

  // Handle UI events from CUO
  handleUIEvent(entityId: number, eventId: number): void {
    this.container.handleUIEvent(entityId, eventId);
  }
}
