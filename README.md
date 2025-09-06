# ClassicUO Cross-Platform

<div align="center">
  <img src="src/ClassicUO.Assets/gumpartassets/logolegion.png" alt="Legion Logo" width="200"/>
  
  [![.NET 8](https://img.shields.io/badge/.NET-8.0-blue.svg)](https://dotnet.microsoft.com/download/dotnet/8.0)
  [![FNA](https://img.shields.io/badge/FNA-25.09-green.svg)](https://github.com/FNA-XNA/FNA)
  [![Cross-Platform](https://img.shields.io/badge/Platform-Windows%20%7C%20Linux%20%7C%20macOS-orange.svg)](https://github.com/andreakarasho/ClassicUO)
  [![License](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE.md)
</div>

## 🎭 **The Dust765 Project**

> **"This project was created to address a problem constructed within the toxicity of this community. This is to show the community that open source projects are not meant for cliques and high school drama but rather the expansion of something greater: innovation."**
> 
> *- A penny for your thoughts, the adder that prays beneath the rose.*

### 🌑 **The Dark Truth**

Welcome to the **Dust765** project - where we don't just break barriers, we **obliterate them**. While others play nice in their little sandboxes, we're here to remind everyone that **true innovation doesn't come from playing favorites**.

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
- **macOS x64** - Because Apple users are people too (surprisingly)

### 🛠️ **Cutting-Edge Technology**
- **.NET 8.0** - The latest and greatest (unlike some projects stuck in the past)
- **FNA 25.09** - Graphics engine that actually works cross-platform
- **Self-Contained** - No more "it works on my machine" excuses
- **Native Libraries** - Optimized for each platform (because we actually care)

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
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) (the real one, not some ancient version)
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

# Build specific platform (if you're into that sort of thing)
dotnet publish src/ClassicUO.Client/ClassicUO.Client.csproj -c Release -r win-x64 --self-contained true
dotnet publish src/ClassicUO.Client/ClassicUO.Client.csproj -c Release -r linux-x64 --self-contained true
dotnet publish src/ClassicUO.Client/ClassicUO.Client.csproj -c Release -r osx-x64 --self-contained true
```

### 🧪 **Build Scripts**
- **Windows**: `scripts\build-cross-platform.cmd`
- **Linux/macOS**: `scripts/build-cross-platform.sh`

## 🎯 **Roadmap**

### ✅ **Completed**
- [x] Migration to .NET 8.0 (because we're not stuck in the past)
- [x] Update to FNA 25.09 (latest and greatest)
- [x] Removal of Windows-specific dependencies (because we're not biased)
- [x] Cross-platform build system (that actually works)
- [x] Automated CI/CD (because manual builds are for peasants)

### 🔄 **In Development**
- [ ] Performance optimizations (because speed matters)
- [ ] Automated testing (because we're not animals)
- [ ] Expanded documentation (because we're not lazy)
- [ ] ARM64 support (because the future is now)

### 🎯 **Future**
- [ ] Web-based configuration interface (because we're modern)
- [ ] Modding API (because customization is king)
- [ ] Modern shader support (because graphics matter)
- [ ] Discord/Steam integration (because we're social)

## 🤝 **Contributing**

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
- **FNA Team** - Cross-platform graphics engine (the unsung heroes)
- **UO Community** - For the feedback and support (even the toxic parts)
- **Contributors** - Everyone who helped make this project possible

## 🌟 **Showcase**

### 🎥 **Demonstration Videos**
- [Part 1 - Introduction](https://youtu.be/aqHiiOhx8Q8)
- [Part 2 - Features](https://youtu.be/P7YBrI3s6ZI)
- [Part 3 - Cross-Platform](https://youtu.be/074Osj1Fcrg)

---

<div align="center">
  <strong>🌟 If this project helped you, consider giving it a ⭐ on the repository! 🌟</strong>
  
  <br><br>
  
  <em>"Innovation doesn't come from cliques, but from true collaboration."</em>
  
  <br><br>
  
  <strong>🎭 The Dust765 Project - Breaking Barriers, Building Bridges 🎭</strong>
</div>