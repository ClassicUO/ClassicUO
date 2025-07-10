import createReconciler, { type ReactContext } from "react-reconciler";
import {
  DefaultEventPriority,
  NoEventPriority,
} from "react-reconciler/constants.js";
import { HostWrapper } from "~/host";
import { ClayContainer } from "./container";
import { ClayElement, ClayElementNames } from "./elements";
import { createContext } from "react";
import { createElement } from "./createElement";
import { TextStyle } from "~/ui/utils";
import { ClayUOCommandType, ClayWidgetType } from "~/host";
import { applyEvents } from "./events";
import { shallowDiff } from "~/support";

type Props = Record<string, unknown>;
type HostContext = {};
export type ClayReconciler = ReturnType<typeof getClayReconciler>;

export function getClayReconciler(container: ClayContainer) {
  let currentUpdatePriority = NoEventPriority;
  let currentRootNode: ClayElement | undefined;

  // Function declarations
  const getRootHostContext = () => ({});

  const getChildHostContext = () => ({});

  const prepareForCommit = () => null;

  const resetAfterCommit = (container: ClayContainer) => {
    container.render();
  };

  const createInstance = (
    type: ClayElementNames,
    props: Props,
    rootContainer: ClayContainer,
    hostContext: HostContext,
    internalHandle: unknown
  ) => {
    // console.log("createInstance", type);
    const instanceId = HostWrapper.spawnEntity();

    const node = createElement(type, props, instanceId);
    applyEvents(
      container,
      instanceId,
      {
        added: Object.keys(props),
        deleted: [],
        updated: [],
        unchanged: [],
      },
      props
    );

    HostWrapper.setNode(node);
    return { type, props, children: [], instanceId, node };
  };

  const createTextInstance = (
    text: string,
    rootContainer: ClayContainer,
    hostContext: HostContext,
    internalHandle: unknown
  ) => {
    const id = HostWrapper.spawnEntity();

    const node = {
      id,
      config: {},
      textConfig: { value: text, textConfig: TextStyle.default },
      widgetType: ClayWidgetType.TextFragment,
    };

    HostWrapper.setNode(node);

    return {
      type: "text-fragment",
      props: {},
      children: [],
      instanceId: id,
      node,
    };
  };

  const appendInitialChild = (parent: ClayElement, child: ClayElement) => {
    // console.log("appendInitialChild", parent.type, child.type);
    parent.children.push(child);
  };

  const finalizeInitialChildren = () => {
    return false;
  };

  const appendChild = (parent: ClayElement, child: ClayElement) => {
    // console.log("appendChild", parent.type, child.type);
    parent.children.push(child);
    HostWrapper.addEntityToParent(child.instanceId, parent.instanceId, -1);
  };

  const appendChildToContainer = (
    container: ClayContainer,
    child: ClayElement
  ) => {
    container.appendChild(child);
  };

  const removeChild = (parent: ClayElement, child: ClayElement) => {
    // console.log("removeChild", parent.type, child.type);
    const index = parent.children.indexOf(child);
    if (index !== -1) {
      parent.children.splice(index, 1);
      HostWrapper.deleteEntity(child.instanceId);
    }
  };

  const removeChildFromContainer = (
    container: ClayContainer,
    child: ClayElement
  ) => {
    // console.log("removeChildFromContainer", child.type);
    container.removeChild(child);
  };

  const insertBefore = (
    parent: ClayElement,
    child: ClayElement,
    beforeChild: ClayElement
  ) => {
    // console.log("insertBefore", parent.type, child.type, beforeChild.type);
    const index = parent.children.indexOf(beforeChild);
    if (index !== -1) {
      parent.children.splice(index, 0, child);
    } else {
      parent.children.push(child);
    }
    HostWrapper.addEntityToParent(child.instanceId, parent.instanceId, index);
  };

  const commitUpdate = (
    instance: ClayElement,
    type: ClayElementNames,
    oldProps: Props,
    newProps: Props
  ) => {
    // console.log("commitUpdate", type, oldProps, newProps, commits++);

    instance.props = newProps;
    const diff = shallowDiff(oldProps, newProps);
    applyEvents(container, instance.instanceId, diff, newProps);

    if (
      Object.keys(diff.added).length > 0 ||
      Object.keys(diff.updated).length > 0 ||
      Object.keys(diff.deleted).length > 0
    ) {
      instance.node = createElement(type, newProps, instance.instanceId);
      HostWrapper.setNode(instance.node);
    }
  };

  const commitTextUpdate = (
    textInstance: ClayElement,
    oldText: string,
    newText: string
  ) => {
    textInstance.node.textConfig.value = newText;
    HostWrapper.setNode(textInstance.node);
  };

  const shouldSetTextContent = (type: ClayElementNames, props: Props) => {
    // console.log("shouldSetTextContent", type, props);

    if (type.toLowerCase() === "text-fragment") {
      return true;
    }

    return false;
  };

  const clearContainer = (container: ClayContainer) => {
    // console.log("clearContainer");
    return container.clear();
  };

  const getPublicInstance = (instance: ClayElement) => instance;

  const setCurrentUpdatePriority = (newPriority: number) =>
    void (currentUpdatePriority = newPriority);

  const getCurrentUpdatePriority = () => currentUpdatePriority;

  const getInstanceFromNode = () => null;

  const prepareScopeUpdate = () => {};

  const getInstanceFromScope = () => null;

  const detachDeletedInstance = () => {};

  const beforeActiveInstanceBlur = () => {};

  const afterActiveInstanceBlur = () => {};

  const preparePortalMount = () => null;

  const resolveUpdatePriority = () =>
    currentUpdatePriority !== NoEventPriority
      ? currentUpdatePriority
      : DefaultEventPriority;

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

    HostTransitionContext: createContext<unknown>(
      null
    ) as unknown as ReactContext<unknown>,

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
  });

  return reconciler;
}
