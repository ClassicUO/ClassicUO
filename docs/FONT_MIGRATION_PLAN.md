# Plano de Migração: Remoção de Fontes TTF da Aplicação

## Contexto

O ClassicUO utiliza **dois sistemas de fontes distintos**:

1. **FontsLoader** (fontes bitmap UO) – `fonts.mul`, `unifont*.mul` – sem TTF em runtime
2. **TrueTypeLoader** – TTF via FontStashSharp – carrega `Roboto-Regular.ttf` embarcada + pasta `Fonts/*.ttf`

O fork [dust765/ClassicUO_old](https://github.com/dust765/ClassicUO_old/commits/dev_dust765) **não possui TrueTypeLoader** – utiliza apenas FontsLoader e fontes bitmap (XNB). Texto e botões são renderizados via fontes UO sem dependência de TTF.

---

## Inventário de Uso de TTF (TrueTypeLoader)

### 1. Componentes de UI que dependem de TTF

| Arquivo | Uso |
|---------|-----|
| `TextBox.cs` | Base: usa `TrueTypeLoader.Instance.GetFont(font, size)` – todos os TextBox dependem de TTF |
| `GothicStyleButton.cs` | `TrueTypeLoader.GetFont("Arial", fontSize)` |
| `GothicStyleCombobox.cs` | `TrueTypeLoader.GetFont("Arial", fontSize)` |
| `GothicStyleSliderBar.cs` | `TrueTypeLoader.GetFont("Arial", fontSize)` |
| `DarkRedButton.cs` | `TrueTypeLoader.GetFont(...)` |

### 2. Gumps que usam TextBox + TrueTypeLoader.EMBEDDED_FONT

| Categoria | Arquivos |
|-----------|----------|
| Login | `LoginGump.cs`, `CharacterSelectionGump.cs`, `ServerSelectionGump.cs`, `LoadingGump.cs`, `LoginMessageBoxGump.cs` |
| Criação de personagem | `CreateCharAppearanceGump.cs`, `CreateCharProfessionGump.cs` |
| Opções/UI | `OptionsGump.cs`, `ModernOptionsGump.cs`, `ModernShopGump.cs`, `ProgressBarGump.cs`, `MobileScaleGump.cs`, `AutoLootOptions.cs`, `NearbyItems.cs`, `InputRequest.cs` |
| LegionScripting | `LegionScriptStudioGump.cs`, `ScriptManagerGump.cs`, `RunningScriptsGump.cs`, `ScriptEditor.cs`, `LineNumberEditor.cs`, `ScriptBrowser.cs` |
| Outros | `VersionHistory.cs`, `SimpleTimedTextGump.cs`, `CommandsGump.cs`, `InfoBarGump.cs`, `NameOverheadGump.cs`, `EntityTextContainer.cs`, `XmlGumpHandler.cs`, `CustomToolTip.cs`, `Tooltip.cs`, etc. |

### 3. ProfileManager – configurações de fonte TTF

- `SelectedTTFJournalFont`
- `InfoBarFont`
- `OverheadChatFont`
- `GameWindowSideChatFont`
- `SelectedToolTipFont`
- `NamePlateFont`

### 4. TrueTypeLoader e dependências

- `src/ClassicUO.Assets/TrueTypeLoader.cs` – carrega TTF
- `ClassicUO.Assets.csproj` – `Roboto-Regular.ttf` como embedded resource
- `external/FontStashSharp` – rasterização de TTF (StbTrueTypeSharp)
- Pasta `Fonts/` em runtime – carrega `*.ttf` adicionais

---

## Alternativas sem TTF em Runtime

### Opção A: Fontes UO (FontsLoader)

**Já em uso em:** RenderedText, StbTextBox, HtmlControl, journal, overhead, gumps de jogo.

- **API:** `FontsLoader.Instance.GenerateUnicode()`, `GenerateASCII()`, etc.
- **Unidades:** `byte font` (0–9 ASCII, 0–19 Unicode), `ushort hue`
- **Formato:** bitmap pré-renderizado em textura

**Prós:** Sem TTF, já integrado ao jogo, aparência UO autêntica  
**Contras:** API diferente de TextBox, fontes fixas do UO, sem redimensionamento fluido

### Opção B: SpriteFont XNB (já existente)

- `Fonts.Regular`, `Fonts.Bold`, `Fonts.Map1`–`Map6` – `regular_font.xnb`, `bold_font.xnb`, `map*.xnb`
- Usado em `WorldMapGump` e `Batcher2D.DrawString()`
- Fontes bitmap compiladas – sem TTF em runtime

**Prós:** Sem TTF em runtime, API SpriteFont simples  
**Contras:** `TextBox` usa `FontStashSharp.RichText` (espera `SpriteFontBase`), incompatibilidade de API

### Opção C: Pré-compilação TTF → XNB (build time)

- Pipeline de conteúdo compila TTF em XNB no build
- Runtime carrega apenas XNB (bitmap)
- TTF só em desenvolvimento, não na distribuição

**Prós:** Mantém aparência das fontes atuais, sem TTF em runtime  
**Contras:** Exige adaptar content pipeline, converter TextBox para SpriteFont XNA

---

## Plano de Migração Recomendado

### Fase 1: Criar camada de abstração de fonte

1. **Interface `IFontProvider`** em `ClassicUO.Assets`:
   - `SpriteFontBase GetFont(string name, float size)` ou equivalente
   - Implementações: `TrueTypeFontProvider`, `UOFontProvider`, `XNBFontProvider`

2. **`FontService`** central:
   - Configurável: `UseTTF`, `UseUOFonts`, `UseXNBFonts`
   - Retorna fonte apropriada conforme configuração

### Fase 2: Implementar UOFontProvider (FontsLoader)

1. **`UOLabel`** – control para texto estático usando FontsLoader:
   - Parâmetros: `byte font`, `ushort hue`, `string text`, `TEXT_ALIGN_TYPE align`
   - Baseado em RenderedText / FontsLoader
   - Equivalente a TextBox para labels estáticos

2. **Mapear tamanhos TTF → font UO:**
   - Ex.: size 12 → font 0, size 16 → font 1, etc.
   - Ou usar Unicode fonts 0–19 como "tamanhos"

### Fase 3: Substituir TextBox por UOLabel onde possível

1. **Labels estáticos** (não editáveis):
   - Login, seleção de personagem, títulos de gumps → `UOLabel`
   - Ex.: `new TextBox("Legion Script Studio", TrueTypeLoader.EMBEDDED_FONT, 20, ...)` → `new UOLabel("Legion Script Studio", 1, hue, ...)`

2. **Áreas editáveis:**
   - Manter `StbTextBox` (já usa FontsLoader) para campos de texto
   - Trocar `TTFTextInputField` por `StbTextBox` onde fizer sentido

### Fase 4: Substituir controles GothicStyle

1. **GothicStyleButton / GothicStyleCombobox / GothicStyleSliderBar:**
   - Trocar `TrueTypeLoader.GetFont()` por FontsLoader ou XNB
   - Ou desenhar texto com `RenderedText`/FontsLoader no botão

2. **DarkRedButton:** mesma abordagem

### Fase 5: ProfileManager e seletores de fonte

1. **Remover referências TTF:**
   - `SelectedTTFJournalFont` → índice de fonte UO (0–19)
   - `InfoBarFont`, `OverheadChatFont`, etc. → `byte font` UO

2. **OptionsGump / ModernOptionsGump:**
   - `GenerateFontSelector` → lista de fontes UO (Unicode 0–19)
   - Remover dependência de `TrueTypeLoader.Instance.Fonts`

### Fase 6: Remover TrueTypeLoader

1. Remover `TrueTypeLoader.cs`
2. Remover `Roboto-Regular.ttf` e referências
3. Remover dependência de FontStashSharp para TextBox (ou manter só para XNB se houver bridge)
4. Avaliar remoção de FontStashSharp se não houver outros usos
5. Remover pasta `Fonts/` do fluxo de carga

---

## Ordem de Execução Sugerida

| # | Tarefa | Complexidade | Impacto |
|---|--------|--------------|---------|
| 1 | Criar `UOLabel` baseado em FontsLoader | Média | Alto |
| 2 | Criar `IFontProvider` + `FontService` | Baixa | Médio |
| 3 | Migrar gumps de Login para UOLabel | Média | Alto |
| 4 | Migrar LegionScriptStudio/Gumps para UOLabel | Média | Alto |
| 5 | Migrar GothicStyle* para FontsLoader | Média | Médio |
| 6 | Atualizar ProfileManager (fontes UO) | Média | Alto |
| 7 | Migrar demais TextBox estáticos | Alta | Alto |
| 8 | Remover TrueTypeLoader e Roboto | Baixa | - |

---

## Documentos Relacionados

- **RENDER_ANALYSIS.md** – Análise da renderização e possíveis causas de travada em relação ao dust765/ClassicUO_old (inclui impacto de TTF e camadas extras)

## Referências

- **FontsLoader** – `src/ClassicUO.Assets/FontsLoader.cs` – API de fontes UO
- **RenderedText** – `src/ClassicUO.Client/Game/GameObjects/RenderedText.cs` – uso de FontsLoader
- **StbTextBox** – `src/ClassicUO.Client/Game/UI/Controls/StbTextBox.cs` – campo de texto sem TTF
- **Fonts** (XNB) – `src/ClassicUO.Renderer/Fonts.cs` – fontes bitmap
- **dust765/ClassicUO_old** – exemplo de projeto sem TrueTypeLoader: [commits dev_dust765](https://github.com/dust765/ClassicUO_old/commits/dev_dust765)
