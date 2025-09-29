<div align="center">
  <img src="src/ClassicUO.Assets/gumpartassets/logodust.png" alt="Legion Logo" width="200"/>

  [![.NET Framework](https://img.shields.io/badge/.NET-Framework%204.8-blue.svg)](https://dotnet.microsoft.com/download/dotnet-framework/net48)
  [![FNA](https://img.shields.io/badge/FNA-21.10-green.svg)](https://github.com/FNA-XNA/FNA)
  [![Cross-Platform](https://img.shields.io/badge/Platform-Windows%20%7C%20Linux%20%7C%20macOS-orange.svg)](https://github.com/andreakarasho/ClassicUO)
  [![License](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE.md)

</div>

## 🎭 **The Dust765 Project**

> **"This project was created to address a problem constructed within the toxicity of this community. This is to show the community that open source projects are not meant for cliques and high school drama but rather the expansion of something greater: innovation."**
>
> *- A penny for your thoughts, the adder that prays beneath the rose.*

### 🌑 **The Dark Truth**

Welcome to the **Dust765** project - where we don't just break barriers, we **obliterate them**. While others play nice in their little sandboxes, we're here to remind everyone that **true innovation doesn't come from playing favorites**.

Discord: dust765#2787

Dust765: 7 Link, 6 Gaechti, 5 Syrupz and jsebold666 (astraroth)

**What makes us different?**

- 🚫 **No Cliques** - We don't care about your "elite" status or who you know
- 🌍 **No Platform Discrimination** - Windows, Linux, macOS - we treat them all equally
- 🔥 **No Drama** - Leave your high school mentality at the door
- 💀 **Pure Innovation** - We're here to build, not to gossip

### 🎯 **The Mission**

The **Dust765** project isn't just another UO client. It's a **statement**. A statement that says:

> *"We're tired of the toxic communities, the exclusive groups, and the drama that plagues open source projects. We're here to show that real innovation comes from collaboration, not from who you know or what platform you use."*

## 🚀 **What We've Built**

### ✨ **Cross-Platform Domination**

- **Windows x64** - Because even Windows users deserve quality
- **Linux x64** - For the penguin lovers who got tired of being ignored
- **macOS x64** - Because Apple users are people too

### 🛠️ **Cutting-Edge Technology**

- **.NET Framework 4.8** - Stable and reliable (proven technology)
- **Mono Support** - Cross-platform compatibility through Mono runtime
- **Native Libraries** - Optimized for each platform (because we actually care)
- **Multi-Platform** - Windows, Linux, and macOS support

### 🎮 **The Complete Experience**

- **Modern UI** - Because 1997 called, and we hung up
- **High Performance** - Optimized for modern hardware (not your grandma's Pentium)
- **Full Compatibility** - Works with all UO servers (even the sketchy ones)
- **Advanced Features** - Macros, tooltips, nameplates, and more (because we're not lazy)

## 📦 **Downloads**

### 🎯 **Automated Builds**

Our builds are generated automatically (unlike some projects that require a blood sacrifice):

- **Windows**: `ClassicUO-Windows-x64.zip`
- **Linux**: `ClassicUO-Linux-x64.tar.gz`
- **macOS**: `ClassicUO-macOS-x64.tar.gz`

### 🔄 **CI/CD That Actually Works**

- ✅ **Automatic Building** - Every commit, every platform
- ✅ **Multi-Platform Testing** - We test on Windows, Linux, and macOS
- ✅ **Automatic Deployment** - Releases created without human intervention
- ✅ **Separate Artifacts** - Downloads organized by platform (because we're not savages)

## 🛠️ **Development**

### 📋 **Prerequisites**

- [.NET Framework 4.8](https://dotnet.microsoft.com/download/dotnet-framework/net48) (for Windows)
- [Mono](https://www.mono-project.com/download/stable/) (for Linux/macOS)
- Git (for cloning, not for drama)
- Visual Studio 2022 / VS Code / Rider (optional, but recommended)

### 🏗️ **Local Build**

```bash
# Clone the repository (the right way)
git clone https://github.com/seu-usuario/ClassicUO.git
cd ClassicUO

# Initialize submodules (because we use them properly)
git submodule update --init --recursive

# Build for all platforms (because we're not lazy)
dotnet build

# Build with .NET Framework 4.8 (Windows)
dotnet build src/ClassicUO.Client/ClassicUO.Client.csproj -c Release

# Build with Mono (Linux/macOS)
mono mscorlib.dll
```

### 🧪 **Build Scripts**

- **Windows**: `scripts\build-cross-platform.cmd`
- **Linux/macOS**: `scripts/build-cross-platform.sh`


### 💡 **How to Contribute**

1. **Fork** the repository (the right way)
2. **Create** a feature branch (`git checkout -b feature/AmazingFeature`)
3. **Commit** your changes (`git commit -m 'Add some AmazingFeature'`)
4. **Push** to the branch (`git push origin feature/AmazingFeature`)
5. **Open** a Pull Request (and be prepared for real feedback)

### 🐛 **Reporting Bugs**

- Use the [Issues](../../issues) system (not Discord DMs)
- Include platform information (because we're not mind readers)
- Attach error logs (because "it doesn't work" isn't helpful)
- Describe reproduction steps (because we can't read your mind)

### 💬 **Discussions**

- [GitHub Discussions](../../discussions) for ideas and suggestions
- [Discord](https://discord.gg/classicuo) for real-time chat (but keep it civil)

## 📜 **License**

This project is licensed under the MIT License - see the [LICENSE.md](LICENSE.md) file for details.

## 🙏 **Acknowledgments**

- **andreakarasho** - Original creator of ClassicUO (the real MVP)
- **gaetchi** - Best cheater
- **FNA Team** - Cross-platform graphics engine (the unsung heroes)
- **UO Community** - For the feedback and support (even the toxic parts)
- **Contributors** - Everyone who helped make this project possible

## 🌟 **Showcase**

## 📋 Table of Contents

1. [Art / Hue Changes](#art--hue-changes)
2. [Visual Helpers](#visual-helpers)
3. [HealthBars](#healthbars)
4. [Cursor](#cursor)
5. [Overhead / Underchar](#overhead--underchar)
6. [Old Healthlines](#old-healthlines)
7. [Misc](#misc)
8. [Misc2](#misc2)
9. [Auto Loot](#auto-loot)
10. [Buffbar UCC](#buffbar-ucc)
11. [Self Automations](#self-automations)
12. [Macros](#macros)
13. [Gumps](#gumps)
14. [Texture Manager](#texture-manager)
15. [Lines (Lines UI)](#lines-lines-ui)

---

## 🎨 Art / Hue Changes

### Visual Modification Features

#### **Color Stealth**
- **Activation**: Color stealth ON/OFF
- **Functionality**: Allows coloring characters in stealth
- **Options**:
  - Custom color via color picker
  - Neon effect (Off, White, Pink, Ice, Fire)

#### **Color Energy Bolt**
- **Activation**: Color Energy bolt ON/OFF
- **Functionality**: Modifies the appearance of energy projectiles
- **Options**:
  - Custom color
  - Neon effect (Off, White, Pink, Ice, Fire)
  - Art change (Normal, Explo, Bagball)

#### **Gold Art Changes**
- **Art Options**: Normal, Cannonball, Prev Coin
- **Coloring**: Activation of colors for cannonball or previous coins
- **Custom color** via picker

#### **Tree Art Changes**
- **Options**: Normal, Stump, Tile
- **Coloring**: Activation of colors for stump or tile
- **Custom color** for modified elements

#### **Blocker Type**
- **Options**: Normal, Stump, Tile
- **Coloring**: Activation of colors for stump or tile
- **Custom color** for modified elements

---

## 👁️ Visual Helpers

### Visual Highlighting Tools

#### **Highlight Tiles on Range**
- **Functionality**: Highlights tiles within a specific range
- **Settings**:
  - Adjustable range (1-20 tiles)
  - Custom color for highlighting
  - Independent activation/deactivation

#### **Highlight Tiles on Range for Spells**
- **Functionality**: Similar to above, but specific for spells
- **Settings**:
  - Adjustable range (1-20 tiles)
  - Custom color
  - Independent control

#### **Preview Fields**
- **Functionality**: Preview of magical fields

#### **Color Own Aura by HP**
- **Requirement**: Aura must be enabled
- **Functionality**: Changes aura color based on current health

#### **Glowing Weapons**
- **Options**: Off, White, Pink, Ice, Fire, Custom
- **Functionality**: Adds glow effect to weapons
- **Custom color** when "Custom" is selected

#### **Highlight Last Target**
- **Options**: Off, White, Pink, Ice, Fire, Custom
- **Functionality**: Highlights the last attacked target
- **Custom color** available

#### **Highlight Last Target Poisoned**
- **Options**: Off, White, Pink, Ice, Fire, Custom
- **Functionality**: Highlights last target when poisoned
- **Custom color** available

#### **Highlight Last Target Paralyzed**
- **Options**: Off, White, Pink, Ice, Fire, Custom
- **Functionality**: Highlights last target when paralyzed
- **Custom color** available

---

## ❤️ HealthBars

### Health Bar Improvements

#### **Highlight LT Healthbar**
- **Functionality**: Highlights the last target's health bar

#### **Highlight Healthbar Border by State**
- **Functionality**: Highlights bar borders based on state (poisoned, paralyzed, etc.)

#### **Flashing Healthbar Outline**
- **Options by category**:
  - Self (own character)
  - Party (group members)
  - Ally (allies)
  - Enemy (enemies)
  - All (everyone)

#### **Flashing Healthbar Settings**
- **Negative Changes Only**: Flashes only on negative changes
- **Threshold**: Minimum threshold setting for activation (0-100)

---

## 🖱️ Cursor

### Cursor Improvements

#### **Show Spells on Cursor**
- **Functionality**: Displays spell icons on cursor
- **Settings**:
  - Adjustable X and Y offset
  - Custom positioning

#### **Color Game Cursor**
- **Functionality**: Colors cursor based on target type (hostile/friendly)

---

## 📊 Overhead / Underchar

### Additional Information

#### **Display Range in Overhead**
- **Requirement**: HP overhead must be enabled
- **Functionality**: Displays range in overhead information

---

## 📈 Old Healthlines

### Classic Health Bar System

#### **Use Old Healthlines**
- **Functionality**: Activates the classic health bar system

#### **Multiple Underlines**
- **Display Mana/Stam**: Shows mana and stamina in underlines for self and party
- **Bigger Underlines**: Uses larger underlines for self and party
- **Transparency**: Transparency setting (0-10)

---

## ⚙️ Misc

### Miscellaneous Features

#### **Offscreen Targeting**
- **Functionality**: Allows targeting off-screen (always active)

#### **Set Target Out of Range**
- **Functionality**: Sets target even when out of range

#### **Override Container Open Range**
- **Functionality**: Ignores range limitations for opening containers

#### **Show Close Friend in WorldMapGump**
- **Functionality**: Shows close friends on world map

#### **Auto Avoid Obstacles and Mobiles**
- **Functionality**: Automatically avoids obstacles and mobiles

#### **Razor Target to LastTarget String**
- **Functionality**: Converts Razor target commands to lasttarget
- **Configuration**: Custom text for target messages

#### **Black Outline Statics**
- **Status**: CURRENTLY BROKEN
- **Functionality**: Outlines statics in black

#### **Ignore Stamina Check**
- **Functionality**: Ignores stamina checks

#### **Block Wall of Stone**
- **Functionality**: Blocks Wall of Stone
- **Settings**:
  - Fel Only: Only in Felucca
  - Art ID: Custom art ID
  - Force AoS: Forces AoS art with hue 945

#### **Block Energy Field**
- **Functionality**: Blocks Energy Field
- **Settings**:
  - Fel Only: Only in Felucca
  - Art ID: Custom art ID
  - Force AoS: Forces AoS art with hue 293

---

## 🔧 Misc2

### Advanced Features

#### **WireFrame View**
- **Status**: CURRENTLY BROKEN
- **Requirement**: Restart required
- **Functionality**: Wireframe visualization

#### **Hue Impassable Tiles**
- **Functionality**: Colors impassable tiles
- **Custom color** configurable

#### **Transparent Houses and Items**
- **Functionality**: Makes houses and items transparent based on Z level
- **Settings**:
  - Z Level: Z level for activation
  - Transparency: Transparency level (0-100)

#### **Invisible Houses and Items**
- **Functionality**: Makes houses and items invisible based on Z level
- **Settings**:
  - Z Level: Z level for activation
  - Don't Remove Below Z: Don't remove below certain Z level

#### **Draw Mobiles with Surface Overhead**
- **Functionality**: Draws mobiles with surface overhead

#### **Ignore List for Circle of Transparency**
- **Functionality**: Enables ignore list for circle of transparency

#### **Show Death Location on World Map**
- **Functionality**: Shows death location on world map for 5 minutes

---

## 🎒 Auto Loot

### Automatic Loot System

#### **Enable UCC - AL**
- **Functionality**: Activates UOClassicCombat Auto Loot system

#### **Grid Loot Coloring**
- **Functionality**: Colors grid based on available loot

#### **Loot Above ID**
- **Functionality**: Loots only items above certain ID

#### **Timing Settings**
- **Loot Delay**: Time between looting two items (ms)
- **Purge Delay**: Time to clear queue of old items (ms)
- **Queue Speed**: Time between processing queue (ms)

#### **Corpse Colors**
- **Gray, Blue, Green, Red**: Configurable colors for different corpse types

---

## 📊 Buffbar UCC

### UOClassicCombat Buff Bar

#### **Enable UCC - Buffbar**
- **Functionality**: Activates the buff bar

#### **Display Options**
- **Show Swing Line**: Shows swing line
- **Show Do Disarm Line**: Shows disarm line
- **Show Got Disarmed Line**: Shows line when disarmed
- **Lock in Place**: Locks bar position

#### **Cooldown Settings**
- **General Cooldown**: General cooldown when disarmed (ms)
- **Disarm Strike Cooldown**: Cooldown after successful disarm (ms)
- **Disarm Attempt Cooldown**: Cooldown after disarm attempt (ms)

---

## 🤖 Self Automations

### Own Character Automations

#### **Enable UCC - Self**
- **Functionality**: Activates automations for own character

#### **Colored Pouches**
- **Functionality**: Colors pouches if colored by server
- **Custom color** configurable

#### **Cooldown Settings**
- **Action Cooldown**: General action cooldown (ms)
- **Pouch Cooldown**: Pouch cooldown (ms)
- **Cure Pot Cooldown**: Cure potion cooldown (ms)
- **Heal Pot Cooldown**: Heal potion cooldown (ms)
- **Refresh Pot Cooldown**: Refresh potion cooldown (ms)
- **Wait For Target**: Wait time for target (ms)
- **Enhanced Apple Cooldown**: Enhanced apple cooldown (ms)

#### **Threshold Settings**
- **Bandies Threshold**: Threshold for using bandages
- **Cure Pot HP Threshold**: HP threshold for cure potion
- **Heal Pot HP Threshold**: HP threshold for heal potion
- **Refresh Pot Stam Threshold**: Stamina threshold for refresh potion

#### **Misc Settings**
- **Auto Rearm Weapons**: Automatically rearms weapons after disarm
- **Cliloc Triggers**: Uses cliloc triggers
- **Macro Triggers**: Uses macro triggers
- **Strength Pot Cooldown**: Strength potion cooldown
- **Agility Pot Cooldown**: Agility potion cooldown
- **Min/Max RNG**: Minimum and maximum values for randomization

---

## ⌨️ Macros

### Special Macros

#### **LastTargetRC**
- **Functionality**: Last target with custom range check
- **Range**: Configurable range (1-30)

#### **ObjectInfo**
- **Functionality**: Macro for -info command

#### **HideX**
- **Functionality**: Removes landtile, entity, mobile or item

#### **HealOnHPChange**
- **Functionality**: Hold pressed, casts heal on own HP change

#### **HarmOnSwing**
- **Functionality**: Hold pressed, casts harm on next swing animation

#### **CureGH**
- **Functionality**: If poisoned cure, else greater heal

#### **SetTargetClientSide**
- **Functionality**: Sets target only on client side

#### **OpenCorpses**
- **Functionality**: Opens 0x2006 corpses within 2 tiles

---

## 🖼️ Gumps

### Graphical Interface

#### **Enable UCC - LastTarget Bar**
- **Functionality**: Activates last target bar
- **Lock**: Double click to lock in place

#### **Bandage Gump**
- **Functionality**: Shows gump when using bandages
- **Settings**:
  - X and Y offset
  - Count up/down toggle

#### **OnCasting Gump**
- **Functionality**: Anti-rubberbanding gump on mouse
- **Hide Option**: Option to hide the gump

---

## 🎨 Texture Manager

### Texture Manager

#### **Enable TextureManager**
- **Functionality**: Activates texture manager

#### **Halos**
- **Enable Halos**: Activates halos
- **Humans Only**: Humans only
- **Color Options**:
  - Purple (last attack/target)
  - Green (allies/party)
  - Red (criminal/gray/murderer)
  - Orange (enemy)
  - Blue (innocent)

#### **Arrows**
- **Enable Arrows**: Activates arrows
- **Humans Only**: Only humans see arrows
- **Color Options**: Same colors as halos

---

## 📏 Lines (Lines UI)

### Lines Interface

#### **Enable UCC - Lines**
- **Functionality**: Activates UOClassicCombat lines interface

---

## 🎮 How to Use

### Feature Activation

1. **Access Options**: Go to Options > Dust765
2. **Navigate Categories**: Use the side menu to access different sections
3. **Configure Options**: Adjust settings as needed
4. **Save Settings**: Settings are saved automatically

### Usage Tips

- **Test Gradually**: Enable features one at a time to understand their effects
- **Backup Settings**: Backup settings before major changes
- **Performance**: Some features may impact performance
- **Compatibility**: Check compatibility with your UO server

---

## ⚠️ Important Warnings

### Broken Features
- **WireFrame View**: Currently non-functional
- **Black Outline Statics**: Currently broken

### Requirements
- Some features require client restart
- Certain options depend on other settings being active
- Check compatibility with your server version

### Performance
- Visual features may impact FPS
- Auto loot may cause lag in areas with many items
- Transparency and invisibility may affect rendering

---


### 🎥 **Demonstration Videos**

- [Part 1 - Introduction](https://youtu.be/aqHiiOhx8Q8)
- [Part 2 - Features](https://youtu.be/P7YBrI3s6ZI)
- [Part 3 - Cross-Platform](https://youtu.be/074Osj1Fcrg)

---


🌟 If this project helped you, consider giving it a ⭐ on the repository! 🌟

"Innovation doesn't come from cliques, but from true collaboration."`

🎭 The Dust765 Project - Breaking Barriers, Building Bridges 🎭

