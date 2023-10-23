using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace ClassicUO.Game.Managers
{
    internal class SpellVisualRangeManager
    {
        public static SpellVisualRangeManager Instance { get; private set; } = new SpellVisualRangeManager();
        public int LastSpellID
        {
            get => lastSpellID;
            set
            {
                lastSpellID = value;
                lastSpellTime = DateTime.Now;
            }
        }

        public Vector2 LastCursorTileLoc { get; set; } = Vector2.Zero;

        private string savePath = Path.Combine(CUOEnviroment.ExecutablePath, "Data", "Profiles", "SpellVisualRange.json");

        private Dictionary<int, SpellRangeInfo> spellRangeCache = new Dictionary<int, SpellRangeInfo>();

        private DateTime lastSpellTime = DateTime.Now;
        private bool loaded = false;
        private int lastSpellID = -1;

        private SpellVisualRangeManager() { Load(); }

        private void Load()
        {
            spellRangeCache.Clear();

            if (!File.Exists(savePath))
            {
                CreateAndLoadDataFile();
                loaded = true;
            }
            else
            {
                Task.Factory.StartNew(() =>
                {
                    try
                    {
                        string data = File.ReadAllText(savePath);
                        SpellRangeInfo[] fileData = JsonSerializer.Deserialize<SpellRangeInfo[]>(data);

                        foreach (var entry in fileData)
                        {
                            spellRangeCache.Add(entry.ID, entry);
                        }
                        loaded = true;
                    }
                    catch
                    {
                        CreateAndLoadDataFile();
                        loaded = true;
                    }
                });
            }
        }

        public SpellRangeInfo GetSpellInfo(int spellID = -1)
        {
            if (spellID == -1)
            {
                spellID = LastSpellID;
            }

            if (!loaded || LastSpellID == -1)
            {
                return null;
            }

            if (spellRangeCache.TryGetValue(spellID, out SpellRangeInfo info))
            {
                return info;
            }

            return null;
        }

        public bool IsCasting()
        {
            if (!loaded) { return false; }

            if (GameActions.LastSpellIndex == LastSpellID && TargetManager.IsTargeting)
            {
                var spell = GetSpellInfo();
                if (spell != null && lastSpellTime + TimeSpan.FromSeconds(spell.MaxDuration) > DateTime.Now)
                {
                    return true;
                }
            }

            return false;
        }

        public ushort ProcessHueForTile(ushort hue, GameObject o)
        {
            if (!loaded) { return hue; }

            SpellRangeInfo spellRangeInfo = GetSpellInfo();
            if (spellRangeInfo != null)
            {
                if (spellRangeInfo.CastRange > 0 && o.Distance <= spellRangeInfo.CastRange)
                {
                    hue = spellRangeInfo.Hue;
                }

                int cDistance = o.DistanceFrom(LastCursorTileLoc);

                if (spellRangeInfo.CursorSize > 0 && cDistance <= spellRangeInfo.CursorSize)
                {
                    if (spellRangeInfo.IsLinear)
                    {
                        if (GetDirection(new Vector2(World.Player.X, World.Player.Y), LastCursorTileLoc) == Spell_Direction.EastWest)
                        { //X
                            if (o.Y == LastCursorTileLoc.Y)
                            {
                                hue = spellRangeInfo.CursorHue;
                            }
                        }
                        else
                        { //Y
                            if (o.X == LastCursorTileLoc.X)
                            {
                                hue = spellRangeInfo.CursorHue;
                            }
                        }
                    }
                    else
                    {
                        hue = spellRangeInfo.CursorHue;
                    }
                }
            }

            return hue;
        }


        private static Spell_Direction GetDirection(Vector2 from, Vector2 to)
        {
            int dx = (int)(from.X - to.X);
            int dy = (int)(from.Y - to.Y);
            int rx = (dx - dy) * 44;
            int ry = (dx + dy) * 44;

            if (rx >= 0 && ry >= 0)
            {
                return Spell_Direction.SouthNorth;
            }
            else if (rx >= 0)
            {
                return Spell_Direction.EastWest;
            }
            else if (ry >= 0)
            {
                return Spell_Direction.EastWest;
            }
            else
            {
                return Spell_Direction.SouthNorth;
            }
        }

        private void CreateAndLoadDataFile()
        {
            foreach (var entry in SpellsMagery.GetAllSpells)
            {
                spellRangeCache.Add(entry.Value.ID, SpellRangeInfo.FromSpellDef(entry.Value));
            }
            foreach (var entry in SpellsNecromancy.GetAllSpells)
            {
                spellRangeCache.Add(entry.Value.ID, SpellRangeInfo.FromSpellDef(entry.Value));
            }
            foreach (var entry in SpellsChivalry.GetAllSpells)
            {
                spellRangeCache.Add(entry.Value.ID, SpellRangeInfo.FromSpellDef(entry.Value));
            }
            foreach (var entry in SpellsBushido.GetAllSpells)
            {
                spellRangeCache.Add(entry.Value.ID, SpellRangeInfo.FromSpellDef(entry.Value));
            }
            foreach (var entry in SpellsNinjitsu.GetAllSpells)
            {
                spellRangeCache.Add(entry.Value.ID, SpellRangeInfo.FromSpellDef(entry.Value));
            }
            foreach (var entry in SpellsSpellweaving.GetAllSpells)
            {
                spellRangeCache.Add(entry.Value.ID, SpellRangeInfo.FromSpellDef(entry.Value));
            }
            foreach (var entry in SpellsMysticism.GetAllSpells)
            {
                spellRangeCache.Add(entry.Value.ID, SpellRangeInfo.FromSpellDef(entry.Value));
            }
            foreach (var entry in SpellsMastery.GetAllSpells)
            {
                spellRangeCache.Add(entry.Value.ID, SpellRangeInfo.FromSpellDef(entry.Value));
            }

            Task.Factory.StartNew(() =>
            {
                Save();
            });
        }

        public void Save()
        {
            try
            {
                string fileData = JsonSerializer.Serialize(spellRangeCache.Values.ToArray());

                File.WriteAllText(savePath, fileData);
            }
            catch (Exception e) { Console.WriteLine(e.ToString()); }
        }

        private enum Spell_Direction
        {
            EastWest,
            SouthNorth
        }

        public class SpellRangeInfo
        {
            public int ID { get; set; } = -1;
            public string Name { get; set; } = "";
            public int CursorSize { get; set; } = 0;
            public int CastRange { get; set; } = 1;
            public ushort Hue { get; set; } = 32;
            public ushort CursorHue { get; set; } = 10;
            public int MaxDuration { get; set; } = 10;
            public bool IsLinear { get; set; } = false;

            public static SpellRangeInfo FromSpellDef(SpellDefinition spell)
            {
                return new SpellRangeInfo() { ID = spell.ID, Name = spell.Name };
            }
        }
    }
}
