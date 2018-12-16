using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ClassicUO.Network;
using ClassicUO.Utility.Logging;

using Microsoft.Xna.Framework;

namespace ClassicUO.Game
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
