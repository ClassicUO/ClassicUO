ClassicUO - an open source implementation of the Ultima Online Classic Client.

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
    Username = "YOURUSERNAME",
    Password = "YOURPASSWORD",
    LastCharacterName = "",
    IP = "YOURSERVER",
    Port = 2593,
    UltimaOnlineDirectory = "YOURUODIRECTORY",
    ClientVersion = "7.0.59.8"
};
~~~

Then, double click ClassicUO.exe and the game will launch!

# Contribute

Everyone is welcome to contribute! The GitHub issues and project tracker are kept up to date with tasks that need work.

# Legal

This work is released under the GPLv3 license. This project does not distribute any copyrighted game assets. In order to run this client you'll need to legally obtain a copy of version 7.0.59.8 or earlier of the Ultima Online Classic Client.

Ultima Online(R) © 2018 Electronic Arts Inc. All Rights Reserved.
