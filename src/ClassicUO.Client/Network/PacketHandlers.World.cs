// SPDX-License-Identifier: BSD-2-Clause

using System;
using ClassicUO.Configuration;
using ClassicUO.Game;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.Scenes;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.IO;
using ClassicUO.Renderer;
using ClassicUO.Utility.Logging;

namespace ClassicUO.Network
{
    internal sealed partial class PacketHandlers
    {
        internal static void RegisterWorldHandlers(PacketHandlers h)
        {
            h.Add(0x4E, PersonalLightLevel);
            h.Add(0x4F, LightLevel);
            h.Add(0x54, PlaySoundEffect);
            h.Add(0x56, MapData);
            h.Add(0x5B, SetTime);
            h.Add(0x65, SetWeather);
            h.Add(0x6D, PlayMusic);
            h.Add(0xBC, Season);
            h.Add(0x70, GraphicEffect);
            h.Add(0xC0, GraphicEffect);
            h.Add(0xC7, GraphicEffect);
            h.Add(0xC4, Semivisible);
            h.Add(0x90, DisplayMap);
            h.Add(0xF5, DisplayMap);
        }

        private static void PersonalLightLevel(World world, ref StackDataReader p)
        {
            if (!world.InGame)
            {
                return;
            }

            if (world.Player == p.ReadUInt32BE())
            {
                byte level = p.ReadUInt8();

                if (level > 0x1E)
                {
                    level = 0x1E;
                }

                world.Light.RealPersonal = level;

                if (!ProfileManager.CurrentProfile.UseCustomLightLevel)
                {
                    world.Light.Personal = level;
                }
            }
        }

        private static void LightLevel(World world, ref StackDataReader p)
        {
            if (!world.InGame)
            {
                return;
            }

            byte level = p.ReadUInt8();

            if (level > 0x1E)
            {
                level = 0x1E;
            }

            world.Light.RealOverall = level;

            if (
                !ProfileManager.CurrentProfile.UseCustomLightLevel
                || ProfileManager.CurrentProfile.LightLevelType == 1
            )
            {
                world.Light.Overall =
                    ProfileManager.CurrentProfile.LightLevelType == 1
                        ? Math.Min(level, ProfileManager.CurrentProfile.LightLevel)
                        : level;
            }
        }

        private static void PlaySoundEffect(World world, ref StackDataReader p)
        {
            if (world.Player == null)
            {
                return;
            }

            p.Skip(1);

            ushort index = p.ReadUInt16BE();
            ushort audio = p.ReadUInt16BE();
            ushort x = p.ReadUInt16BE();
            ushort y = p.ReadUInt16BE();
            short z = (short)p.ReadUInt16BE();

            Client.Game.Audio.PlaySoundWithDistance(world, index, x, y);
        }

        private static void PlayMusic(World world, ref StackDataReader p)
        {
            if (p.Length == 3) // Play Midi Music packet (0x6D, 0x10, index)
            {
                byte cmd = p.ReadUInt8();
                byte index = p.ReadUInt8();

                // Check for stop music packet (6D 1F FF)
                if (cmd == 0x1F && index == 0xFF)
                {
                    Client.Game.Audio.StopMusic();
                }
                else
                {
                    Client.Game.Audio.PlayMusic(index);
                }
            }
            else
            {
                ushort index = p.ReadUInt16BE();
                Client.Game.Audio.PlayMusic(index);
            }
        }

        private static void MapData(World world, ref StackDataReader p)
        {
            if (!world.InGame)
            {
                return;
            }

            uint serial = p.ReadUInt32BE();

            MapGump gump = UIManager.GetGump<MapGump>(serial);

            if (gump != null)
            {
                switch ((MapMessageType)p.ReadUInt8())
                {
                    case MapMessageType.Add:
                        p.Skip(1);

                        ushort x = p.ReadUInt16BE();
                        ushort y = p.ReadUInt16BE();

                        gump.AddPin(x, y);

                        break;

                    case MapMessageType.Insert:
                        break;
                    case MapMessageType.Move:
                        break;
                    case MapMessageType.Remove:
                        break;

                    case MapMessageType.Clear:
                        gump.ClearContainer();

                        break;

                    case MapMessageType.Edit:
                        break;

                    case MapMessageType.EditResponse:
                        gump.SetPlotState(p.ReadUInt8());

                        break;
                }
            }
        }

        private static void SetTime(World world, ref StackDataReader p) { }

        private static void SetWeather(World world, ref StackDataReader p)
        {
            GameScene scene = Client.Game.GetScene<GameScene>();

            if (scene == null)
            {
                return;
            }

            WeatherType type = (WeatherType)p.ReadUInt8();

            if (world.Weather.CurrentWeather != type)
            {
                byte count = p.ReadUInt8();
                byte temp = p.ReadUInt8();

                world.Weather.Generate(type, count, temp);
            }
        }

        private static void Season(World world, ref StackDataReader p)
        {
            if (world.Player == null)
            {
                return;
            }

            byte season = p.ReadUInt8();
            byte music = p.ReadUInt8();

            if (season > 4)
            {
                season = 0;
            }

            if (world.Player.IsDead && season == 4)
            {
                return;
            }

            world.OldSeason = (Season)season;
            world.OldMusicIndex = music;

            if (world.Season == Game.Managers.Season.Desolation)
            {
                world.OldMusicIndex = 42;
            }

            world.ChangeSeason((Season)season, music);
        }

        private static void GraphicEffect(World world, ref StackDataReader p)
        {
            if (world.Player == null)
            {
                return;
            }

            GraphicEffectType type = (GraphicEffectType)p.ReadUInt8();

            if (type > GraphicEffectType.FixedFrom)
            {
                if (type == GraphicEffectType.ScreenFade && p[0] == 0x70)
                {
                    p.Skip(8);
                    ushort val = p.ReadUInt16BE();

                    if (val > 4)
                    {
                        val = 4;
                    }

                    Log.Warn("Effect not implemented");
                }

                return;
            }

            uint source = p.ReadUInt32BE();
            uint target = p.ReadUInt32BE();
            ushort graphic = p.ReadUInt16BE();
            ushort srcX = p.ReadUInt16BE();
            ushort srcY = p.ReadUInt16BE();
            sbyte srcZ = p.ReadInt8();
            ushort targetX = p.ReadUInt16BE();
            ushort targetY = p.ReadUInt16BE();
            sbyte targetZ = p.ReadInt8();
            byte speed = p.ReadUInt8();
            byte duration = p.ReadUInt8();
            ushort unk = p.ReadUInt16BE();
            bool fixedDirection = p.ReadBool();
            bool doesExplode = p.ReadBool();
            uint hue = 0;
            GraphicEffectBlendMode blendmode = 0;

            if (p[0] == 0x70) { }
            else
            {
                hue = p.ReadUInt32BE();
                blendmode = (GraphicEffectBlendMode)(p.ReadUInt32BE() % 7);

                if (p[0] == 0xC7)
                {
                    var tileID = p.ReadUInt16BE();
                    var explodeEffect = p.ReadUInt16BE();
                    var explodeSound = p.ReadUInt16BE();
                    var serial = p.ReadUInt32BE();
                    var layer = p.ReadUInt8();
                    p.Skip(2);
                }
            }

            world.SpawnEffect(
                type,
                source,
                target,
                graphic,
                (ushort)hue,
                srcX,
                srcY,
                srcZ,
                targetX,
                targetY,
                targetZ,
                speed,
                duration,
                fixedDirection,
                doesExplode,
                false,
                blendmode
            );
        }

        private static void Semivisible(World world, ref StackDataReader p) { }

        private static void DisplayMap(World world, ref StackDataReader p)
        {
            uint serial = p.ReadUInt32BE();
            ushort gumpid = p.ReadUInt16BE();
            ushort startX = p.ReadUInt16BE();
            ushort startY = p.ReadUInt16BE();
            ushort endX = p.ReadUInt16BE();
            ushort endY = p.ReadUInt16BE();
            ushort width = p.ReadUInt16BE();
            ushort height = p.ReadUInt16BE();

            MapGump gump = new MapGump(world, serial, gumpid, width, height);
            SpriteInfo multiMapInfo;

            if (p[0] == 0xF5 || Client.Game.UO.Version >= Utility.ClientVersion.CV_308Z)
            {
                ushort facet = 0;

                if (p[0] == 0xF5)
                {
                    facet = p.ReadUInt16BE();
                }

                multiMapInfo = Client.Game.UO.MultiMaps.GetMap(facet, width, height, startX, startY, endX, endY);
            }
            else
            {
                multiMapInfo = Client.Game.UO.MultiMaps.GetMap(null, width, height, startX, startY, endX, endY);
            }

            if (multiMapInfo.Texture != null)
                gump.SetMapTexture(multiMapInfo.Texture);

            UIManager.Add(gump);

            Item it = world.Items.Get(serial);

            if (it != null)
            {
                it.Opened = true;
            }
        }
    }
}
