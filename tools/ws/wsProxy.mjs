import websockify from '@maximegris/node-websockify';

/**
 * Note: 
 *  use `ignore_relay_ip` in CUO settings.json if testing
 *  also this won't work on OSI or any shard that uses a different host for the gameserver than the loginserver
 */
websockify({ source: '127.0.0.1:2594', target: '127.0.0.1:2593' });
