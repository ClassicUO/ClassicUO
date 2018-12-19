using ClassicUO.Network;

using Microsoft.Xna.Framework;

namespace ClassicUO.Game.Managers
{
#if !JAEDAN_MOVEMENT_PATCH
    struct StepInfo
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

    class FastWalkStack
    {
        private readonly uint[] _keys = new uint[Constants.MAX_FAST_WALK_STACK_SIZE];

        public void SetValue(int index, uint value)
        {
            if (index >= 0 && index < Constants.MAX_FAST_WALK_STACK_SIZE)
                _keys[index] = value;
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

    class WalkerManager
    {
        public long LastStepRequestTime { get; set; }
        public int UnacceptedPacketsCount { get; set; }
        public int StepsCount { get; set; }
        public byte WalkSequence { get; set; }
        public byte CurrentWalkSequence { get; set; }
        public bool ResendPacketSended { get; set; }
        public bool WantChangeCoordinates { get; set; }
        public bool WalkingFailed { get; set; }
        public ushort CurrentPlayerZ { get; set; }
        public ushort NewPlayerZ { get; set; }

        public StepInfo[] StepInfos = new StepInfo[Constants.MAX_STEP_COUNT];

        public FastWalkStack FastWalkStack { get; } = new FastWalkStack();

        public void DenyWalk(byte sequence, int x, int y, sbyte z)
        {
            World.Player.Steps.Clear();

            World.Player.Offset = Vector3.Zero;

            Reset();

            if (x != -1)
            {
                World.Player.Position = new Position((ushort) x , (ushort)y, z);

                World.Player.AddToTile();
                World.Player.ProcessDelta();
            }
        }

        public void ConfirmWalk(byte sequence)
        {
            if (UnacceptedPacketsCount != 0)
                UnacceptedPacketsCount--;

            int stepIndex = 0;

            for (int i = 0; i < StepsCount; i++)
            {
                if (StepInfos[i].Sequence == sequence)
                    break;

                stepIndex++;
            }

            bool isBadStep = stepIndex == StepsCount;

            if (!isBadStep)
            {
                if (stepIndex >= CurrentWalkSequence)
                {
                    StepInfos[stepIndex].Accepted = true;
                }
                else if (stepIndex == 0)
                {
                    for (int i = 1; i < StepsCount; i++)
                        StepInfos[i - 1] = StepInfos[i];

                    StepsCount--;
                    CurrentWalkSequence--;
                }
                else
                    isBadStep = true;
            }

            if (isBadStep)
            {
                if (!ResendPacketSended)
                {
                    NetClient.Socket.Send(new PResend());
                    ResendPacketSended = true;
                }

                WalkingFailed = true;
                StepsCount = 0;
                CurrentWalkSequence = 0;
            }
            else
            {
            }
        }

        public void Reset()
        {
            UnacceptedPacketsCount = 0;
            StepsCount = 0;
            WalkSequence = 0;
            CurrentWalkSequence = 0;
            WalkingFailed = false;
            ResendPacketSended = false;
            LastStepRequestTime = 0;
        }
    }
#endif
}
