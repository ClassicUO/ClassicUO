// Minimal packet-filter mod: blocks incoming packet id 0x99, passes the rest.
// Proves the on-incoming-packet hook's block path end-to-end (real guest + the
// host ModNetTap.Filter → ModRuntimes.AnyReturnsTrue path).

wit_bindgen::generate!({
    world: "blocktest-mod",
    path: "wit",
    with: { "tinyecs:modding/app@0.1.0": generate },
});

struct BlocktestMod;

impl Guest for BlocktestMod {
    fn setup(_app: App) {
        println!("[blocktest] setup");
    }

    // true blocks the packet; everything except 0x99 continues to the host.
    fn on_incoming_packet(id: u8, _data: Vec<u8>) -> bool {
        id == 0x99
    }
}

export!(BlocktestMod);
