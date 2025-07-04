# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

ClassicUO is an open source implementation of the Ultima Online Classic Client written in C# (.NET 9.0). It uses FNA-XNA for cross-platform game development and features an Entity Component System (ECS) architecture.

## Common Development Commands

### Build Commands
```bash
# Initial setup (installs Zig, builds Clay UI library)
./scripts/setup.sh

# Restore dependencies
dotnet restore

# Development build
dotnet build

# Release build
dotnet build -c Release

# Native AOT release build (recommended for distribution)
./scripts/build-naot.sh

# Run tests
dotnet test

# Publish for distribution
dotnet publish -c Release
```

### Running the Application
```bash
# Run in development mode
dotnet run --project src/ClassicUO.Bootstrap

# Run with specific renderer (DirectX, OpenGL, Vulkan, Metal)
dotnet run --project src/ClassicUO.Bootstrap -- --renderer OpenGL
```

## High-Level Architecture

### Core Components

1. **Entity Component System (ECS)**
   - Uses TinyEcs library
   - All game objects are entities with components
   - Systems process components each frame
   - Located in `src/ClassicUO.Client/Game/`

2. **Plugin System (WASM)**
   - Uses Extism for WebAssembly plugins
   - Plugins can override packet handlers and UI
   - Plugin interface in `src/ClassicUO.Client/Plugins/`
   - Examples in `src/Mods/`

3. **Rendering Pipeline**
   - Abstracted through FNA framework
   - Multiple backend support (DirectX, OpenGL, Vulkan, Metal)
   - Custom effects in `src/ClassicUO.Renderer/Effects/`
   - Batcher system for efficient rendering

4. **Asset Management**
   - Custom loaders for UO file formats in `src/ClassicUO.Assets/`
   - Supports animations, art, sounds, maps, gumps, etc.
   - Asset loading is lazy and cached

### Project Structure
```
src/
├── ClassicUO.Assets/      # UO file format loaders (ART, MAP, SOUND, etc.)
├── ClassicUO.Bootstrap/    # Entry point and plugin host initialization
├── ClassicUO.Client/       # Main game logic, ECS, networking
├── ClassicUO.IO/           # Low-level file I/O and data structures
├── ClassicUO.Renderer/     # Rendering system, effects, batching
├── ClassicUO.Utility/      # Common utilities and helpers
└── Mods/                   # Plugin/mod examples (Rust, TypeScript)
```

### Key Architectural Patterns

1. **Packet System**: Network packets are handled through a registered handler system. Plugins can override packet handlers.

2. **UI System**: 
   - Moving towards React-based UI mods (see recent commits)
   - Clay-cs for layout calculations
   - Gump system for traditional UO UI elements

3. **Resource Management**: Assets are loaded on-demand and cached. The game uses memory-mapped files for efficient access to large UO data files.

4. **Cross-Platform**: All platform-specific code is abstracted through FNA. The game runs on Windows, Linux, macOS, and WebAssembly.

## Development Tips

- When modifying ECS components, ensure proper cleanup in disposal methods
- Plugin development: Start with examples in `src/Mods/`
- For UI modifications, check the new React reconciler system being developed
- Use Native AOT builds for testing performance-critical changes
- The game expects UO client files to be present for full functionality

## React UI System (TypeScript Mods)

ClassicUO features a custom React reconciler that bridges React components with the Clay UI library (written in C). This allows mod developers to create UI using familiar React patterns while leveraging Clay's efficient layout engine.

### Key Components
- **React Components**: Located in `src/Mods/ts-example/src/react/`
  - Basic components: View, Gump, Button, TextInput, Text, Checkbox, Label, HSliderBar
  - Components map to Clay UI elements and ClassicUO's gump system
- **Custom Reconciler**: Translates React component tree to Clay UI calls
- **TypeScript Support**: Full type definitions for all components and props

### UI Development Notes
- All positioning uses Clay's floating layout system with absolute positioning
- Gump IDs correspond to Ultima Online's sprite system
- Components support hue modifications for coloring
- The reconciler handles efficient updates and batching
- **IMPORTANT**: This is NOT a web application. Do not use browser APIs like `window`, `document`, `setTimeout`, `setInterval`, etc.
- Animations and timers should be handled by the game engine, not React hooks
- The React reconciler runs in a custom environment within the ClassicUO game engine

### Example Usage
```typescript
import { View, Gump, Button, Label } from "../react";

<Gump gumpId={0x014e} size={{ width: 640, height: 480 }}>
  <Label text="Hello World" floating={{ offset: { x: 100, y: 100 } }} />
  <Button gumpIds={{ normal: 0x05ca, pressed: 0x05c9, over: 0x05c8 }} />
</Gump>
```