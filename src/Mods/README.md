## The idea

I desire to make ClassicUO as much moddable as possible for all platforms.
That's the reason behind the choice of https://github.com/extism which compiles plugins to wasm.
ClassicUO would be the main host. Plugins will be able to override core game behaviours like packet handlers, UI and whatever is possible to put behind an api.

My desire would be allowing users to customize things, focusing to their business and at the same time keeping ClassicUO central.
So fork the project would not be necessary anymore.

Also extism allows to load plugins from urls which means it's possible to centralize all mods in a single repo (like Zed does).

## How to build a plugin

Chose one framework from the PDKs available here https://github.com/extism#compile-webassembly-to-run-in-extism-hosts
In `src/Mods/` you will find a sandbox plugin example written in Rust, but you can choose what language you prefer.
Atm ClassicUO requires an exposed plugin function `register` to use the plugin.

## How to install a plugin

Simply open the `settings.json` and put path of your wasm plugin into the `plugins` json section like any other assistant.
