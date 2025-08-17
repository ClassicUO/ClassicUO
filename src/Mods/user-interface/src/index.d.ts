/**
 * These are the plugin functions, only export functions declarations here (and in index.tsx) will be exposed to the host.
 * Extism will use these definitions to build the WebAssembly exports.
 */
declare module 'main' {
  export function on_init(): I32;
  export function on_update(): I32;
  export function on_event(): I32;
  export function on_ui_event(): I32;
}

/**
 * These are host-side functions, Extism will use these definitions to build the WebAssembly imports from the host (CUO).
 * The names and parameters need to match exactly to what CUO is exposing.
 */
declare module 'extism:host' {
  interface user {
    cuo_send_to_server(ptr: I64): void;
    cuo_set_sprite(ptr: I64): void;
    cuo_get_sprite(ptr: I64): I64;
    cuo_get_player_serial(): I64;
    cuo_get_entity_graphic(ptr: I64): I64;
    cuo_set_entity_graphic(ptr: I64, ptr2: I64): void;
    cuo_ui_node(ptr: I64): void;
    cuo_ui_add_event_listener(ptr: I64): I64;
    cuo_ui_remove_event_listener(ptr: I64): I64;
    cuo_ecs_spawn_entity(): I64;
    cuo_ecs_delete_entity(id: I64): void;
    cuo_add_entity_to_parent(id: I64, parentId: I64, index: I64): void;
    cuo_send_events(ptr: I64): void;
  }
}
