ClassicUO - an open source implementation of the Ultima Online Classic Client.

# Contacts
Join [Discord channel](https://discord.gg/VdyCpjQ)

# Introduction
ClassicUO is an open source implementation of the Ultima Online Classic Client. This client is intended to emulate client versions 7.0.59.8 and older and is primarily tested against Ultima Online free shards based on RunUO and [ServUO](https://github.com/servuo/servuo). This client will not work on the official game shards.

The client is currently under heavy development but is functional. The code is based on the [FNA-XNA](https://fna-xna.github.io/) framework. C# is chosen because there is a large community of developers working on Ultima Online server emulators in C#, because FNA-XNA exists and seems reasonably suitable for creating this type of game, and because the game is inexpensive enough to run that performance is not a major concern.

ClassicUO is natively cross platform and supports:
* Windows
* Linux
* MacOS

The code itself has been written using the following projects as a reference:

* [OrionUO](https://github.com/hotride/orionuo)
* [Razor](https://github.com/msturgill/razor)
* [UltimaXNA](https://github.com/ZaneDubya/UltimaXNA)
* [ServUO](https://github.com/servuo/servuo)

# Building  
[![Build status](https://ci.appveyor.com/api/projects/status/qvqctcf8oss5bqh8?svg=true)](https://ci.appveyor.com/project/Pack4Duck/classicuo)

Currently, only Windows is supported for building. The binary produced will work on all supported platforms.

You'll need [Visual Studio 2017](https://www.visualstudio.com/downloads/). The free community edition should be fine. Once that
is installed:

- Open ClassicUO.sln in the root of the repository.
- Select "Debug" or "Release" at the top.
- Hit F5 to build. The output will be in the "bin/Release" or "bin/Debug" directory.

# Running

First, create a file in the same directory as ClassicUO.exe named 'settings.json' that contains the following:

~~~
{
  "username": "YOUR_USERNAME",
  "password": "YOUR_PASSWORD",
  "ip": "SERVER_IP",
  "port": SERVER_PORT,
  "lastcharactername": "",
  "ultimaonlinedirectory": "YOUR\\PATH\\TO\\ULTIMAONLINE",
  "clientversion": "YOUR.CLIENT.0.VERSION",
  "maxfps": 144,
  "debug": true,
  "profiler": true,
  "sound": true,
  "sound_volume": 255,
  "music": true,
  "music_volume": 255,
  "footsteps_sounds": false,
  "combat_music": false,
  "background_sound": false,
  "chat_font": 0,
  "enable_pathfind": false,
  "always_run": false,
  "reduce_fps_inactive_window": false,
  "container_default_x": 0,
  "container_default_y": 0,
  "backpack_style": 0,
  "game_window_x": 20,
  "game_window_y": 20,
  "game_window_width": 640,
  "game_window_height": 500,
  "speech_delay": 100,
  "scale_speech_delay": false,
  "speech_color": 0,
  "emote_color": 0,
  "party_message_color": 0,
  "guild_message_color": 0,
  "ally_message_color": 0,
  "innocent_color": 0,
  "friend_color": 0,
  "criminal_color": 0,
  "enemy_color": 0,
  "murderer_color": 0,
  "criminal_action_query": false,
  "show_incoming_names": false,
  "stat_report": false,
  "skill_report": false,
  "use_tooltips": false,
  "delay_appear_tooltips": 2857,
  "tooltips_text_color": 65535,
  "highlight_gameobjects": true,
  "smooth_movement": true,
  "status_gump_style": "modern"
}
~~~

Then, double click ClassicUO.exe and the game will launch!

# Contribute

Everyone is welcome to contribute! The GitHub issues and project tracker are kept up to date with tasks that need work.

# Legal

This work is released under the GPLv3 license. This project does not distribute any copyrighted game assets. In order to run this client you'll need to legally obtain a copy of version 7.0.59.8 or earlier of the Ultima Online Classic Client.

Ultima Online(R) © 2018 Electronic Arts Inc. All Rights Reserved.
