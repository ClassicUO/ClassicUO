using System.Collections.Generic;
using Clay_cs;

namespace ClassicUO.Ecs.Modding.UI;

enum ClayWidgetType
{
    None,
    Button,
    TextInput,
    TextFragment
}

internal record struct UITextProxy(string Value, char ReplacedChar = '\0', ClayTextProxy TextConfig = default);
internal record struct ClayTextProxy(Clay_Color TextColor, ushort FontId, ushort FontSize, ushort LetterSpacing, ushort LineHeight, Clay_TextElementConfigWrapMode WrapMode, Clay_TextAlignment TextAlignment);
internal record struct ClayElementIdProxy(uint Id, uint Offset, uint BaseId, string StringId);
internal record struct ClayImageProxy(string Base64Data);
internal struct ClayElementDeclProxy
{
    public ClayElementIdProxy? Id;
    public Clay_LayoutConfig? Layout;
    public Clay_Color? BackgroundColor;
    public Clay_CornerRadius? CornerRadius;
    public ClayImageProxy? Image;
    public Clay_FloatingElementConfig? Floating;
    public Clay_ClipElementConfig? Clip;
    public Clay_BorderElementConfig? Border;
}

internal record struct UOButtonWidgetProxy(ushort Normal, ushort Pressed, ushort Over);

internal record struct UINodeProxy(
    ulong Id,
    ClayElementDeclProxy Config,
    ClayUOCommandData? UOConfig = null,
    UITextProxy? TextConfig = null,
    UOButtonWidgetProxy? UOButton = null,
    ClayWidgetType WidgetType = ClayWidgetType.None,
    bool Movable = false
);

internal record struct UINodes(List<UINodeProxy> Nodes, Dictionary<ulong, ulong> Relations);
