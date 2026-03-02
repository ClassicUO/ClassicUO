# Guia de testes – Opções (ModernOptionsGump)

Como abrir: **in-game** → botão de Opções na barra superior (ou atalho configurado) → abre a janela **Options** com barra lateral de categorias.

Cada categoria da barra lateral tem **sub-abas** à esquerda do conteúdo. Alterações são salvas no perfil ao fechar o gump ou ao aplicar, conforme o caso.

---

## General

**Sub-abas:** General, Mobiles, Gumps & Context, Misc, Terrain and statics.

| Opção | Como testar |
|-------|-------------|
| **Highlight objects** | Ativar: clicar em item/mobile no mundo deve destacar com contorno. Desativar: sem destaque. |
| **Pathfinding** | Ativar e clicar no chão: personagem anda até o ponto. Desativar: movimento direto sem pathfinding. |
| **Shift pathfinding / Single click pathfind** | Com pathfinding ligado, testar com e sem Shift; clicar uma vez e ver se anda. |
| **Always run / Run unless hidden** | Andar no mundo: personagem corre ou anda; escondido deve respeitar "run unless hidden". |
| **Auto open doors** | Aproximar de porta: deve abrir sozinha. |
| **Auto open corpse** | Clicar em corpse ou passar perto (conforme distância): abre automaticamente. Ajustar **Corpse open distance** e **Skip empty**. |
| **Out range color** | Objetos fora do range de visão ficam sem cor (acinzentados) se ativado. |
| **Show mobile HP** | Barras de vida em mobiles: sempre, só &lt;100%, ou "smart". Ver **Mobile HP type** (%, barra ou ambos). |
| **Highlight poisoned / paralyzed / invul** | Envenenado/paralisado/invulnerável com cor de destaque (e cor escolhida no color picker). |
| **Aura under feet** | Desativado / só war / Ctrl+Shift / sempre. Andar e ver aura no chão; **Party aura** e cor. |
| **Disable top menu** | Barra superior some ou aparece. |
| **Alt for anchors / Alt to move gumps** | Segurar Alt para fechar gumps ancorados ou para mover gumps. |
| **Modern health bars / Save HP bars** | Abrir HP bar de um mobile; ativar opções; fechar e reabrir: barra deve reaparecer se "save" estiver ligado. |
| **Close HP gumps when** | Desativado / out of range / morto / ambos: barras fecham conforme regra. |
| **Circle of transparency (COT)** | Ativar e ajustar distância/tipo: área em volta do personagem fica transparente. |
| **Object fade / Text fade** | Objetos e textos longe ficam esmaecidos. |
| **Cursor range** | Mostra indicador de alcance ao mirar alvo. |
| **Drag select HP** | Arrastar seleção com modificador (Ctrl/Shift/Alt) para abrir barras de HP; testar **Drag key mod** e opções. |
| **Hide roof / Trees to stumps / Hide vegetation** | Teto, árvores e vegetação mudam ou somem. |
| **Mark cave tiles** | Bordas de cavernas destacadas. |
| **Magic field type** | Normal / estático / tile: aparência dos campos mágicos no chão. |

---

## Sound

| Opção | Como testar |
|-------|-------------|
| **Enable sound** | Liga/desliga todos os sons (combate, passos, UI). |
| **Sound volume** | Slider: 0 = mudo, 100 = máximo. Tocar um som e ajustar. |
| **Enable music** | Liga/desliga música de fundo. |
| **Music volume** | Slider da música. |
| **Footsteps / Combat music / Sounds in background** | Passos ao andar; música de combate; som com janela em segundo plano. |

---

## Video

**Sub-abas:** Game window, Zoom, Lighting, Misc, Shadows.

| Opção | Como testar |
|-------|-------------|
| **FPS cap** | Slider: limitar FPS (ex.: 60, 120). Ver taxa na tela se tiver contador. |
| **Enable VSync** | Ativar/desativar: deve reduzir tearing. |
| **Disable frame limiting** | FPS livre até o cap; desativar limita ao valor do cap. |
| **Disable combat/health lines overlay (performance)** | Menos overlays na tela; ver se linhas de combate/HP somem. |
| **Disable lights render target** | Luzes do mundo mudam (performance); cena pode ficar mais clara/escura. |
| **Reduce FPS when inactive** | Com janela sem foco, FPS cai. |
| **Fullsize viewport** | Viewport ocupa toda a janela ou não. |
| **Full screen** | Modo borderless (janela sem bordas). |
| **Lock viewport** | Impede redimensionar/mover a janela do jogo. |
| **Viewport X/Y/W/H** | Posição e tamanho da viewport; alterar e ver movimento/redimensionamento. |
| **Default zoom** | Slider: zoom inicial da câmera. |
| **Zoom with wheel** | Scroll do mouse aumenta/diminui zoom. |
| **Return default zoom** | Soltar Ctrl restaura zoom padrão. |
| **Alternative lights / Custom light level** | Estilo de iluminação e nível (slider). |
| **Dark night / Colored lights** | Noite mais escura; luzes coloridas. |
| **Enable death screen / BW dead** | Tela ao morrer; efeito preto e branco. |
| **Target aura** | Aura no alvo do mouse. |
| **Animated water** | Água animada. |
| **Visual style** | Classic vs Enhanced. |
| **Enable shadows / Rock-tree shadows** | Sombras no terreno e em rochas/árvores. |
| **Terrain shadow level** | Intensidade das sombras no terreno. |

---

## Macros

**Sub-abas:** Nova macro, lista de macros, edição.

| Opção | Como testar |
|-------|-------------|
| **New macro** | Criar macro com nome; deve aparecer na lista. |
| **Lista de macros** | Clicar em macro: abre editor. Editar tecla/ação, salvar. |
| **Executar macro** | Pressionar tecla de atalho no jogo e ver se a ação roda (falar, usar skill, etc.). |

---

## Tooltips

| Opção | Como testar |
|-------|-------------|
| **Enable tooltips** | Passar o mouse em item ou mobile no mundo: tooltip aparece (ativado) ou não (desativado). |
| **Tooltip delay** | Slider (0–1000 ms): tempo até o tooltip aparecer; aumentar = atraso maior. |
| **Tooltip background opacity** | Slider (0–100): opacidade do fundo do tooltip; 0 = transparente, 100 = opaco. |
| **Tooltip font color** | Color picker: cor do texto do tooltip. |

---

## Speech

| Opção | Como testar |
|-------|-------------|
| **Scale speech delay** | Se ativado, o delay de envio escala com o tamanho do texto. |
| **Speech delay** | Slider (0–1000): atraso em ms entre enviar mensagens no chat; testar falando várias vezes. |
| **Save journal to file** | Ativado: journal é salvo em arquivo no diretório do cliente; verificar se o arquivo é criado/atualizado. |
| **Chat enter activation** | Enter abre/foca o chat. |
| **Chat enter special / Shift+Enter** | Teclas adicionais para ativar chat (ex.: teclas especiais, Shift+Enter). |
| **Hide chat gradient** | Esconde o gradiente no fundo do chat. |
| **Hide guild chat** | Mensagens de guild não aparecem no journal. |
| **Hide alliance chat** | Mensagens de aliança não aparecem. |
| **Speech / Yell / Party / Alliance / Emote / Whisper / Guild / Chat color** | Color pickers: cor de cada tipo de mensagem no overhead e no journal. |

---

## Combat & Spells

| Opção | Como testar |
|-------|-------------|
| **Hold Tab for combat** | Manter Tab pressionado para abrir/manter painel de combate. |
| **Query before attack** | Pergunta antes de atacar (evitar criminal). |
| **Query before beneficial** | Pergunta antes de lançar magia benéfica em alvo que pode dar criminal. |
| **Enable overhead spell format** | Formato customizado do texto de magia no overhead. |
| **Spell overhead format** | Campo de texto: máscara (ex.: {spell}, {target}); ver tooltip. |
| **Enable overhead spell hue** | Usar cor customizada no overhead de magia. |
| **Single click for spell icons** | Um clique no ícone da magia já lança (sem confirmação). |
| **Show buff duration on old-style buff bar** | Duração dos buffs na barra clássica. |
| **Enable fast spell hotkey assigning** | Atribuição rápida de teclas a magias (ver tooltip). |
| **Innocent / Beneficial / Friend / Harmful / Criminal / Neutral / Can be attacked / Murderer / Enemy hue** | Color pickers: cor de cada notoriety e tipo de magia (overhead, cursor, etc.). |

---

## Counters

| Opção | Como testar |
|-------|-------------|
| **Enable counters** | Liga/desliga a barra de contadores; ao ativar, o gump aparece (ou é criado). |
| **Highlight items on use** | Ao usar item contado, destaca na barra. |
| **Abbreviated values** | Valores grandes mostrados abreviados (ex.: 1K, 1M). |
| **Abbreviate if amount exceeds** | Campo numérico: acima desse valor, usa abreviação. |
| **Highlight red when amount is low** | Contador fica vermelho quando quantidade está baixa. |
| **Highlight red if amount is below** | Campo numérico: limite para ficar vermelho. |
| **Grid size** | Slider: tamanho de cada célula da grade (30–100). |
| **Rows / Columns** | Número de linhas e colunas da barra de contadores. |

---

## Infobar

| Opção | Como testar |
|-------|-------------|
| **Show infobar** | Liga/desliga o gump da barra de informações (HP, mana, stam, etc.); ao ativar, cria ou exibe o gump. |
| **Highlight type** | Text color ou Colored bars: como os valores são destacados. |
| **Add item** | Botão: adiciona novo item à lista (ex.: HP, Mana, Gold); preenche label, cor e variável (Data). |
| **Lista de itens** | Cada item tem Label, Color, Data; editar e ver mudança na barra na tela. |

---

## Action Bar

| Opção | Como testar |
|-------|-------------|
| **Enable action bar** | Mostra ou esconde a barra de ações. |
| **Slots** | Cada slot pode receber skill ou spell (arrastar do paperdoll/skill gump/spell book). |
| **Clicar no slot** | Executa a ação configurada (ou abre submenu se houver). |
| **Posição** | Barra pode ser movida; posição é salva no perfil. |

---

## Containers

| Opção | Como testar |
|-------|-------------|
| **Character backpack style** | Default / Suede / Polar Bear / Ghoul Skin (se cliente ≥ 7.0.53.1): aparência da mochila. |
| **Container scale** | Slider: escala percentual do gump do container; abrir backpack/baú e ver tamanho. |
| **Also scale items** | Se ativado, ícones dos itens dentro do container também escalam. |
| **Use large container gumps** | Usar gumps grandes de container (se cliente ≥ 7.0.60.0). |
| **Double click to loot items inside containers** | Duplo clique no item dentro do container envia para backpack. |
| **Relative drag and drop** | Arrastar/soltar relativo à posição dentro do container. |
| **Highlight container on ground when mouse over gump** | Ao passar mouse no gump do container, o container no chão destaca. |
| **Recolor container gump by container hue** | Gump do container usa a cor (hue) do container. |
| **Override container gump locations** | Posição dos gumps de container é forçada. |
| **Override position** | Near container / Top right / Last dragged / Remember each: regra de posição. |
| **Rebuild containers** | Botão: reconstrói o arquivo de posições dos containers. |

---

## Nameplate Options

| Opção | Como testar |
|-------|-------------|
| **New entry** | Botão: cria novo perfil de nameplate (nome); aparece na lista à esquerda. |
| **Delete entry** | Com um perfil selecionado, remove o perfil. |
| **Lista de perfis** | Cada perfil tem nome; ao clicar, abre o painel de configuração à direita. |
| **Por perfil** | Configurar quando mostrar nameplate (ex.: sempre, em war, por notoriety), cor, opacidade, etc.; andar no mundo e ver nome/barra de HP sobre mobiles conforme as regras. |

---

## Cooldown bars

| Opção | Como testar |
|-------|-------------|
| **Position X / Y** | Campos numéricos: posição da barra de cooldown na tela. |
| **Use last moved bar position** | Se ativado, usa a última posição em que a barra foi arrastada. |
| **Lock cooldown bar** | Impede mover a barra. |
| **Add condition** | Botão: adiciona nova condição (ex.: por spell ID, por mensagem de sistema); reabre Options na aba Cooldowns. |
| **Cada condição** | Define quando uma barra de cooldown aparece (tipo, ID, texto, etc.); em combate ou ao usar magia, ver se a barra aparece e conta o tempo. |

---

## TazUO Specific

**Sub-abas:** Grid containers, Journal, Modern paperdoll, Nameplates, Mobiles, Misc, Tooltips, Font settings, Settings transfers, Gump scaling, Visible layers.

| Opção | Como testar |
|-------|-------------|
| **Enable grid containers** | Abrir backpack/baú: layout em grid. |
| **Grid container scale / Also scale items** | Tamanho dos ícones no grid. |
| **Border opacity, Border color** | Borda dos itens no grid. |
| **Container opacity, Background color** | Fundo do container. |
| **Search style** | Only show vs Highlight ao buscar no container. |
| **Enable container preview** | Preview ao passar mouse (se suportado). |
| **Make anchorable** | Grid pode ser ancorado a outros gumps. |
| **Container style / Hide borders** | Estilo da borda do grid. |
| **Default grid rows/columns** | Linhas e colunas padrão. |
| **Grid highlight settings** | Botão abre GridHightlightMenu; configurar destaque por propriedade. |
| **Max journal entries / Journal opacity** | Tamanho e transparência do journal. |
| **Journal style / Hide borders / Hide timestamp** | Aparência do journal. |
| **Enable modern paperdoll** | Paperdoll moderno; cores e barra de durabilidade. |
| **Nameplates (TazUO)** | HP nos nameplates; opacidade; esconder se vida cheia. |
| **Mobiles (TazUO)** | Cores de dano (self/others/pets/allies); party chat overhead; largura do texto overhead; barras de HP; opacidade de corpo escondido. |
| **Misc (TazUO)** | System chat, buff gump, cor de fundo da janela, health indicator, spell icon scale, shop gump, skill bar, spell indicators, import from URL (spell config), etc. |
| **Tooltips (TazUO)** | Alinhamento, cor de fundo, formato do header, override settings (abre ToolTipOverideMenu). |
| **Font settings** | Fonte e tamanho do infobar, system chat. |
| **Settings transfers** | Aviso e botões "Override all" / "Override same server": aplica perfil atual a outros personagens (cuidado: destrutivo). |
| **Gump scaling** | Escala do paperdoll e outros gumps. |
| **Visible layers** | Esconder camadas no paperdoll (capas, botas, etc.) por checkbox. |

---

## Dust765

Módulo Dust765: opções de combate, visuais e automação. **Sub-abas:** Art Hue Changes, Visual Helpers, Health Bars, Cursor, Overhead/Underchar, Old Healthlines, Misc, Misc2, Auto Loot, Buffbar UCC, Self Automations, Macros, Gumps, Texture Manager, Lines (Lines UI), PvM/PvP Section.

### Art Hue Changes (sub-aba 1)

| Opção | Como testar |
|-------|-------------|
| **Color stealth** | Personagem em stealth com cor customizada; ativar e escolher **Stealth color** ou **Or Neon** (Off/White/Pink/Ice/Fire). |
| **Color energy bolt** | Projétil de Energy Bolt com cor ou neon; **Change energy bolt art to** (Normal/Explo/Bagball). |
| **Change gold art to** | Ouro como Normal, Cannonball ou Prev Coin; **Color cannonball/prev coin** e cor. |
| **Change tree art to** | Árvores como Normal, Stump ou Tile; **Color stump/tile** e cor. |
| **Blocker type** | Bloqueadores como Normal/Stump/Tile; cor opcional. |

### Visual Helpers (sub-aba 2)

| Opção | Como testar |
|-------|-------------|
| **Highlight tiles on range** | Destaca tiles no chão ao alcance; **At range** (1–20) e **Tile color**. |
| **Highlight tiles on range (spell)** | Idem para alcance de magia; range e cor separados. |
| **Preview fields** | Preview visual de campos (poison, energy, etc.) ao mirar. |
| **Preview teleport tiles** | Preview dos tiles de teleport com cor configurável. |
| **Color own aura by HP** | Sua aura no chão muda de cor conforme sua vida. |
| **Glowing weapons** | Armas brilham (Off/White/Pink/Ice/Fire/Custom); cor custom se Custom. |
| **Highlight last target** | Último alvo destacado (mesmas opções de cor/neon). |
| **Highlight last target poisoned/paralyzed** | Destaque extra para envenenado e paralisado no último alvo. |

### Health Bars (sub-aba 3)

| Opção | Como testar |
|-------|-------------|
| **Highlight last target healthbar** | Contorno na barra de HP do último alvo. |
| **Highlight health bar by state** | Cor/estado da barra conforme notoriety/condição. |
| **Flashing healthbar outline** | Contorno piscando: Self, Party, Ally, Enemy, All; **Negative only** e **Only flash on HP change** (slider 0–100). |

### Cursor (sub-aba 4)

| Opção | Como testar |
|-------|-------------|
| **Show spells on cursor** | Ícone da magia selecionada aparece no cursor. |
| **Spell icon offset X/Y** | Ajuste fino da posição do ícone no cursor. |
| **Color game cursor when targeting** | Cor do cursor ao mirar alvo. |

### Overhead / Underchar (sub-aba 5)

| Opção | Como testar |
|-------|-------------|
| **Display range in overhead** | Mostra alcance no overhead (ex.: ao mirar magia). |

### Old Healthlines (sub-aba 6)

| Opção | Como testar |
|-------|-------------|
| **Use old healthlines** | Barras de HP no estilo antigo (linhas sob o mobile). |
| **Display mana/stam in underline** | Mana e stamina nas underlines para self/party. |
| **Use bigger underlines** | Underlines maiores. |
| **Transparency for self and party** | Slider de transparência (0–10). |

### Misc (sub-aba 7)

| Opção | Como testar |
|-------|-------------|
| **Offscreen targeting** | Permite mirar alvo fora da tela. |
| **Set target out range** | Comportamento ao definir alvo fora do alcance. |
| **Override container open range** | Alcance custom para abrir containers. |
| **Show close friend in world map** | Amigos próximos no mapa do mundo. |
| **Auto avoid obstacules and mobiles** | Pathfinding evita obstáculos e mobiles. |
| **Show use loot modal on Ctrl** | Modal de loot próximo ao segurar Ctrl. |
| **Razor target to last target string** | Texto custom (cliloc) para “last target”. |
| **Outline statics black** | Contorno preto em statics. |
| **Ignore stamina check** | Ignora verificação de stamina (movimento/ação). |
| **Scale monsters (non-humanoid)** | Slider de escala para monstros; selecionar no mapa e usar [−] [+]. |
| **Block Wall of Stone** | Bloqueia Wall of Stone como impassável; **Fel only**, **Wall of Stone art**, **Force WoS art/hue**. |
| **Block Energy Field** | Bloqueia Energy Field; **Fell only**, **Energy Field art**, **Force art/hue**. |

### Misc2 (sub-aba 8)

| Opção | Como testar |
|-------|-------------|
| **WireFrame view** | (Marcado como CURRENTLY BROKEN) Modo wireframe; reinício necessário. |
| **Hue impassable tiles** | Tiles impassáveis com cor (hue) para debug. |
| **Transparent houses and items (Z level)** | Casas/itens acima de Z ficam transparentes; **Transparency Z** e **Transparency** (sliders). |
| **Invisible houses and items (Z level)** | Idem mas invisíveis; **Invisible Z**; **Don’t make invisible/transparent below Z**. |
| **Draw mobiles with surface overhead** | Mobiles desenhados com superfície de overhead. |
| **Ignore list for circle of transparency** | Lista de ignorados afeta o COT. |
| **Show death location on world map (5 min)** | Marca local da morte no mapa por 5 minutos. |
| **Macros:** ToggleTransparentHouses / ToggleInvisibleHouses | Atalhos para ligar/desligar transparência/invisibilidade. |

### Auto Loot (sub-aba 9)

| Opção | Como testar |
|-------|-------------|
| **Enable UCC - AL** | Liga o gump UO Classic Combat Auto Loot; posição salva. |
| **Enable GridLootColoring** | Cores no grid de loot. |
| **Enable LootAboveID** | Loot apenas itens com ID acima do valor. |
| **Time between looting two items (ms)** | Delay entre pegar dois itens. |
| **Time to purge queue (ms)** / **Time between processing queue (ms)** | Controle da fila de loot. |
| **Loot above ID** | ID mínimo para lootear. |
| **Gray / Blue / Green / Red corpse color** | Cores por notoriety do corpse (IDs de hue). |

### Buffbar UCC (sub-aba 10)

| Opção | Como testar |
|-------|-------------|
| **Enable UCC - Buffbar** | Liga a barra de buffs do UO Classic Combat. |
| **Show Swing Line** | Linha de swing na barra. |
| **Show Do Disarm Line** | Linha de Do Disarm. |

### Self Automations (sub-aba 11)

Opções de automação do próprio personagem (curar, buff, etc.). Testar cada ação configurada em combate ou fora.

### Macros (Dust765, sub-aba 12)

Macros específicos do Dust765. Criar/editar e executar no jogo.

### Gumps (sub-aba 13)

Configurações de gumps do Dust765 (posição, visibilidade, etc.).

### Texture Manager (sub-aba 14)

Gestão de texturas/artes substituídas pelo Dust765. Ver se itens/mobiles mudam de gráfico conforme config.

### Lines (Lines UI) (sub-aba 15)

Opções da UI de linhas (combate, HP, etc.). Ver linhas na tela e ajustar estilo/posição.

### PvM/PvP Section (sub-aba 16)

| Opção | Como testar |
|-------|-------------|
| **PvM: Damage counter on last target** | Contador de dano no último alvo. |
| **PvM: Damage counter as overhead** | Contador como texto no overhead. |
| **PvM: Aggro indicator on health bar** | Indicador de aggro na barra de HP. |
| **PvM: Corpse filter by notoriety** | Filtro de corpses por notoriety; **Corpse filter mode** (All/Friendly/Enemy). |
| **PvM: Low HP alert on last target** | Alerta quando último alvo está com vida baixa. |
| **PvM: Kill count marker per session** | Marca kills na sessão. |
| **PvM: Loot highlight on corpse** | Destaque de loot no corpse. |
| **PvP: Criminal attackable alert** | Alerta quando criminal pode ser atacado. |
| **PvP: War mode indicator** | Indicador de war mode. |
| **PvP: Grey criminal timer** | Timer de criminal grey. |
| **PvP: Last attacker highlight** | Destaque do último atacante. |
| **PvP: Spell range on cursor** | Alcance da magia no cursor. |
| **PvP: Quick target enemy list** | Lista rápida de alvos inimigos. |
| **PvP: Optimized mode** | Modo otimizado PvP. |
| **PvX: Name overhead profiles by context** | Perfis de nameplate por contexto. |
| **PvX: Configurable sounds per event** | Sons por evento; **Criminal alert sound ID**. |
| **PvX: Block beneficial on enemies** | Bloqueia magia benéfica em inimigos. |
| **PvX: Last target direction indicator** | Indicador de direção do último alvo. |
| **PvX: Lock last target** | Trava o último alvo. |

---

## Experimental

| Opção | Como testar |
|-------|-------------|
| **Disable default UO hotkeys** | Atalhos padrão do UO (ex.: I para inventário) deixam de funcionar. |
| **Disable arrows / Numlock arrows for movement** | Setas e Numlock não movem o personagem. |
| **Disable Tab for toggle warmode** | Tab não alterna war mode. |
| **Disable Ctrl+Q/W for message history** | Ctrl+Q e Ctrl+W não navegam no histórico do chat. |
| **Disable right/left click auto move** | Clique direito/esquerdo no chão não movem automaticamente. |

Ativar uma por vez e verificar comportamento e estabilidade.

---

## Ignore List

Botão na barra lateral: abre **Ignore Manager** (lista de ignorados). Adicionar/remover jogadores e ver se mensagens e visibilidade são bloqueadas.

---

## Busca

Campo de busca no topo: digitar texto filtra as opções exibidas (por nome da opção ou da categoria). Testar termos como "sound", "zoom", "grid".

---

## Dicas para testes

1. **Perfil:** Opções são por personagem (perfil). Trocar de personagem e ver se as opções mudam.
2. **Aplicar/Fechar:** Algumas opções aplicam na hora; outras ao fechar o gump. Fechar e reabrir para confirmar persistência.
3. **Reinício:** Algumas (ex.: FPS, fullscreen) podem precisar de restart do cliente para efeito completo.
4. **Performance:** Opções em Video (disable overlays, disable lights) e TazUO (grid, journal) afetam FPS; comparar com e sem.
