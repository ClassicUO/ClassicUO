import { createContext } from 'react';
import createReconciler, { type ReactContext } from 'react-reconciler';
import { DefaultEventPriority, NoEventPriority } from 'react-reconciler/constants.js';
import { HostWrapper } from '~/host';
import { shallowDiffKeys } from '~/support';
import { ClayElement, ClayElementNames } from '../elements';
import { applyEvents, EventManager } from '../events';
import { ClayContainer } from './container';
import { createElement, createTextFragment } from './createElement';

type Props = Record<string, unknown>;
type HostContext = object;
export type ClayReconciler = ReturnType<typeof getClayReconciler>;

export function getClayReconciler(events: EventManager) {
  let currentUpdatePriority = NoEventPriority;

  // Function declarations
  const getRootHostContext = () => ({});

  const getChildHostContext = () => ({});

  const prepareForCommit = () => null;

  const resetAfterCommit = (_container: ClayContainer) => {
  };

  const createInstance = (
    type: ClayElementNames,
    props: Props,
    _rootContainer: ClayContainer,
    _hostContext: HostContext,
    _internalHandle: unknown,
  ) => {
    const instanceId = HostWrapper.spawnEntity();

    const node = createElement(type, props, instanceId);
    applyEvents(
      events,
      instanceId,
      {
        added: Object.keys(props),
        deleted: [],
        updated: [],
        unchanged: [],
      },
      props,
    );

    HostWrapper.setNode(node);
    return { type, props, children: [], instanceId, node };
  };

  const createTextInstance = (
    text: string,
    _rootContainer: ClayContainer,
    _hostContext: HostContext,
    _internalHandle: unknown,
  ) => {
    const id = HostWrapper.spawnEntity();

    const node = createTextFragment(id, text);

    HostWrapper.setNode(node);

    return {
      type: 'text-fragment',
      props: {},
      children: [],
      instanceId: id,
      node,
    };
  };

  const appendInitialChild = (parent: ClayElement, child: ClayElement) => {
    parent.children.push(child);
    HostWrapper.addEntityToParent(child.instanceId, parent.instanceId, -1);
  };

  const finalizeInitialChildren = () => {
    return true;
  };

  const appendChild = (parent: ClayElement, child: ClayElement) => {
    parent.children.push(child);
    HostWrapper.addEntityToParent(child.instanceId, parent.instanceId, -1);
  };

  const appendChildToContainer = (container: ClayContainer, child: ClayElement) => {
    container.appendChild(child);
  };

  const removeChild = (parent: ClayElement, child: ClayElement) => {
    const index = parent.children.indexOf(child);
    if (index !== -1) {
      parent.children.splice(index, 1);
      // Clear listeners for the whole subtree so they don't leak (host also
      // drops them on delete, but keep the mod-side map in sync).
      clearSubtreeEvents(child);
      HostWrapper.deleteEntity(child.instanceId);
    }
  };

  const clearSubtreeEvents = (element: ClayElement) => {
    events.clearEntityEvents(element.instanceId);
    for (const c of element.children) clearSubtreeEvents(c);
  };

  const removeChildFromContainer = (container: ClayContainer, child: ClayElement) => {
    container.removeChild(child);
  };

  const insertBefore = (parent: ClayElement, child: ClayElement, beforeChild: ClayElement) => {
    const index = parent.children.indexOf(beforeChild);
    if (index !== -1) {
      parent.children.splice(index, 0, child);
    } else {
      parent.children.push(child);
    }
    HostWrapper.addEntityToParent(child.instanceId, parent.instanceId, index);
  };

  const commitUpdate = (instance: ClayElement, type: ClayElementNames, oldProps: Props, newProps: Props) => {

    instance.props = newProps;
    const diff = shallowDiffKeys(oldProps, newProps);
    applyEvents(events, instance.instanceId, diff, newProps, oldProps);

    if (
      Object.keys(diff.added).length > 0 ||
      Object.keys(diff.updated).length > 0 ||
      Object.keys(diff.deleted).length > 0
    ) {
      instance.node = createElement(type, newProps, instance.instanceId);
      HostWrapper.setNode(instance.node);
    }
  };

  const commitTextUpdate = (textInstance: ClayElement, _oldText: string, newText: string) => {
    if (textInstance.node?.text) textInstance.node.text.value = newText;
    if (textInstance.node) HostWrapper.setNode(textInstance.node);
  };

  const shouldSetTextContent = (type: ClayElementNames, _props: Props) => {

    if (type.toLowerCase() === 'text-fragment') {
      return true;
    }

    return false;
  };

  const clearContainer = (container: ClayContainer) => {
    return container.clear();
  };

  const getPublicInstance = (instance: ClayElement) => instance;

  const setCurrentUpdatePriority = (newPriority: number) => void (currentUpdatePriority = newPriority);

  const getCurrentUpdatePriority = () => currentUpdatePriority;

  const getInstanceFromNode = () => null;

  const prepareScopeUpdate = () => {};

  const getInstanceFromScope = () => null;

  const detachDeletedInstance = () => {};

  const beforeActiveInstanceBlur = () => {};

  const afterActiveInstanceBlur = () => {};

  const preparePortalMount = () => null;

  const resolveUpdatePriority = () =>
    currentUpdatePriority !== NoEventPriority ? currentUpdatePriority : DefaultEventPriority;

  const maySuspendCommit = () => false;

  const resetFormInstance = () => {};

  const requestPostPaintCallback = () => {};

  const shouldAttemptEagerTransition = () => false;

  const trackSchedulerEvent = () => {};

  const resolveEventType = () => null;

  const resolveEventTimeStamp = () => -1.1;

  const preloadInstance = () => true;

  const startSuspendingCommit = () => {};

  const suspendInstance = () => {};

  const waitForCommitToBeReady = () => null;

  const commitMount = (_instance: ClayElement) => {
    // mutate nodes before committing
  };

  const reconciler = createReconciler<
    ClayElementNames,
    Props,
    ClayContainer,
    ClayElement,
    ClayElement,
    ClayElement,
    ClayElement,
    unknown,
    unknown,
    HostContext,
    unknown,
    unknown,
    unknown,
    unknown
  >({
    supportsMutation: true,
    isPrimaryRenderer: true,
    warnsIfNotActing: false,
    supportsPersistence: false,
    supportsHydration: false,
    noTimeout: -1,
    NotPendingTransition: null,

    HostTransitionContext: createContext<unknown>(null) as unknown as ReactContext<unknown>,

    getRootHostContext,
    getChildHostContext,
    prepareForCommit,
    resetAfterCommit,
    createInstance,
    createTextInstance,
    appendInitialChild,
    finalizeInitialChildren,

    // Mutation methods
    appendChild,
    appendChildToContainer,
    removeChild,
    removeChildFromContainer,
    insertBefore,
    commitUpdate,
    commitTextUpdate,
    shouldSetTextContent,

    clearContainer,

    // Other required methods
    getPublicInstance,
    scheduleTimeout: setTimeout,
    cancelTimeout: clearTimeout,
    setCurrentUpdatePriority,
    getCurrentUpdatePriority,
    getInstanceFromNode,
    prepareScopeUpdate,
    getInstanceFromScope,
    detachDeletedInstance,

    beforeActiveInstanceBlur,
    afterActiveInstanceBlur,

    // Disable warnings for missing methods
    preparePortalMount,
    resolveUpdatePriority,
    maySuspendCommit,

    resetFormInstance,
    requestPostPaintCallback,
    shouldAttemptEagerTransition,
    trackSchedulerEvent,
    resolveEventType,
    resolveEventTimeStamp,
    preloadInstance,
    startSuspendingCommit,
    suspendInstance,
    waitForCommitToBeReady,
    commitMount,
  });

  return reconciler;
}
