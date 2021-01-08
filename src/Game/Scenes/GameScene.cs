#region license

// Copyright (C) 2020 ClassicUO Development Community on Github
// 
// This project is an alternative client for the game Ultima Online.
// The goal of this is to develop a lightweight client considering
// new technologies.
// 
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
// 
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <https://www.gnu.org/licenses/>.

#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using ClassicUO.Configuration;
// ## BEGIN - END ## //
using ClassicUO.Game.InteropServices.Runtime.UOClassicCombat;
using ClassicUO.Game.InteropServices.Runtime.External;
// ## BEGIN - END ## //
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Input;
using ClassicUO.IO.Resources;
using ClassicUO.Network;
using ClassicUO.Renderer;
using ClassicUO.Resources;
using ClassicUO.Utility;
using ClassicUO.Utility.Logging;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SDL2;

namespace ClassicUO.Game.Scenes
{
    internal partial class GameScene : Scene
    {
        private static readonly Lazy<BlendState> _darknessBlend = new Lazy<BlendState>
        (
            () =>
            {
                BlendState state = new BlendState();
                state.ColorSourceBlend = Blend.Zero;
                state.ColorDestinationBlend = Blend.SourceColor;
                state.ColorBlendFunction = BlendFunction.Add;

                return state;
            }
        );

        private static readonly Lazy<BlendState> _altLightsBlend = new Lazy<BlendState>
        (
            () =>
            {
                BlendState state = new BlendState();
                state.ColorSourceBlend = Blend.DestinationColor;
                state.ColorDestinationBlend = Blend.One;
                state.ColorBlendFunction = BlendFunction.Add;

                return state;
            }
        );


        private static XBREffect _xbr;
        private bool _alphaChanged;
        private long _alphaTimer;
        private bool _forceStopScene;
        private HealthLinesManager _healthLinesManager;
        // ## BEGIN - END ## // 
        private UOClassicCombatLines _UOClassicCombatLines;
        private TextureManager _textureManager;
        // ## BEGIN - END ## //
        private bool _isListReady;
        private Point _lastSelectedMultiPositionInHouseCustomization;
        private int _lightCount;
        private readonly LightData[] _lights = new LightData[Constants.MAX_LIGHTS_DATA_INDEX_COUNT];
        private Item _multi;
        private Rectangle _rectangleObj = Rectangle.Empty, _rectanglePlayer;
        private Vector3 _selectionLines = Vector3.Zero;
        private long _timePing;

        private uint _timeToPlaceMultiInHouseCustomization;
        private readonly bool _use_render_target = false;
        private UseItemQueue _useItemQueue = new UseItemQueue();
        private bool _useObjectHandles;
        private Vector4 _vectorClear = new Vector4(Vector3.Zero, 1);
        private RenderTarget2D _world_render_target, _lightRenderTarget;


        public GameScene() : base((int) SceneType.Game, true, true, true)
        {
        }

        public bool UpdateDrawPosition { get; set; }

        public HotkeysManager Hotkeys { get; private set; }

        public MacroManager Macros { get; private set; }

        public InfoBarManager InfoBars { get; private set; }

        public Weather Weather { get; private set; }

        public bool UseLights => ProfileManager.CurrentProfile != null && ProfileManager.CurrentProfile.UseCustomLightLevel ?
            World.Light.Personal < World.Light.Overall :
            World.Light.RealPersonal < World.Light.RealOverall;

        public bool UseAltLights => ProfileManager.CurrentProfile != null && ProfileManager.CurrentProfile.UseAlternativeLights;

        public void DoubleClickDelayed(uint serial)
        {
            _useItemQueue.Add(serial);
        }

        public override void Load()
        {
            base.Load();

            ItemHold.Clear();
            Hotkeys = new HotkeysManager();
            Macros = new MacroManager();

            // #########################################################
            // [FILE_FIX]
            // TODO: this code is a workaround to port old macros to the new xml system.
            if (ProfileManager.CurrentProfile.Macros != null)
            {
                for (int i = 0; i < ProfileManager.CurrentProfile.Macros.Length; i++)
                {
                    Macros.PushToBack(ProfileManager.CurrentProfile.Macros[i]);
                }

                Macros.Save();

                ProfileManager.CurrentProfile.Macros = null;
            }
            // #########################################################

            Macros.Load();

            InfoBars = new InfoBarManager();
            InfoBars.Load();
            _healthLinesManager = new HealthLinesManager();
            // ## BEGIN - END ## // 
            _UOClassicCombatLines = new UOClassicCombatLines();
            _textureManager = new TextureManager();
            // ## BEGIN - END ## //
            Weather = new Weather();

            WorldViewportGump viewport = new WorldViewportGump(this);

            UIManager.Add(viewport);

            if (!ProfileManager.CurrentProfile.TopbarGumpIsDisabled)
            {
                TopBarGump.Create();
            }


            CommandManager.Initialize();
            NetClient.Socket.Disconnected += SocketOnDisconnected;

            MessageManager.MessageReceived += ChatOnMessageReceived;

            UIManager.ContainerScale = ProfileManager.CurrentProfile.ContainersScale / 100f;

            SDL.SDL_SetWindowMinimumSize(Client.Game.Window.Handle, 640, 480);

            if (ProfileManager.CurrentProfile.WindowBorderless)
            {
                Client.Game.SetWindowBorderless(true);
            }
            else if (Settings.GlobalSettings.IsWindowMaximized)
            {
                Client.Game.MaximizeWindow();
            }
            else if (Settings.GlobalSettings.WindowSize.HasValue)
            {
                int w = Settings.GlobalSettings.WindowSize.Value.X;
                int h = Settings.GlobalSettings.WindowSize.Value.Y;

                w = Math.Max(640, w);
                h = Math.Max(480, h);

                Client.Game.SetWindowSize(w, h);
            }

            // ## BEGIN - END ## //
            if (ProfileManager.CurrentProfile.UOClassicCombatSelf)
            {
                UIManager.Add(new UOClassicCombatSelf
                {
                    X = ProfileManager.CurrentProfile.UOClassicCombatSelfLocation.X,
                    Y = ProfileManager.CurrentProfile.UOClassicCombatSelfLocation.Y
                });

            }
            if (ProfileManager.CurrentProfile.UOClassicCombatBuffbar)
            {
                UIManager.Add(new UOClassicCombatBuffbar
                {
                    X = ProfileManager.CurrentProfile.UOClassicCombatBuffbarLocation.X,
                    Y = ProfileManager.CurrentProfile.UOClassicCombatBuffbarLocation.Y
                });

            }
            if (ProfileManager.CurrentProfile.UOClassicCombatLines)
            {
                UIManager.Add(new UOClassicCombatLines
                {
                    X = ProfileManager.CurrentProfile.UOClassicCombatLinesLocation.X,
                    Y = ProfileManager.CurrentProfile.UOClassicCombatLinesLocation.Y
                });

            }
            if (ProfileManager.CurrentProfile.UOClassicCombatAL)
            {
                UIManager.Add(new UOClassicCombatAL
                {
                    X = ProfileManager.CurrentProfile.UOClassicCombatALLocation.X,
                    Y = ProfileManager.CurrentProfile.UOClassicCombatALLocation.Y
                });

            }
            if (ProfileManager.CurrentProfile.BandageGump)
            {
                UIManager.Add(new BandageGump());
            }
            // ## BEGIN - END ## //

            CircleOfTransparency.Create(ProfileManager.CurrentProfile.CircleOfTransparencyRadius);
            Plugin.OnConnected();

            // ## BEGIN - END ## //
            ModulesManager.Load();
            // ## BEGIN - END ## //

            Camera.SetZoomValues
            (
                new[]
                {
                    .5f, .6f, .7f, .8f, 0.9f, 1f, 1.1f, 1.2f, 1.3f, 1.5f, 1.6f, 1.7f, 1.8f, 1.9f, 2.0f, 2.1f, 2.2f,
                    2.3f, 2.4f, 2.5f
                }
            );

            Camera.Zoom = ProfileManager.CurrentProfile.DefaultScale;
        }

        private void ChatOnMessageReceived(object sender, MessageEventArgs e)
        {
            if (e.Type == MessageType.Command)
            {
                return;
            }

            string name;
            string text;

            ushort hue = e.Hue;

            switch (e.Type)
            {
                case MessageType.Regular:
                case MessageType.Limit3Spell:

                    if (e.Parent == null || !SerialHelper.IsValid(e.Parent.Serial))
                    {
                        name = ResGeneral.System;
                    }
                    else
                    {
                        name = e.Name;
                    }

                    text = e.Text;

                    break;

                case MessageType.System:
                    name = string.IsNullOrEmpty(e.Name) || e.Name.ToLowerInvariant() == "system" ?
                        ResGeneral.System :
                        e.Name;

                    text = e.Text;

                    break;

                case MessageType.Emote:
                    name = e.Name;
                    text = $"{e.Text}";

                    if (e.Hue == 0)
                    {
                        hue = ProfileManager.CurrentProfile.EmoteHue;
                    }

                    break;

                case MessageType.Label:
                    name = ResGeneral.YouSee;
                    text = e.Text;

                    break;

                case MessageType.Spell:
                    name = e.Name;
                    text = e.Text;

                    break;

                case MessageType.Party:
                    text = e.Text;
                    name = string.Format(ResGeneral.Party0, e.Name);
                    hue = ProfileManager.CurrentProfile.PartyMessageHue;

                    break;

                case MessageType.Alliance:
                    text = e.Text;
                    name = string.Format(ResGeneral.Alliance0, e.Name);
                    hue = ProfileManager.CurrentProfile.AllyMessageHue;

                    break;

                case MessageType.Guild:
                    text = e.Text;
                    name = string.Format(ResGeneral.Guild0, e.Name);
                    hue = ProfileManager.CurrentProfile.GuildMessageHue;

                    break;

                default:
                    text = e.Text;
                    name = e.Name;
                    hue = e.Hue;

                    Log.Warn($"Unhandled text type {e.Type}  -  text: '{e.Text}'");

                    break;
            }

            // ## BEGIN - END ## //
            if (ProfileManager.CurrentProfile.UOClassicCombatSelf || ProfileManager.CurrentProfile.UOClassicCombatBuffbar)
            {
                World.UOClassicCombatCliloc.OnMessage(text, hue, name, e.IsUnicode);
            }

            World.Player?.BandageTimer.OnMessage(text, hue, name, e.IsUnicode);
            // ## BEGIN - END ## //

            if (!string.IsNullOrEmpty(text))
            {
                World.Journal.Add(text, hue, name, e.TextType, e.IsUnicode);
            }
        }

        public override void Unload()
        {
            Client.Game.SetWindowTitle(string.Empty);

            ItemHold.Clear();

            try
            {
                Plugin.OnDisconnected();
            }
            catch
            {
            }
            // ## BEGIN - END ## // 
            try
            {
                ModulesManager.Unload();
            }
            catch
            {
            }
            // ## BEGIN - END ## //
            TargetManager.Reset();

            // special case for wmap. this allow us to save settings
            UIManager.GetGump<WorldMapGump>()?.SaveSettings();

            ProfileManager.CurrentProfile?.Save(ProfileManager.ProfilePath);

            Macros.Save();
            InfoBars.Save();
            ProfileManager.UnLoadProfile();

            StaticFilters.CleanCaveTextures();
            StaticFilters.CleanTreeTextures();

            NetClient.Socket.Disconnected -= SocketOnDisconnected;
            NetClient.Socket.Disconnect();
            _lightRenderTarget?.Dispose();
            _world_render_target?.Dispose();

            CommandManager.UnRegisterAll();
            Weather.Reset();
            UIManager.Clear();
            World.Clear();
            ChatManager.Clear();
            DelayedObjectClickManager.Clear();

            _useItemQueue?.Clear();
            _useItemQueue = null;
            Hotkeys = null;
            Macros = null;
            MessageManager.MessageReceived -= ChatOnMessageReceived;


            Settings.GlobalSettings.WindowSize = new Point
                (Client.Game.Window.ClientBounds.Width, Client.Game.Window.ClientBounds.Height);

            Settings.GlobalSettings.IsWindowMaximized = Client.Game.IsWindowMaximized();
            Client.Game.SetWindowBorderless(false);

            base.Unload();
        }

        private void SocketOnDisconnected(object sender, SocketError e)
        {
            if (Settings.GlobalSettings.Reconnect)
            {
                _forceStopScene = true;
            }
            else
            {
                UIManager.Add
                (
                    new MessageBoxGump
                    (
                        200, 200,
                        string.Format(ResGeneral.ConnectionLost0, StringHelper.AddSpaceBeforeCapital(e.ToString())),
                        s =>
                        {
                            if (s)
                            {
                                Client.Game.SetScene(new LoginScene());
                            }
                        }
                    )
                );
            }
        }

        public void RequestQuitGame()
        {
            UIManager.Add
            (
                new QuestionGump
                (
                    ResGeneral.QuitPrompt, s =>
                    {
                        if (s)
                        {
                            NetClient.Socket.Disconnect();
                            Client.Game.SetScene(new LoginScene());
                        }
                    }
                )
            );
        }

        public void AddLight(GameObject obj, GameObject lightObject, int x, int y)
        {
            if (_lightCount >= Constants.MAX_LIGHTS_DATA_INDEX_COUNT || !UseLights && !UseAltLights || obj == null)
            {
                return;
            }

            bool canBeAdded = true;

            int testX = obj.X + 1;
            int testY = obj.Y + 1;

            GameObject tile = World.Map.GetTile(testX, testY);

            if (tile != null)
            {
                sbyte z5 = (sbyte) (obj.Z + 5);

                for (GameObject o = tile; o != null; o = o.TNext)
                {
                    if ((!(o is Static s) || s.ItemData.IsTransparent) &&
                        (!(o is Multi m) || m.ItemData.IsTransparent) || !o.AllowedToDraw)
                    {
                        continue;
                    }

                    if (o.Z < _maxZ && o.Z >= z5)
                    {
                        canBeAdded = false;

                        break;
                    }
                }
            }


            if (canBeAdded)
            {
                ref LightData light = ref _lights[_lightCount];

                ushort graphic = lightObject.Graphic;

                if (graphic >= 0x3E02 && graphic <= 0x3E0B || graphic >= 0x3914 && graphic <= 0x3929 ||
                    graphic == 0x0B1D)
                {
                    light.ID = 2;
                }
                else
                {
                    if (obj == lightObject && obj is Item item)
                    {
                        light.ID = item.LightID;
                    }
                    else if (lightObject is Item it)
                    {
                        light.ID = (byte) it.ItemData.LightIndex;
                    }
                    else if (obj is Mobile _)
                    {
                        light.ID = 1;
                    }
                    else
                    {
                        ref StaticTiles data = ref TileDataLoader.Instance.StaticData[obj.Graphic];
                        light.ID = data.Layer;
                    }
                }

                if (light.ID >= Constants.MAX_LIGHTS_DATA_INDEX_COUNT)
                {
                    return;
                }

                light.Color = ProfileManager.CurrentProfile.UseColoredLights ? LightColors.GetHue(graphic) : (ushort) 0;

                if (light.Color != 0)
                {
                    light.Color++;
                }

                light.DrawX = x;
                light.DrawY = y;
                _lightCount++;
            }
        }

        private void FillGameObjectList()
        {
            _renderListCount = 0;
            _foliageCount = 0;

            if (!World.InGame)
            {
                return;
            }

            _isListReady = false;
            _alphaChanged = _alphaTimer < Time.Ticks;

            if (_alphaChanged)
            {
                _alphaTimer = Time.Ticks + Constants.ALPHA_TIME;
            }

            FoliageIndex++;

            if (FoliageIndex >= 100)
            {
                FoliageIndex = 1;
            }

            GetViewPort();

            _objectHandlesCount = 0;
            _useObjectHandles = NameOverHeadManager.IsToggled || Keyboard.Ctrl && Keyboard.Shift;

            if (_useObjectHandles)
            {
                NameOverHeadManager.Open();
            }
            else
            {
                NameOverHeadManager.Close();
            }

            _rectanglePlayer.X = (int) (World.Player.RealScreenPosition.X - World.Player.FrameInfo.X + 22 +
                                        World.Player.Offset.X);

            _rectanglePlayer.Y = (int) (World.Player.RealScreenPosition.Y - World.Player.FrameInfo.Y + 22 +
                                        (World.Player.Offset.Y - World.Player.Offset.Z));

            _rectanglePlayer.Width = World.Player.FrameInfo.Width;
            _rectanglePlayer.Height = World.Player.FrameInfo.Height;


            int minX = _minTile.X;
            int minY = _minTile.Y;
            int maxX = _maxTile.X;
            int maxY = _maxTile.Y;
            Map.Map map = World.Map;
            bool use_handles = _useObjectHandles;

            for (int i = 0; i < 2; ++i)
            {
                int minValue = minY;
                int maxValue = maxY;

                if (i != 0)
                {
                    minValue = minX;
                    maxValue = maxX;
                }

                for (int lead = minValue; lead < maxValue; ++lead)
                {
                    int x = minX;
                    int y = lead;

                    if (i != 0)
                    {
                        x = lead;
                        y = maxY;
                    }

                    while (x >= minX && x <= maxX && y >= minY && y <= maxY)
                    {
                        AddTileToRenderList(map.GetTile(x, y), x, y, use_handles, 150 /*, null*/);

                        ++x;
                        --y;
                    }
                }
            }

            if (_alphaChanged)
            {
                for (int i = 0; i < _foliageCount; i++)
                {
                    GameObject f = _foliages[i];

                    if (f.FoliageIndex == FoliageIndex)
                    {
                        f.ProcessAlpha(Constants.FOLIAGE_ALPHA);
                    }
                    else
                    {
                        f.ProcessAlpha(0xFF);
                    }
                }
            }


            UpdateTextServerEntities(World.Mobiles, true);
            UpdateTextServerEntities(World.Items, false);

            _renderIndex++;

            if (_renderIndex >= 100)
            {
                _renderIndex = 1;
            }

            UpdateDrawPosition = false;
            _isListReady = true;
        }

        private void UpdateTextServerEntities<T>(IEnumerable<T> entities, bool force) where T : Entity
        {
            foreach (T e in entities)
            {
                if (e.UseInRender != _renderIndex && e.TextContainer != null && !e.TextContainer.IsEmpty &&
                    (force || e.Graphic == 0x2006))
                {
                    e.UpdateRealScreenPosition(_offset.X, _offset.Y);
                    e.UseInRender = (byte) _renderIndex;
                }
            }
        }

        public override void Update(double totalTime, double frameTime)
        {
            Profile currentProfile = ProfileManager.CurrentProfile;
            Camera.SetGameWindowBounds
            (
                currentProfile.GameWindowPosition.X + 5, currentProfile.GameWindowPosition.Y + 5,
                currentProfile.GameWindowSize.X, currentProfile.GameWindowSize.Y
            );

            SelectedObject.TranslatedMousePositionByViewport = Camera.MouseToWorldPosition();

            base.Update(totalTime, frameTime);

            PacketHandlers.SendMegaClilocRequests();

            if (_forceStopScene)
            {
                LoginScene loginScene = new LoginScene();
                Client.Game.SetScene(loginScene);
                loginScene.Reconnect = true;

                return;
            }

            if (!World.InGame)
            {
                return;
            }

            _healthLinesManager.Update();
            World.Update(totalTime, frameTime);
            AnimatedStaticsManager.Process();
            BoatMovingManager.Update();
            Pathfinder.ProcessAutoWalk();
            DelayedObjectClickManager.Update();

            if (!MoveCharacterByMouseInput() && !currentProfile.DisableArrowBtn)
            {
                Direction dir = DirectionHelper.DirectionFromKeyboardArrows(_flags[0], _flags[2], _flags[1], _flags[3]);

                if (World.InGame && !Pathfinder.AutoWalking && dir != Direction.NONE)
                {
                    World.Player.Walk(dir, currentProfile.AlwaysRun);
                }
            }

            if (_followingMode && SerialHelper.IsMobile(_followingTarget) && !Pathfinder.AutoWalking)
            {
                Mobile follow = World.Mobiles.Get(_followingTarget);

                if (follow != null)
                {
                    int distance = follow.Distance;

                    if (distance > World.ClientViewRange)
                    {
                        StopFollowing();
                    }
                    else if (distance > 3)
                    {
                        Pathfinder.WalkTo(follow.X, follow.Y, follow.Z, 1);
                    }
                }
                else
                {
                    StopFollowing();
                }
            }


            if (totalTime > _timePing)
            {
                NetClient.Socket.Statistics.SendPing();
                _timePing = (long) totalTime + 1000;
            }

            Macros.Update();

            if ((currentProfile.CorpseOpenOptions == 1 || currentProfile.CorpseOpenOptions == 3) &&
                TargetManager.IsTargeting ||
                (currentProfile.CorpseOpenOptions == 2 || currentProfile.CorpseOpenOptions == 3) &&
                World.Player.IsHidden)
            {
                _useItemQueue.ClearCorpses();
            }

            _useItemQueue.Update(totalTime, frameTime);

            if (!UIManager.IsMouseOverWorld)
            {
                SelectedObject.Object = SelectedObject.LastObject = null;
            }


            if (TargetManager.IsTargeting && TargetManager.TargetingState == CursorTarget.MultiPlacement &&
                World.CustomHouseManager == null && TargetManager.MultiTargetInfo != null)
            {
                if (_multi == null)
                {
                    _multi = Item.Create(0);
                    _multi.Graphic = TargetManager.MultiTargetInfo.Model;
                    _multi.Hue = TargetManager.MultiTargetInfo.Hue;
                    _multi.IsMulti = true;
                }

                if (SelectedObject.Object is GameObject gobj)
                {
                    ushort x, y;
                    sbyte z;

                    int cellX = gobj.X % 8;
                    int cellY = gobj.Y % 8;

                    GameObject o = World.Map.GetChunk(gobj.X, gobj.Y)?.Tiles[cellX, cellY];

                    if (o != null)
                    {
                        x = o.X;
                        y = o.Y;
                        z = o.Z;
                    }
                    else
                    {
                        x = gobj.X;
                        y = gobj.Y;
                        z = gobj.Z;
                    }

                    World.Map.GetMapZ(x, y, out sbyte groundZ, out sbyte _);

                    if (gobj is Static st && st.ItemData.IsWet)
                    {
                        groundZ = gobj.Z;
                    }

                    x = (ushort) (x - TargetManager.MultiTargetInfo.XOff);
                    y = (ushort) (y - TargetManager.MultiTargetInfo.YOff);
                    z = (sbyte) (groundZ - TargetManager.MultiTargetInfo.ZOff);

                    _multi.X = x;
                    _multi.Y = y;
                    _multi.Z = z;
                    _multi.UpdateScreenPosition();
                    _multi.CheckGraphicChange();
                    _multi.AddToTile();
                    World.HouseManager.TryGetHouse(_multi.Serial, out House house);

                    foreach (Multi s in house.Components)
                    {
                        s.IsHousePreview = true;
                        s.X = (ushort) (_multi.X + s.MultiOffsetX);
                        s.Y = (ushort) (_multi.Y + s.MultiOffsetY);
                        s.Z = (sbyte) (_multi.Z + s.MultiOffsetZ);
                        s.UpdateScreenPosition();
                        s.AddToTile();
                    }
                }
            }
            else if (_multi != null)
            {
                World.HouseManager.RemoveMultiTargetHouse();
                _multi.Destroy();
                _multi = null;
            }


            if (_isMouseLeftDown && !ItemHold.Enabled)
            {
                if (World.CustomHouseManager != null && World.CustomHouseManager.SelectedGraphic != 0 &&
                    !World.CustomHouseManager.SeekTile && !World.CustomHouseManager.Erasing &&
                    Time.Ticks > _timeToPlaceMultiInHouseCustomization)
                {
                    if (SelectedObject.LastObject is GameObject obj &&
                        (obj.X != _lastSelectedMultiPositionInHouseCustomization.X ||
                         obj.Y != _lastSelectedMultiPositionInHouseCustomization.Y))
                    {
                        World.CustomHouseManager.OnTargetWorld(obj);
                        _timeToPlaceMultiInHouseCustomization = Time.Ticks + 50;
                        _lastSelectedMultiPositionInHouseCustomization.X = obj.X;
                        _lastSelectedMultiPositionInHouseCustomization.Y = obj.Y;
                    }
                }
                else if (Time.Ticks - _holdMouse2secOverItemTime >= 1000)
                {
                    if (SelectedObject.LastObject is Item it && GameActions.PickUp(it.Serial, 0, 0))
                    {
                        _isMouseLeftDown = false;
                        _holdMouse2secOverItemTime = 0;
                    }
                }
            }
        }

        public override void FixedUpdate(double totalTime, double frameTime)
        {
            FillGameObjectList();
        }


        public override bool Draw(UltimaBatcher2D batcher)
        {
            if (!World.InGame || !_isListReady)
            {
                return false;
            }


            int posX = ProfileManager.CurrentProfile.GameWindowPosition.X + 5;
            int posY = ProfileManager.CurrentProfile.GameWindowPosition.Y + 5;
            int width = ProfileManager.CurrentProfile.GameWindowSize.X;
            int height = ProfileManager.CurrentProfile.GameWindowSize.Y;

            if (CheckDeathScreen(batcher, posX, posY, width, height))
            {
                return true;
            }

            
            Viewport r_viewport = batcher.GraphicsDevice.Viewport;
            Viewport camera_viewport = Camera.GetViewport();

            Matrix matrix = _use_render_target ? Matrix.Identity : Camera.ViewTransformMatrix;


            bool can_draw_lights = false;

            if (!_use_render_target)
            {
                can_draw_lights = PrepareLightsRendering(batcher, ref matrix);
                batcher.GraphicsDevice.Viewport = camera_viewport;
            }

            DrawWorld(batcher, ref matrix, _use_render_target);

            if (_use_render_target)
            {
                can_draw_lights = PrepareLightsRendering(batcher, ref matrix);
                batcher.GraphicsDevice.Viewport = camera_viewport;
            }

            // draw world rt
            Vector3 hue = Vector3.Zero;


            if (_use_render_target)
            {
                //switch (ProfileManager.CurrentProfile.FilterType)
                //{
                //    default:
                //    case 0:
                //        batcher.SetSampler(SamplerState.PointClamp);
                //        break;
                //    case 1:
                //        batcher.SetSampler(SamplerState.AnisotropicClamp);
                //        break;
                //    case 2:
                //        batcher.SetSampler(SamplerState.LinearClamp);
                //        break;
                //}

                if (_xbr == null)
                {
                    _xbr = new XBREffect(batcher.GraphicsDevice);
                }

                _xbr.SetSize(width, height);


                //Point p = Point.Zero;

                //p = Camera.ScreenToWorld(p);
                //int minPixelsX = p.X;
                //int minPixelsY = p.Y;

                //p.X = Camera.Bounds.Width;
                //p.Y = Camera.Bounds.Height;
                //p = Camera.ScreenToWorld(p);
                //int maxPixelsX = p.X;
                //int maxPixelsY = p.Y;

                batcher.Begin(null, Camera.ViewTransformMatrix);
                batcher.Draw2D(_world_render_target, 0, 0, width, height, ref hue);
                batcher.End();

                //batcher.SetSampler(null);
            }

            // draw lights
            if (can_draw_lights)
            {
                batcher.Begin();

                if (UseAltLights)
                {
                    hue.Z = .5f;
                    batcher.SetBlendState(_altLightsBlend.Value);
                }
                else
                {
                    batcher.SetBlendState(_darknessBlend.Value);
                }

                batcher.Draw2D(_lightRenderTarget, 0, 0, width, height, ref hue);
                batcher.SetBlendState(null);
                batcher.End();

                hue.Z = 0f;
            }


            batcher.Begin();
            DrawOverheads(batcher, posX, posY);
            DrawSelection(batcher);
            batcher.End();


            batcher.GraphicsDevice.Viewport = r_viewport;

            return base.Draw(batcher);
        }

        private void DrawWorld(UltimaBatcher2D batcher, ref Matrix matrix, bool use_render_target)
        {
            SelectedObject.Object = null;

            if (use_render_target)
            {
                batcher.GraphicsDevice.SetRenderTarget(_world_render_target);
                batcher.GraphicsDevice.Clear(ClearOptions.Target, Color.Black, 0f, 0);
            }
            else
            {
                switch (ProfileManager.CurrentProfile.FilterType)
                {
                    default:
                    case 0:
                        batcher.SetSampler(SamplerState.PointClamp);

                        break;

                    case 1:
                        batcher.SetSampler(SamplerState.AnisotropicClamp);

                        break;

                    case 2:
                        batcher.SetSampler(SamplerState.LinearClamp);

                        break;
                }
            }


            batcher.Begin(null, matrix);


            bool usecircle = ProfileManager.CurrentProfile.UseCircleOfTransparency;


            if (usecircle)
            {
                int fx = (int) (World.Player.RealScreenPosition.X + World.Player.Offset.X);
                int fy = (int) (World.Player.RealScreenPosition.Y + (World.Player.Offset.Y - World.Player.Offset.Z));

                fx += 22;
                //fy -= 22;

                CircleOfTransparency.Draw(batcher, fx, fy);
            }

            RenderedObjectsCount = 0;

            int z = World.Player.Z + 5;

            for (int i = 0; i < _renderListCount; ++i)
            {
                GameObject obj = _renderList[i];

                if (obj.Z <= _maxGroundZ)
                {
                    GameObject.DrawTransparent = usecircle && obj.TransparentTest(z);

                    if (obj.Draw(batcher, obj.RealScreenPosition.X, obj.RealScreenPosition.Y))
                    {
                        ++RenderedObjectsCount;
                    }
                }
            }

            if (_multi != null && TargetManager.IsTargeting &&
                TargetManager.TargetingState == CursorTarget.MultiPlacement)
            {
                _multi.Draw(batcher, _multi.RealScreenPosition.X, _multi.RealScreenPosition.Y);
            }

            // draw weather
            Weather.Draw(batcher, 0, 0);
            batcher.End();
            batcher.SetSampler(null);

            if (use_render_target)
            {
                batcher.GraphicsDevice.SetRenderTarget(null);
            }
        }

        private bool PrepareLightsRendering(UltimaBatcher2D batcher, ref Matrix matrix)
        {
            if (!UseLights && !UseAltLights ||
                World.Player.IsDead && ProfileManager.CurrentProfile.EnableBlackWhiteEffect || _lightRenderTarget == null)
            {
                return false;
            }

            batcher.GraphicsDevice.SetRenderTarget(_lightRenderTarget);
            batcher.GraphicsDevice.Clear(ClearOptions.Target, Color.Black, 0f, 0);

            if (!UseAltLights)
            {
                float lightColor = World.Light.IsometricLevel;

                if (ProfileManager.CurrentProfile.UseDarkNights)
                {
                    lightColor -= 0.04f;
                }

                _vectorClear.X = _vectorClear.Y = _vectorClear.Z = lightColor;

                batcher.GraphicsDevice.Clear(ClearOptions.Target, _vectorClear, 0f, 0);
            }

            batcher.Begin(null, matrix);
            batcher.SetBlendState(BlendState.Additive);

            Vector3 hue = Vector3.Zero;
            hue.Y = ShaderHueTranslator.SHADER_LIGHTS;
            hue.Z = 0;

            for (int i = 0; i < _lightCount; i++)
            {
                ref LightData l = ref _lights[i];

                UOTexture texture = LightsLoader.Instance.GetTexture(l.ID);

                if (texture == null)
                {
                    continue;
                }

                hue.X = l.Color;

                batcher.DrawSprite
                    (texture, l.DrawX - (texture.Width >> 1), l.DrawY - (texture.Height >> 1), false, ref hue);
            }

            _lightCount = 0;

            batcher.SetBlendState(null);
            batcher.End();

            batcher.GraphicsDevice.SetRenderTarget(null);

            return true;
        }

        public void DrawOverheads(UltimaBatcher2D batcher, int x, int y)
        {
            // ## BEGIN - END ## //
            _UOClassicCombatLines.Draw(batcher);
            _textureManager.Draw(batcher);
            // ## BEGIN - END ## //

            _healthLinesManager.Draw(batcher);

            int renderIndex = _renderIndex - 1;

            if (renderIndex < 1)
            {
                renderIndex = 99;
            }

            if (!UIManager.IsMouseOverWorld)
            {
                SelectedObject.Object = null;
            }

            World.WorldTextManager.ProcessWorldText(true);
            World.WorldTextManager.Draw(batcher, x, y, renderIndex);

            SelectedObject.LastObject = SelectedObject.Object;
        }

        public void DrawSelection(UltimaBatcher2D batcher)
        {
            if (_isSelectionActive)
            {
                _selectionLines.Z = 0.3F;

                batcher.Draw2D
                (
                    SolidColorTextureCache.GetTexture(Color.Black), _selectionStart.X - Camera.Bounds.X,
                    _selectionStart.Y - Camera.Bounds.Y, Mouse.Position.X - _selectionStart.X,
                    Mouse.Position.Y - _selectionStart.Y, ref _selectionLines
                );

                _selectionLines.Z = 0.7f;

                batcher.DrawRectangle
                (
                    SolidColorTextureCache.GetTexture(Color.DeepSkyBlue), _selectionStart.X - Camera.Bounds.X,
                    _selectionStart.Y - Camera.Bounds.Y, Mouse.Position.X - _selectionStart.X,
                    Mouse.Position.Y - _selectionStart.Y, ref _selectionLines
                );
            }
        }

        private static readonly RenderedText _youAreDeadText = RenderedText.Create(ResGeneral.YouAreDead, 0xFFFF, 3, false, FontStyle.BlackBorder, TEXT_ALIGN_TYPE.TS_LEFT);
       
        private bool CheckDeathScreen(UltimaBatcher2D batcher, int x, int y, int width, int height)
        {
            if (ProfileManager.CurrentProfile != null && ProfileManager.CurrentProfile.EnableDeathScreen)
            {
                if (World.InGame)
                {
                    if (World.Player.IsDead && World.Player.DeathScreenTimer > Time.Ticks)
                    {
                        batcher.Begin();
                        _youAreDeadText.Draw
                        (
                            batcher,
                            x + (width / 2 - _youAreDeadText.Width / 2),
                            y + height / 2
                        );
                        batcher.End();

                        return true;
                    }
                }
            }

            return false;
        }

        private void StopFollowing()
        {
            if (_followingMode)
            {
                _followingMode = false;
                _followingTarget = 0;
                Pathfinder.StopAutoWalk();

                MessageManager.HandleMessage
                (
                    World.Player, ResGeneral.StoppedFollowing, string.Empty, 0, MessageType.Regular, 3, TextType.CLIENT
                );
            }
        }

        private struct LightData
        {
            public byte ID;
            public ushort Color;
            public int DrawX, DrawY;
        }
    }
}