using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using ClassicUO.Configuration;
using ClassicUO.Game;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.Scenes;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.LegionScripting.PyClasses;
using ClassicUO.Network;
using Microsoft.Xna.Framework;

namespace ClassicUO.LegionScripting
{
    public class API
    {
        private readonly ConcurrentBag<Gump> _gumps = new ConcurrentBag<Gump>();
        private static readonly ConcurrentDictionary<string, object> _sharedVars = new ConcurrentDictionary<string, object>();
        private readonly Queue<Action> _scheduledCallbacks = new Queue<Action>();
        private readonly object _callbackLock = new object();
        private ConcurrentBag<uint> _ignoreList = new ConcurrentBag<uint>();
        private readonly ConcurrentQueue<JournalEntry> _journalEntries = new ConcurrentQueue<JournalEntry>();

        public API(Microsoft.Scripting.Hosting.ScriptEngine engine)
        {
        }

        public ConcurrentQueue<JournalEntry> JournalEntries => _journalEntries;

        private void ScheduleCallback(Action action)
        {
            lock (_scheduledCallbacks)
            {
                _scheduledCallbacks.Enqueue(action);
                while (_scheduledCallbacks.Count > 100)
                {
                    _scheduledCallbacks.Dequeue();
                    GameActions.Print("Python Scripting Error: Too many callbacks registered!");
                }
            }
        }

        public uint Backpack => MainThreadQueue.InvokeOnMainThread(() => World.Player?.FindItemByLayer(Layer.Backpack)?.Serial ?? 0);
        public PlayerMobile Player => MainThreadQueue.InvokeOnMainThread(() => World.Player);
        public uint Bank => MainThreadQueue.InvokeOnMainThread(() => World.Player?.FindItemByLayer(Layer.Bank)?.Serial ?? 0);
        public Random Random { get; set; } = new Random();
        public uint LastTargetSerial => MainThreadQueue.InvokeOnMainThread(() => TargetManager.LastTargetInfo.Serial);
        public Vector3 LastTargetPos => MainThreadQueue.InvokeOnMainThread(() =>
        {
            var t = TargetManager.LastTargetInfo;
            return new Vector3(t.X, t.Y, t.Z);
        });
        public ushort LastTargetGraphic => MainThreadQueue.InvokeOnMainThread(() => TargetManager.LastTargetInfo.Graphic);
        public uint Found { get; set; }
        public static PyProfile PyProfile => new PyProfile();

        public enum ScanType { Hostile = 0, Party, Followers, Objects, Mobiles }
        public enum Notoriety : byte { Unknown = 0x00, Innocent = 0x01, Ally = 0x02, Gray = 0x03, Criminal = 0x04, Enemy = 0x05, Murderer = 0x06, Invulnerable = 0x07 }
        public enum PersistentVar { Char, Account, Server, Global }

        public void SetSharedVar(string name, object value) => _sharedVars[name] = value;
        public object GetSharedVar(string name) => _sharedVars.TryGetValue(name, out var v) ? v : null;
        public void RemoveSharedVar(string name) => _sharedVars.TryRemove(name, out _);
        public void ClearSharedVars() => _sharedVars.Clear();

        public void CloseGumps()
        {
            int c = 0;
            while (_gumps.TryTake(out var g) && c < 1000)
            {
                if (g != null && !g.IsDisposed)
                    MainThreadQueue.EnqueueAction(() => g?.Dispose());
                c++;
            }
        }

        public void Attack(uint serial) => MainThreadQueue.EnqueueAction(() => GameActions.Attack(serial));
        public bool BandageSelf() => MainThreadQueue.InvokeOnMainThread<bool>(() =>
        {
            if (World.Player?.FindBandage() != null) { GameActions.BandageSelf(); return true; }
            return false;
        });

        public PyItem ClearLeftHand() => MainThreadQueue.InvokeOnMainThread<PyItem>(() =>
        {
            Item i = World.Player?.FindItemByLayer(Layer.OneHanded);
            if (i == null) { Found = 0; return null; }
            Item bp = World.Player.FindItemByLayer(Layer.Backpack);
            if (bp == null) { Found = 0; return null; }
            GameActions.PickUp(i.Serial, 0, 0, 1);
            GameActions.DropItem(i.Serial, 0xFFFF, 0xFFFF, 0, bp.Serial);
            Found = i.Serial;
            return new PyItem(i);
        });

        public PyItem ClearRightHand() => MainThreadQueue.InvokeOnMainThread<PyItem>(() =>
        {
            Item i = World.Player?.FindItemByLayer(Layer.TwoHanded);
            if (i == null) { Found = 0; return null; }
            Item bp = World.Player.FindItemByLayer(Layer.Backpack);
            if (bp == null) { Found = 0; return null; }
            GameActions.PickUp(i.Serial, 0, 0, 1);
            GameActions.DropItem(i.Serial, 0xFFFF, 0xFFFF, 0, bp.Serial);
            Found = i.Serial;
            return new PyItem(i);
        });

        public void ClickObject(uint serial) => MainThreadQueue.EnqueueAction(() => GameActions.SingleClick(serial));
        public void UseObject(uint serial, bool skipQueue = true) => MainThreadQueue.EnqueueAction(() =>
        {
            if (skipQueue) GameActions.DoubleClick(serial);
            else GameActions.DoubleClickQueued(serial);
        });

        public int Contents(uint serial) => MainThreadQueue.InvokeOnMainThread(() =>
        {
            Item i = World.Items.Get(serial);
            return i != null ? (int)Utility.ContentsCount(i) : 0;
        });

        public void ContextMenu(uint serial, ushort entry) => MainThreadQueue.EnqueueAction(() =>
        {
            PopupMenuGump.CloseNext = serial;
            NetClient.Socket.Send_RequestPopupMenu(serial);
            NetClient.Socket.Send_PopupMenuSelection(serial, entry);
        });

        public void EquipItem(uint serial) => MainThreadQueue.EnqueueAction(() =>
        {
            GameActions.PickUp(serial, 0, 0, 1);
            GameActions.Equip();
        });

        public void ClearMoveQueue() { }

        public void QueMoveItem(uint serial, uint destination, ushort amt = 0, int x = 0xFFFF, int y = 0xFFFF) =>
            MoveItem(serial, destination, amt, x, y);

        public void MoveItem(uint serial, uint destination, int amt = 0, int x = 0xFFFF, int y = 0xFFFF) => MainThreadQueue.EnqueueAction(() =>
        {
            GameActions.PickUp(serial, 0, 0, amt);
            GameActions.DropItem(serial, x, y, 0, destination);
        });

        public void QueMoveItemOffset(uint serial, ushort amt = 0, int x = 0, int y = 0, int z = 0, bool OSI = false) =>
            MoveItemOffset(serial, amt, x, y, z, OSI);

        public void MoveItemOffset(uint serial, int amt = 0, int x = 0, int y = 0, int z = 0, bool OSI = false) => MainThreadQueue.EnqueueAction(() =>
        {
            World.Map.GetMapZ(World.Player.X + x, World.Player.Y + y, out sbyte gz, out sbyte gz2);
            int iz = z;
            if (gz > iz) iz = gz;
            if (gz2 > iz) iz = gz2;
            if (iz == z) iz = World.Player.Z + z;
            GameActions.PickUp(serial, 0, 0, amt);
            GameActions.DropItem(serial, World.Player.X + x, World.Player.Y + y, iz, OSI ? uint.MaxValue : 0);
        });

        public void UseSkill(string skillName) => MainThreadQueue.EnqueueAction(() =>
        {
            if (string.IsNullOrEmpty(skillName) || World.Player?.Skills == null) return;
            for (int i = 0; i < World.Player.Skills.Length; i++)
            {
                if (World.Player.Skills[i].Name.IndexOf(skillName, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    GameActions.UseSkill(World.Player.Skills[i].Index);
                    break;
                }
            }
        });

        public void CastSpell(string spellName) => MainThreadQueue.EnqueueAction(() => GameActions.CastSpellByName(spellName));
        public void Cast(string spellName) => CastSpell(spellName);

        public bool BuffExists(string buffName) => MainThreadQueue.InvokeOnMainThread(() =>
        {
            if (World.Player?.BuffIcons == null) return false;
            foreach (BuffIcon buff in World.Player.BuffIcons.Values)
                if (buff.Title != null && buff.Title.Contains(buffName)) return true;
            return false;
        });

        public Buff[] ActiveBuffs() => MainThreadQueue.InvokeOnMainThread(() =>
        {
            if (World.Player?.BuffIcons == null) return new Buff[0];
            return World.Player.BuffIcons.Values.Select(b => new Buff(b)).ToArray();
        });

        public void SysMsg(string message, ushort hue = 946) => MainThreadQueue.EnqueueAction(() => GameActions.Print(message, hue));
        public void SysMessage(string message, ushort hue = 946) => SysMsg(message, hue);
        public void Msg(string message) => MainThreadQueue.EnqueueAction(() => GameActions.Say(message, ProfileManager.CurrentProfile.SpeechHue));
        public void Say(string message) => Msg(message);

        public void HeadMsg(string message, uint serial, ushort hue = ushort.MaxValue) => MainThreadQueue.EnqueueAction(() =>
        {
            Entity e = World.Get(serial);
            if (e == null) return;
            if (hue == ushort.MaxValue) hue = ProfileManager.CurrentProfile.SpeechHue;
            MessageManager.HandleMessage(e, message, "", hue, MessageType.Label, 3, TextType.OBJECT);
        });
        public void HeadMsg(string message, uint serial) => HeadMsg(message, serial, ushort.MaxValue);

        public void PartyMsg(string message) => MainThreadQueue.EnqueueAction(() => GameActions.SayParty(message));
        public void GuildMsg(string message) => MainThreadQueue.EnqueueAction(() => GameActions.Say(message, ProfileManager.CurrentProfile.GuildMessageHue, MessageType.Guild));
        public void AllyMsg(string message) => MainThreadQueue.EnqueueAction(() => GameActions.Say(message, ProfileManager.CurrentProfile.AllyMessageHue, MessageType.Alliance));
        public void WhisperMsg(string message) => MainThreadQueue.EnqueueAction(() => GameActions.Say(message, ProfileManager.CurrentProfile.WhisperHue, MessageType.Whisper));
        public void YellMsg(string message) => MainThreadQueue.EnqueueAction(() => GameActions.Say(message, ProfileManager.CurrentProfile.YellHue, MessageType.Yell));
        public void EmoteMsg(string message) => MainThreadQueue.EnqueueAction(() => GameActions.Say(message, ProfileManager.CurrentProfile.EmoteHue, MessageType.Emote));

        public void PromptResponse(string message) => MainThreadQueue.EnqueueAction(() =>
        {
            if (MessageManager.PromptData.Prompt != ConsolePrompt.None)
            {
                if (MessageManager.PromptData.Prompt == ConsolePrompt.ASCII)
                    NetClient.Socket.Send_ASCIIPromptResponse(message, message.Length < 1);
                else if (MessageManager.PromptData.Prompt == ConsolePrompt.Unicode)
                    NetClient.Socket.Send_UnicodePromptResponse(message, Settings.GlobalSettings.Language, message.Length < 1);
                MessageManager.PromptData = default;
            }
        });

        public PyItem FindItem(uint serial) => MainThreadQueue.InvokeOnMainThread(() =>
        {
            Item i = World.Items.Get(serial);
            Found = i?.Serial ?? 0;
            return new PyItem(i);
        });

        public PyItem FindType(uint graphic, uint container = uint.MaxValue, ushort range = ushort.MaxValue, ushort hue = ushort.MaxValue, ushort minamount = 0) =>
            MainThreadQueue.InvokeOnMainThread(() =>
            {
                var list = Utility.FindItems(graphic, uint.MaxValue, uint.MaxValue, container, hue, range, true);
                foreach (Item i in list)
                {
                    if (i.Amount >= minamount && !_ignoreList.Contains(i.Serial))
                    {
                        Found = i.Serial;
                        LScript.Interpreter.SetAlias("found", i.Serial);
                        return new PyItem(i);
                    }
                }
                Found = 0;
                LScript.Interpreter.SetAlias("found", uint.MaxValue);
                return null;
            });

        public PyItem[] FindTypeAll(uint graphic, uint container = uint.MaxValue, ushort range = ushort.MaxValue, ushort hue = ushort.MaxValue, ushort minamount = 0) =>
            MainThreadQueue.InvokeOnMainThread(() =>
                Utility.FindItems(graphic, uint.MaxValue, uint.MaxValue, container, hue, range, true)
                    .Where(i => !_ignoreList.Contains(i.Serial) && i.Amount >= minamount)
                    .Select(i => new PyItem(i))
                    .ToArray());

        public PyItem FindLayer(string layer, uint serial = uint.MaxValue) => MainThreadQueue.InvokeOnMainThread(() =>
        {
            Found = 0;
            Mobile m = serial == uint.MaxValue ? World.Player : World.Mobiles.Get(serial);
            if (m == null) return null;
            Layer matchedLayer = Utility.GetItemLayer(layer.ToLower());
            Item item = m.FindItemByLayer(matchedLayer);
            if (item != null) { Found = item.Serial; return new PyItem(item); }
            return null;
        });

        public PyItem[] GetItemsOnGround(int distance = int.MaxValue, uint graphic = uint.MaxValue) => MainThreadQueue.InvokeOnMainThread(() =>
        {
            var list = new List<PyItem>();
            foreach (Item item in World.Items.Values)
            {
                if (item == null || item.IsDestroyed || !item.OnGround || OnIgnoreList(item)) continue;
                if (distance != int.MaxValue && item.Distance > distance) continue;
                if (graphic != uint.MaxValue && item.Graphic != graphic) continue;
                list.Add(new PyItem(item));
            }
            return list.ToArray();
        });

        public PyItem[] ItemsInContainer(uint container, bool recursive = false) => MainThreadQueue.InvokeOnMainThread(() =>
        {
            if (!recursive)
                return Utility.FindItems(uint.MaxValue, uint.MaxValue, uint.MaxValue, container, ushort.MaxValue, int.MaxValue, true)
                    .Select(i => new PyItem(i)).ToArray();
            var results = new List<PyItem>();
            var stack = new Stack<uint>();
            stack.Push(container);
            while (stack.Count > 0)
            {
                uint current = stack.Pop();
                foreach (Item item in Utility.FindItems(uint.MaxValue, uint.MaxValue, uint.MaxValue, current, ushort.MaxValue, int.MaxValue, true))
                {
                    results.Add(new PyItem(item));
                    stack.Push(item.Serial);
                }
            }
            return results.ToArray();
        });

        public void UseType(uint graphic, ushort hue = ushort.MaxValue, uint container = uint.MaxValue, bool skipQueue = true) => MainThreadQueue.EnqueueAction(() =>
        {
            foreach (Item i in Utility.FindItems(graphic, uint.MaxValue, uint.MaxValue, container, hue, int.MaxValue, true))
            {
                if (!_ignoreList.Contains(i.Serial))
                {
                    if (skipQueue) GameActions.DoubleClick(i.Serial);
                    else GameActions.DoubleClickQueued(i.Serial);
                    return;
                }
            }
        });

        public void CreateCooldownBar(double seconds, string text, ushort hue) => MainThreadQueue.EnqueueAction(() =>
            CoolDownBarManager.AddCoolDownBar(TimeSpan.FromSeconds(seconds), text, hue, false));

        public void Pause(double seconds) => Thread.Sleep((int)(seconds * 1000));

        public void IgnoreObject(uint serial) => _ignoreList.Add(serial);
        public void ClearIgnoreList() => _ignoreList = new ConcurrentBag<uint>();
        public bool OnIgnoreList(uint serial) => _ignoreList.Contains(serial);

        private bool OnIgnoreList(Item item) => item != null && _ignoreList.Contains(item.Serial);

        public void SetPersistentVar(string name, object value, PersistentVar scope)
        {
            PersistentVars.SaveVar((global::ClassicUO.LegionScripting.PersistentVar)(int)scope, name, value?.ToString() ?? "");
        }

        public string GetPersistentVar(string name, PersistentVar scope, string defaultValue = "")
        {
            return PersistentVars.GetVar((global::ClassicUO.LegionScripting.PersistentVar)(int)scope, name, defaultValue);
        }

        public void DeletePersistentVar(string name, PersistentVar scope)
        {
            PersistentVars.DeleteVar((global::ClassicUO.LegionScripting.PersistentVar)(int)scope, name);
        }

        public bool WaitForJournal(string text, double timeoutSeconds = 10)
        {
            var deadline = DateTime.Now.AddSeconds(timeoutSeconds);
            while (DateTime.Now < deadline)
            {
                ProcessCallbacks();
                while (_journalEntries.TryDequeue(out var entry))
                {
                    if (entry?.Text != null && entry.Text.IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0)
                        return true;
                }
                Pause(0.1);
            }
            return false;
        }

        public bool InJournal(string text)
        {
            var temp = new List<JournalEntry>();
            while (_journalEntries.TryDequeue(out var e))
                temp.Add(e);
            bool found = false;
            foreach (var e in temp)
            {
                if (e?.Text != null && e.Text.IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0)
                    found = true;
                _journalEntries.Enqueue(e);
            }
            return found;
        }

        public void ClearJournal()
        {
            while (_journalEntries.TryDequeue(out _)) { }
        }

        public uint GetAlias(string name) => LScript.Interpreter.GetAlias(name);
        public void SetAlias(string name, uint serial) => LScript.Interpreter.SetAlias(name, serial);

        public PyItem NearestCorpse(int range = 12) => MainThreadQueue.InvokeOnMainThread(() =>
        {
            Item nearest = null;
            int bestDist = int.MaxValue;
            foreach (Item item in World.Items.Values)
            {
                if (item == null || item.IsDestroyed || !item.IsCorpse || item.Distance > range) continue;
                if (_ignoreList.Contains(item.Serial)) continue;
                if (item.Distance < bestDist) { bestDist = item.Distance; nearest = item; }
            }
            if (nearest != null) { Found = nearest.Serial; return new PyItem(nearest); }
            Found = 0;
            return null;
        });

        public string ItemNameAndProps(uint serial) => MainThreadQueue.InvokeOnMainThread(() =>
        {
            if (World.OPL.TryGetNameAndData(serial, out string name, out string data))
                return (name ?? "") + (data != null ? " " + data : "");
            Item i = World.Items.Get(serial);
            if (i == null) return "";
            return i.Name ?? (i.ItemData.Name ?? "");
        });

        public uint RequestTarget()
        {
            MainThreadQueue.EnqueueAction(() =>
            {
                TargetManager.LastTargetInfo.Clear();
                TargetManager.SetTargeting(CursorTarget.Internal, CursorType.Target, TargetType.Neutral);
            });
            return 0;
        }

        public void WaitForTarget(int timeoutMs = 5000)
        {
            int elapsed = 0;
            while (elapsed < timeoutMs && TargetManager.IsTargeting && TargetManager.LastTargetInfo.Serial == 0)
            {
                Pause(0.1);
                elapsed += 100;
            }
        }

        public void Target(uint serial) => MainThreadQueue.EnqueueAction(() => TargetManager.LastTargetInfo?.SetEntity(serial));

        public bool Pathfind(int x, int y, int z = int.MinValue, int distance = 1, bool wait = false, int timeout = 10)
        {
            int zVal = z;
            bool pathFindStatus = MainThreadQueue.InvokeOnMainThread(() =>
            {
                if (zVal == int.MinValue) zVal = World.Map.GetTileZ(x, y);
                return Pathfinder.WalkTo(x, y, zVal, distance);
            });
            if (!wait) return pathFindStatus;
            if (timeout > 30) timeout = 30;
            var expire = DateTime.Now.AddSeconds(timeout);
            while (MainThreadQueue.InvokeOnMainThread(() => Pathfinder.AutoWalking))
            {
                if (DateTime.Now >= expire) { MainThreadQueue.EnqueueAction(Pathfinder.StopAutoWalk); return false; }
                Pause(0.25);
            }
            MainThreadQueue.EnqueueAction(Pathfinder.StopAutoWalk);
            return MainThreadQueue.InvokeOnMainThread(() => World.Player.DistanceFrom(new Microsoft.Xna.Framework.Vector2(x, y)) <= distance);
        }

        public bool PathfindEntity(uint entity, int distance = 1, bool wait = false, int timeout = 10)
        {
            int x = 0, y = 0, z = 0;
            bool pathFindStatus = MainThreadQueue.InvokeOnMainThread(() =>
            {
                var mob = World.Get(entity);
                if (mob != null) { x = mob.X; y = mob.Y; z = mob.Z; return Pathfinder.WalkTo(x, y, z, distance); }
                return false;
            });
            if (!wait || (x == 0 && y == 0)) return pathFindStatus;
            if (timeout > 30) timeout = 30;
            var expire = DateTime.Now.AddSeconds(timeout);
            while (MainThreadQueue.InvokeOnMainThread(() => Pathfinder.AutoWalking))
            {
                if (DateTime.Now >= expire) { MainThreadQueue.EnqueueAction(Pathfinder.StopAutoWalk); return false; }
                Pause(0.25);
            }
            MainThreadQueue.EnqueueAction(Pathfinder.StopAutoWalk);
            return MainThreadQueue.InvokeOnMainThread(() => World.Player.DistanceFrom(new Microsoft.Xna.Framework.Vector2(x, y)) <= distance);
        }

        public bool Pathfinding() => MainThreadQueue.InvokeOnMainThread(() => Pathfinder.AutoWalking);
        public void CancelPathfinding() => MainThreadQueue.EnqueueAction(Pathfinder.StopAutoWalk);

        public PyMobile NearestMobile(Notoriety[] notorieties, int range = 12)
        {
            return MainThreadQueue.InvokeOnMainThread(() =>
            {
                if (World.Mobiles == null) return null;
                Mobile nearest = null;
                int bestDist = int.MaxValue;
                foreach (Mobile m in World.Mobiles.Values)
                {
                    if (m == null || m.IsDestroyed || m.Distance > range) continue;
                    if (notorieties != null && notorieties.Length > 0 && !notorieties.Contains((Notoriety)(byte)m.NotorietyFlag)) continue;
                    if (m.Distance < bestDist) { bestDist = m.Distance; nearest = m; }
                }
                return nearest != null ? new PyMobile(nearest) : null;
            });
        }

        public void ProcessCallbacks()
        {
            while (true)
            {
                Action next = null;
                lock (_scheduledCallbacks)
                {
                    if (_scheduledCallbacks.Count > 0)
                        next = _scheduledCallbacks.Dequeue();
                }
                if (next != null)
                    next();
                else
                    break;
            }
        }
    }
}
