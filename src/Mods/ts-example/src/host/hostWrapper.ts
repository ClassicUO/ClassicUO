import {
  SpriteDescription,
  Graphic,
  UINodes,
  QueryRequest,
  QueryResponse,
} from "../types";

const hostFunctions = Host.getFunctions();

// Host wrapper functions
export class HostWrapper {
  static getInputString(): string {
    return Host.inputString();
  }

  static sendPacketToServer(data: Uint8Array): void {
    const mem = Memory.fromBuffer(data);
    hostFunctions.cuo_send_to_server(mem.offset);
  }

  static setSprite(spriteDesc: SpriteDescription): void {
    const json = JSON.stringify(spriteDesc);
    const mem = Memory.fromString(json);
    hostFunctions.cuo_set_sprite(mem.offset);
  }

  static getSprite(spriteDesc: SpriteDescription): SpriteDescription {
    const json = JSON.stringify(spriteDesc);
    const memIn = Memory.fromString(json);
    const memOut = hostFunctions.cuo_get_sprite(memIn.offset);
    const jsonOut = Memory.find(memOut).readString();
    return JSON.parse(jsonOut) as SpriteDescription;
  }

  static getEntityGraphic(serial: I32): Graphic {
    const memOut = hostFunctions.cuo_get_entity_graphic(serial);
    const jsonOut = Memory.find(memOut).readString();
    return JSON.parse(jsonOut);
  }

  static setEntityGraphic(serial: I32, graphic: Graphic): void {
    const memIn1 = Memory.fromString(JSON.stringify(graphic));
    hostFunctions.cuo_set_entity_graphic(serial, memIn1.offset);
  }

  static getPlayerSerial(): I32 {
    const mem = hostFunctions.cuo_get_player_serial();
    const bytes = Memory.find(mem).readBytes();
    return new Uint32Array(bytes)[0];
  }

  static createUINodes(nodes: UINodes): void {
    const json = JSON.stringify(nodes, null, 2);
    console.log("createUINodes", json);
    const memIn = Memory.fromString(json);
    hostFunctions.cuo_ui_node(memIn.offset);
  }

  static spawnEcsEntity(): I32 {
    return hostFunctions.cuo_ecs_spawn_entity() as number;
  }

  static deleteEcsEntity(id: I32): void {
    hostFunctions.cuo_ecs_delete_entity(id);
  }

  static query(query: QueryRequest): QueryResponse {
    const memIn = Memory.fromString(JSON.stringify(query));
    const offset = hostFunctions.cuo_ecs_query(memIn.offset);
    const memOut = Memory.find(offset).readString();
    return JSON.parse(memOut);
  }
}

// Zlib compression (simplified - in real implementation you'd need a proper zlib library)
export class Zlib {
  static compress(data: Uint8Array): Uint8Array {
    // This is a placeholder - in a real implementation you'd use a zlib library
    // For now, we'll just return the data as-is
    return data;
  }

  static uncompress(data: Uint8Array): Uint8Array {
    // This is a placeholder - in a real implementation you'd use a zlib library
    return data;
  }
}
