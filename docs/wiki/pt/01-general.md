# General

Opções gerais do cliente, organizadas em subsecções: **General**, **Mobiles**, **Gumps & Context**, **Misc**, **Terrain & Statics**.

---

## Subsecção: General

| Opção | Tipo | Descrição |
|-------|------|-----------|
| **Highlight objects under cursor** | Checkbox | Quando activo, objectos sob o cursor são realçados visualmente. Ajuda a identificar alvos e itens. |
| **Enable pathfinding** | Checkbox | Activa pathfinding automático: o personagem contorna obstáculos ao clicar para se mover. |
| **Use shift for pathfinding** | Checkbox | (Requer pathfinding.) Só usa pathfinding quando mantém **Shift** premido ao clicar. Sem Shift, movimento em linha recta. |
| **Single click for pathfinding** | Checkbox | (Requer pathfinding.) Um único clique inicia o movimento com pathfinding, em vez de duplo clique. |
| **Always run** | Checkbox | O personagem corre por defeito em vez de andar. |
| **Unless hidden** | Checkbox | (Requer Always run.) Corre sempre excepto quando está em modo stealth/hidden. |
| **Automatically open doors** | Checkbox | Abre portas automaticamente ao passar por elas. |
| **Open doors while pathfinding** | Checkbox | (Requer pathfinding.) Durante o pathfinding, abre portas no caminho para não ficar bloqueado. |
| **Automatically open corpses** | Checkbox | Abre automaticamente corpos (corpses) ao aproximar-se. |
| **Corpse open distance** | Slider (0–5) | Distância em tiles à qual os corpos são abertos automaticamente. |
| **Skip empty corpses** | Checkbox | Não abre corpos que já estão vazios (sem itens). |
| **Corpse open options** | Combo | Quando não abrir: **None**, **Not targeting** (não abrir se tiver alvo), **Not hiding** (não abrir se estiver hidden), **Both**. |
| **No color for out of range objects** | Checkbox | Objectos fora do alcance de visão são desenhados sem cor (cinzentos), para destacar o que está ao alcance. |
| **Enable sallos easy grab** | Checkbox | Modo de agarrar itens estilo Sallos. Não recomendado com grid containers activos (ver tooltip). |
| **Show house content** | Checkbox | (Depende da versão do cliente.) Mostra conteúdo de casas no mapa ou em contexto. |
| **Smooth boat movements** | Checkbox | (Depende da versão do cliente.) Movimento de barcos mais suave. |

---

## Subsecção: Mobiles

| Opção | Tipo | Descrição |
|-------|------|-----------|
| **Show mobile's HP** | Checkbox | Mostra a vida (HP) dos mobiles – pode ser em percentagem, barra ou ambos. |
| **Type** | Combo | **Percentage** (%), **Bar** (barra de vida), **Both** (os dois). |
| **Show when** | Combo | **Always** (sempre), **Less than 100%** (só quando vida &lt; 100%), **Smart** (lógica automática). |
| **Highlight poisoned mobiles** | Checkbox | Mobiles envenenados são realçados com uma cor. |
| **Highlight color** (poison) | Color picker | Cor do realce para envenenados. |
| **Highlight paralyzed mobiles** | Checkbox | Mobiles paralizados são realçados. |
| **Highlight color** (paralyze) | Color picker | Cor do realce para paralizados. |
| **Highlight invulnerable mobiles** | Checkbox | Mobiles invulneráveis são realçados. |
| **Highlight color** (invul) | Color picker | Cor do realce para invulneráveis. |
| **Show incoming mobile names** | Checkbox | Mostra o nome dos mobiles quando entram no ecrã. |
| **Show incoming corpse names** | Checkbox | Mostra o nome dos corpos quando entram no ecrã. |
| **Show aura under feet** | Combo | **Disabled**, **Warmode** (só em warmode), **Ctrl + Shift** (com teclas), **Always**. |
| **Use a custom color for party members** | Checkbox | Aura dos membros do grupo com cor personalizada. |
| **Party aura color** | Color picker | Cor da aura do grupo. |

---

## Subsecção: Gumps & Context

| Opção | Tipo | Descrição |
|-------|------|-----------|
| **Disable top menu bar** | Checkbox | Esconde a barra de menu no topo do ecrã. |
| **Require alt to close anchored gumps** | Checkbox | Gumps ancorados só fecham se mantiver **Alt** premido. Evita fechar por engano. |
| **Require alt to move gumps** | Checkbox | Só permite arrastar/mover gumps com **Alt** premido. |
| **Close entire group of anchored gumps with right click** | Checkbox | Clique direito num gump ancorado fecha todo o grupo ancorado. |
| **Use original skills gump** | Checkbox | Usa o gump clássico de skills em vez do moderno. |
| **Use old status gump** | Checkbox | Usa o gump de status antigo. |
| **Show party invite gump** | Checkbox | Mostra janela de convite para grupo. |
| **Use modern health bar gumps** | Checkbox | Usa as barras de vida modernas (Dust765/estilo novo). |
| **Use black background** | Checkbox | (Com modern health bars.) Fundo preto nas barras de vida. |
| **Save health bars on logout** | Checkbox | Mantém as barras de vida abertas e posições ao sair e voltar. |
| **Close health bars when** | Combo | **Disabled**, **Out of range**, **Dead**, **Both** (OOR + morto). |
| **Grid Loot** | Combo | **Disabled**, **Grid loot only**, **Grid loot and normal container**. Ver tooltip: é o gump de loot em grelha para corpos, não os grid containers. |
| **Require shift to open context menus** | Checkbox | Menus de contexto (clique direito) só abrem com **Shift** premido. |
| **Require shift to split stacks of items** | Checkbox | Dividir pilhas de itens requer **Shift**. |

---

## Subsecção: Misc

| Opção | Tipo | Descrição |
|-------|------|-----------|
| **Enable circle of transparency** | Checkbox | Activa o círculo de transparência à volta do personagem (objectos dentro ficam transparentes). |
| **Distance** | Slider | Raio do círculo de transparência. |
| **Type** | Combo | **Full**, **Gradient**, **Modern** – estilo visual do círculo. |
| **Hide 'screenshot stored in' message** | Checkbox | Esconde a mensagem que indica onde o screenshot foi guardado. |
| **Enable object fading** | Checkbox | Objectos longe desvanecerem gradualmente. |
| **Enable text fading** | Checkbox | Texto (overhead, etc.) desvanece com o tempo. |
| **Show target range indicator** | Checkbox | Mostra no cursor o alcance do alvo actual. |
| **Enable drag select for health bars** | Checkbox | Permite seleccionar várias barras de vida arrastando (com modificador). |
| **Key modifier** | Combo | Tecla para drag select: **None**, **Ctrl**, **Shift**, **Alt**. |
| **Players only** | Combo | Modificador para limitar a jogadores. |
| **Monsters only** | Combo | Modificador para limitar a monstros. |
| **Visible nameplates only** | Combo | Modificador para limitar a nameplates visíveis. |
| **X Position of healthbars** | Slider | Posição X inicial das barras de vida ao abrir por drag. |
| **Y Position of healthbars** | Slider | Posição Y inicial. |
| **Anchor opened health bars together** | Checkbox | Barras abertas por drag select ficam ancoradas em grupo. |
| **Show stats changed messages** | Checkbox | Mensagem quando os stats (str, dex, int) mudam. |
| **Show skills changed messages** | Checkbox | Mensagem quando as skills mudam. |
| **Changed by** | Slider (0–100) | Valor mínimo de mudança (em “volume”) para mostrar mensagem de skills. |

---

## Subsecção: Terrain & Statics

| Opção | Tipo | Descrição |
|-------|------|-----------|
| **Hide roof tiles** | Checkbox | Esconde telhados para ver o interior de edifícios. Útil em casas e cidades. |
| **Change trees to stumps** | Checkbox | Substitui árvores por tocos (stumps) visualmente; reduz obstrução de vista e arte alternada. |
| **Hide vegetation** | Checkbox | Esconde vegetação (erva, arbustos, etc.) para melhor visibilidade. |
| **Mark cave tiles** | Checkbox | Marca tiles de caverna e activa o estilo/borda de caverna. |
| **Magic field type** | Combo | Como desenhar campos mágicos: **Normal**, **Static** (como estático), **Tile** (como tile). |

---

## Detalhe por opção (General)

- **Highlight objects under cursor** – Ajuda a ver exactamente o que está sob o rato (mobiles, itens, portas). Não altera jogabilidade.
- **Enable pathfinding** – O cliente calcula um caminho em volta de obstáculos ao clicar no chão. **Shift for pathfinding** limita este comportamento a quando mantém Shift; **Single click** usa um único clique em vez de duplo para iniciar o movimento.
- **Always run** – O personagem corre por defeito; **Unless hidden** faz com que deixe de correr automaticamente quando está em stealth.
- **Automatically open doors** – Ao passar por uma porta, abre-a. **Open doors while pathfinding** abre portas no caminho durante o pathfinding.
- **Automatically open corpses** – Abre corpos ao aproximar. **Corpse open distance** (0–5) define quantos tiles de distância. **Skip empty corpses** evita abrir corpos já vazios. **Corpse open options** restringe quando abrir: não quando tem alvo, não quando está hidden, ou ambos.
- **No color for out of range objects** – Objectos fora do alcance de visão ficam a cinzento; o que está ao alcance mantém cor.
- **Sallos easy grab** – Modo de agarrar itens ao estilo Sallos; pode conflituar com grid containers (ver tooltip no cliente).
- **Show mobile's HP** – Mostra vida dos mobiles. **Type**: percentagem, barra ou ambos. **Show when**: sempre, só quando &lt; 100%, ou modo “smart”.
- **Highlight poisoned/paralyzed/invulnerable** – Realça mobiles com esses estados; cada um tem **color picker** para a cor do realce.
- **Show incoming mobile/corpse names** – Mensagem ou indicador quando um mobile ou corpse entra no ecrã.
- **Show aura under feet** – Quando mostrar aura: desactivado, só em warmode, com Ctrl+Shift, ou sempre. **Party aura** usa cor custom para o grupo.
- **Disable top menu bar** – Esconde a barra de menu no topo. **Alt to close anchored gumps** exige Alt para fechar gumps ancorados; **Alt to move gumps** exige Alt para os arrastar.
- **Close entire group of anchored gumps with right click** – Clique direito num gump ancorado fecha todo o grupo.
- **Use original skills gump / Use old status gump** – Usa as versões clássicas desses gumps.
- **Use modern health bar gumps** – Barras de vida estilo Dust765. **Black background** aplica fundo preto. **Save health bars on logout** mantém posições ao sair. **Close health bars when**: desactivado, fora de alcance, morto ou ambos.
- **Grid Loot** – Gump de loot em grelha para corpos (não confundir com grid containers). Opções: desactivado, só grid loot, ou grid + contentor normal.
- **Require shift for context menus / to split stacks** – Reduz cliques acidentais em menus de contexto e em dividir pilhas.
- **Circle of transparency** – Círculo à volta do personagem onde objectos ficam transparentes. **Distance** é o raio; **Type** altera o estilo (Full, Gradient, Modern).
- **Object fading / Text fading** – Objectos e texto desvanecerem com distância ou tempo.
- **Show target range indicator** – Mostra no cursor o alcance até ao alvo actual.
- **Enable drag select for health bars** – Permite seleccionar várias barras de vida arrastando; **Key modifier** e os combos **Players only**, **Monsters only**, **Nameplates only** definem o comportamento. **X/Y Position** e **Anchor opened health bars together** configuram onde abrem e se ficam ancoradas.
- **Show stats/skills changed messages** – Mensagens quando str/dex/int ou skills mudam. **Change volume** (slider) define o limiar mínimo de mudança para mostrar a mensagem de skills.
- **Terrain & Statics** – **Hide roof** para ver dentro de edifícios. **Trees to stumps** substitui árvores por tocos. **Hide vegetation** remove erva/arbustos. **Mark cave tiles** activa borda/estilo de caverna. **Magic field type** controla o desenho de campos mágicos (normal, estático ou tile).

---

[Voltar ao índice](README.md)
