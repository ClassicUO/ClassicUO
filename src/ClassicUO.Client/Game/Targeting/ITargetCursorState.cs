// SPDX-License-Identifier: BSD-2-Clause

using System;
using ClassicUO.Game.Data;
using ClassicUO.Game.Events;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;

namespace ClassicUO.Game.Targeting
{
    /// <summary>
    /// Owns the current cursor-targeting state: the active <see cref="CursorTarget"/>,
    /// targeting type, last cursor id, multi-placement info and the optional
    /// callback target action. Handles <see cref="EventSink.TargetCursorReceived"/>
    /// and exposes the <c>SetTargeting</c> / clear primitives the facade exposes
    /// to callers.
    /// </summary>
    internal interface ITargetCursorState
    {
        CursorTarget TargetingState { get; }
        TargetType TargetingType { get; }
        bool IsTargeting { get; set; }
        uint TargetCursorId { get; }
        MultiTargetInfo MultiTargetInfo { get; }
        Action<GameObject> TargetCallback { get; }

        void SetTargeting(CursorTarget targeting, uint cursorID, TargetType cursorType);
        void SetTargeting(Action<GameObject> callback, uint cursorID, TargetType cursorType);
        void SetTargetingMulti(uint deedSerial, ushort model, ushort x, ushort y, ushort z, ushort hue);
        void ClearMultiTargetInfo();
        void ClearTargetingWithoutTargetCancelPacket();
        void ResetAll();
        void OnTargetCursorReceived(TargetCursorReceivedArgs e);
    }
}
