<p align="center">
    <img src="https://i.imgur.com/CgpwyIQ.png" width="190" height="200" >
</p>

An open source implementation of the Ultima Online Classic Client.

Individuals/hobbyists: support continued maintenance and development via the monthly Patreon:
<br>&nbsp;&nbsp;[![Patreon](https://raw.githubusercontent.com/wiki/ocornut/imgui/web/patreon_02.png)](http://www.patreon.com/classicuo)

Individuals/hobbyists: support continued maintenance and development via PayPal:
<br>&nbsp;&nbsp;[![PayPal](https://www.paypalobjects.com/en_US/i/btn/btn_donate_LG.gif)](https://www.paypal.com/cgi-bin/webscr?cmd=_s-xclick&hosted_button_id=9ZWJBY6MS99D8)

<a href="https://discord.gg/VdyCpjQ">
<img src="https://img.shields.io/discord/458277173208547350.svg?logo=discord"
alt="chat on Discord"></a>

[![Build status](https://ci.appveyor.com/api/projects/status/qvqctcf8oss5bqh8?svg=true)](https://ci.appveyor.com/project/andreakarasho/classicuo)
[![GitHub Actions Status](https://github.com/ClassicUO/ClassicUO/workflows/Build/badge.svg)](https://github.com/ClassicUO/ClassicUO/actions)

# Introduction
ClassicUO is an open source implementation of the Ultima Online Classic Client. This client is intended to emulate all standard client versions and is primarily tested against Ultima Online free shards.

The client is currently under heavy development but is functional. The code is based on the [FNA-XNA](https://fna-xna.github.io/) framework. C# is chosen because there is a large community of developers working on Ultima Online server emulators in C#, because FNA-XNA exists and seems reasonably suitable for creating this type of game.

ClassicUO is natively cross platform and supports:
* Windows [DirectX 11, OpenGL, Vulkan]
* Linux   [OpenGL, Vulkan]
* macOS   [Metal, OpenGL, MoltenVK]

# Download & Play!
| Platform | Link |
| --- | --- |
| Windows x64 | [Download](https://www.classicuo.eu/launcher/win-x64/ClassicUOLauncher-win-x64-release.zip) |
| Linux x64 | [Download](https://www.classicuo.eu/launcher/linux-x64/ClassicUOLauncher-linux-x64-release.zip) |
| macOS | [Download](https://www.classicuo.eu/launcher/osx/ClassicUOLauncher-osx-x64-release.zip) |

Or visit the [ClassicUO Website](https://www.classicuo.eu/)

# How to build the project

Clone repository with:
```
git clone --recursive https://github.com/andreakarasho/ClassicUO.git
```

### Windows
The binary produced will work on all supported platforms.

You'll need [Visual Studio 2019](https://www.visualstudio.com/downloads/). The free community edition should be fine. Once that
is installed:

- Open ClassicUO.sln from the root of the repository.

- Select "Debug" or "Release" at the top.

- Hit F5 to build. The output will be in the "bin/Release" or "bin/Debug" directory.

# Linux

- Open a terminal and enter the following commands.

## Ubuntu
![Ubuntu](https://assets.ubuntu.com/v1/ad9a02ac-ubuntu-orange.gif)
```bash
sudo apt update
sudo apt install dirmngr gnupg apt-transport-https ca-certificates software-properties-common lsb-release
sudo apt-key adv --keyserver hkp://keyserver.ubuntu.com:80 --recv-keys 3FA7E0328081BFF6A14DA29AA6A19B38D3D831EF
```

```
sudo apt-add-repository "deb https://download.mono-project.com/repo/ubuntu stable-`lsb_release -sc` main"
```

Check signature
```
gpg: key A6A19B38D3D831EF: public key "Xamarin Public Jenkins (auto-signing) <releng@xamarin.com>" imported
gpg: Total number processed: 1
gpg:               imported: 1
```
```bash
sudo apt install mono-complete
```

## Fedora
![Fedora](https://fedoraproject.org/w/uploads/thumb/3/3c/Fedora_logo.png/150px-Fedora_logo.png)

### Fedora 29
```bash
rpm --import "https://keyserver.ubuntu.com/pks/lookup?op=get&search=0x3FA7E0328081BFF6A14DA29AA6A19B38D3D831EF"
su -c 'curl https://download.mono-project.com/repo/centos8-stable.repo | tee /etc/yum.repos.d/mono-centos8-stable.repo'
dnf update
```

### Fedora 28
```bash
rpm --import "https://keyserver.ubuntu.com/pks/lookup?op=get&search=0x3FA7E0328081BFF6A14DA29AA6A19B38D3D831EF"
su -c 'curl https://download.mono-project.com/repo/centos7-stable.repo | tee /etc/yum.repos.d/mono-centos7-stable.repo'
dnf update
```

```bash
sudo dnf install mono-devel
```

## ArchLinux
![ArchLinux](https://www.archlinux.org/static/logos/archlinux-logo-dark-scalable.518881f04ca9.svg)

```bash
sudo pacman -S mono mono-tools
```

Verify
```bash
mono --version
```
```
Mono JIT compiler version 6.6.0.161 (tarball Tue Dec 10 10:36:32 UTC 2019)
Copyright (C) 2002-2014 Novell, Inc, Xamarin Inc and Contributors. www.mono-project.com
    TLS:           __thread
    SIGSEGV:       altstack
    Notifications: epoll
    Architecture:  amd64
    Disabled:      none
    Misc:          softdebug
    Interpreter:   yes
    LLVM:          yes(610)
    Suspend:       hybrid
    GC:            sgen (concurrent by default)
```

- Navigate to ClassicUO scripts folder:
  `cd /your/path/to/ClassicUO/scripts`

- Execute `build.sh` script. If you want build a debug version of ClassicUO just pass "debug" as argument like: `./build.sh debug`.
  Probably you have to set the `build.sh` file executable with with the command `chmod -x build.sh`

- Navigate to `/your/path/to/ClassicUO/bin/[Debug or Release]`


### macOS
All the commands should be executed in terminal. All global package installs should be done only if not yet installed.

- Install Homebrew, a package manager for macOS (if not yet installed):
  Follow instructions on https://brew.sh/

- Install Mono (https://www.mono-project.com/):
  `brew install mono`

- Install NuGet, a package manager for .NET (https://docs.microsoft.com/en-us/nuget/):
  `brew install nuget`

- Navigate to ClassicUO root folder:
  `cd /your/path/to/ClassicUO`

- Restore packages (https://docs.microsoft.com/en-us/nuget/reference/cli-reference/cli-ref-restore):
  `nuget restore`

- Build:
  - Debug version: `msbuild /t:Rebuild /p:Configuration=Debug`
  - Release version: `msbuild /t:Rebuild /p:Configuration=Release`

- Run ClassicUO via Mono:
  - Debug version: `DYLD_LIBRARY_PATH=./bin/Debug/osx/ mono ./bin/Debug/ClassicUO.exe`
  - Release version: `DYLD_LIBRARY_PATH=./bin/Release/osx/ mono ./bin/Release/ClassicUO.exe`

After the first run, ignore the error message and a new file called `settings.json` will be automatically created in the directory that contains ClassicUO.exe.

Other useful commands:
- `msbuild /t:Clean /p:Configuration=Debug`
- `msbuild /t:Clean /p:Configuration=Release`

# Contribute
Everyone is welcome to contribute! The GitHub issues and project tracker are kept up to date with tasks that need work.

# Legal
The code itself has been written using the following projects as a reference:

* [OrionUO](https://github.com/hotride/orionuo)
* [Razor](https://github.com/msturgill/razor)
* [UltimaXNA](https://github.com/ZaneDubya/UltimaXNA)
* [ServUO](https://github.com/servuo/servuo)

Backend:
* [FNA](https://github.com/FNA-XNA/FNA)

This work is released under the BSD 4 license. This project does not distribute any copyrighted game assets. In order to run this client you'll need to legally obtain a copy of the Ultima Online Classic Client.
Using a custom client to connect to official UO servers is strictly forbidden. We do not assume any responsibility of the usage of this client.

Ultima Online(R) © 2020 Electronic Arts Inc. All Rights Reserved.
