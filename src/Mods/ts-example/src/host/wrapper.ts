import {
  SpriteDescription,
  Graphic,
  UINodes,
  QueryRequest,
  QueryResponse,
} from "../types";

type HostFunctions = ReturnType<typeof Host.getFunctions>;

// Host wrapper functions
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

  static createUINodes(nodes: UINodes): void {
    const json = JSON.stringify(nodes, null, 2);
    console.log("createUINodes", json);
    const memIn = Memory.fromString(json);
    this.functions.cuo_ui_node(memIn.offset);
  }

  static spawnEcsEntity(): number {
    return this.functions.cuo_ecs_spawn_entity() as number;
  }

  static deleteEcsEntity(id: number): void {
    this.functions.cuo_ecs_delete_entity(id);
  }

  static query(query: QueryRequest): QueryResponse {
    const memIn = Memory.fromString(JSON.stringify(query));
    const offset = this.functions.cuo_ecs_query(memIn.offset);
    const memOut = Memory.find(offset).readString();
    return JSON.parse(memOut);
  }

  static deleteUINode(entityId: number): void {
    console.log("deleteUINode", entityId);
    this.functions.cuo_ui_delete_node(entityId);
  }

  static addUINode(entityId: number, parentId: number): void {
    console.log("addUINode", entityId, parentId);
    this.functions.cuo_ui_add_node(entityId, parentId);
  }

  static insertUINode(entityId: number, parentId: number, index: number): void {
    console.log("insertUINode", entityId, parentId, index);
    this.functions.cuo_ui_insert_node(entityId, parentId, index);
  }

  static setUIText(entityId: number, text: string): void {
    console.log("setUIText", entityId, text);
    const mem = Memory.fromString(text);
    this.functions.cuo_ui_set_text(entityId, mem.offset);
  }

  static setUILayout(entityId: number, layout: any): void {
    console.log("setUILayout", entityId, layout);
    const mem = Memory.fromString(JSON.stringify(layout));
    this.functions.cuo_ui_set_layout(entityId, mem.offset);
  }

  static setUIBackground(entityId: number, color: any): void {
    console.log("setUIBackground", entityId, color);
    const mem = Memory.fromString(JSON.stringify(color));
    this.functions.cuo_ui_set_background(entityId, mem.offset);
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
