import createReconciler, { type ReactContext } from "react-reconciler";
import {
  DefaultEventPriority,
  NoEventPriority,
} from "react-reconciler/constants.js";
import { HostWrapper } from "~/host";
import { ClayContainer } from "./container";
import { ClayElement, ClayElementNames } from "./components";
import { createContext } from "react";
import { createElement } from "./createElement";
import { TextStyle } from "~ui/utils";
import { ClayUOCommandType, ClayWidgetType } from "~/types";

type Props = Record<string, unknown>;
type HostContext = {};
export type ClayReconciler = ReturnType<typeof getClayReconciler>;

export function getClayReconciler() {
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
      const instanceId = HostWrapper.spawnEntity();

      console.log("createInstance", instanceId, type, props);
      const node = createElement(type, props, instanceId);

      HostWrapper.setNode(node);
      return { type, props, children: [], instanceId, node };
    },
    createTextInstance: (text, rootContainer, hostContext, internalHandle) => {
      console.log(
        "createTextInstance",
        text,
        rootContainer,
        hostContext,
        internalHandle
      );
      const id = HostWrapper.spawnEntity();
      // const node = createElement("text", { children: text }, instanceId);

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
    },
    appendInitialChild: (parent, child) => {
      // console.log("appendInitialChild", parent.type, child.type);
      parent.children.push(child);
    },
    finalizeInitialChildren: () => false,

    // Mutation methods
    appendChild: (parent, child) => {
      // console.log("appendChild", parent.type, child.type);
      parent.children.push(child);
      HostWrapper.addEntityToParent(child.instanceId, parent.instanceId, -1);
    },
    appendChildToContainer: (container, child) => {
      console.log("appendChildToContainer", child.type);
      container.appendChild(child);
    },
    removeChild: (parent, child) => {
      // console.log("removeChild", parent.type, child.type);
      const index = parent.children.indexOf(child);
      if (index !== -1) {
        parent.children.splice(index, 1);
        HostWrapper.deleteEntity(child.instanceId);
      }
    },
    removeChildFromContainer: (container, child) => {
      // console.log("removeChildFromContainer", child.type);
      container.removeChild(child);
    },
    insertBefore: (parent, child, beforeChild) => {
      // console.log("insertBefore", parent.type, child.type, beforeChild.type);
      const index = parent.children.indexOf(beforeChild);
      if (index !== -1) {
        parent.children.splice(index, 0, child);
      } else {
        parent.children.push(child);
      }
      HostWrapper.addEntityToParent(child.instanceId, parent.instanceId, index);
    },
    commitUpdate: (instance, type, oldProps, newProps) => {
      // console.log("commitUpdate", type, oldProps, newProps);
      instance.props = newProps;
      const oldNode = instance.node;
      instance.node = createElement(type, newProps, instance.instanceId);
      HostWrapper.setNode(instance.node);
    },
    commitTextUpdate: (textInstance, oldText, newText) => {
      console.log("commitTextUpdate", textInstance.node.id, oldText, newText);

      textInstance.node.textConfig.value = newText;
      HostWrapper.setNode(textInstance.node);
    },
    shouldSetTextContent: (type, props) => {
      // console.log("shouldSetTextContent", type, props);

      if (type.toLowerCase() === "text-fragment") {
        return true;
      }

      return false;
    },

    clearContainer: (container) => {
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
