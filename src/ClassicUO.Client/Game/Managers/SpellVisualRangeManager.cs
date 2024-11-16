using ClassicUO.Configuration;
using ClassicUO.Dust765.Managers;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ClassicUO.Game.Managers
{
    internal class SpellVisualRangeManager
    {
        public static SpellVisualRangeManager Instance => instance ??= new SpellVisualRangeManager();

        public Vector2 LastCursorTileLoc { get; set; } = Vector2.Zero;
        public DateTime LastSpellTime { get; private set; } = DateTime.Now;

        private string savePath = Path.Combine(CUOEnviroment.ExecutablePath ?? "", "Data", "Profiles", "SpellVisualRange.json");
        private string overridePath = Path.Combine(ProfileManager.ProfilePath ?? "", "SpellVisualRange.json");

        private Dictionary<int, SpellRangeInfo> spellRangeCache = new Dictionary<int, SpellRangeInfo>();
        private Dictionary<int, SpellRangeInfo> spellRangeOverrideCache = new Dictionary<int, SpellRangeInfo>();
        private Dictionary<string, SpellRangeInfo> spellRangePowerWordCache = new Dictionary<string, SpellRangeInfo>();

        private bool loaded = false;
        private static SpellVisualRangeManager instance;

        private bool isCasting { get; set; } = false;
        private SpellRangeInfo currentSpell { get; set; }

        //Taken from Dust client
        private static readonly int[] stopAtClilocs = new int[]
        {
            500641,     // Your concentration is disturbed, thus ruining thy spell.
            502625,     // Insufficient mana. You must have at least ~1_MANA_REQUIREMENT~ Mana to use this spell.
            502630,     // More reagents are needed for this spell.
            500946,     // You cannot cast this in town!
            500015,     // You do not have that spell
            502643,     // You can not cast a spell while frozen.
            1061091,    // You cannot cast that spell in this form.
            502644,     // You have not yet recovered from casting a spell.
            1072060,    // You cannot cast a spell while calmed.
        };

        private SpellVisualRangeManager()
        {
            Load();
        }

        public static string RemoveContentInBrackets(string input)
        {
            // Expressão regular para encontrar o conteúdo entre colchetes
            string pattern = @"\[.*?\]";

            // Substituir o conteúdo encontrado por uma string vazia
            string result = Regex.Replace(input, pattern, "").Trim();

            return result;
        }

        private void OnRawMessageReceived(object sender, MessageEventArgs e)
        {
            Task.Factory.StartNew(() =>
            {
                if (loaded && e.Parent != null && ReferenceEquals(e.Parent, World.Player))
                {
                    // ## BEGIN - END ## // ONCASTINGGUMP
                    if (ProfileManager.CurrentProfile.OnCastingGump)
                    {
                        if (spellRangePowerWordCache.TryGetValue(RemoveContentInBrackets(e.Text.Trim()), out SpellRangeInfo spell))
                        {
                            GameActions.LastSpellIndex = spell.ID;
                            // ## BEGIN - END ## // VISUAL HELPERS
                            GameActions.LastSpellIndexCursor = spell.ID;
                            GameCursor._spellTime = 0;
                            // ## BEGIN - END ## // VISUAL HELPERS
                            // ## BEGIN - END ## // ONCASTINGGUMP
                            if (!GameActions.iscasting)
                                World.Player.OnCasting.Start((uint)spell.ID);
                        }
                            
                    }
                    if (ProfileManager.CurrentProfile.EnableSpellIndicators)
                    {
                        if (spellRangePowerWordCache.TryGetValue(RemoveContentInBrackets(e.Text.Trim()), out SpellRangeInfo spell))
                        {
                            SetCasting(spell);

                        }
                    }
                   
                }
            });
        }

        public void OnClilocReceived(int cliloc)
        {
            Task.Factory.StartNew(() =>
            {
                if (isCasting && stopAtClilocs.Contains(cliloc))
                {
                    ClearCasting();
                }
            });
        }

        private void SetCasting(SpellRangeInfo spell)
        {
            LastSpellTime = DateTime.Now;
            currentSpell = spell;
            isCasting = true;
            


        }

        public void ClearCasting()
        {
            isCasting = false;
            currentSpell = null;
            LastSpellTime = DateTime.MinValue;
            World.Player.Flags &= ~Flags.Frozen;
        }

        public SpellRangeInfo GetCurrentSpell()
        {
            return currentSpell;
        }

        #region Load and unload
        public void OnSceneLoad()
        {
            EventSink.RawMessageReceived += OnRawMessageReceived;
        }

        public void OnSceneUnload()
        {
            EventSink.RawMessageReceived -= OnRawMessageReceived;
            instance = null;
        }
        #endregion

        public bool IsTargetingAfterCasting()
        {
            if (!loaded || currentSpell == null || !isCasting || ProfileManager.CurrentProfile == null || !ProfileManager.CurrentProfile.EnableSpellIndicators)
            {
                return false;
            }

            if (TargetManager.IsTargeting || (currentSpell.ShowCastRangeDuringCasting && IsCastingWithoutTarget()))
            {
                if (LastSpellTime + TimeSpan.FromSeconds(currentSpell.MaxDuration) > DateTime.Now)
                {
                    return true;
                }
            }

            return false;
        }

        public bool IsCastingWithoutTarget()
        {
            if (!loaded || currentSpell == null || !isCasting || currentSpell.CastTime <= 0 || TargetManager.IsTargeting || ProfileManager.CurrentProfile == null || !ProfileManager.CurrentProfile.EnableSpellIndicators)
            {
                return false;
            }

            if (LastSpellTime + TimeSpan.FromSeconds(currentSpell.MaxDuration) > DateTime.Now)
            {
                if (LastSpellTime + TimeSpan.FromSeconds(currentSpell.CastTime) > DateTime.Now)
                {
                    return true;
                }
                else if (currentSpell.FreezeCharacterWhileCasting)
                {
                    World.Player.Flags &= ~Flags.Frozen;
                }
            }
            else if (currentSpell.FreezeCharacterWhileCasting)
            {
                World.Player.Flags &= ~Flags.Frozen;
            }

            return false;
        }

        public ushort ProcessHueForTile(ushort hue, GameObject o)
        {
            if (!loaded || currentSpell == null) { return hue; }

            if (currentSpell.CastRange > 0 && o.Distance <= currentSpell.CastRange)
            {
                hue = currentSpell.Hue;
            }

            int cDistance = o.DistanceFrom(LastCursorTileLoc);

            if (currentSpell.CursorSize > 0 && cDistance < currentSpell.CursorSize)
            {
                if (currentSpell.IsLinear)
                {
                    if (GetDirection(new Vector2(World.Player.X, World.Player.Y), LastCursorTileLoc) == SpellDirection.EastWest)
                    { //X
                        if (o.Y == LastCursorTileLoc.Y)
                        {
                            hue = currentSpell.CursorHue;
                        }
                    }
                    else
                    { //Y
                        if (o.X == LastCursorTileLoc.X)
                        {
                            hue = currentSpell.CursorHue;
                        }
                    }
                }
                else
                {
                    hue = currentSpell.CursorHue;
                }
            }

            return hue;
        }

        private static SpellDirection GetDirection(Vector2 from, Vector2 to)
        {
            int dx = (int)(from.X - to.X);
            int dy = (int)(from.Y - to.Y);
            int rx = (dx - dy) * 44;
            int ry = (dx + dy) * 44;

            if (rx >= 0 && ry >= 0)
            {
                return SpellDirection.SouthNorth;
            }
            else if (rx >= 0)
            {
                return SpellDirection.EastWest;
            }
            else if (ry >= 0)
            {
                return SpellDirection.EastWest;
            }
            else
            {
                return SpellDirection.SouthNorth;
            }
        }

        #region Save and load
        private void Load()
        {
            spellRangeCache.Clear();
            Task.Factory.StartNew(() =>
            {
                if (!File.Exists(savePath))
                {
                    CreateAndLoadDataFile();
                    AfterLoad();
                    loaded = true;
                }
                else
                {
                    try
                    {
                        string data = File.ReadAllText(savePath);
                        SpellRangeInfo[] fileData = JsonSerializer.Deserialize<SpellRangeInfo[]>(data);

                        foreach (var entry in fileData)
                        {
                            spellRangeCache.Add(entry.ID, entry);
                        }
                        AfterLoad();
                        loaded = true;
                    }
                    catch
                    {
                        CreateAndLoadDataFile();
                        AfterLoad();
                        loaded = true;
                    }

                }
            });
        }

        private void LoadOverrides()
        {
            spellRangeOverrideCache.Clear();

            if (File.Exists(overridePath))
            {
                try
                {
                    string data = File.ReadAllText(overridePath);
                    SpellRangeInfo[] fileData = JsonSerializer.Deserialize<SpellRangeInfo[]>(data);

                    foreach (var entry in fileData)
                    {
                        spellRangeOverrideCache.Add(entry.ID, entry);
                    }

                    foreach (var entry in spellRangeOverrideCache.Values)
                    {
                        if (string.IsNullOrEmpty(entry.PowerWords))
                        {
                            SpellDefinition spellD = SpellDefinition.FullIndexGetSpell(entry.ID);
                            if (spellD == SpellDefinition.EmptySpell)
                            {
                                SpellDefinition.TryGetSpellFromName(entry.Name, out spellD);
                            }

                            if (spellD != SpellDefinition.EmptySpell)
                            {
                                entry.PowerWords = spellD.PowerWords;
                            }
                        }
                        if (!string.IsNullOrEmpty(entry.PowerWords))
                        {
                            if (spellRangePowerWordCache.ContainsKey(entry.PowerWords))
                            {
                                spellRangePowerWordCache[entry.PowerWords] = entry;
                            }
                            else
                            {
                                spellRangePowerWordCache.Add(entry.PowerWords, entry);
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                }
            }
        }

        public bool LoadFromString(string json)
        {
            try
            {
                SpellRangeInfo[] fileData = JsonSerializer.Deserialize<SpellRangeInfo[]>(json);

                loaded = false;
                spellRangeCache.Clear();

                foreach (var entry in fileData)
                {
                    spellRangeCache.Add(entry.ID, entry);
                }
                AfterLoad();
                LoadOverrides();
                loaded = true;
                return true;
            }
            catch (Exception ex)
            {
                loaded = true;
                Console.WriteLine(ex.ToString());
                return false;
            }
        }

        private void AfterLoad()
        {
            spellRangePowerWordCache.Clear();
            foreach (var entry in spellRangeCache.Values)
            {
                if (string.IsNullOrEmpty(entry.PowerWords))
                {
                    SpellDefinition spellD = SpellDefinition.FullIndexGetSpell(entry.ID);
                    if (spellD == SpellDefinition.EmptySpell)
                    {
                        SpellDefinition.TryGetSpellFromName(entry.Name, out spellD);
                    }

                    if (spellD != SpellDefinition.EmptySpell)
                    {
                        entry.PowerWords = spellD.PowerWords;
                    }
                }
                if (!string.IsNullOrEmpty(entry.PowerWords))
                {
                    spellRangePowerWordCache.Add(entry.PowerWords, entry);
                }
            }
            LoadOverrides();
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
                var options = new JsonSerializerOptions() { WriteIndented = true };
                string fileData = JsonSerializer.Serialize(spellRangeCache.Values.ToArray(), options);
                File.WriteAllText(savePath, fileData);
            }
            catch (Exception e) { Console.WriteLine(e.ToString()); }
        }
        #endregion

        private enum SpellDirection
        {
            EastWest,
            SouthNorth
        }

        public class SpellRangeInfo
        {
            public int ID { get; set; } = -1;
            public string Name { get; set; } = "";
            public string PowerWords { get; set; } = "";
            public int CursorSize { get; set; } = 0;
            public int CastRange { get; set; } = 1;
            public ushort Hue { get; set; } = 32;
            public ushort CursorHue { get; set; } = 10;
            public int MaxDuration { get; set; } = 10;
            public bool IsLinear { get; set; } = false;
            public double CastTime { get; set; } = 0.0;
            public bool ShowCastRangeDuringCasting { get; set; } = false;
            public bool FreezeCharacterWhileCasting { get; set; } = false;

            public static SpellRangeInfo FromSpellDef(SpellDefinition spell)
            {
                return new SpellRangeInfo() { ID = spell.ID, Name = spell.Name, PowerWords = spell.PowerWords };
            }
        }

        #region Cast Timer Bar


        public class CastTimerProgressBar : Gump
        {
            private Rectangle barBounds, barBoundsF;
            private Texture2D background;
            private Texture2D foreground;
            private Vector3 hue = ShaderHueTranslator.GetHueVector(0);


            public CastTimerProgressBar() : base(0, 0)
            {
                CanMove = false;
                AcceptMouseInput = false;
                CanCloseWithEsc = false;
                CanCloseWithRightClick = false;

                ref readonly var gi = ref Client.Game.Gumps.GetGump(0x0805);
                background = gi.Texture;
                barBounds = gi.UV;

                gi = ref Client.Game.Gumps.GetGump(0x0806);
                foreground = gi.Texture;
                barBoundsF = gi.UV;
            }

            public override bool Draw(UltimaBatcher2D batcher, int x, int y)
            {
                if (SpellVisualRangeManager.Instance.IsCastingWithoutTarget())
                {
                    SpellRangeInfo i = SpellVisualRangeManager.Instance.GetCurrentSpell();
                    if (i != null)
                    {
                        if (i.CastTime > 0)
                        {
                            if (background != null && foreground != null)
                            {
                                Mobile m = World.Player;
                                Client.Game.Animations.GetAnimationDimensions(
                                    m.AnimIndex,
                                    m.GetGraphicForAnimation(),
                                    0,
                                    0,
                                    m.IsMounted,
                                    0,
                                    out int centerX,
                                    out int centerY,
                                    out int width,
                                    out int height
                                );

                                WorldViewportGump vp = UIManager.GetGump<WorldViewportGump>();

                                x = vp.Location.X + (int)(m.RealScreenPosition.X - (m.Offset.X + 22 + 5));
                                y = vp.Location.Y + (int)(m.RealScreenPosition.Y - ((m.Offset.Y - m.Offset.Z) - (height + centerY + 15) + (m.IsGargoyle && m.IsFlying ? -22 : !m.IsMounted ? 22 : 0)));

                                batcher.Draw(background, new Rectangle(x, y, barBounds.Width, barBounds.Height), barBounds, hue);

                                double percent = (DateTime.Now - SpellVisualRangeManager.Instance.LastSpellTime).TotalSeconds / i.CastTime;

                                int widthFromPercent = (int)(barBounds.Width * percent);
                                widthFromPercent = widthFromPercent > barBounds.Width ? barBounds.Width : widthFromPercent; //Max width is the bar width

                                if (widthFromPercent > 0)
                                {
                                    batcher.DrawTiled(foreground, new Rectangle(x, y, widthFromPercent, barBoundsF.Height), barBoundsF, hue);
                                }

                                if (percent <= 0 && i.FreezeCharacterWhileCasting)
                                {
                                    World.Player.Flags &= ~Flags.Frozen;
                                }
                            }
                        }
                    }
                }
                return base.Draw(batcher, x, y);
            }
        }
        #endregion
    }
}
