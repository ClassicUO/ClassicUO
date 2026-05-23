// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Game.UI.Controls;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Input;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;
using System.Collections.Generic;

namespace ClassicUO.Game.UI
{
    /// <summary>
    ///     Static facade over <see cref="UIManagerCore"/>. Preserves the
    ///     long-standing static API surface while delegating all state and
    ///     behavior to a single backing instance. This is the Phase 0 DI seam:
    ///     call sites remain unchanged, but the instance core is available for
    ///     future testing and dependency injection scenarios.
    /// </summary>
    internal static class UIManager
    {
        private static readonly UIManagerCore _core = new UIManagerCore();

        public static float ContainerScale
        {
            get => _core.ContainerScale;
            set => _core.ContainerScale = value;
        }

        public static AnchorManager AnchorManager => _core.AnchorManager;

        public static LinkedList<Gump> Gumps => _core.Gumps;

        public static Control MouseOverControl => _core.MouseOverControl;

        public static bool IsModalOpen => _core.IsModalOpen;

        public static bool IsMouseOverWorld => _core.IsMouseOverWorld;

        public static Control DraggingControl => _core.DraggingControl;

        public static SystemChatControl SystemChat
        {
            get => _core.SystemChat;
            set => _core.SystemChat = value;
        }

        public static PopupMenuGump PopupMenu => _core.PopupMenu;

        public static Control KeyboardFocusControl
        {
            get => _core.KeyboardFocusControl;
            set => _core.KeyboardFocusControl = value;
        }

        public static bool IsDragging => _core.IsDragging;

        public static ContextMenuShowMenu ContextMenu => _core.ContextMenu;

        public static void ShowGamePopup(PopupMenuGump popup) => _core.ShowGamePopup(popup);

        public static bool IsModalControlOpen() => _core.IsModalControlOpen();

        public static void OnMouseDragging() => _core.OnMouseDragging();

        public static void OnMouseButtonDown(MouseButtonType button) => _core.OnMouseButtonDown(button);

        public static void OnMouseButtonUp(MouseButtonType button) => _core.OnMouseButtonUp(button);

        public static bool OnMouseDoubleClick(MouseButtonType button) => _core.OnMouseDoubleClick(button);

        public static void OnMouseWheel(bool isup) => _core.OnMouseWheel(isup);

        public static Control LastControlMouseDown(MouseButtonType button) => _core.LastControlMouseDown(button);

        public static void SavePosition(uint serverSerial, Point point) => _core.SavePosition(serverSerial, point);

        public static bool RemovePosition(uint serverSerial) => _core.RemovePosition(serverSerial);

        public static bool GetGumpCachePosition(uint id, out Point pos) => _core.GetGumpCachePosition(id, out pos);

        public static void ShowContextMenu(ContextMenuShowMenu menu) => _core.ShowContextMenu(menu);

        public static T GetGump<T>(uint? serial = null) where T : Control => _core.GetGump<T>(serial);

        public static Gump GetGump(uint serial) => _core.GetGump(serial);

        public static TradingGump GetTradingGump(uint serial) => _core.GetTradingGump(serial);

        public static void Update() => _core.Update();

        public static void Draw(UltimaBatcher2D batcher) => _core.Draw(batcher);

        public static void Add(Gump gump, bool front = true) => _core.Add(gump, front);

        public static void Clear() => _core.Clear();

        /// <summary>
        ///     Returns all controls which are part of a Gump from the provided list,
        ///     as long as they are below the current mouse cursor position.
        ///
        ///     Never returns null, but an empty enumerable instead.
        /// </summary>
        /// <returns>Read-only enumerable with the desired controls.</returns>
        public static IEnumerable<Control> GetAllMouseOverControlsOfType<T>() where T : Control => _core.GetAllMouseOverControlsOfType<T>();

        public static void MakeTopMostGump(Control control) => _core.MakeTopMostGump(control);

        public static void AttemptDragControl(Control control, bool attemptAlwaysSuccessful = false) => _core.AttemptDragControl(control, attemptAlwaysSuccessful);
    }
}
