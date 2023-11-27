<p align="center"><img src="https://github.com/bittiez/TazUO/assets/3859393/832c4cf3-8525-419b-ad16-3c5f7de1b80c" width="300" height="300"></p>

<p align="center">
    <a href="https://discord.gg/VdyCpjQ">
    <img src="https://img.shields.io/discord/1087124353155608617.svg?logo=discord"
    alt="chat on Discord"></a>
</p>
------------------------------------------------------------------


Release: [![Release](https://github.com/bittiez/TazUO/actions/workflows/build-test.yml/badge.svg?branch=main)](https://github.com/bittiez/TazUO/actions/workflows/build-test.yml)  Dev: [![Dev](https://github.com/bittiez/TazUO/actions/workflows/build-test.yml/badge.svg?branch=dev)](https://github.com/bittiez/TazUO/actions/workflows/build-test.yml)   


Join TazUO's discord for ideas/support/updates -> https://discord.gg/SqwtB5g95H

Check out our [wiki](../../wiki) for details on all of the changes TazUO has made for players!

This version of CUO adds [grid containers](../../wiki/TazUO.Grid-Containers) and many other features to the regular CUO client

    Searchable

    Resizable

    Scrollable

    Can lock items in specific place

    Quick preview for containers inside *if the client has already cached that bag*

    Item scaling!

[Cool down bars](../../wiki/TazUO.Cooldown-bars)

[Follow mode improvements](../../wiki/TazUO.Follow-mode)

[Improved journal](../../wiki/TazUO.Journal)

[Nameplate healthbars](../../wiki/TazUO.Nameplate-Healthbars)

And others in our [wiki](../../wiki)

![Cooldown](https://user-images.githubusercontent.com/3859393/227056224-ef1c6958-fff5-4698-a21a-c63c5814877c.gif)
![SlottedInv](https://user-images.githubusercontent.com/3859393/226514464-32919a68-ebad-4ec0-8bcf-8614a5055f7d.gif)
![Grid Previe](https://user-images.githubusercontent.com/3859393/222873187-c88ad321-8b19-4cfd-9617-7e23b2443b6a.gif)
![image](https://user-images.githubusercontent.com/3859393/222975241-319e5fa6-2c1e-441d-97e6-b04a5e1f6f3b.png)
![Journal](https://user-images.githubusercontent.com/3859393/222942915-e31d26aa-e9a7-41df-9c99-570bcc00d1fb.gif)
![image](https://user-images.githubusercontent.com/3859393/225168130-5ce83950-853d-43ce-9583-65ec4b0ae9d6.png)
![image](https://user-images.githubusercontent.com/3859393/225307385-c8e8014f-9b84-4fe4-a2cd-f33fbeee9563.png)
![image](https://user-images.githubusercontent.com/3859393/226114408-28c6556d-6ba8-43c7-bf1a-079342aaeacd.png)
![image](https://user-images.githubusercontent.com/3859393/226114417-e68b1653-f719-49b3-b799-0beb07e0a211.png)


# Original CUO Readme:
------------------------------------------------------------------

An open source implementation of the Ultima Online Classic Client.

Individuals/hobbyists: support continued maintenance and development via the monthly Patreon:
<br>&nbsp;&nbsp;[![Patreon](https://raw.githubusercontent.com/wiki/ocornut/imgui/web/patreon_02.png)](http://www.patreon.com/classicuo)

Individuals/hobbyists: support continued maintenance and development via PayPal:
<br>&nbsp;&nbsp;[![PayPal](https://www.paypalobjects.com/en_US/i/btn/btn_donate_LG.gif)](https://www.paypal.com/cgi-bin/webscr?cmd=_s-xclick&hosted_button_id=9ZWJBY6MS99D8)

<a href="https://discord.gg/VdyCpjQ">
<img src="https://img.shields.io/discord/458277173208547350.svg?logo=discord"
alt="chat on Discord"></a>

[![GitHub Actions Status](https://github.com/ClassicUO/ClassicUO/workflows/Build-Test/badge.svg)](https://github.com/ClassicUO/ClassicUO/actions)
[![GitHub Actions Status](https://github.com/ClassicUO/ClassicUO/workflows/Deploy/badge.svg)](https://github.com/ClassicUO/ClassicUO/actions)

# Introduction
ClassicUO is an open source implementation of the Ultima Online Classic Client. This client is intended to emulate all standard client versions and is primarily tested against Ultima Online free shards.

The client is currently under heavy development but is functional. The code is based on the [FNA-XNA](https://fna-xna.github.io/) framework. C# is chosen because there is a large community of developers working on Ultima Online server emulators in C#, because FNA-XNA exists and seems reasonably suitable for creating this type of game.

![screenshot_2020-07-06_12-29-02](https://user-images.githubusercontent.com/20810422/208747312-04f6782f-3dc8-4951-b0a0-73d2305bbfca.png)


ClassicUO is natively cross platform and supports:
* Browser [Chrome]
* Windows [DirectX 11, OpenGL, Vulkan]
* Linux   [OpenGL, Vulkan]
* macOS   [Metal, OpenGL, MoltenVK]

# Download & Play!
| Platform | Link |
| --- | --- |
| Browser | [Play!](https://play.classicuo.org) |
| Windows x64 | [Download](https://www.classicuo.eu/launcher/win-x64/ClassicUOLauncher-win-x64-release.zip) |
| Linux x64 | [Download](https://www.classicuo.eu/launcher/linux-x64/ClassicUOLauncher-linux-x64-release.zip) |
| macOS | [Download](https://www.classicuo.eu/launcher/osx/ClassicUOLauncher-osx-x64-release.zip) |

Or visit the [ClassicUO Website](https://www.classicuo.eu/)

# How to build the project

Clone repository with:
```
git config --global url."https://".insteadOf git://
git clone --recursive https://github.com/ClassicUO/ClassicUO.git
```

Build the project:
```
dotnet build -c Release
```

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

Ultima Online(R) © 2022 Electronic Arts Inc. All Rights Reserved.
