# Cross-Cutting Gump Foundation — Settings Res + Widget Kit

Built before per-gump porting to close the shared infra gaps the specs flagged.
Build verified: `dotnet build src/ClassicUO.Ecs` → 0 errors; cuo-ecs boots clean.

## (A) Settings Res — `Res<Profile>` expanded

`src/ClassicUO.Ecs/Configuration/Profile.cs` now carries the gump-relevant flags
(names + defaults mirror legacy `src/ClassicUO.Client/Configuration/Profile.cs`,
so a future `profile.json` load deserializes straight in). Already registered via
`app.AddResource(new Profile())` in `WorldRenderingPlugin` → read with `Res<Profile>`.

| Flag | Default | Unblocks |
|------|---------|----------|
| `UseOldStatusGump` | false | statusbar-healthbar |
| `StatusGumpBarMutuallyExclusive` | true | statusbar-healthbar |
| `CloseHealthBarType` | 0 | statusbar-healthbar |
| `CustomBarsToggled` | false | statusbar-healthbar |
| `BuffBarTime` | false | buff |
| `CastSpellsByOneClick` | false | macro, counterbar |
| `CounterBarHighlightOnChange` | true | counterbar |
| `CounterBarHighlightOnAmount` | false | counterbar |
| `CounterBarDisplayAbbreviatedAmount` | false | counterbar |
| `CounterBarAbbreviatedAmount` | 1000 | counterbar |
| `CounterBarHighlightAmount` | 5 | counterbar |
| `CounterBarCellSize` | 40 | counterbar |
| `PartyInviteGump` | false | party |
| `VendorGumpHeight` | 60 | shop |
| `GridLootType` | 0 | grid loot |

**Spec correction:** `UseUOPGumps` is NOT a profile flag — in legacy it's
`FileManager.Gumps.UseUOPGumps` (asset-loader property). Status-gump layout reads
it from the asset side; left out of Profile.

**Deferred:** profile.json load/save (no ECS gump-persistence layer yet). Flags
default to legacy values = exactly what gumps assume for v1.

## (B) Widget kit

Bevy.UI already shipped complete `Checkbox` / `Slider` / `Scrollbar` plugins — only
`ScrollbarPlugin` was registered. Now all three registered in `GuiPlugin` and given
UO-sprite builder helpers.

### Registered (`GuiPlugin.cs`)
- `ScrollbarPlugin` (pre-existing), `SliderPlugin`, `CheckboxPlugin`.

### New components/systems (`GuiPlugin.cs`)
- `UOCheckbox { ushort Off, On }` — sprite ids for the two states.
- `UpdateUOCheckboxes` (UiPostLayoutStage, mirrors `UpdateUOButtonsState`) — swaps
  the visible sprite to match `Checkbox.Checked` with no one-frame lag.

### New builder helpers (`GumpBuilder.cs`)
- `AddCheckbox(commands, isChecked, position?, off=0x00D2, on=0x00D3, hue?)` →
  gump + `UOCheckbox` + `Checkbox` + `Interaction`. Observe `CheckboxChanged` for value.
- `AddVScrollbar(commands, targetEntityId, position, height, hue?)` → tiled track
  (bg 256) + draggable thumb child (SLIDER 254). Target = an `Overflow.Scroll` +
  `ScrollPosition` container. `ScrollbarPlugin` drives it.
- `AddHSlider(commands, min, max, value, position, width, hue?)` → invisible-track
  + knob child (0x845). `SliderPlugin` drives it, fires `SliderChanged`.

Buttons/labels/gump-sprites/nine-patch/art/tiled already existed; text-input exists
(raw `TextInput` + `MaskedText`).

## Still missing (build-vs-defer per gump, not done here)
- **ComboBox**, **ContextMenu**, **ColorPicker** — no Bevy backing; net-new. Needed by
  Options content, Macro editor, journal/skill/counterbar right-click menus. Defer
  until a consuming gump needs them.
- **Resizable-window frame** — generic resize handle infra (journal/skills/counterbar).
- **Cliloc tooltip** text path (modern status ~40 tooltips, buff per-icon).
- **Text fidelity:** `AddLabel` still hard-codes FontId 0 / size 12; UO bitmap fonts
  (1/2/6/8/9) + per-gump hues need `UoFontRenderer.Bake` path (as ServerGumpPlugin does)
  for pixel parity.

## Next
With settings + checkbox/slider/scrollbar in place, **MiniMap** (no widgets, no profile
deps) and **Options** tab-shell (checkboxes + tab nav) are the cleanest first ports.

---

## MiniMap — implemented (first gump port)

`src/ClassicUO.Ecs/UI/MiniMapPlugin.cs` + edits to GuiPlugin (UOCustomKind.MiniMap
+ UOCustomRender.Dynamic), GuiRenderingPlugin (draw case), UiHitTest (shares Gump
mask), TopBarPlugin (Map button → MiniMapOpenButton marker), Boot (register).

Verified live on cuo-ecs (agent build) against ModernUO: window spawns, radar
terrain bakes + renders (land-only v1), blink + dots, right-click-close/drag/z
from UIMovable. Screenshot: `.planning/gumps/minimap/ecs_autospawn.png`.

v1 deferred (documented in SPEC): statics/multis radar overlay (ECS has no Chunk
tile grid), notoriety-hued dots (no Notoriety component — dots drawn red),
gump save/restore.

## ⚠ ECS agent-harness gaps found (block future gump verification)

1. **Agent build output is `bin/agent/net10.0/cuo.agent.exe`** (the agent props
   renames + redirects), NOT `bin/Debug/.../cuo-ecs.exe`. Run that exe to get the
   JSON-RPC server (writes port.json + logs `[agent] listening`).
2. **The `dotnet` shell hook strips `-p:AGENT_BUILD=true`** — build the agent
   flavor with `rtk proxy dotnet build ... -p:AGENT_BUILD=true` (raw), else the
   harness is silently absent.
3. ~~Synthetic LEFT-click does not produce `UiClick`~~ **CORRECTED — the latch
   works.** Investigated end-to-end with traces: `input.mouseClick` enqueues
   (down,down,up); MouseContext.IsPressed → UiPointer.Down transitions
   false→true→false; InteractionSystem fires the press edge + `UiClick` on
   release when press/release land on the same entity. **Verified live**: clicking
   a paperdoll button and the top-bar Map button both fire `UiClick` and the
   Map click opens the minimap (`minimap/ecs_clickopen_final.png`).

   Two REAL constraints that made clicks look broken:
   - **Pixel-perfect hit-test rejects transparent pixels.** A click inside a
     gump's bbox but over a transparent pixel passes through (correct — same as a
     real user must hit the visible sprite). Harness clicks must target an opaque
     pixel (e.g. paperdoll's hollow centre and button edges miss).
   - **Clay floats + `PointerOverIds`.** Clay treats every *absolute*-positioned
     element as its own float, and `PointerOverIds` returns only the topmost
     float's element stack at a point — sibling floats beneath are not returned.
     So an absolute caption *label* drawn over a button is a separate float that
     shadows the button, making it unhittable wherever the text covers it.

     **FIXED** (`TopBarPlugin.cs`) using Bevy event **bubbling**: the caption
     stays absolute (renders correctly — an in-flow caption measures to 0 with
     FontId 0 and vanishes) but is an **entity-child of the button** with its own
     `Interaction`. Clicking the caption resolves hover to the caption, fires
     `UiClick` on it with `propagate: true`, and Bevy's `EmitTriggerInner` walks
     the entity `Parent` chain → the button's entity-scoped `On<UiClick>` observer
     fires. The button's own strip (above the caption) is hit directly. Either
     path runs the button's action. No core/InteractionSystem change, no submodule
     edit. Verified: captions render AND clicking the "Map" caption opens the
     minimap (`_refs/ecs_final.png`).

     General rule for ECS gumps: a caption/icon overlaying a button must be an
     **entity-child of that button with `Interaction`**, and the button action
     must use an **entity-scoped** `.Observe` (so caption clicks bubble to it) —
     a global observer keyed on the button entity won't see the bubbled trigger
     (global observers fire once with the original target id).
