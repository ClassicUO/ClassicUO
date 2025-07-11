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
  - Reconcilcer code is found in `src/Mods/ts-example/src/react/reconciler.ts`
  - The custom reconciler sends UI element declarations as a JSON tree structure via the `cuo_ui_node` function, on the C# side they are deserialized as `UINode` structs
- **TypeScript Support**: Full type definitions for all components and props

### UI Development Notes

- All positioning uses Clay's floating layout system with absolute positioning
- Gump IDs correspond to Ultima Online's sprite system
- Components support hue modifications for coloring
- The reconciler handles efficient updates and batching
- **IMPORTANT**: This is NOT a web application. Do not use browser APIs like `window`, `document`, `setTimeout`, `setInterval`, etc.
- Animations and timers should be handled by the game engine, not React hooks
- The React reconciler runs in a custom environment within the ClassicUO game engine

### React-to-UINode Bridge Architecture

The React reconciler transforms React components into UINode entities through a sophisticated bridge system:

#### React Reconciler (`src/Mods/ts-example/src/react/reconciler.ts`)

- **Entity Creation**: Uses `HostWrapper.spawnEntity()` to create unique ECS entities for each React component
- **UINode Generation**: Calls `createElement()` to convert React props into UINode structures
- **Host Communication**: Sends UINode data to C# host via `HostWrapper.setNode()` function
- **Tree Management**: Maintains React component hierarchy and synchronizes with ECS parent-child relationships

#### UINode Structure (`src/ClassicUO.Client/Ecs/UI/GuiPlugin.cs:185-189`)

```csharp
struct UINode
{
    public Clay_ElementDeclaration Config;  // Clay UI layout configuration
    public ClayUOCommandData UOConfig;     // ClassicUO-specific rendering data
}
```

#### Element Creation (`src/Mods/ts-example/src/react/createElement.ts`)

- **Component Mapping**: Maps React component types to UINode configurations
- **Clay Configuration**: Generates Clay UI layout settings (sizing, positioning, floating)
- **UO Command Setup**: Creates ClassicUO-specific rendering commands via `ClayUOCommandType`
- **Widget Types**: Assigns appropriate `ClayWidgetType` for interaction handling

### Native ClassicUO C# GUI Rendering Pipeline

#### GuiRenderingPlugin (`src/ClassicUO.Client/Ecs/Rendering/GuiRenderingPlugin.cs`)

The GUI rendering system processes UINode entities through a multi-stage pipeline:

1. **ECS Query Phase**: Queries all UINode entities with optional Text, UIMouseAction, and Children components
2. **Clay Layout Phase**:
   - Calls `Clay.BeginLayout()` to start layout calculation
   - Recursively processes UINode hierarchy via `renderNodes()` function
   - Configures Clay elements with `Clay.ConfigureOpenElement()`
3. **Interaction Processing**: Handles mouse interactions and updates UIMouseAction components
4. **Clay Rendering Phase**:
   - `Clay.EndLayout()` generates optimized render commands
   - Processes `Clay_RenderCommand` array containing layout-calculated elements
5. **Graphics Rendering**: Translates Clay commands to FNA graphics calls

#### Clay Command Processing

The renderer handles different Clay command types:

- **CLAY_RENDER_COMMAND_TYPE_TEXT**: Renders text using FontStashSharp
- **CLAY_RENDER_COMMAND_TYPE_RECTANGLE**: Draws solid rectangles
- **CLAY_RENDER_COMMAND_TYPE_IMAGE**: Renders cached textures
- **CLAY_RENDER_COMMAND_TYPE_CUSTOM**: Processes ClassicUO-specific commands via `ClayUOCommandType`
- **CLAY_RENDER_COMMAND_TYPE_SCISSOR_START/END**: Handles clipping regions

#### ClayUOCommandType System

Custom rendering commands extend Clay's capabilities for Ultima Online assets:

##### Available Command Types (`src/ClassicUO.Client/Ecs/UI/GuiPlugin.cs:225-234`)

```csharp
enum ClayUOCommandType : byte
{
    None,           // No custom rendering
    Text,           // Custom text rendering (handled by Clay)
    Gump,           // Single gump sprite
    GumpNinePatch,  // Nine-patch scalable gump
    Art,            // Art/item sprites
    Land,           // Land tile sprites
    Animation,      // Animated sprites (placeholder)
}
```

##### Command Data Structure (`src/ClassicUO.Client/Ecs/UI/GuiPlugin.cs:237-243`)

```csharp
struct ClayUOCommandData
{
    public ClayUOCommandType Type;
    public uint Id;        // Asset ID (gump, art, etc.)
    public Vector3 Hue;    // Color hue modification
}
```

#### Implementing New ClayUOCommandType

To add a new primitive component:

1. **Add Enum Value**: Extend `ClayUOCommandType` with your new type
2. **Update Rendering**: Add case in `GuiRenderingPlugin.cs` custom command switch (line 170)
3. **Asset Integration**: Use appropriate `AssetsServer` property (Gumps, Arts, etc.)
4. **React Integration**: Add support in `createElement.ts` for React components

##### Example Implementation Pattern:

```csharp
case ClayUOCommandType.YourNewType:
    // Get asset from AssetsServer
    ref readonly var assetInfo = ref assets.Value.YourAssets.GetAsset(uoCommand.Id);
    if (assetInfo.Texture != null)
    {
        // Render using UltimaBatcher2D
        b.Draw(assetInfo.Texture,
               new Vector2(boundingBox.x, boundingBox.y),
               assetInfo.UV,
               uoCommand.Hue,
               cmd.zIndex);
    }
    break;
```

#### Component Buffer System

`ClayUOCommandBuffer` manages custom render commands:

- **Command Storage**: Thread-safe array of `ClayUOCommandData`
- **Index Management**: Returns handles for Clay to reference commands
- **Memory Efficiency**: Grows dynamically, resets each frame

#### Helper Classes

- **GumpBuilder** (`src/ClassicUO.Client/Ecs/UI/GumpBuilder.cs`): Provides C# API for creating common UI elements
- **FocusedInput**: Manages keyboard focus for text inputs
- **ImageCache**: Caches textures for image rendering

### Example Usage

```typescript
import { View, Gump, Button, Label, Art } from "../react";

<Gump gumpId={0x014e} size={{ width: 640, height: 480 }}>
  <Label text="Hello World" floating={{ offset: { x: 100, y: 100 } }} />
  <Button gumpIds={{ normal: 0x05ca, pressed: 0x05c9, over: 0x05c8 }} />
  <Art artId={0x1234} hue={{ x: 255, y: 0, z: 0 }} />
</Gump>;
```

## Design System Development

### Design System Component TODO List

The following components are planned for implementation in the design system located at `src/Mods/ts-example/src/components/design-system/`:

### Implementation Priority

Components are prioritized based on:

1. **Phase 1 (High Priority)**: Core foundation components needed for basic UIs
2. **Phase 2 (Medium Priority)**: Enhanced controls and game-specific components
3. **Phase 3 (Low Priority)**: Advanced composed components and specialized tools

#### **Foundation Components (High Priority)**

- [ ] **Panel** - Basic container with background and border support
- [ ] **Card** - Elevated container with shadow/border styling
- [ ] **ScrollView** - Scrollable content container
- [ ] **Flex** - Flexible layout container (row/column)
- [ ] **Stack** - Vertical/horizontal stack with spacing
- [ ] **Modal** - Modal dialog
- [ ] **Tooltip** - Hover information

#### **Input Components (High Priority)**

- [ ] **Input** - Enhanced text input with validation
- [ ] **PasswordInput** - Password field with toggle visibility
- [ ] **Button** - Primary action button
- [ ] **IconButton** - Button with icon only

#### **Foundation Components (Medium Priority)**

- [ ] **Window** - Draggable, resizable container with title bar
- [ ] **Grid** - Grid layout container
- [ ] **Spacer** - Flexible spacing element
- [ ] **Divider** - Visual separator (horizontal/vertical)
- [ ] **Center** - Centers child content

#### **Input Components (Medium Priority)**

- [ ] **TextArea** - Multi-line text input
- [ ] **NumberInput** - Numeric input with increment/decrement
- [ ] **Select** - Dropdown selection
- [ ] **Combobox** - Searchable dropdown
- [ ] **RadioGroup** - Radio button group
- [ ] **CheckboxGroup** - Checkbox group
- [ ] **Switch** - Toggle switch
- [ ] **Slider** - Range slider (horizontal/vertical)
- [ ] **ItemPicker** - UO item selection component

#### **Action Components (Medium Priority)**

- [ ] **ButtonGroup** - Grouped buttons
- [ ] **ToggleButton** - Toggle state button
- [ ] **LinkButton** - Link-styled button
- [ ] **Tabs** - Tab navigation
- [ ] **TabPanel** - Tab content panel

#### **Display Components (Medium Priority)**

- [ ] **Table** - Data table with sorting/filtering
- [ ] **List** - Vertical list of items
- [ ] **PropertySheet** - Key-value property display
- [ ] **Stats** - Numeric statistics display
- [ ] **Avatar** - Character/player avatar
- [ ] **Badge** - Small status indicator
- [ ] **Chip** - Compact information display
- [ ] **Tag** - Categorical labels
- [ ] **Progress** - Progress indication
- [ ] **Spinner** - Loading indicator

#### **Game-Specific Components (Medium Priority)**

- [ ] **Paperdoll** - Character equipment display
- [ ] **ItemIcon** - Item display with tooltip
- [ ] **ItemGrid** - Container item grid
- [ ] **HealthBar** - Health/mana/stamina bars
- [ ] **CharacterSheet** - Character statistics
- [ ] **SkillMeter** - Skill progression display
- [ ] **Gump** - Enhanced gump component with common patterns
- [ ] **ResizableGump** - Auto-resizing gump backgrounds
- [ ] **BookPage** - Book/journal page layout
- [ ] **ScrollBackground** - Scroll-like background
- [ ] **DialogBox** - Game dialog/message box
- [ ] **ContextMenu** - Right-click context menu

#### **Feedback Components (Medium Priority)**

- [ ] **Toast** - Temporary notifications
- [ ] **Alert** - Persistent notifications
- [ ] **Popover** - Floating content
- [ ] **Confirmation** - Confirmation dialogs
- [ ] **StatusDot** - Connection/status indicator
- [ ] **LoadingOverlay** - Full-screen loading
- [ ] **EmptyState** - No data state
- [ ] **ErrorBoundary** - Error handling wrapper

#### **Low Priority Components**

- [ ] **Absolute** - Absolute positioning wrapper
- [ ] **ColorPicker** - Color selection (using UO hue system)
- [ ] **GumpPicker** - UO gump ID selection component
- [ ] **FloatingActionButton** - Floating action button
- [ ] **Breadcrumb** - Navigation breadcrumb
- [ ] **Pagination** - Page navigation
- [ ] **DataGrid** - Advanced data grid
- [ ] **SearchBox** - Search input with results
- [ ] **DatePicker** - Date selection (if needed)
- [ ] **ColorSchemeSelector** - UO color scheme picker
- [ ] **SettingsPanel** - Common settings layout
- [ ] **LoginForm** - Complete login form
- [ ] **ChatWindow** - Game chat interface
- [ ] **SplitPane** - Resizable split layout
- [ ] **Accordion** - Expandable sections
- [ ] **Drawer** - Side drawer/panel
- [ ] **Sidebar** - Navigation sidebar
- [ ] **AppShell** - Main application layout
- [ ] **Conditional** - Conditional rendering
- [ ] **RepeatGrid** - Repeated element grid
- [ ] **ErrorFallback** - Error fallback UI

### Design System Principles

Based on research into Ultima Online's interface design and community feedback, the design system follows these core principles:

#### **1. Authentic Legacy Feel with Modern Usability**

- **Preserve Visual Identity**: Maintain the classic UO aesthetic with original gump sprites and art assets
- **Modernize Interaction**: Enhance usability without sacrificing the nostalgic visual experience
- **Respect Player Expectations**: Honor the established UO interface conventions while fixing known pain points

#### **2. Customization and Player Agency**

- **Comprehensive Customization**: Allow players to modify virtually every aspect of the UI
- **Flexible Layouts**: Support both Classic Client and Enhanced Client style arrangements
- **Macro Integration**: Seamless integration with macro systems and third-party tools
- **Profile System**: Multiple UI profiles for different play styles

#### **3. Information Density and Visual Hierarchy**

- **Reduce Clutter**: Address the "cluttered UI" problem identified in MMORPG discussions
- **Smart Defaults**: Provide clean, minimal defaults with options to add complexity
- **Context-Aware Display**: Show relevant information when needed, hide when not
- **Scalable Information**: Allow players to control information density per their preference

#### **4. Accessibility and Readability**

- **Contrast and Legibility**: Address readability issues mentioned in player feedback
- **Font Rendering**: Crisp text rendering at all sizes and resolutions
- **Color Accessibility**: Support for colorblind players and high contrast modes
- **Keyboard Navigation**: Full keyboard accessibility for all UI elements

#### **5. Performance and Responsiveness**

- **Smooth Interactions**: Eliminate UI lag and responsiveness issues
- **Memory Efficiency**: Optimize for long gaming sessions without performance degradation
- **Animation Polish**: Subtle, purposeful animations that enhance rather than distract
- **Instant Feedback**: Immediate visual feedback for all user actions

#### **6. Cross-Client Compatibility**

- **Unified API**: Consistent component behavior across different client implementations
- **Resolution Independence**: Support for modern high-DPI displays and ultrawide monitors
- **Backward Compatibility**: Maintain compatibility with existing UO conventions
- **Migration Path**: Easy transition for players switching between clients

#### **7. Developer Experience**

- **Component Composability**: Build complex UIs from simple, reusable components
- **Type Safety**: Full TypeScript support with comprehensive prop validation
- **Documentation**: Clear examples and guidelines for each component
- **Testing**: Built-in testing utilities and accessibility checks

### Key Issues Addressed

The design system specifically addresses these issues identified in community feedback:

- **Interface Problems**: Classic Client interfaces described as "terrible in design and very difficult to even see"
- **Information Overload**: Screens that "can be an overload of info for newer players"
- **Screen Real Estate**: Avoiding "covering 1/3 of my screen with UI elements"
- **Macro Interface**: Replacing "incredibly limited, ugly and outdated" macro systems
- **Customization**: Providing "customizability is king when it comes to MMOs"
