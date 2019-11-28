<p align="center">
  <img width="300" height="320" src="https://i.imgur.com/CgpwyIQ.png">
</p>

An open source implementation of the Ultima Online Classic Client.

#### Paypal

[![PayPal](https://img.shields.io/badge/paypal-donate-yellow.svg)](https://www.paypal.me/muskara)

#### Discord

 <a href="https://discord.gg/VdyCpjQ">
        <img src="https://img.shields.io/discord/458277173208547350.svg?logo=discord"
            alt="chat on Discord"></a>

#### Current release

[![Build status](https://ci.appveyor.com/api/projects/status/qvqctcf8oss5bqh8?svg=true)](https://ci.appveyor.com/project/andreakarasho/classicuo)

# Introduction

ClassicUO is an open source implementation of the Ultima Online Classic Client. This client is intended to emulate all standard client versions and is primarily tested against Ultima Online free shards. This client will not work on the official game shards at the moment.

The client is currently under heavy development but is functional. The code is based on the [FNA-XNA](https://fna-xna.github.io/) framework. C# is chosen because there is a large community of developers working on Ultima Online server emulators in C#, because FNA-XNA exists and seems reasonably suitable for creating this type of game.

ClassicUO is natively cross platform and supports:

- Windows
- Linux
- MacOS

# Running

Follow the [Wiki](https://github.com/andreakarasho/ClassicUO/wiki) to setup correctly ClassicUO

# Building (Windows)

The binary produced will work on all supported platforms.

You'll need [Visual Studio 2019](https://www.visualstudio.com/downloads/). The free community edition should be fine. Once that
is installed:

1. Open ClassicUO.sln in the root of the repository.

2. Select "Debug" or "Release" at the top.

3. Hit F5 to build. The output will be in the "bin/Release" or "bin/Debug" directory.

# Building (Linux)

Open a terminal instance and put the following commands:

1. `sudo apt-get install mono-complete`

2. `sudo apt-get install monodevelop`

3. Select "Debug" or "Release" at the top.

4. Hit F5 to build. The output will be in the "bin/Release" or "bin/Debug" directory.

# Building (macOS)

All the commands should be executed in terminal. All global package installs should be done only if not yet installed.

1. Install Homebrew, a package manager for macOS:
   Follow instructions on https://brew.sh/

2. Install Mono, a cross platform, open source .NET framework (https://www.mono-project.com/):
   `brew install mono`

3. Install NuGet, a package manager for .NET (https://www.nuget.org/):
   `brew install nuget`

4. Navigate to your ClassicUO root folder:
   `cd /your/path/to/ClassicUO`

5. Restore required packages:
   `nuget restore`

6. Build:

- Debug version: `msbuild /t:Rebuild`
- Release version: `msbuild /t:Rebuild /p:Configuration=Release`

8. Start ClassicUO via Mono (to properly set up all required constants use provided bash script):

- Debug version: `./bin/Debug/ClassicUO-mono.sh`
- Release version: `./bin/Release/ClassicUO-mono.sh`

X. [Optional] If you want to run a debugger for .NET (in VS Code, for example), install .NET SDK:
`brew cask install dotnet-sdk`

# Mononetworking problems (macOS)

You're going to want to download mono 6.0 for macOS with this link: https://download.mono-project.com/archive/6.0.0/macos-10-universal/MonoFramework-MDK-6.0.0.172.macos10.xamarin.universal.pkg

After that, you're going to need to install Homebrew (if you don't already have it installed), just google homebrew installation, and you'll find something online.

you're going to need to install the package: (this is related to sound errors)

```
brew install sdl2
```

If you're lucky, and have done everything above and your client works, pat yourself on the back - you're lucky.
If not, and you get an error like this
**System.Net.Sockets.SocketException (0x80004005): Could not resolve host 'YOUR_LOCAL_MAC_NAME.local'**

then the reason you're getting this, is because your macs local name isn't registered in /etc/hosts

you're going to want to run:

```
cat /etc/hosts
```

and you should get an output similar to:

```
127.0.0.1       localhost
255.255.255.255 broadcasthost
::1             localhost
```

You need to find the name of your local user, so go to system preferences > sharing, then at the top, you will find your user local which should end in ".local", remember this whole string, we will have to write it right now.

next we want to run this file in vim (sudo because it is in root user directory)

```
sudo vim /etc/hosts
```

you use the arrow keys to move around, and you want to press "i" to insert text, so we are going to insert a new entry (i just put mine after the first localhost)

```
127.0.0.1 YOUR_LOCAL_MAC_NAME.local
```

make sure you press tab after the local ip, so you can format it like the rest of the document.

after you are done INSERTING text, you wanna go ahead, hit escape, type :w then hit enter then :q then hit enter again. you are ready to run UO Outlands.

just run in terminal

```
mono UO_CLASSIC_DIRECTORY/ClassicUO.exe
```

and you should have no issues, the client should start.

# Contribute

Everyone is welcome to contribute! The GitHub issues and project tracker are kept up to date with tasks that need work.

# Legal

The code itself has been written using the following projects as a reference:

- [OrionUO](https://github.com/hotride/orionuo)
- [Razor](https://github.com/msturgill/razor)
- [UltimaXNA](https://github.com/ZaneDubya/UltimaXNA)
- [ServUO](https://github.com/servuo/servuo)

This work is released under the GPLv3 license. This project does not distribute any copyrighted game assets. In order to run this client you'll need to legally obtain a copy of version 7.0.59.8 or earlier of the Ultima Online Classic Client.

Ultima Online(R) © 2019 Electronic Arts Inc. All Rights Reserved.
