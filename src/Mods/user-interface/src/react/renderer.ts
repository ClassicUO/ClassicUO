import React from 'react';
import { OpaqueRoot } from 'react-reconciler';
import { UIEvent } from '~/host';
import { ClayContainer } from './container';
import { EventManager } from './events';
import { ClayReconciler, getClayReconciler } from './reconciler';

export class ClayReactRenderer {
  private container: ClayContainer;
  private reconciler: ClayReconciler;
  private fiberRoot: OpaqueRoot;
  private events: EventManager;

  constructor() {
    this.events = new EventManager();
    this.container = new ClayContainer(this.events);
    this.reconciler = getClayReconciler(this.events);
    this.fiberRoot = this.reconciler.createContainer(
      this.container,
      0, // ConcurrentRoot
      null,
      false,
      null,
      '',
      (error: unknown) => {
        console.error('onRecoverableError', error);
      },
      null,
    );
  }

  render(element: React.ReactElement): void {
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
  handleUIEvent(event: UIEvent): void {
    this.events.dispatch(event);
  }
}
