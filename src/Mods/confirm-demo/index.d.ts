/**
 * Exported plugin functions (Extism builds these as WASM exports).
 */
declare module 'main' {
  export function on_init(): I32;
  export function on_update(): I32;
  export function on_event(): I32;
  export function on_ui_event(): I32;
}

/**
 * Host functions imported from ClassicUO. Names/params must match Api.cs.
 */
declare module 'extism:host' {
  interface user {
    cuo_ui_node(ptr: I64): void;
    cuo_ui_add_event_listener(ptr: I64): I64;
    cuo_ui_remove_event_listener(ptr: I64): I64;
    cuo_ecs_spawn_entity(): I64;
    cuo_ecs_delete_entity(id: I64): void;
    cuo_add_entity_to_parent(id: I64, parentId: I64, index: I64): void;
    cuo_send_events(ptr: I64): void;
  }
}
