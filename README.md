# ClassicUO

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
