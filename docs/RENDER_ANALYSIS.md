# Análise: Renderização Mais Leve do ClassicUO_old (dust765)

## Contexto

O fork [dust765/ClassicUO_old](https://github.com/dust765/ClassicUO_old/commits/dev_dust765) aparentemente apresenta renderização mais leve e sem travadas em comparação à versão atual. Esta análise compara os dois repositórios para identificar as diferenças que podem explicar o desempenho.

---

## Diferenças Principais (ClassicUO_old vs. Atual)

### 1. TargetElapsedTime / Controle de FPS

| Versão      | TargetElapsedTime           | Efeito                          |
|------------|-----------------------------|----------------------------------|
| **dust765 old** | `1000.0 / 250.0` ≈ 4 ms    | Cap de ~250 FPS, frame pacing estável |
| **Atual**  | `1` ms                      | Cap de 1000 FPS, CPU muito solicitada |

**Impacto:** `TargetElapsedTime = 1` força Update/Draw a rodar com meta de 1000 FPS, aumentando uso de CPU e risco de micro-stuttering e frame pacing irregular. O dust765 usa um cap mais moderado (250 FPS), o que tende a estabilizar o loop.

### 2. Fontes TTF (TrueTypeLoader)

| Versão      | Fontes TTF                  | Impacto na renderização                |
|------------|-----------------------------|-----------------------------------------|
| **dust765 old** | Sem TrueTypeLoader        | Apenas fontes UO (bitmap)               |
| **Atual**  | TrueTypeLoader + FontStashSharp | Rasterização TTF em tempo real |

**Impacto:** FontStashSharp gera glifos sob demanda. A primeira vez que um caractere aparece causa um pico de CPU na rasterização TTF. Cada novo caractere pode gerar um pequeno micro-stutter, o que contribui para a sensação de travada. O dust765 evita isso ao usar só fontes bitmap UO.

### 3. Camadas extras de overlay no Draw

| Camada                | dust765 old     | Atual          |
|-----------------------|-----------------|----------------|
| TextureManager        | Ausente         | Presente       |
| UOClassicCombatLines  | Ausente         | Presente       |
| HealthLinesManager    | Ausente         | Presente       |

**Impacto:** O loop `DrawOverheads` atual chama `_textureManager.Draw`, `_UOClassicCombatLines.Draw` e `_healthLinesManager.Draw` em todo frame. Cada um aumenta draw calls, state changes e trabalho de CPU/GPU. O dust765 (versão base) não tem essas camadas, reduzindo custo por frame.

### 4. Render targets

| Versão      | Render targets                             |
|------------|---------------------------------------------|
| **dust765 old** | Pipeline mais simples                      |
| **Atual**  | `_world_render_target`, `_lightRenderTarget` |

**Impacto:** Cada `SetRenderTarget` força flush/barrier no pipeline gráfico. Usar dois render targets (mundo + luzes) aumenta o número de switches e pode gerar micro-stutters, principalmente em GPUs mais fracas ou integradas.

### 5. Inicialização e otimizações adicionais

| Versão      | Inicialização                                           |
|------------|----------------------------------------------------------|
| **dust765 old** | `Initialize` simples, sem otimizadores extras           |
| **Atual**  | `PerformanceOptimizer.ApplyGraphicsQualitySettings()`, `OptimizeForPerformance()`, `OptimizeSDL2ForHighFPS()` |

**Impacto:** Essas rotinas podem alterar estado de SDL2, gráficos e otimizações. Em alguns cenários, podem gerar overhead ou interações inesperadas com drivers/SDL, em vez de só ganho.

### 6. Update por frame

| Versão      | Update adicional por frame                                |
|------------|------------------------------------------------------------|
| **dust765 old** | Apenas `Scene.Update()`, `UIManager.Update()`            |
| **Atual**  | + `LegionScripting.OnUpdate()`, `PerformanceOptimizer.UpdatePvPMode()`, `EventSink.GameUpdate` |

**Impacto:** `LegionScripting.OnUpdate` processa scripts (Python, LScript, UOScript) em todo frame. Com scripts rodando, aumenta uso de CPU e pode afetar consistência do frame time. O dust765 não possui LegionScripting integrado no loop principal.

### 7. UIManager.SlowUpdate

| Versão      | SlowUpdate                               |
|------------|-------------------------------------------|
| **dust765 old** | Ausente                                 |
| **Atual**  | Chamado a cada 500 ms                     |

**Impacto:** Overhead menor, mas soma trabalho periódico à carga de Update.

### 8. Profiler

| Versão      | Profiler                              |
|------------|----------------------------------------|
| **dust765 old** | Ausente ou mínimo                     |
| **Atual**  | `Profiler.EnterContext`, `Profiler.ExitContext` em vários pontos |

**Impacto:** O profiler adiciona custo de contexto e possivelmente alocação; em builds de release isso pode ser reduzido, mas em debug contribui para stuttering.

---

## Causas prováveis de travada/stuttering na versão atual

1. **TargetElapsedTime = 1 ms** – loop muito agressivo, CPU em alta carga.
2. **Rasterização TTF em tempo real** – picos de CPU quando novos glifos são gerados.
3. **Várias camadas de overlay** – TextureManager, CombatLines, HealthLines aumentam draw calls e uso de GPU.
4. **Uso de render targets** – switches de target adicionais aumentam latência.
5. **LegionScripting.OnUpdate** – processamento de scripts em todo frame.
6. **Falta de frame pacing estável** – com `DisableFrameLimiting`, não há throttling e o FPS pode oscilar, causando micro-stutters.

---

## Recomendações para aproximar o desempenho ao dust765

1. ~~Ajustar `TargetElapsedTime` de `1` ms para algo como `1000.0 / 250.0` (4 ms).~~ **Feito:** constructor e `SetRefreshRate` usam 250 FPS cap quando frame limiting desativado.
2. ~~Migrar fontes de TTF para fontes bitmap (UO ou XNB)~~ **Feito:** `UOLabel` (FontsLoader), `IFontProvider`/`FontService`, `UOLabelHue` (Text/Accent/Hover). Labels estáticos migrados para `UOLabel` em: Login (LoginGump, CharacterSelectionGump, LoadingGump, LoginMessageBoxGump, ServerSelectionGump), LegionScripting (LegionScriptStudioGump, ScriptManagerGump, RunningScriptsGump, ScriptEditor), CreateChar (CreateCharAppearanceGump, CreateCharProfessionGump), ProgressBarGump, InputRequest, CommandsGump, VersionHistory, ModernShopGump, NearbyItems, ModernOptionsGump (títulos, headers, labels de features/settings). Pendentes: controles compostos em ModernOptionsGump (_label em ColorPickerWithLabel, SliderWithLabel, ComboBoxWithLabel, etc.), LineNumberEditor (números de linha), GothicStyle* (usa TTF internamente), ProfileManager (fontes TTF), remoção TrueTypeLoader/Roboto.
3. ~~Disponibilizar opções para desabilitar TextureManager, UOClassicCombatLines e HealthLines quando prioridade for desempenho.~~ **Feito:** `PerformanceDisableCombatLinesOverlay`, `PerformanceDisableHealthLinesOverlay` em Profile; TextureManager já respeitava `TextureManagerEnabled`. Opções em Opções > Game Window.
4. ~~Avaliar simplificação do pipeline de lights (menos render targets ou fallback quando não necessários).~~ **Feito:** `PerformanceDisableLightsRenderTarget` em Profile; quando ativo, `PrepareLightsRendering` retorna sem usar o render target.
5. ~~Executar `LegionScripting.OnUpdate` em tick rate menor (ex.: 10–20 Hz) em vez de todo frame.~~ **Feito:** chamada limitada a cada 50 ms (20 Hz) em `GameController.Update`.
6. ~~Garantir throttling adequado quando `DisableFrameLimiting` estiver desativado.~~ **Já garantido:** lógica de `_intervalFixedUpdate` e `_suppressedDraw` mantida; com limiting ativado o cap de FPS segue `Settings.GlobalSettings.FPS`.

---

## Referências

- **dust765 ClassicUO_old** – [main branch GameController](https://raw.githubusercontent.com/dust765/ClassicUO_old/main/src/ClassicUO.Client/GameController.cs)
- **Commits dust765** – [dev_dust765](https://github.com/dust765/ClassicUO_old/commits/dev_dust765)
- **Plano de migração de fontes** – `FONT_MIGRATION_PLAN.md`
