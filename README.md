# ClassicUO
 
![alt text](https://serving.photos.photobox.com/16802059d3745e0750d2b1d054284d0e8bd13156ba39240b1c8a37658eb68c2f89b769c1.jpg)
Image of ClassicUO on UOForever (UOF)

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
