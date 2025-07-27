import { Graphic, QueryRequest, QueryResponse, SpriteDescription, UIEvent, UINode, UINodes } from '~/host';

type HostFunctions = ReturnType<typeof Host.getFunctions>;

/**
 * This is the wrapper for the host functions.
 * We handle the data serialization going in <-> out to CUO.
 */
export class HostWrapper {
  static hostFunctions: HostFunctions;

  private static get functions(): HostFunctions {
    return (this.hostFunctions ??= Host.getFunctions());
  }

  static getInputString(): string {
    return Host.inputString();
  }

  static sendPacketToServer(data: Uint8Array): void {
    const mem = Memory.fromBuffer(data);
    this.functions.cuo_send_to_server(mem.offset);
  }

  static setSprite(spriteDesc: SpriteDescription): void {
    const json = JSON.stringify(spriteDesc);
    const mem = Memory.fromString(json);
    this.functions.cuo_set_sprite(mem.offset);
  }

  static getSprite(spriteDesc: SpriteDescription): SpriteDescription {
    const json = JSON.stringify(spriteDesc);
    const memIn = Memory.fromString(json);
    const memOut = this.functions.cuo_get_sprite(memIn.offset);
    const jsonOut = Memory.find(memOut).readString();
    return JSON.parse(jsonOut) as SpriteDescription;
  }

  static getEntityGraphic(serial: number): Graphic {
    const memOut = this.functions.cuo_get_entity_graphic(serial);
    const jsonOut = Memory.find(memOut).readString();
    return JSON.parse(jsonOut);
  }

  static setEntityGraphic(serial: number, graphic: Graphic): void {
    const memIn1 = Memory.fromString(JSON.stringify(graphic));
    this.functions.cuo_set_entity_graphic(serial, memIn1.offset);
  }

  static getPlayerSerial(): number {
    const mem = this.functions.cuo_get_player_serial();
    const bytes = Memory.find(mem).readBytes();
    return new Uint32Array(bytes)[0];
  }

  static setNode(data: UINode | UINodes): void {
    const json = JSON.stringify('id' in data ? { nodes: [data], relations: {} } : data, null, 2);
    // console.log("setNode", json);
    const memIn = Memory.fromString(json);
    this.functions.cuo_ui_node(memIn.offset);
  }

  static addEventListener(event: UIEvent): number {
    const json = JSON.stringify(event);
    const memIn = Memory.fromString(json);
    console.log('HostWrapper:addEventListener', json);
    return this.functions.cuo_ui_add_event_listener(memIn.offset) as number;
  }

  static removeEventListener(event: UIEvent): number {
    const json = JSON.stringify(event);
    const memIn = Memory.fromString(json);
    return this.functions.cuo_ui_remove_event_listener(memIn.offset) as number;
  }

  static spawnEntity(): number {
    const id = this.functions.cuo_ecs_spawn_entity() as number;
    return id;
  }

  static deleteEntity(id: number): void {
    this.functions.cuo_ecs_delete_entity(id);
  }

  static addEntityToParent(entityId: number, parentId: number, index: number = -1): void {
    console.log('addEntityToParent', entityId, parentId, index);
    this.functions.cuo_add_entity_to_parent(entityId, parentId, index);
  }

  static query(query: QueryRequest): QueryResponse {
    const memIn = Memory.fromString(JSON.stringify(query));
    const offset = this.functions.cuo_ecs_query(memIn.offset);
    const memOut = Memory.find(offset).readString();
    return JSON.parse(memOut);
  }
}

// TODO: Implement Zlib compression
export class Zlib {
  static compress(data: Uint8Array): Uint8Array {
    return data;
  }

  static uncompress(data: Uint8Array): Uint8Array {
    return data;
  }
}
