
using System.Collections.Generic;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Input;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.Services;

internal class UIService : IService
{
    private readonly UIManager _ui;

    public UIService(UIManager ui)
    {
        _ui = ui;
    }

    public Control? MouseOverControl => _ui.MouseOverControl;
    public Control? KeyboardFocusControl { get => _ui.KeyboardFocusControl; set => _ui.KeyboardFocusControl = value; }
    public Control? DraggingControl => _ui.DraggingControl;
    public LinkedList<Gump> Gumps => _ui.Gumps;
    public SystemChatControl? SystemChat { get => _ui.SystemChat; set => _ui.SystemChat = value; }
    public float ContainerScale { get => _ui.ContainerScale; set => _ui.ContainerScale = value; }
    public AnchorManager AnchorManager => _ui.AnchorManager;
    public bool IsDragging => _ui.IsDragging;
    public bool IsMouseOverWorld => _ui.IsMouseOverWorld;
    public PopupMenuGump? PopupMenu => _ui.PopupMenu;


    public void Clear() => _ui.Clear();

    public T? GetGump<T>(uint? serial = null) where T : Gump => _ui.GetGump<T>(serial);

    public TradingGump? GetTradingGump(uint serial) => _ui.GetTradingGump(serial);

    public void ShowGamePopup(PopupMenuGump? popupMenuGump) => _ui.ShowGamePopup(popupMenuGump);

    public void ShowContextMenu(ContextMenuShowMenu? menu) => _ui.ShowContextMenu(menu);

    public void AttemptDragControl(Control control, bool attemptAlwaysSuccessful = false) => _ui.AttemptDragControl(control, attemptAlwaysSuccessful);

    public void Add(Gump gump, bool front = true) => _ui.Add(gump, front);

    public bool GetGumpCachePosition(uint id, out Point point) => _ui.GetGumpCachePosition(id, out point);

    public void SavePosition(uint id, Point point) => _ui.SavePosition(id, point);

    public bool RemovePosition(uint id) => _ui.RemovePosition(id);

    public Control? LastControlMouseDown(MouseButtonType button) => _ui.LastControlMouseDown(button);

    public void MakeTopMostGump(Control control) => _ui.MakeTopMostGump(control);

    public void OnMouseButtonUp(MouseButtonType button) => _ui.OnMouseButtonUp(button);
    public void OnMouseButtonDown(MouseButtonType button) => _ui.OnMouseButtonDown(button);
    public bool OnMouseDoubleClick(MouseButtonType button) => _ui.OnMouseDoubleClick(button);
    public void OnMouseDragging() => _ui.OnMouseDragging();
    public void OnMouseWheel(bool isUp) => _ui.OnMouseWheel(isUp);
}