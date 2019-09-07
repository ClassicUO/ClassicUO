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
* Windows
* Linux
* MacOS

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

1. Install Homebrew, a package manager for macOS (if not yet installed):
Follow instructions on https://brew.sh/

2. Install Mono (https://www.mono-project.com/):
`brew install mono`

3. Install Paket, a dependency manager for .NET and mono projects (https://fsprojects.github.io/Paket/):
`brew install paket`

4. Navigate to ClassicUO root folder:
`cd /your/path/to/ClassicUO`

5. Initialize Paket environment:
`paket init`

6. Install required/missing dependencies:
`paket add Newtonsoft.Json --version 12.0.2`

7. Build:
  - Debug version: `msbuild /t:Rebuild`
  - Release version: `msbuild /t:Rebuild /p:Configuration=Release`

8. Start ClassicUO via Mono (to properly set up all required constants use provided bash script):
  - Debug version: `./bin/Debug/ClassicUO-mono.sh`
  - Release version: `./bin/Release/ClassicUO-mono.sh`

Other useful commands:
- `msbuild /t:Clean`
- `msbuild /t:Clean /p:Configuration=Release`
- `msbuild /t:RestorePackages`

# Running
Follow the [Wiki](https://github.com/andreakarasho/ClassicUO/wiki) to setup correctly ClassicUO

# Contribute
Everyone is welcome to contribute! The GitHub issues and project tracker are kept up to date with tasks that need work.

# Legal
The code itself has been written using the following projects as a reference:

* [OrionUO](https://github.com/hotride/orionuo)
* [Razor](https://github.com/msturgill/razor)
* [UltimaXNA](https://github.com/ZaneDubya/UltimaXNA)
* [ServUO](https://github.com/servuo/servuo)

This work is released under the GPLv3 license. This project does not distribute any copyrighted game assets. In order to run this client you'll need to legally obtain a copy of version 7.0.59.8 or earlier of the Ultima Online Classic Client.

Ultima Online(R) © 2019 Electronic Arts Inc. All Rights Reserved.
