#region license

// Copyright (c) 2024, andreakarasho
// All rights reserved.
// 
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are met:
// 1. Redistributions of source code must retain the above copyright
//    notice, this list of conditions and the following disclaimer.
// 2. Redistributions in binary form must reproduce the above copyright
//    notice, this list of conditions and the following disclaimer in the
//    documentation and/or other materials provided with the distribution.
// 3. All advertising materials mentioning features or use of this software
//    must display the following acknowledgement:
//    This product includes software developed by andreakarasho - https://github.com/andreakarasho
// 4. Neither the name of the copyright holder nor the
//    names of its contributors may be used to endorse or promote products
//    derived from this software without specific prior written permission.
// 
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS ''AS IS'' AND ANY
// EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
// WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
// DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER BE LIABLE FOR ANY
// DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
// (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
// LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
// ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
// (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
// SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

#endregion

using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.IO;
using ClassicUO.Assets;
using ClassicUO.Utility.Collections;

namespace ClassicUO.Game.Managers
{
    sealed class AnimatedStaticsManager
    {
        private readonly FastList<StaticAnimationInfo> _staticInfos = new FastList<StaticAnimationInfo>();
        private uint _processTime;


        public unsafe void Initialize()
        {
            UOFile file = AnimDataLoader.Instance.AnimDataFile;

            if (file == null)
            {
                return;
            }

            long startAddr = file.StartAddress.ToInt64();
            uint lastaddr = (uint) (startAddr + file.Length - sizeof(AnimDataFrame));

            for (int i = 0; i < TileDataLoader.Instance.StaticData.Length; i++)
            {
                if (TileDataLoader.Instance.StaticData[i].IsAnimated)
                {
                    uint addr = (uint) (i * 68 + 4 * (i / 8 + 1));
                    uint offset = (uint) (startAddr + addr);

                    if (offset <= lastaddr)
                    {
                        _staticInfos.Add
                        (
                            new StaticAnimationInfo
                            {
                                Index = (ushort) i,
                                IsField = StaticFilters.IsField((ushort) i)
                            }
                        );
                    }
                }
            }
        }

        public unsafe void Process()
        {
            if (_staticInfos == null || _staticInfos.Length == 0 || _processTime >= Time.Ticks)
            {
                return;
            }

            UOFile file = AnimDataLoader.Instance.AnimDataFile;

            if (file == null)
            {
                return;
            }

            // fix static animations time to reflect the standard client
            uint delay = Constants.ITEM_EFFECT_ANIMATION_DELAY * 2;
            uint next_time = Time.Ticks + 250;
            bool no_animated_field = ProfileManager.CurrentProfile != null && ProfileManager.CurrentProfile.FieldsType != 0;
            long startAddr = file.StartAddress.ToInt64();
            UOFileIndex[] static_data = ArtLoader.Instance.Entries;

            for (int i = 0; i < _staticInfos.Length; i++)
            {
                ref StaticAnimationInfo o = ref _staticInfos.Buffer[i];

                if (no_animated_field && o.IsField)
                {
                    o.AnimIndex = 0;

                    continue;
                }

                if (o.Time < Time.Ticks)
                {
                    uint addr = (uint) (o.Index * 68 + 4 * (o.Index / 8 + 1));
                    AnimDataFrame* info = (AnimDataFrame*) (startAddr + addr);

                    byte offset = o.AnimIndex;

                    if (info->FrameInterval > 0)
                    {
                        o.Time = Time.Ticks + info->FrameInterval * delay + 1;
                    }
                    else
                    {
                        o.Time = Time.Ticks + delay;
                    }

                    if (offset < info->FrameCount && o.Index + 0x4000 < static_data.Length)
                    {
                        static_data[o.Index + 0x4000].AnimOffset = info->FrameData[offset++];
                    }

                    if (offset >= info->FrameCount)
                    {
                        offset = 0;
                    }

                    o.AnimIndex = offset;
                }

                if (o.Time < next_time)
                {
                    next_time = o.Time;
                }
            }

            _processTime = next_time;
        }


        private struct StaticAnimationInfo
        {
            public uint Time;
            public ushort Index;
            public byte AnimIndex;
            public bool IsField;
        }
    }
}