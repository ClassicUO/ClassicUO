import createReconciler, { type ReactContext } from "react-reconciler";
import {
  DefaultEventPriority,
  NoEventPriority,
} from "react-reconciler/constants.js";
import { HostWrapper } from "../host/wrapper";
import { ClayContainer } from "./container";
import { ClayElement, ClayElementNames } from "./components";
import { createContext } from "react";
import { createElement } from "./createElement";

type Props = Record<string, unknown>;
type HostContext = {};
export type ClayReconciler = ReturnType<typeof getClayReconciler>;

// Host configuration for react-reconciler
export function getClayReconciler() {
  console.log("creating HostConfig");

  let currentUpdatePriority = NoEventPriority;
  let currentRootNode: ClayElement | undefined;

  return createReconciler<
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

    getRootHostContext: () => ({}),
    getChildHostContext: () => ({}),
    prepareForCommit: () => null,
    resetAfterCommit: (container) => {
      container.render();
    },
    createInstance: (
      type,
      props,
      rootContainer,
      hostContext,
      internalHandle
    ) => {
      const instanceId = HostWrapper.spawnEcsEntity();

      console.log("createInstance", instanceId, type, props);
      const node = createElement(type, props, instanceId);

      HostWrapper.createUINodes({ nodes: [node], relations: {} });
      return { type, props, children: [], instanceId, node };
    },
    createTextInstance: (text, rootContainer, hostContext, internalHandle) => {
      console.log("createTextInstance", text);
      const instanceId = HostWrapper.spawnEcsEntity();
      const node = createElement("text", { children: text }, instanceId);
      HostWrapper.createUINodes({ nodes: [node], relations: {} });

      return {
        type: "text",
        props: { children: text },
        children: [],
        instanceId,
        node: node,
      };
    },
    appendInitialChild: (parent, child) => {
      console.log("appendInitialChild", parent.type, child.type);
      parent.children.push(child);
    },
    finalizeInitialChildren: () => false,
    shouldSetTextContent: () => false,

    // Mutation methods
    appendChild: (parent, child) => {
      console.log("appendChild", parent.type, child.type);
      parent.children.push(child);
      HostWrapper.addUINode(child.instanceId, parent.instanceId);
    },
    appendChildToContainer: (container, child) => {
      console.log("appendChildToContainer", child.type);
      container.appendChild(child);
    },
    removeChild: (parent, child) => {
      console.log("removeChild", parent.type, child.type);
      const index = parent.children.indexOf(child);
      if (index !== -1) {
        parent.children.splice(index, 1);
        HostWrapper.deleteUINode(child.instanceId);
      }
    },
    removeChildFromContainer: (container, child) => {
      console.log("removeChildFromContainer", child.type);
      container.removeChild(child);
    },
    insertBefore: (parent, child, beforeChild) => {
      console.log("insertBefore", parent.type, child.type, beforeChild.type);
      const index = parent.children.indexOf(beforeChild);
      if (index !== -1) {
        parent.children.splice(index, 0, child);
      } else {
        parent.children.push(child);
      }
      HostWrapper.insertUINode(child.instanceId, parent.instanceId, index);
    },
    commitUpdate: (instance, type, oldProps, newProps) => {
      // console.log("commitUpdate", type, oldProps, newProps);
      instance.props = newProps;
      const oldNode = instance.node;
      instance.node = createElement(type, newProps, instance.instanceId);
      HostWrapper.setUILayout(instance.instanceId, instance.node.config.layout);
    },
    commitTextUpdate: (textInstance, oldText, newText) => {
      // console.log("commitTextUpdate", oldText, newText);
      if (textInstance.node) {
        textInstance.node.textConfig = {
          value: newText,
          textConfig: textInstance.props.style,
        };
      }
    },
    clearContainer: (container) => {
      console.log("clearContainer");
      container.clear();
    },

    // Other required methods
    getPublicInstance: (instance) => instance,
    // @ts-ignore
    scheduleTimeout: setTimeout,
    // @ts-ignore
    cancelTimeout: clearTimeout,
    noTimeout: -1,
    setCurrentUpdatePriority(newPriority: number) {
      currentUpdatePriority = newPriority;
    },
    getCurrentUpdatePriority: () => currentUpdatePriority,
    getInstanceFromNode: () => null,
    prepareScopeUpdate: () => {},
    getInstanceFromScope: () => null,
    detachDeletedInstance: () => {},

    beforeActiveInstanceBlur: () => {},
    afterActiveInstanceBlur: () => {},

    // Disable warnings for missing methods
    preparePortalMount: () => null,
    resolveUpdatePriority() {
      if (currentUpdatePriority !== NoEventPriority) {
        return currentUpdatePriority;
      }

      return DefaultEventPriority;
    },
    maySuspendCommit() {
      return false;
    },
    // eslint-disable-next-line @typescript-eslint/naming-convention
    NotPendingTransition: undefined,
    // eslint-disable-next-line @typescript-eslint/naming-convention
    HostTransitionContext: createContext(
      null
    ) as unknown as ReactContext<unknown>,
    resetFormInstance() {},
    requestPostPaintCallback() {},
    shouldAttemptEagerTransition() {
      return false;
    },
    trackSchedulerEvent() {},
    resolveEventType() {
      return null;
    },
    resolveEventTimeStamp() {
      return -1.1;
    },
    preloadInstance() {
      return true;
    },
    startSuspendingCommit() {},
    suspendInstance() {},
    waitForCommitToBeReady() {
      return null;
    },
  });
}
