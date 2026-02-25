# General

General client options, organised in subsections: **General**, **Mobiles**, **Gumps & Context**, **Misc**, **Terrain & Statics**.

---

## Subsection: General

| Option | Type | Description |
|--------|------|-------------|
| **Highlight objects under cursor** | Checkbox | When on, objects under the cursor are visually highlighted. Helps identify targets and items. |
| **Enable pathfinding** | Checkbox | Enables automatic pathfinding: the character walks around obstacles when you click to move. |
| **Use shift for pathfinding** | Checkbox | (Requires pathfinding.) Only uses pathfinding when holding **Shift** while clicking. Without Shift, straight-line movement. |
| **Single click for pathfinding** | Checkbox | (Requires pathfinding.) Single click starts pathfinding movement instead of double-click. |
| **Always run** | Checkbox | Character runs by default instead of walking. |
| **Unless hidden** | Checkbox | (Requires Always run.) Always run except when in stealth/hidden mode. |
| **Automatically open doors** | Checkbox | Opens doors automatically when passing through them. |
| **Open doors while pathfinding** | Checkbox | (Requires pathfinding.) During pathfinding, opens doors in the path so you don't get stuck. |
| **Automatically open corpses** | Checkbox | Opens corpses automatically when you get close. |
| **Corpse open distance** | Slider (0–5) | Distance in tiles at which corpses are opened automatically. |
| **Skip empty corpses** | Checkbox | Does not open corpses that are already empty (no items). |
| **Corpse open options** | Combo | When not to open: **None**, **Not targeting**, **Not hiding**, **Both**. |
| **No color for out of range objects** | Checkbox | Objects out of view range are drawn without color (gray) to highlight what is in range. |
| **Enable sallos easy grab** | Checkbox | Sallos-style item grab mode. Not recommended with grid containers (see tooltip). |
| **Show house content** | Checkbox | (Client version dependent.) Shows house content on map or in context. |
| **Smooth boat movements** | Checkbox | (Client version dependent.) Smoother boat movement. |

---

## Subsection: Mobiles

| Option | Type | Description |
|--------|------|-------------|
| **Show mobile's HP** | Checkbox | Shows mobiles' health – as percentage, bar, or both. |
| **Type** | Combo | **Percentage**, **Bar**, **Both**. |
| **Show when** | Combo | **Always**, **Less than 100%**, **Smart**. |
| **Highlight poisoned mobiles** | Checkbox | Poisoned mobiles are highlighted with a color. |
| **Highlight color** (poison) | Color picker | Highlight color for poisoned. |
| **Highlight paralyzed mobiles** | Checkbox | Paralyzed mobiles are highlighted. |
| **Highlight color** (paralyze) | Color picker | Highlight color for paralyzed. |
| **Highlight invulnerable mobiles** | Checkbox | Invulnerable mobiles are highlighted. |
| **Highlight color** (invul) | Color picker | Highlight color for invulnerable. |
| **Show incoming mobile names** | Checkbox | Shows mobile names when they enter the screen. |
| **Show incoming corpse names** | Checkbox | Shows corpse names when they enter the screen. |
| **Show aura under feet** | Combo | **Disabled**, **Warmode**, **Ctrl + Shift**, **Always**. |
| **Use a custom color for party members** | Checkbox | Party members' aura uses a custom color. |
| **Party aura color** | Color picker | Party aura color. |

---

## Subsection: Gumps & Context

| Option | Type | Description |
|--------|------|-------------|
| **Disable top menu bar** | Checkbox | Hides the top menu bar. |
| **Require alt to close anchored gumps** | Checkbox | Anchored gumps only close when holding **Alt**. Avoids accidental closing. |
| **Require alt to move gumps** | Checkbox | Only allows dragging gumps when **Alt** is held. |
| **Close entire group of anchored gumps with right click** | Checkbox | Right-click on an anchored gump closes the whole anchored group. |
| **Use original skills gump** | Checkbox | Uses the classic skills gump. |
| **Use old status gump** | Checkbox | Uses the old status gump. |
| **Show party invite gump** | Checkbox | Shows party invite window. |
| **Use modern health bar gumps** | Checkbox | Uses modern health bars (Dust765 style). |
| **Use black background** | Checkbox | (With modern health bars.) Black background on health bars. |
| **Save health bars on logout** | Checkbox | Keeps health bars open and positions when you leave and return. |
| **Close health bars when** | Combo | **Disabled**, **Out of range**, **Dead**, **Both**. |
| **Grid Loot** | Combo | **Disabled**, **Grid loot only**, **Grid loot and normal container**. See tooltip: grid loot gump for corpses, not grid containers. |
| **Require shift to open context menus** | Checkbox | Context menus (right-click) only open when **Shift** is held. |
| **Require shift to split stacks of items** | Checkbox | Splitting item stacks requires **Shift**. |

---

## Subsection: Misc

| Option | Type | Description |
|--------|------|-------------|
| **Enable circle of transparency** | Checkbox | Enables the transparency circle around the character (objects inside become transparent). |
| **Distance** | Slider | Radius of the circle of transparency. |
| **Type** | Combo | **Full**, **Gradient**, **Modern** – visual style of the circle. |
| **Hide 'screenshot stored in' message** | Checkbox | Hides the message that says where the screenshot was saved. |
| **Enable object fading** | Checkbox | Distant objects fade gradually. |
| **Enable text fading** | Checkbox | Text (overhead, etc.) fades over time. |
| **Show target range indicator** | Checkbox | Shows current target range on the cursor. |
| **Enable drag select for health bars** | Checkbox | Allows selecting multiple health bars by dragging (with modifier). |
| **Key modifier** | Combo | Key for drag select: **None**, **Ctrl**, **Shift**, **Alt**. |
| **Players only** / **Monsters only** / **Visible nameplates only** | Combo | Modifier to limit selection. |
| **X/Y Position of healthbars** | Slider | Initial position of health bars when opened by drag. |
| **Anchor opened health bars together** | Checkbox | Health bars opened by drag select are anchored as a group. |
| **Show stats changed messages** | Checkbox | Message when stats (str, dex, int) change. |
| **Show skills changed messages** | Checkbox | Message when skills change. |
| **Changed by** | Slider (0–100) | Minimum change value to show skills message. |

---

## Subsection: Terrain & Statics

| Option | Type | Description |
|--------|------|-------------|
| **Hide roof tiles** | Checkbox | Hides roofs to see inside buildings. |
| **Change trees to stumps** | Checkbox | Replaces trees with stumps visually; reduces view obstruction. |
| **Hide vegetation** | Checkbox | Hides vegetation (grass, bushes, etc.) for better visibility. |
| **Mark cave tiles** | Checkbox | Marks cave tiles and enables cave border/style. |
| **Magic field type** | Combo | How to draw magic fields: **Normal**, **Static**, **Tile**. |

---

[Back to index](README.md)
