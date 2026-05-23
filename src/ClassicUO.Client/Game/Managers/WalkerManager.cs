// SPDX-License-Identifier: BSD-2-Clause

// UO Movement System
//
// Turns (direction changes) have no delay. The client sends a 0x02 walk request
// with the new direction and the server confirms it immediately. Both turns and
// tile movements use the same packet (0x02) and confirmation (0x22).
//
// Movement speeds (retail):
//   Walking:       400ms per tile  (2.5 tiles/sec)
//   Running:       200ms per tile  (5.0 tiles/sec)
//   Mounted walk:  200ms per tile  (5.0 tiles/sec)
//   Mounted run:   100ms per tile  (10.0 tiles/sec)
//
// The client may queue up to 5 unconfirmed walk packets (MaxStepCount).
// The Enhanced Client (EC) allows 4 queued moves including the initial turn.
// If the server falls behind on confirmations and the queue fills, the client
// must wait before sending more movement packets.
//
// Each walk request carries a sequence byte (0-255, wrapping). The server echoes
// the sequence in its 0x22 confirmation. If a confirmation arrives for an unknown
// sequence, the client triggers a resync (0x22 "bad step" path).

using ClassicUO.Game.GameObjects;
using ClassicUO.Network;

namespace ClassicUO.Game.Managers
{
    internal struct StepInfo
    {
        public byte Direction;
        public byte OldDirection;
        public byte Sequence;
        public bool Accepted;
        public bool Running;
        public bool NoRotation;
        public long Timer;
        public ushort X, Y;
        public sbyte Z;
    }

    internal sealed class FastWalkStack
    {
        private readonly uint[] _keys = new uint[Constants.MAX_FAST_WALK_STACK_SIZE];

        public void SetValue(int index, uint value)
        {
            if (index >= 0 && index < Constants.MAX_FAST_WALK_STACK_SIZE)
            {
                _keys[index] = value;
            }
        }

        public void AddValue(uint value)
        {
            for (int i = 0; i < Constants.MAX_FAST_WALK_STACK_SIZE; i++)
            {
                if (_keys[i] == 0)
                {
                    _keys[i] = value;

                    break;
                }
            }
        }

        public uint GetValue()
        {
            for (int i = 0; i < Constants.MAX_FAST_WALK_STACK_SIZE; i++)
            {
                uint key = _keys[i];

                if (key != 0)
                {
                    _keys[i] = 0;

                    return key;
                }
            }

            return 0;
        }
    }

    internal sealed class WalkerManager
    {
        private readonly PlayerMobile _player;

        public WalkerManager(PlayerMobile player)
        {
            _player = player;
        }


        public FastWalkStack FastWalkStack { get; } = new FastWalkStack();
        public ushort CurrentPlayerZ;
        public byte CurrentWalkSequence;
        public long LastStepRequestTime;
        public ushort NewPlayerZ;
        public bool ResendPacketResync;
        public readonly StepInfo[] StepInfos = new StepInfo[Constants.MAX_STEP_COUNT];
        public int StepsCount;
        public int UnacceptedPacketsCount;
        public bool WalkingFailed;
        public byte WalkSequence;
        public bool WantChangeCoordinates;

        public void DenyWalk(byte sequence, int x, int y, sbyte z)
        {
            _player.ClearSteps();

            Reset();

            if (x != -1)
            {
                _player.World.RangeSize.X = x;
                _player.World.RangeSize.Y = y;

                _player.SetInWorldTile((ushort) x, (ushort) y, z);
            }

            _player.SyncServerDirection();
        }

        public void ConfirmWalk(byte sequence)
        {
            if (UnacceptedPacketsCount != 0)
            {
                UnacceptedPacketsCount--;
            }

            int stepIndex = 0;

            for (int i = 0; i < StepsCount; i++)
            {
                if (StepInfos[i].Sequence == sequence)
                {
                    break;
                }

                stepIndex++;
            }

            bool isBadStep = stepIndex == StepsCount;


            if (!isBadStep)
            {
                if (stepIndex >= CurrentWalkSequence)
                {
                    StepInfos[stepIndex].Accepted = true;

                    _player.World.RangeSize.X = StepInfos[stepIndex].X;
                    _player.World.RangeSize.Y = StepInfos[stepIndex].Y;
                }
                else if (stepIndex == 0)
                {
                    _player.World.RangeSize.X = StepInfos[0].X;
                    _player.World.RangeSize.Y = StepInfos[0].Y;

                    for (int i = 1; i < StepsCount; i++)
                    {
                        StepInfos[i - 1] = StepInfos[i];
                    }

                    StepsCount--;
                    CurrentWalkSequence--;
                }
                else
                {
                    isBadStep = true;
                }
            }

            if (isBadStep)
            {
                if (!ResendPacketResync)
                {
                    NetClient.Socket.Send_Resync();
                    ResendPacketResync = true;
                }

                WalkingFailed = true;
                StepsCount = 0;
                CurrentWalkSequence = 0;
            }
        }

        public void Reset()
        {
            UnacceptedPacketsCount = 0;
            StepsCount = 0;
            WalkSequence = 0;
            CurrentWalkSequence = 0;
            WalkingFailed = false;
            ResendPacketResync = false;
            LastStepRequestTime = 0;
        }
    }
}