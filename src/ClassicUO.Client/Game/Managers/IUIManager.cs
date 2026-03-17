// SPDX-License-Identifier: BSD-2-Clause

using System.Collections.Generic;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Input;
using ClassicUO.Renderer;

namespace ClassicUO.Game.Managers
{
    internal interface IUIManager
    {
        LinkedList<Gump> Gumps { get; }
        Control MouseOverControl { get; }
        Control KeyboardFocusControl { get; set; }
        SystemChatControl SystemChat { get; set; }
        bool IsModalOpen { get; }
        bool IsMouseOverWorld { get; }
        float ContainerScale { get; set; }
        Control DraggingControl { get; }
        bool IsDragging { get; }
        AnchorManager AnchorManager { get; }
        PopupMenuGump PopupMenu { get; }
        ContextMenuShowMenu ContextMenu { get; }

        T GetGump<T>(uint? serial = null) where T : Control;
        Gump GetGump(uint serial);
        TradingGump GetTradingGump(uint serial);
        void Add(Gump gump, bool front = true);
        void Clear();
        void SavePosition(uint serverSerial, Microsoft.Xna.Framework.Point point);
        bool RemovePosition(uint serverSerial);
        bool GetGumpCachePosition(uint id, out Microsoft.Xna.Framework.Point pos);
        void ShowContextMenu(ContextMenuShowMenu menu);
        void ShowGamePopup(PopupMenuGump popup);
        void MakeTopMostGump(Control control);
        bool IsModalControlOpen();
        void AttemptDragControl(Control control, bool attemptAlwaysSuccessful = false);
        Control LastControlMouseDown(MouseButtonType button);
        void OnMouseDragging();
        void OnMouseButtonDown(MouseButtonType button);
        void OnMouseButtonUp(MouseButtonType button);
        void OnMouseWheel(bool isup);
        bool OnMouseDoubleClick(MouseButtonType button);
        void Update();
        void Draw(UltimaBatcher2D batcher);
        IEnumerable<Control> GetAllMouseOverControlsOfType<T>() where T : Control;
    }
}
