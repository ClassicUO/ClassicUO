use extism_pdk::*;

#[plugin_fn]
pub fn on_init() -> FnResult<()> {
    register_all_packets();
    Ok(())
}

#[plugin_fn]
pub fn on_update() -> FnResult<()> {
    Ok(())
}

#[plugin_fn]
pub fn on_event() -> FnResult<()> {
    Ok(())
}

#[plugin_fn]
pub fn handler_0x73(packet: Vec<u8>) -> FnResult<()> {
    info!("hello handler 0x73");
    Ok(())
}

#[plugin_fn]
pub fn packet_recv(mut packet: Vec<u8>) -> FnResult<Vec<u8>> {
    if packet.is_empty() {
        error!("packet is empty");
        return Ok(vec![]);
    }

    let packet_id = *packet.first().unwrap();
    let bytes = unsafe { cuo_get_packet_size(vec![packet_id]).unwrap() };
    let mut packet_size = u16::from_le_bytes([bytes[0], bytes[1]]);
    let fixed_size = packet_size != u16::MAX;

    if !fixed_size {
        let b0 = packet[1];
        let b1 = packet[2];
        packet_size = ((b0 as u16) << 8) | (b1 as u16);
    }

    if packet_id == 0x73 {
        // warn!("0x73 blocked");
        // return Ok(vec![]);
        // unsafe {
        //     cuo_send_to_server(vec![0x73, 0xFF]).unwrap();
        // }
    } else if packet_id == 0xF3 {
        // let offset = if fixed_size { 1 } else { 3 };
        // packet[offset + 7] = 1;
        // packet[offset + 8] = 2;
    } else if packet_id == 0xD2 || packet_id == 0x77 {
        // let offset = if fixed_size { 1 } else { 3 };
        // packet[offset + 4] = 1;
        // packet[offset + 5] = 2;
    }

    info!(
        "{}",
        format!("recv: 0x{:02X?} size: {}", packet_id, packet_size)
    );

    // let player_serial = unsafe { cuo_get_player_serial().unwrap() };
    // let player_graphic = unsafe {
    //     cuo_get_entity_graphic(player_serial)
    //         .unwrap()
    //         .try_into()
    //         .map(u16::from_le_bytes)
    //         .unwrap_or(0)
    // };

    let player_serial = get_player();
    let player_position = get_entity_position(player_serial);
    let player_components = ecs_get_components(player_serial);
    // let player_equipment = get_entity_equipment(player_serial);
    let player_graphic = get_entity_graphic(player_serial);

    info!(
        "{}",
        format!(
            "player position {} {} {}",
            player_position.x, player_position.y, player_position.z
        )
    );

    info!("{}", format!("components {:?}", player_components));
    // info!("{}", format!("equipment {:?}", player_equipment));
    info!("{}", format!("graphic {:02X?}", player_graphic.value));

    set_entity_graphic(player_serial, Graphic { value: 0x12 });

    // if let Some(table_mem) = config::get_memory("packet_table").unwrap() {
    //     let table: Vec<u16> = table_mem
    //         .to_vec()
    //         .chunks_exact(2)
    //         .map(|chunk| u16::from_le_bytes([chunk[0], chunk[1]]))
    //         .collect();

    //     let packet_id = *packet.first().unwrap();

    //     if let Some(&packet_size) = table.get(packet_id as usize) {
    //         if packet_size == u16::MAX {
    //             todo!()
    //         }

    //         info!(
    //             "{}",
    //             format!("recv: {:02X?} size: {}", packet_id, packet_size)
    //         );
    //     }
    // }

    Ok(packet)
}

fn register_all_packets() {
    register_handler(0x73, stringify!(handler_0x73));
}

fn register_handler(packet_id: u8, fn_name: &str) {
    unsafe {
        cuo_add_packet_handler(Json(PacketHandlerInfo {
            packet_id,
            func_name: fn_name.to_string(),
        }))
        .unwrap();
    }
}

fn get_player() -> u32 {
    unsafe {
        cuo_get_player_serial()
            .unwrap()
            .try_into()
            .map(u32::from_le_bytes)
            .unwrap_or(0)
    }
}

fn get_entity_graphic(serial: u32) -> Graphic {
    unsafe {
        cuo_get_entity_graphic(serial.to_le_bytes().to_vec())
            .unwrap_or(Json(Graphic { value: 0 }))
            .0
        // .try_into()
        // .map(u16::from_le_bytes)
        // .unwrap_or(0)
    }
}

fn get_entity_position(serial: u32) -> WorldPosition {
    unsafe {
        cuo_get_entity_position(serial.to_le_bytes().to_vec())
            .unwrap_or(Json(WorldPosition { x: 0, y: 0, z: 0 }))
            .0
    }
}

fn set_entity_graphic(serial: u32, val: Graphic) {
    unsafe {
        cuo_set_entity_graphic(serial.to_le_bytes().to_vec(), Json(val)).unwrap();
    }
}

// fn get_entity_equipment(serial: u32) -> String {
//     unsafe { cuo_get_entity_equipment(serial.to_le_bytes().to_vec()).unwrap_or(String::from("")) }
// }

fn ecs_get_components(serial: u32) -> Vec<ComponentInfo> {
    unsafe {
        cuo_ecs_get_components(serial.to_le_bytes().to_vec())
            .unwrap_or(Json(vec![]))
            .0
    }
}

#[derive(serde::Deserialize, serde::Serialize)]
struct WorldPosition {
    x: u16,
    y: u16,
    z: i8,
}

#[derive(serde::Deserialize, serde::Serialize)]
struct Serial {
    value: u32,
}

#[derive(serde::Deserialize, serde::Serialize)]
struct Graphic {
    value: u16,
}

#[derive(serde::Deserialize, serde::Serialize)]
struct Hue {
    value: u16,
}

#[derive(serde::Deserialize, serde::Serialize)]
struct Facing {
    value: u8,
}

#[derive(Debug, serde::Deserialize)]
struct ComponentInfo {
    id: u64,
    size: u32,
}

#[derive(Debug, serde::Serialize)]
#[serde(rename_all = "camelCase")]
struct PacketHandlerInfo {
    packet_id: u8,
    func_name: String,
}

#[host_fn]
extern "ExtismHost" {

    fn cuo_ecs_get_components(serial: Vec<u8>) -> Json<Vec<ComponentInfo>>;

    fn cuo_add_packet_handler(handler_desc: Json<PacketHandlerInfo>);

    fn cuo_get_packet_size(id: Vec<u8>) -> Vec<u8>;
    fn cuo_send_to_server(packet: Vec<u8>);

    fn cuo_get_player_serial() -> Vec<u8>;

    fn cuo_get_entity_graphic(serial: Vec<u8>) -> Json<Graphic>;
    fn cuo_get_entity_hue(serial: Vec<u8>) -> Json<Hue>;
    fn cuo_get_entity_direction(serial: Vec<u8>) -> Json<Facing>;
    fn cuo_get_entity_position(serial: Vec<u8>) -> Json<WorldPosition>;

    fn cuo_set_entity_graphic(serial: Vec<u8>, val: Json<Graphic>);
    fn cuo_set_entity_hue(serial: Vec<u8>, val: Json<Hue>);
    fn cuo_set_entity_direction(serial: Vec<u8>, val: Json<Facing>);
    fn cuo_set_entity_position(serial: Vec<u8>, val: Json<WorldPosition>);

    // fn cuo_get_entity_equipment(serial: Vec<u8>) -> String;
}
