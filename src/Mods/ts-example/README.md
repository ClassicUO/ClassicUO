# TypeScript Extism Plugin for ClassicUO

This is a TypeScript implementation of the C# Extism plugin for ClassicUO. It provides the same functionality as the original C# plugin but written in TypeScript with a modular architecture.

## Project Structure

The TypeScript implementation is organized into several modules for better maintainability:

### Core Files

- **`src/index.ts`** - Main plugin entry point with lifecycle functions
- **`src/types.ts`** - All type definitions, enums, and interfaces
- **`src/utils.ts`** - Utility functions and base64 encoding
- **`src/host-wrapper.ts`** - Host communication wrapper and Zlib compression
- **`src/gump-builder.ts`** - UI building helper class
- **`src/ui-builders.ts`** - UI creation functions and callback management

## Features

- **Plugin Lifecycle Management**: `on_init`, `on_update`, `on_event` functions
- **UI Event Handling**: Mouse and keyboard event callbacks
- **Host Communication**: Functions to interact with the ClassicUO host
- **UI Building**: Complete UI system with Clay layout engine integration
- **Asset Management**: Sprite and graphic manipulation
- **ECS Integration**: Entity-Component-System querying and manipulation

## Key Components

### Main Plugin Functions (`index.ts`)

- `on_init()`: Initializes the plugin and sends initial packet to server
- `on_update()`: Handles update events with time information
- `on_event()`: Processes host messages and keyboard events
- `on_ui_mouse_event()`: Handles UI mouse interactions
- `on_ui_keyboard_event()`: Handles UI keyboard events
- `Handler_0x73()`: Custom packet handler

### Type System (`types.ts`)

- **Enums**: Keys, AssetType, CompressionType, Clay enums, etc.
- **Interfaces**: Vector2/3, ClayColor, UINodeProxy, and all data structures
- **Event Types**: HostMessage, UIMouseEvent, QueryRequest/Response

### Host Communication (`host-wrapper.ts`)

- **HostWrapper class**: Clean interface to host functions
  - `sendPacketToServer()`: Send data to game server
  - `setSprite()` / `getSprite()`: Manage sprite assets
  - `getEntityGraphic()` / `setEntityGraphic()`: Entity graphic manipulation
  - `getPlayerSerial()`: Get current player serial
  - `createUINodes()`: Create UI elements
  - `spawnEcsEntity()` / `deleteEcsEntity()`: ECS entity management
  - `query()`: ECS querying
- **Zlib class**: Compression utilities (placeholder implementation)

### UI Building (`gump-builder.ts`)

- **GumpBuilder class**: Helper for creating UI elements
  - `addLabel()`: Create text labels
  - `addButton()`: Create interactive buttons
  - `addGump()`: Create gump elements
  - `addGumpNinePatch()`: Create scalable gump elements

### UI Creation (`ui-builders.ts`)

- **createLoginScreenMenu()**: Complete login screen UI
- **createMenu()**: Example menu UI
- **uiCallbacks**: Global callback management for UI interactions

### Utilities (`utils.ts`)

- **base64Encode()**: Base64 encoding function (replaces btoa)
- **createClaySizingAxis()**: Clay sizing helper
- **createClayColor()**: Color creation helper
- **createVector2()** / **createVector3()**: Vector creation helpers

## Usage

### Keyboard Controls

- **A**: Compress and set sprite data
- **S**: Get sprite information
- **D**: Get player entity graphic
- **F**: Set player entity graphic
- **G**: Create login screen menu
- **H**: Perform ECS query

### UI Features

- **Login Screen**: Complete login interface with username/password fields
- **Interactive Buttons**: Clickable buttons with callbacks
- **Text Input**: Username and password input fields
- **Movable Windows**: Draggable UI elements
- **Responsive Layout**: Clay-based layout system

## Development

### Building

```bash
npm install
npm run build
```

### TypeScript Configuration

- Target: ES2020
- Module: ES2020
- Strict type checking enabled
- Extism PDK types included

### Dependencies

- `@extism/js-pdk`: Extism JavaScript PDK
- TypeScript for type safety and development experience

## Architecture Benefits

1. **Modularity**: Each file has a single responsibility
2. **Type Safety**: Full TypeScript coverage with proper interfaces
3. **Maintainability**: Easy to locate and modify specific functionality
4. **Reusability**: Utility functions and classes can be easily reused
5. **Testability**: Modular structure allows for easier unit testing

## Comparison with C# Version

The TypeScript implementation maintains feature parity with the C# version while providing:

- Better modular organization
- TypeScript's type safety
- More maintainable code structure
- Easier debugging and development experience
