# ClassicUO

<img src="https://user-images.githubusercontent.com/4610892/46581209-fdcd4b00-ca34-11e8-8560-c6c1e08b8463.jpg" width="350">

## What is ClassicUO?
UOClassic is an alternative client for the MMORPG Ultima Online. The goal of the development is to give players a unique and fluid gaming experience with the focus on speed and performance. In addition, the client should address the broad masses of players because it is platform independent, is constantly developed by an experienced team and listen to the opinions of the players.

<img src="https://user-images.githubusercontent.com/4610892/46581321-c8c1f800-ca36-11e8-8c31-571134d7ce27.png" width="800">
ClassicUO Ultima Online Client - Image taken on UOForever (UOF)

## So what paltforms is ClassicUO made for?
In order to provide a platform independence, modern technologies are used, which are represented on the various system types, including:
* Windows
* Linux
* MacOS

## Technologies
We rely on classic and modern technologies, which are updated constantly or as needed to the current state. Main technologies are for example:
* Mono
* FNA

# Play
The development is still in the status of Prealpha. It means there is no official and playable version yet. In the coming weeks, however, a group of people will be invited to the test.

# Contribute
Would you like to support us in the development of the alternative client ClassicUO for the game Ultima Online? Feel free to contact us to join the core team. Alternatively, you can of course help with programming and create your own pull request. As a rule, our team will answer within 1-10 hours.

### Build for Visual Studio (2017)
Please notice that you should keep VS up to date. 

1. Clone repository
2. Open the project file
3. Build the project
4. If you start it for the first time, please comment in following lines. ClassicUO -> GameLoop.cs
```
Configuration.Settings settings1 = new Configuration.Settings()
            {
                Username = "",
                Password = "",
                LastCharacterName = "",
                IP = "",
                Port = 2599,
                UltimaOnlineDirectory = "",
                ClientVersion = "7.0.59.8"
            };

            Configuration.ConfigurationResolver.Save(settings1, "settings.json");
```
5. Select "ClassicUO" to run
6. Navigate to "Debug" folder and edit "settings.json". Here is an example:
```
{
  "username": "YOURUSERNAME",
  "password": "YOURPASSWORD",
  "ip": "YOURSERVER",
  "port": "YOURSERVERPORT",
  "lastcharactername": "",
  "ultimaonlinedirectory": "YOURUODIRECTORY",
  "clientversion": "7.0.59.8",
  "maxfps": 144,
  "debug": true,
  "profiler": true,
  "sound": false,
  "sound_volume": 0,
  "music": false,
  "music_volume": 0,
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
  "game_window_x": 0,
  "game_window_y": 0,
  "game_window_width": 800,
  "game_window_height": 600,
  "speech_delay": 0,
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
  "skill_report": false
}
```

7. Enjoy
