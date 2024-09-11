# Test WebSocket TCP proxy
This is just a simple node script for testing CUO websocket connections. 

**Do not use it in production it's not built for that.**

## Installation
Install a recent version of Node.js (tested with node v20+)

Run:
```bash
npm install
```

## Usage
- Set `ip` in CUO settings.json to `ws://127.0.0.1:2594`
- Set `ignore_relay_ip` in CUO settings.json (this requires the target to be both the login server & game server).
- Start a shard running at 127.0.0.1:2593, otherwise update the `target` in `proxy.mjs`.
- Run `node proxy.mjs`
