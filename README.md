# ClassicUO

![alt text](https://serving.photos.photobox.com/16802059d3745e0750d2b1d054284d0e8bd13156ba39240b1c8a37658eb68c2f89b769c1.jpg)
Image of ClassicUO on UOForever (UOF)

# What is ClassicUO?
UOClassic is an alternative client for the MMORPG Ultima Online. The goal of the development is to give players a unique and fluid gaming experience with the focus on speed and performance. In addition, the client should address the broad masses of players because it is platform independent, is constantly developed by an experienced team and listen to the opinions of the players.

# So what paltforms is ClassicUO made for?
In order to provide a platform independence, modern technologies are used, which are represented on the various system types, including:
* Windows
* Linux
* MacOS

# Technologies
We rely on classic and modern technologies, which are updated constantly or as needed to the current state. Main technologies are for example:
* .Net Core
* FNA


## Contribute

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
6. Navigate to "Debug" folder and edit "settings.json"
7. Enjoy
