// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Sdk.Assets;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using ClassicUO.Services;

namespace ClassicUO.Game.Managers
{
    sealed class AnimatedStaticsManager
    {
        private readonly List<StaticAnimationInfo> _staticInfos = new List<StaticAnimationInfo>();
        private uint _processTime;


        public unsafe void Initialize()
        {
            var file = ServiceProvider.Get<AssetsService>().AnimData.AnimDataFile;

            if (file == null)
            {
                return;
            }

            uint lastaddr = (uint)(file.Length - sizeof(AnimDataFrame));

            for (int i = 0; i < ServiceProvider.Get<AssetsService>().TileData.StaticData.Length; i++)
            {
                if (ServiceProvider.Get<AssetsService>().TileData.StaticData[i].IsAnimated)
                {
                    uint addr = (uint)(i * 68 + 4 * (i / 8 + 1));

                    if (addr <= lastaddr)
                    {
                        _staticInfos.Add
                        (
                            new StaticAnimationInfo
                            {
                                Index = (ushort)i,
                                IsField = StaticFilters.IsField((ushort)i)
                            }
                        );
                    }
                }
            }
        }

        public unsafe void Process()
        {
            if (_staticInfos == null || _staticInfos.Count == 0 || _processTime >= Time.Ticks)
            {
                return;
            }

            var file = ServiceProvider.Get<AssetsService>().AnimData.AnimDataFile;

            if (file == null)
            {
                return;
            }


            // fix static animations time to reflect the standard client
            uint delay = Constants.ITEM_EFFECT_ANIMATION_DELAY * 2;
            uint next_time = Time.Ticks + 250;
            bool no_animated_field = ProfileManager.CurrentProfile != null && ProfileManager.CurrentProfile.FieldsType != 0;
            var static_data = ServiceProvider.Get<AssetsService>().Arts.File.Entries;

            foreach (ref var o in CollectionsMarshal.AsSpan(_staticInfos))
            {
                if (no_animated_field && o.IsField)
                {
                    o.AnimIndex = 0;

                    continue;
                }

                if (o.Time < Time.Ticks)
                {
                    uint addr = (uint)(o.Index * 68 + 4 * (o.Index / 8 + 1));
                    file.Seek(addr, System.IO.SeekOrigin.Begin);
                    var info = file.Read<AnimDataFrame>();

                    byte offset = o.AnimIndex;

                    if (info.FrameInterval > 0)
                    {
                        o.Time = Time.Ticks + info.FrameInterval * delay + 1;
                    }
                    else
                    {
                        o.Time = Time.Ticks + delay;
                    }

                    if (offset < info.FrameCount && o.Index + 0x4000 < static_data.Length)
                    {
                        static_data[o.Index + 0x4000].AnimOffset = info.FrameData[offset++];
                    }

                    if (offset >= info.FrameCount)
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