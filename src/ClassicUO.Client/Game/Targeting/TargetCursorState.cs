// SPDX-License-Identifier: BSD-2-Clause

using System;
using ClassicUO.Game.Data;
using ClassicUO.Game.Events;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Utility;

namespace ClassicUO.Game.Targeting
{
    /// <summary>
    /// Holds the in-flight cursor-targeting state for <see cref="TargetManager"/>.
    /// Single responsibility: maintain the current cursor type / id / target type
    /// triple, the multi-placement info, the callback target and translate the
    /// raw <see cref="TargetCursorReceivedArgs"/> packet into a SetTargeting call.
    /// </summary>
    internal sealed class TargetCursorState : ITargetCursorState
    {
        private readonly World _world;

        public TargetCursorState(World world)
        {
            _world = world;
        }

        public CursorTarget TargetingState { get; private set; } = CursorTarget.Invalid;
        public TargetType TargetingType { get; private set; }
        public bool IsTargeting { get; set; }
        public uint TargetCursorId { get; private set; }
        public MultiTargetInfo MultiTargetInfo { get; private set; }
        public Action<GameObject> TargetCallback { get; private set; }

        public void OnTargetCursorReceived(TargetCursorReceivedArgs e)
        {
            SetTargeting((CursorTarget)e.CursorType, e.TargetId, (TargetType)e.TargetType);

            if (_world == null) return;

            if (_world.Party.PartyHealTimer < Time.Ticks && _world.Party.PartyHealTarget != 0)
            {
                _world.TargetManager.Target(_world.Party.PartyHealTarget);
                _world.Party.PartyHealTimer = 0;
                _world.Party.PartyHealTarget = 0;
            }
        }

        public void SetTargeting(Action<GameObject> callback, uint cursorID, TargetType cursorType)
        {
            SetTargeting(CursorTarget.CallbackTarget, cursorID, cursorType);
            TargetCallback = callback;
        }

        public void SetTargeting(CursorTarget targeting, uint cursorID, TargetType cursorType)
        {
            if (targeting == CursorTarget.Invalid)
            {
                return;
            }

            bool lastTargetting = IsTargeting;
            IsTargeting = cursorType < TargetType.Cancel;
            TargetingState = targeting;
            TargetingType = cursorType;

            if (IsTargeting)
            {
                //UIManager.RemoveTargetLineGump(LastTarget);
            }
            else if (lastTargetting)
            {
                _world?.TargetManager.CancelTarget();
            }

            // https://github.com/andreakarasho/ClassicUO/issues/1373
            // when receiving a cancellation target from the server we need
            // to send the last active cursorID, so update cursor data later

            TargetCursorId = cursorID;
        }

        public void SetTargetingMulti(uint deedSerial, ushort model, ushort x, ushort y, ushort z, ushort hue)
        {
            SetTargeting(CursorTarget.MultiPlacement, deedSerial, TargetType.Neutral);

            //if (model != 0)
            MultiTargetInfo = new MultiTargetInfo(model, x, y, z, hue);
        }

        public void ClearMultiTargetInfo() => MultiTargetInfo = null;

        public void ClearTargetingWithoutTargetCancelPacket()
        {
            if (TargetingState == CursorTarget.MultiPlacement)
            {
                MultiTargetInfo = null;
                TargetingState = 0;
                _world?.HouseManager.Remove(0);
            }

            IsTargeting = false;
        }

        public void ResetAll()
        {
            ClearTargetingWithoutTargetCancelPacket();

            TargetCallback = null;
            TargetingState = 0;
            TargetCursorId = 0;
            MultiTargetInfo = null;
            TargetingType = 0;
        }
    }
}
