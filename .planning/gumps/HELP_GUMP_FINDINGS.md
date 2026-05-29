# Help server-gump (CUO.Ecs) — session findings

Help button → `Send_HelpRequest` (0x9B) → ModernUO replies with a generic gump
(packet 0xB0, gumpId 1964046991). It's the classic UO **Help Menu**:
`resizepic 2600` bg, title, pages 0–3, each row = gem button (5540/5541) +
`xmfhtmlgump` (450×74, hasBg=1 hasScroll=1) showing a help cliloc.

Evidence shots in this dir: `_help_ECS_white_before.png`, `_help_ECS_black_after.png`.

## FIXED — body text rendered white (invisible) on the parchment

Root cause: `xmfhtmlgump` text is HTML with **no colour tags** (e.g. cliloc 1001003
`<U>General question about Ultima Online</U>: Opens the UO wiki where you...`).
Untagged HTML segments use FontsLoader's *html start colour*. ECS called
`fonts.SetUseHTML(true)` → default start colour **white (0xFFFFFFFF)** → white text
on the light 0x2486 bg → unreadable.

Legacy (`HtmlControl.InternalBuild` + `RenderedText.CreateTexture`) sets
`HTMLColor = 0x010101FF` (near-black) for the has-background / no-scrollbar case and
calls `SetUseHTML(true, HTMLColor, HasBackgroundColor)`.

Fix (mirrors legacy exactly):
- `UoFontRenderer.Bake` / `GenerateColored` now thread `uint htmlStartColor` +
  `bool htmlBgColored` into `SetUseHTML(true, htmlStartColor, htmlBgColored)`
  (`src/ClassicUO.Ecs/UI/UoFontPlugin.cs`). `BakeKey` includes the start colour.
- `ServerGumpPlugin.HtmlStartColor(hue, hasBg, hasScroll)` replicates legacy
  `InternalBuild` branch logic; `SpawnWrappedText` takes `hasBg/hasScroll` and passes
  the computed colour. The three html call sites (`htmlgump`, `xmfhtmlgump[color]`,
  `xmfhtmltok`) forward `hasBg, hasScroll`.

Verified: pixel-sampled text is now near-black (1,1,1); visually readable. 0 build errors.

## NON-BUG — the "vertical offset + missing words" was a measurement artifact

Originally suspected a wrap/offset bug. It DOES NOT EXIST. The ECS Help gump renders
correctly — full wrapped text, properly positioned, matching legacy
(`_compare_legacy_vs_ecs.png`: left=legacy, right=ECS).

Root cause of the false alarm: ECS UI renders into a **logical-size** RT
(`backbuffer / DpiScale`) then upscales by `DpiScale` to the physical backbuffer that
the agent screenshot captures. On this machine `DpiScale ≈ 1.39` (Win11 display
scaling). So a Clay bbox at logical `(164,119)` lands at physical `(~228,~165)` in the
800×600 screenshot. All earlier crops used **logical** coords on the **physical**
screenshot → sampled the wrong band (looked "offset ~25px down") and cut off the right
of each line (looked like "missing words"). Re-cropping at `logical × DpiScale` shows
row1 = "General question about Ultima Online: Opens the UO wiki where you / can find
answers to most gameplay questions." — complete.

Lesson for future agent-screenshot UI work: agent shots are **physical** pixels;
Clay/ECS UI coords are **logical**. Multiply logical coords by `DpiScale` (≈1.39 here)
before cropping, or detect the gump's actual pixel bounds first. Legacy client renders
UI ~1:1 (no logical RT), so legacy shots use logical≈physical — don't assume the same
mapping for both clients.

## Harness notes (this session)
- **Legacy client will NOT boot** for reference capture: `DllNotFoundException: SDL2`
  in `GameCursor` — `external/FNA` is switched to SDL3 (uncommitted), legacy still
  P/Invokes SDL2. So no pixel reference; used legacy *source* as the contract.
- Top-bar **Help button ≈ x500, y9** at 800×600 (NOT ~648; small/large gump widths
  put Help center near 500). Sweep with a probe if unsure.
- ECS agent: `rtk proxy dotnet build src/ClassicUO.Ecs/ClassicUO.Ecs.csproj -p:AGENT_BUILD=true`
  → `bin/agent/net10.0/cuo.agent.dll` (+TinyEcs.dll). `down` verb is unimplemented;
  kill via pid in `tools/agent-desktop/.runtime/pids.json`.
