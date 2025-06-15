use extism_pdk::*;

#[plugin_fn]
pub fn register(name: String) -> FnResult<()> {
    register_all_packets();
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
    }

    info!(
        "{}",
        format!("recv: 0x{:02X?} size: {}", packet_id, packet_size)
    );
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
    let mut vec: Vec<u8> = vec![packet_id];
    vec.extend_from_slice(fn_name.as_bytes());
    unsafe {
        cuo_add_packet_handler(vec).unwrap();
    }
}

#[host_fn]
extern "ExtismHost" {
    fn cuo_add_packet_handler(handler_desc: Vec<u8>);
    fn cuo_get_packet_size(id: Vec<u8>) -> Vec<u8>;
    fn cuo_send_to_server(packet: Vec<u8>);
}
