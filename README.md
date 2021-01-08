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
[![GitHub Actions Status](https://github.com/andreakarasho/ClassicUO/workflows/Build/badge.svg)](https://github.com/andreakarasho/ClassicUO/actions)

# Project dust765
This project is to address a problem constructed within the toxicity of this community. This is to show the community, open source projects are not meant for cliques and high school drama but rather the expansion of something greater, innovation. -A penny for your thoughts, the adder that prays beneath the rose.

# Features

Most features in the options or macros need no explanation.

Razor lasttarget string - set this to the same lasttarget overhead string as set in Razor, so the ClassicUO lasttarget will be the same as in Razor.

While there is a toggle in options for offscreen targeting, it has no use and is always enabled.

# UCC UI

Please specify the correct settings to make theese properly work!

UI UCC AL - Is an autoloot feature / UI. Only works with GridLoot enabled. You can add items to the txt file created in your /Data/Client folder. Recommendation is to set high value items to the autolootlist.txt (items you potentialy would go gray for) and low value items to autolootlistlow. If you check SL (SafeLoot (available for both lists)), items will ONLY be auto looted if you have looting rights. Loot Above ID adds all items to the loot list higher than X, so you dont have to add hundreds of items to the list.

UI UCC LINES - Draws a line to HUMANS on your screen.

UI UCC BUFFBAR - Provides a visible timer for next swing and other stuff. You can enable lines individually enable them and also lock the UI to prevent moving it. There is a txt in /Data/Client to modify the timers for weapons. It does NOT change calculation with SSI or the like.

UI UCC SELF - Is an Automation feature to bandaid yourself, use pouches and pots and auto rearms a weapon after being disarmed. 

Checkboxes on the UI

Rearm Pot - Auto rearm after pot. 

Armorer Guild -  Auto rearm after being disarmed.

Thiefes Guild - Disables any actions when hidden.

Mages Guild - Disables any actions when a spellcursor is up.


# Added files
/src/ally.png

/src/enemy.png

/src/halo

/src/Game/InteropServices/Runtime folder

# Changed files and line number

no comment possible:

/src/Properties/launchSettings.json	5

/src/Properties/launchSettings.json	6

comments:

/src/ClassicUO.csproj	65

/src/ClassicUO.csproj	79

/src/Configuration/Profile.cs	272

/src/Configuration/Profile.cs	424

/src/Configuration/Profile.cs	430

/src/Configuration/Profile.cs	796

/src/Configuration/Profile.cs	801

/src/Configuration/Profile.cs	241

/src/Configuration/Profile.cs	35

/src/Game/Constants.cs	118

/src/Game/Constants.cs	31

/src/Game/Constants.cs	78

/src/Game/GameActions.cs	45

/src/Game/GameActions.cs	47

/src/Game/GameActions.cs	580

/src/Game/GameActions.cs	583

/src/Game/GameActions.cs	594

/src/Game/GameActions.cs	597

/src/Game/GameCursor.cs	28

/src/Game/GameCursor.cs	30

/src/Game/GameCursor.cs	78

/src/Game/GameCursor.cs	84

/src/Game/GameCursor.cs	554

/src/Game/GameCursor.cs	569

/src/Game/GameObjects/Entity.cs	62

/src/Game/GameObjects/Entity.cs	69

/src/Game/GameObjects/Entity.cs	84

/src/Game/GameObjects/Entity.cs	86

/src/Game/GameObjects/Entity.cs	360

/src/Game/GameObjects/Entity.cs	369

/src/Game/GameObjects/Entity.cs	384

/src/Game/GameObjects/Entity.cs	389

/src/Game/GameObjects/EntityCollection.cs	54

/src/Game/GameObjects/EntityCollection.cs	58

/src/Game/GameObjects/Item.cs	26

/src/Game/GameObjects/Item.cs	29

/src/Game/GameObjects/Item.cs	94

/src/Game/GameObjects/Item.cs	96

/src/Game/GameObjects/Item.cs	128

/src/Game/GameObjects/Item.cs	148

/src/Game/GameObjects/Item.cs	1055

/src/Game/GameObjects/Item.cs	1060

/src/Game/GameObjects/Item.cs	120

/src/Game/GameObjects/Mobile.cs	127

/src/Game/GameObjects/Mobile.cs	136

/src/Game/GameObjects/PlayerMobile.cs	28

/src/Game/GameObjects/PlayerMobile.cs	30

/src/Game/GameObjects/PlayerMobile.cs	45

/src/Game/GameObjects/PlayerMobile.cs	47

/src/Game/GameObjects/PlayerMobile.cs	53

/src/Game/GameObjects/PlayerMobile.cs	55

/src/Game/GameObjects/PlayerMobile.cs	1364

/src/Game/GameObjects/PlayerMobile.cs	1410

/src/Game/GameObjects/Static.cs	47

/src/Game/GameObjects/Static.cs	49

/src/Game/GameObjects/Static.cs	62

/src/Game/GameObjects/Static.cs	64

/src/Game/GameObjects/Static.cs	104

/src/Game/GameObjects/Static.cs	109

/src/Game/GameObjects/Static.cs	60

/src/Game/GameObjects/Views/ItemView.cs	27

/src/Game/GameObjects/Views/ItemView.cs	29

/src/Game/GameObjects/Views/ItemView.cs	78

/src/Game/GameObjects/Views/ItemView.cs	84

/src/Game/GameObjects/Views/MobileView.cs	29

/src/Game/GameObjects/Views/MobileView.cs	31

/src/Game/GameObjects/Views/MobileView.cs	50

/src/Game/GameObjects/Views/MobileView.cs	55

/src/Game/GameObjects/Views/MobileView.cs	80

/src/Game/GameObjects/Views/MobileView.cs	87

/src/Game/GameObjects/Views/MobileView.cs	113

/src/Game/GameObjects/Views/MobileView.cs	123

/src/Game/GameObjects/Views/MobileView.cs	185

/src/Game/GameObjects/Views/MobileView.cs	204

/src/Game/GameObjects/Views/MobileView.cs	325

/src/Game/GameObjects/Views/MobileView.cs	331

/src/Game/GameObjects/Views/MobileView.cs	585

/src/Game/GameObjects/Views/MobileView.cs	591

/src/Game/GameObjects/Views/MobileView.cs	109

/src/Game/GameObjects/Views/MovingEffectView.cs	25

/src/Game/GameObjects/Views/MovingEffectView.cs	27

/src/Game/GameObjects/Views/MovingEffectView.cs	45

/src/Game/GameObjects/Views/MovingEffectView.cs	54

/src/Game/GameObjects/Views/MultiView.cs	25

/src/Game/GameObjects/Views/MultiView.cs	27

/src/Game/GameObjects/Views/MultiView.cs	120

/src/Game/GameObjects/Views/MultiView.cs	147

/src/Game/GameObjects/Views/StaticView.cs	25

/src/Game/GameObjects/Views/StaticView.cs	27

/src/Game/GameObjects/Views/StaticView.cs	87

/src/Game/GameObjects/Views/StaticView.cs	114

/src/Game/GameObjects/Views/StaticView.cs	128

/src/Game/GameObjects/Views/StaticView.cs	115

/src/Game/GameObjects/Views/StaticView.cs	124

/src/Game/GameObjects/Views/TileView.cs	25

/src/Game/GameObjects/Views/TileView.cs	27

/src/Game/GameObjects/Views/TileView.cs	70

/src/Game/GameObjects/Views/TileView.cs	105

/src/Game/GameObjects/Views/TileView.cs	119

/src/Game/GameObjects/Views/TileView.cs	136

/src/Game/GameObjects/Views/TileView.cs	147

/src/Game/GameObjects/Views/TileView.cs	156

/src/Game/GameObjects/Views/TileView.cs	111

/src/Game/GameObjects/Views/TileView.cs	145

/src/Game/GameObjects/Views/View.cs	151

/src/Game/GameObjects/Views/View.cs	191

/src/Game/Managers/CommandManager.cs	26

/src/Game/Managers/CommandManager.cs	28

/src/Game/Managers/EffectManager.cs	33

/src/Game/Managers/EffectManager.cs	35

/src/Game/Managers/EffectManager.cs	37

/src/Game/Managers/HealthLinesManager.cs	25

/src/Game/Managers/HealthLinesManager.cs	28

/src/Game/Managers/HealthLinesManager.cs	48

/src/Game/Managers/HealthLinesManager.cs	73

/src/Game/Managers/HealthLinesManager.cs	192

/src/Game/Managers/HealthLinesManager.cs	208

/src/Game/Managers/HealthLinesManager.cs	238

/src/Game/Managers/HealthLinesManager.cs	247

/src/Game/Managers/HealthLinesManager.cs	249

/src/Game/Managers/HealthLinesManager.cs	260

/src/Game/Managers/HealthLinesManager.cs	293

/src/Game/Managers/HealthLinesManager.cs	302

/src/Game/Managers/HealthLinesManager.cs	510

/src/Game/Managers/HealthLinesManager.cs	455

/src/Game/Managers/HealthLinesManager.cs	382

/src/Game/Managers/HealthLinesManager.cs	236

/src/Game/Managers/HealthLinesManager.cs	291

/src/Game/Managers/MacroManager.cs	32

/src/Game/Managers/MacroManager.cs	37

/src/Game/Managers/MacroManager.cs	1341

/src/Game/Managers/MacroManager.cs	1345

/src/Game/Managers/MacroManager.cs	1355

/src/Game/Managers/MacroManager.cs	1357

/src/Game/Managers/MacroManager.cs	1475

/src/Game/Managers/MacroManager.cs	1486

/src/Game/Managers/MacroManager.cs	1534

/src/Game/Managers/MacroManager.cs	1892

/src/Game/Managers/MacroManager.cs	2233

/src/Game/Managers/MacroManager.cs	2235

/src/Game/Managers/MacroManager.cs	2366

/src/Game/Managers/MacroManager.cs	2397

/src/Game/Managers/MacroManager.cs	2638

/src/Game/Managers/MacroManager.cs	2678

/src/Game/Managers/TargetManager.cs	26

/src/Game/Managers/TargetManager.cs	28

/src/Game/Managers/TargetManager.cs	48

/src/Game/Managers/TargetManager.cs	50

/src/Game/Managers/TargetManager.cs	156

/src/Game/Managers/TargetManager.cs	159

/src/Game/Managers/TargetManager.cs	183

/src/Game/Managers/TargetManager.cs	185

/src/Game/Managers/TargetManager.cs	221

/src/Game/Managers/TargetManager.cs	224

/src/Game/Managers/TargetManager.cs	382

/src/Game/Managers/TargetManager.cs	390

/src/Game/Managers/TargetManager.cs	392

/src/Game/Managers/TargetManager.cs	407

/src/Game/Managers/UseItemQueue.cs	40

/src/Game/Managers/UseItemQueue.cs	42

/src/Game/Managers/UseItemQueue.cs	51

/src/Game/Managers/UseItemQueue.cs	53

/src/Game/Managers/UseItemQueue.cs	38

/src/Game/Managers/UseItemQueue.cs	49

/src/Game/Map/Chunk.cs	26

/src/Game/Map/Chunk.cs	28

/src/Game/Map/Chunk.cs	131

/src/Game/Map/Chunk.cs	134

/src/Game/Scenes/GameScene.cs	29

/src/Game/Scenes/GameScene.cs	32

/src/Game/Scenes/GameScene.cs	85

/src/Game/Scenes/GameScene.cs	88

/src/Game/Scenes/GameScene.cs	160

/src/Game/Scenes/GameScene.cs	163

/src/Game/Scenes/GameScene.cs	204

/src/Game/Scenes/GameScene.cs	245

/src/Game/Scenes/GameScene.cs	250

/src/Game/Scenes/GameScene.cs	252

/src/Game/Scenes/GameScene.cs	359

/src/Game/Scenes/GameScene.cs	366

/src/Game/Scenes/GameScene.cs	387

/src/Game/Scenes/GameScene.cs	395

/src/Game/Scenes/GameScene.cs	1168

/src/Game/Scenes/GameScene.cs	1171

/src/Game/Scenes/GameSceneDrawingSorting.cs	26

/src/Game/Scenes/GameSceneDrawingSorting.cs	28

/src/Game/Scenes/GameSceneDrawingSorting.cs	397

/src/Game/Scenes/GameSceneDrawingSorting.cs	405

/src/Game/Scenes/GameSceneDrawingSorting.cs	388

/src/Game/Scenes/GameSceneInputHandler.cs	27

/src/Game/Scenes/GameSceneInputHandler.cs	29

/src/Game/Scenes/GameSceneInputHandler.cs	496

/src/Game/Scenes/GameSceneInputHandler.cs	498

/src/Game/Scenes/GameSceneInputHandler.cs	837

/src/Game/Scenes/GameSceneInputHandler.cs	840

/src/Game/Scenes/GameSceneInputHandler.cs	1276

/src/Game/Scenes/GameSceneInputHandler.cs	1287

/src/Game/UI/Controls/Control.cs	27

/src/Game/UI/Controls/Control.cs	29

/src/Game/UI/Controls/Control.cs	74

/src/Game/UI/Controls/Control.cs	77

/src/Game/UI/Controls/Control.cs	366

/src/Game/UI/Gumps/GridLootGump.cs	26

/src/Game/UI/Gumps/GridLootGump.cs	28

/src/Game/UI/Gumps/GridLootGump.cs	88

/src/Game/UI/Gumps/GridLootGump.cs	106

/src/Game/UI/Gumps/GridLootGump.cs	190

/src/Game/UI/Gumps/GridLootGump.cs	197

/src/Game/UI/Gumps/GumpType.cs	28

/src/Game/UI/Gumps/GumpType.cs	30

/src/Game/UI/Gumps/HealthBarGump.cs	29

/src/Game/UI/Gumps/HealthBarGump.cs	31

/src/Game/UI/Gumps/HealthBarGump.cs	49

/src/Game/UI/Gumps/HealthBarGump.cs	51

/src/Game/UI/Gumps/HealthBarGump.cs	67

/src/Game/UI/Gumps/HealthBarGump.cs	69

/src/Game/UI/Gumps/HealthBarGump.cs	102

/src/Game/UI/Gumps/HealthBarGump.cs	104

/src/Game/UI/Gumps/HealthBarGump.cs	116

/src/Game/UI/Gumps/HealthBarGump.cs	119

/src/Game/UI/Gumps/HealthBarGump.cs	205

/src/Game/UI/Gumps/HealthBarGump.cs	207

/src/Game/UI/Gumps/HealthBarGump.cs	215

/src/Game/UI/Gumps/HealthBarGump.cs	263

/src/Game/UI/Gumps/HealthBarGump.cs	266

/src/Game/UI/Gumps/HealthBarGump.cs	276

/src/Game/UI/Gumps/HealthBarGump.cs	410

/src/Game/UI/Gumps/HealthBarGump.cs	414

/src/Game/UI/Gumps/HealthBarGump.cs	451

/src/Game/UI/Gumps/HealthBarGump.cs	453

/src/Game/UI/Gumps/HealthBarGump.cs	545

/src/Game/UI/Gumps/HealthBarGump.cs	548

/src/Game/UI/Gumps/HealthBarGump.cs	604

/src/Game/UI/Gumps/HealthBarGump.cs	639

/src/Game/UI/Gumps/HealthBarGump.cs	672

/src/Game/UI/Gumps/HealthBarGump.cs	675

/src/Game/UI/Gumps/HealthBarGump.cs	686

/src/Game/UI/Gumps/HealthBarGump.cs	689

/src/Game/UI/Gumps/HealthBarGump.cs	714

/src/Game/UI/Gumps/HealthBarGump.cs	791

/src/Game/UI/Gumps/HealthBarGump.cs	852

/src/Game/UI/Gumps/HealthBarGump.cs	894

/src/Game/UI/Gumps/HealthBarGump.cs	1085

/src/Game/UI/Gumps/HealthBarGump.cs	1090

/src/Game/UI/Gumps/HealthBarGump.cs	1198

/src/Game/UI/Gumps/HealthBarGump.cs	1203

/src/Game/UI/Gumps/HealthBarGump.cs	1275

/src/Game/UI/Gumps/HealthBarGump.cs	1280

/src/Game/UI/Gumps/HealthBarGump.cs	1303

/src/Game/UI/Gumps/HealthBarGump.cs	1316

/src/Game/UI/Gumps/HealthBarGump.cs	1334

/src/Game/UI/Gumps/HealthBarGump.cs	1337

/src/Game/UI/Gumps/HealthBarGump.cs	1457

/src/Game/UI/Gumps/HealthBarGump.cs	1459

/src/Game/UI/Gumps/HealthBarGump.cs	1614

/src/Game/UI/Gumps/HealthBarGump.cs	1627

/src/Game/UI/Gumps/HealthBarGump.cs	1782

/src/Game/UI/Gumps/HealthBarGump.cs	1817

/src/Game/UI/Gumps/HealthBarGump.cs	1933

/src/Game/UI/Gumps/HealthBarGump.cs	1937

/src/Game/UI/Gumps/HealthBarGump.cs	693

/src/Game/UI/Gumps/HealthBarGump.cs	203

/src/Game/UI/Gumps/HealthBarGump.cs	213

/src/Game/UI/Gumps/HealthBarGump.cs	259

/src/Game/UI/Gumps/Login/LoginGump.cs	343

/src/Game/UI/Gumps/Login/LoginGump.cs	353

/src/Game/UI/Gumps/OptionsGump.cs	31

/src/Game/UI/Gumps/OptionsGump.cs	33

/src/Game/UI/Gumps/OptionsGump.cs	221

/src/Game/UI/Gumps/OptionsGump.cs	250

/src/Game/UI/Gumps/OptionsGump.cs	343

/src/Game/UI/Gumps/OptionsGump.cs	347

/src/Game/UI/Gumps/OptionsGump.cs	404

/src/Game/UI/Gumps/OptionsGump.cs	408

/src/Game/UI/Gumps/OptionsGump.cs	2137

/src/Game/UI/Gumps/OptionsGump.cs	3580

/src/Game/UI/Gumps/OptionsGump.cs	3651

/src/Game/UI/Gumps/OptionsGump.cs	4374

/src/Game/UI/Gumps/OptionsGump.cs	4629

/src/Game/UI/Gumps/OptionsGump.cs	4632

/src/Game/UI/Gumps/OptionsGump.cs	4659

/src/Game/UI/Gumps/OptionsGump.cs	3896

/src/Game/UI/Gumps/OptionsGump.cs	3904

/src/Game/UI/Gumps/OptionsGump.cs	885

/src/Game/UI/Gumps/OptionsGump.cs	893

/src/Game/UI/Gumps/OptionsGump.cs	3649

/src/Game/UI/Gumps/OptionsGump.cs	127

/src/Game/UI/Gumps/OptionsGump.cs	129

/src/Game/UI/Gumps/SystemChatControl.cs	789

/src/Game/UI/Gumps/SystemChatControl.cs	801

/src/Game/UI/Gumps/WorldMapGump.cs	55

/src/Game/UI/Gumps/WorldMapGump.cs	66

/src/Game/UI/Gumps/WorldMapGump.cs	1680

/src/Game/UI/Gumps/WorldMapGump.cs	1764

/src/Game/World.cs	26

/src/Game/World.cs	28

/src/Game/World.cs	82

/src/Game/World.cs	85

/src/Game/World.cs	324

/src/Game/World.cs	326

/src/IO/Resources/ArtLoader.cs	135

/src/IO/Resources/ArtLoader.cs	161

/src/IO/Resources/ArtLoader.cs	294

/src/IO/Resources/ArtLoader.cs	297

/src/IO/Resources/ArtLoader.cs	299

/src/IO/Resources/ArtLoader.cs	427

/src/IO/Resources/ArtLoader.cs	498

/src/IO/Resources/TexmapsLoader.cs	27

/src/IO/Resources/TexmapsLoader.cs	29

/src/IO/Resources/TexmapsLoader.cs	198

/src/IO/Resources/TexmapsLoader.cs	211

/src/IO/Resources/TexmapsLoader.cs	221

/src/IO/Resources/TexmapsLoader.cs	305

/src/IO/Resources/TexmapsLoader.cs	222

/src/Main.cs	361

/src/Main.cs	363

/src/Main.cs	365

/src/Network/PacketHandlers.cs	30

/src/Network/PacketHandlers.cs	33

/src/Network/PacketHandlers.cs	833

/src/Network/PacketHandlers.cs	849

/src/Network/PacketHandlers.cs	2055

/src/Network/PacketHandlers.cs	2057

/src/Network/PacketHandlers.cs	2249

/src/Network/PacketHandlers.cs	2252

/src/Network/PacketHandlers.cs	2311

/src/Network/PacketHandlers.cs	2316

/src/Network/PacketHandlers.cs	2949

/src/Network/PacketHandlers.cs	2951

/src/Network/PacketHandlers.cs	3410

/src/Network/PacketHandlers.cs	3427

/src/Network/PacketHandlers.cs	4407

/src/Network/PacketHandlers.cs	4419

/src/Network/Plugin.cs	436

/src/Network/Plugin.cs	446

/src/Network/Plugin.cs	433

/src/Resources/ResGumps.Designer.cs	22

/src/Resources/ResGumps.Designer.cs	26

/src/Utility/Extensions.cs	101

/src/Utility/Extensions.cs	76

/src/Utility/Platforms/UoAssist.cs	138

/src/Utility/Platforms/UoAssist.cs	206

/src/Utility/Profiler.cs	33

/tools/ManifestCreator/Program.cs	184

/tools/ManifestCreator/Program.cs	186

/tools/ManifestCreator/Program.cs	216

/tools/ManifestCreator/Program.cs	171

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

This work is released under the GPLv3 license. This project does not distribute any copyrighted game assets. In order to run this client you'll need to legally obtain a copy of the Ultima Online Classic Client.
Using a custom client to connect to official UO servers is strictly forbidden. We do not assume any responsibility of the usage of this client.

Ultima Online(R) © 2020 Electronic Arts Inc. All Rights Reserved.
