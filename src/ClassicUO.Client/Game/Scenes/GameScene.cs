// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Assets;
using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Input;
using ClassicUO.Network;
using ClassicUO.Renderer;
using ClassicUO.Resources;
using ClassicUO.Utility;
using ClassicUO.Utility.Logging;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SDL3;
using System;
using System.Collections.Generic;
using System.Net.Sockets;

namespace ClassicUO.Game.Scenes
{
    internal partial class GameScene : Scene
    {
        private static readonly Func<BlendState> _darknessBlend = new(() =>
        {
            return new BlendState
            {
                ColorSourceBlend = Blend.Zero,
                ColorDestinationBlend = Blend.SourceColor,
                ColorBlendFunction = BlendFunction.Add
            };
        });

        private static readonly Func<BlendState> _altLightsBlend = new(() =>
        {
            return new BlendState
            {
                ColorSourceBlend = Blend.DestinationColor,
                ColorDestinationBlend = Blend.One,
                ColorBlendFunction = BlendFunction.Add
            };
        });

        private const float MAX_LAYER_DEPTH = 0x8000;
        private uint _time_cleanup = Time.Ticks + 5000;
        private bool _alphaChanged;
        private long _alphaTimer;
        private bool _forceStopScene;
        private HealthLinesManager _healthLinesManager;

        private Point _lastSelectedMultiPositionInHouseCustomization;
        private int _lightCount;
        private readonly LightData[] _lights = new LightData[
            LightsLoader.MAX_LIGHTS_DATA_INDEX_COUNT
        ];
        private Item _multi;
        private Rectangle _rectangleObj = Rectangle.Empty,
            _rectanglePlayer;
        private long _timePing;

        private uint _timeToPlaceMultiInHouseCustomization;
        private readonly UseItemQueue _useItemQueue;
        private bool _useObjectHandles;
        private AnimatedStaticsManager _animatedStaticsManager;

        private readonly World _world;

        public GameScene(World world)
        {
            _world = world;
            _useItemQueue = new UseItemQueue(world);

            _lightsPass = new Passes.LightsPass(this);
            _worldPass = new Passes.WorldPass(this);
            _deathScreenPass = new Passes.DeathScreenPass(this);
            _lightsClearPass = new Passes.ClearPass("LightsClear");
        }

        public bool UpdateDrawPosition { get; set; }
        public bool DisconnectionRequested { get; set; }
        public bool UseLights =>
            _world.Profile.CurrentProfile != null
            && _world.Profile.CurrentProfile.UseCustomLightLevel
                ? _world.Light.Personal < _world.Light.Overall
                : _world.Light.RealPersonal < _world.Light.RealOverall;
        public bool UseAltLights =>
            _world.Profile.CurrentProfile != null
            && _world.Profile.CurrentProfile.UseAlternativeLights;

        public void DoubleClickDelayed(uint serial)
        {
            _useItemQueue.Add(serial);
        }

        public override void Load()
        {
            base.Load();

            _world.Context.Game.Window.AllowUserResizing = true;

            Camera.Zoom = _world.Profile.CurrentProfile.DefaultScale;
            Camera.Bounds.X = Math.Max(0, _world.Profile.CurrentProfile.GameWindowPosition.X);
            Camera.Bounds.Y = Math.Max(0, _world.Profile.CurrentProfile.GameWindowPosition.Y);
            Camera.Bounds.Width = Math.Max(0, _world.Profile.CurrentProfile.GameWindowSize.X);
            Camera.Bounds.Height = Math.Max(0, _world.Profile.CurrentProfile.GameWindowSize.Y);

            _world.Context.Game.UO.GameCursor.ItemHold.Clear();

            _world.Macros.Clear();
            _world.Macros.Load();
            _animatedStaticsManager = new AnimatedStaticsManager(_world.Context.Game.UO, _world.Profile);
            _animatedStaticsManager.Initialize();
            _world.InfoBars.Load();
            _healthLinesManager = new HealthLinesManager(_world);

            _world.CommandManager.Initialize();

            WorldViewportGump viewport = new WorldViewportGump(_world, this);
            _world.Context.UI.Add(viewport, false);

            if (!_world.Profile.CurrentProfile.TopbarGumpIsDisabled)
            {
                TopBarGump.Create(_world);
            }

            _world.Network.Disconnected += SocketOnDisconnected;
            _world.MessageManager.MessageReceived += ChatOnMessageReceived;
            _world.Context.UI.ContainerScale = _world.Profile.CurrentProfile.ContainersScale / 100f;
            Data.MovementSpeed.FastRotation = _world.Profile.CurrentProfile.FastRotation;

            SDL.SDL_SetWindowMinimumSize(_world.Context.Game.Window.Handle, _world.Context.Game.ScaleWithDpi(640), _world.Context.Game.ScaleWithDpi(480));

            if (_world.Profile.CurrentProfile.WindowBorderless)
            {
                _world.Context.Game.SetWindowBorderless(true);
            }
            else if (_world.Settings.IsWindowMaximized)
            {
                _world.Context.Game.MaximizeWindow();
            }
            else if (_world.Settings.WindowSize.HasValue)
            {
                int w = _world.Settings.WindowSize.Value.X;
                int h = _world.Settings.WindowSize.Value.Y;

                w = Math.Max(_world.Context.Game.ScaleWithDpi(640), w);
                h = Math.Max(_world.Context.Game.ScaleWithDpi(480), h);

                _world.Context.Game.SetWindowSize(w, h);
            }

            Plugin.OnConnected();
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
                case MessageType.GmChat:
                    name =
                        string.IsNullOrEmpty(e.Name)
                        || string.Equals(
                            e.Name,
                            "system",
                            StringComparison.InvariantCultureIgnoreCase
                        )
                            ? ResGeneral.System
                            : e.Name;

                    text = e.Text;

                    break;

                case MessageType.Emote:
                    name = e.Name;
                    text = $"{e.Text}";

                    if (e.Hue == 0)
                    {
                        hue = _world.Profile.CurrentProfile.EmoteHue;
                    }

                    break;

                case MessageType.Label:

                    if (e.Parent == null || !SerialHelper.IsValid(e.Parent.Serial))
                    {
                        name = string.Empty;
                    }
                    else if (string.IsNullOrEmpty(e.Name))
                    {
                        name = ResGeneral.YouSee;
                    }
                    else
                    {
                        name = e.Name;
                    }

                    text = e.Text;

                    break;

                case MessageType.Spell:
                    name = e.Name;
                    text = e.Text;

                    break;

                case MessageType.Party:
                    text = e.Text;
                    name = string.Format(ResGeneral.Party0, e.Name);
                    hue = _world.Profile.CurrentProfile.PartyMessageHue;

                    break;

                case MessageType.Alliance:
                    text = e.Text;
                    name = string.Format(ResGeneral.Alliance0, e.Name);
                    hue = _world.Profile.CurrentProfile.AllyMessageHue;

                    break;

                case MessageType.Guild:
                    text = e.Text;
                    name = string.Format(ResGeneral.Guild0, e.Name);
                    hue = _world.Profile.CurrentProfile.GuildMessageHue;

                    break;

                default:
                    text = e.Text;
                    name = e.Name;
                    hue = e.Hue;

                    Log.Warn($"Unhandled text type {e.Type}  -  text: '{e.Text}'");

                    break;
            }

            if (!string.IsNullOrEmpty(text))
            {
                _world.Journal.Add(text, hue, name, e.Parent?.Serial, e.TextType, e.IsUnicode, e.Type);
            }
        }

        public override void Unload()
        {
            if (IsDestroyed)
            {
                return;
            }

            _world.Profile.CurrentProfile.GameWindowPosition = new Point(
                Camera.Bounds.X,
                Camera.Bounds.Y
            );
            _world.Profile.CurrentProfile.GameWindowSize = new Point(
                Camera.Bounds.Width,
                Camera.Bounds.Height
            );
            _world.Profile.CurrentProfile.DefaultScale = Camera.Zoom;

            _world.Context.Game.Audio?.StopMusic();
            _world.Context.Game.Audio?.StopSounds();

            _world.Context.Game.SetWindowTitle(string.Empty);
            _world.Context.Game.UO.GameCursor.ItemHold.Clear();

            try
            {
                Plugin.OnDisconnected();
            }
            catch { }

            _world.TargetManager.Reset();

            // special case for wmap. this allow us to save settings
            _world.Context.UI.GetGump<WorldMapGump>()?.SaveSettings();

            _world.Profile.CurrentProfile?.Save(_world, _world.Profile.ProfilePath);

            _world.Macros.Save();
            _world.Macros.Clear();
            _world.InfoBars.Save();
            _world.Profile.UnLoadProfile();

            StaticFilters.CleanCaveTextures();
            StaticFilters.CleanTreeTextures();

            _world.Network.Disconnected -= SocketOnDisconnected;
            _world.Network.Disconnect();

            _world.CommandManager.UnRegisterAll();
            _world.Weather.Reset();
            _world.Context.UI.Clear();
            _world.Clear();
            _world.ChatManager.Clear();
            _world.DelayedObjectClickManager.Clear();

            _useItemQueue?.Clear();
            _world.MessageManager.MessageReceived -= ChatOnMessageReceived;

            _world.Settings.WindowSize = new Point(
                _world.Context.Game.ClientBounds.Width,
                _world.Context.Game.ClientBounds.Height
            );

            _world.Settings.IsWindowMaximized = _world.Context.Game.IsWindowMaximized();
            _world.Context.Game.SetWindowBorderless(false);

            base.Unload();
        }

        private void SocketOnDisconnected(object sender, SocketError e)
        {
            if (_world.Settings.Reconnect)
            {
                _forceStopScene = true;
            }
            else
            {
                _world.Context.UI.Add(
                    new MessageBoxGump(
                        _world,
                        200,
                        200,
                        string.Format(
                            ResGeneral.ConnectionLost0,
                            StringHelper.AddSpaceBeforeCapital(e.ToString())
                        ),
                        s =>
                        {
                            if (s)
                            {
                                _world.Context.Game.SetScene(new LoginScene(_world));
                            }
                        }
                    )
                );
            }
        }

        public void RequestQuitGame()
        {
            _world.Context.UI.Add(
                new QuestionGump(
                    _world,
                    ResGeneral.QuitPrompt,
                    s =>
                    {
                        if (s)
                        {
                            if (
                                (
                                    _world.ClientFeatures.Flags
                                    & CharacterListFlags.CLF_OWERWRITE_CONFIGURATION_BUTTON
                                ) != 0
                            )
                            {
                                DisconnectionRequested = true;
                                _world.Network.Send_LogoutNotification();
                            }
                            else
                            {
                                _world.Network.Disconnect();
                                _world.Context.Game.SetScene(new LoginScene(_world));
                            }
                        }
                    }
                )
            );
        }

        public void AddLight(GameObject obj, GameObject lightObject, int x, int y)
        {
            if (
                _lightCount >= LightsLoader.MAX_LIGHTS_DATA_INDEX_COUNT
                || !UseLights && !UseAltLights
                || obj == null
            )
            {
                return;
            }

            bool canBeAdded = true;

            int testX = obj.X + 1;
            int testY = obj.Y + 1;

            GameObject tile = _world.Map.GetTile(testX, testY);

            if (tile != null)
            {
                sbyte z5 = (sbyte)(obj.Z + 5);

                for (GameObject o = tile; o != null; o = o.TNext)
                {
                    if (
                        (!(o is Static s) || s.ItemData.IsTransparent)
                            && (!(o is Multi m) || m.ItemData.IsTransparent)
                        || !o.AllowedToDraw
                    )
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

                if (
                    graphic >= 0x3E02 && graphic <= 0x3E0B
                    || graphic >= 0x3914 && graphic <= 0x3929
                    || graphic == 0x0B1D
                )
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
                        light.ID = (byte)it.ItemData.LightIndex;

                        if (obj is Mobile mob)
                        {
                            switch (mob.Direction)
                            {
                                case Direction.Right:
                                    y += 33;
                                    x += 22;

                                    break;

                                case Direction.Left:
                                    y += 33;
                                    x -= 22;

                                    break;

                                case Direction.East:
                                    x += 22;
                                    y += 55;

                                    break;

                                case Direction.Down:
                                    y += 55;

                                    break;

                                case Direction.South:
                                    x -= 22;
                                    y += 55;

                                    break;
                            }
                        }
                    }
                    else if (obj is Mobile _)
                    {
                        light.ID = 1;
                    }
                    else
                    {
                        ref StaticTiles data = ref _world.Context.Game.UO.FileManager.TileData.StaticData[obj.Graphic];
                        light.ID = data.Layer;
                    }
                }

                light.Color = 0;
                light.IsHue = false;

                if (_world.Profile.CurrentProfile.UseColoredLights)
                {
                    if (light.ID > 200)
                    {
                        light.Color = (ushort)(light.ID - 200);
                        light.ID = 1;
                    }

                    if (LightColors.GetHue(graphic, out ushort color, out bool ishue))
                    {
                        light.Color = color;
                        light.IsHue = ishue;
                    }
                }

                if (light.ID >= LightsLoader.MAX_LIGHTS_DATA_INDEX_COUNT)
                {
                    return;
                }

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
            _renderLists.Clear();
            _visibleChunks.Clear();

            _foliageCount = 0;

            if (!_world.InGame)
            {
                return;
            }

            _alphaChanged = _alphaTimer < Time.Ticks;

            if (_alphaChanged)
            {
                _alphaTimer = Time.Ticks + Constants.ALPHA_TIME;
            }

            if (_world.Profile.CurrentProfile.UseCircleOfTransparency)
            {
                float r = _world.Profile.CurrentProfile.CircleOfTransparencyRadius;
                _cotRadiusSq = r * r;
                _cotPlayerScreenPos = _world.Player.GetScreenPosition();
                _cotGradientMode = _world.Profile.CurrentProfile.CircleOfTransparencyType == 1;
            }
            else
            {
                _cotRadiusSq = 0;
                _cotGradientMode = false;
            }

            FoliageIndex++;

            if (FoliageIndex >= 100)
            {
                FoliageIndex = 1;
            }

            GetViewPort();

            var ctrlShiftHeld = Keyboard.Ctrl && Keyboard.Shift;
            var useObjectHandles = _world.NameOverHeadManager.IsToggled || ctrlShiftHeld;
            if (useObjectHandles != _useObjectHandles)
            {
                _useObjectHandles = useObjectHandles;
                if (_useObjectHandles)
                {
                    _world.NameOverHeadManager.Open();
                    if (_world.NameOverHeadManager.IsToggled && !ctrlShiftHeld)
                    {
                        _world.NameOverHeadManager.SetMenuVisible(false);
                    }
                }
                else
                {
                    _world.NameOverHeadManager.Close();
                }
            }
            else if (_useObjectHandles && _world.NameOverHeadManager.IsToggled)
            {
                _world.NameOverHeadManager.SetMenuVisible(ctrlShiftHeld);
            }

            _rectanglePlayer.X = (int)(
                _world.Player.RealScreenPosition.X
                - _world.Player.FrameInfo.X
                + 22
                + _world.Player.Offset.X
            );
            _rectanglePlayer.Y = (int)(
                _world.Player.RealScreenPosition.Y
                - _world.Player.FrameInfo.Y
                + 22
                + (_world.Player.Offset.Y - _world.Player.Offset.Z)
            );
            _rectanglePlayer.Width = _world.Player.FrameInfo.Width;
            _rectanglePlayer.Height = _world.Player.FrameInfo.Height;

            int minX = _minTile.X;
            int minY = _minTile.Y;
            int maxX = _maxTile.X;
            int maxY = _maxTile.Y;
            Map.Map map = _world.Map;
            bool use_handles = _useObjectHandles;
            (var minChunkX, var minChunkY) = (minX >> 3, minY >> 3);
            (var maxChunkX, var maxChunkY) = (maxX >> 3, maxY >> 3);

            for (var chunkX = minChunkX; chunkX <= maxChunkX; chunkX++)
            {
                for (var chunkY = minChunkY; chunkY <= maxChunkY; chunkY++)
                {
                    var chunk = map.GetChunk2(chunkX, chunkY, true);
                    if (chunk == null || chunk.IsDestroyed)
                        continue;

                    // Build chunk mesh if dirty
                    if (chunk.Mesh.IsDirty)
                    {
                        chunk.Mesh.Build(chunk, _world, _world.Context.Game.GraphicsDevice);
                    }

                    // Reset visibility and alpha for this frame
                    chunk.Mesh.Land.ResetVisibility();
                    chunk.Mesh.Land.ResetAlpha();
                    chunk.Mesh.Statics.ResetVisibility();
                    chunk.Mesh.Statics.ResetAlpha();

                    _visibleChunks.Add(chunk);

                    for (var x = 0; x < 8; x++)
                    {
                        for (var y = 0; y < 8; y++)
                        {
                            var firstObj = chunk.GetHeadObject(x, y);
                            if (firstObj == null || firstObj.IsDestroyed)
                                continue;

                            AddTileToRenderList(
                                firstObj,
                                use_handles,
                                150,
                                chunk
                            );
                        }
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
                        CalculateAlpha(ref f.AlphaHue, Constants.FOLIAGE_ALPHA);
                    }
                    else if (f.Z < _maxZ)
                    {
                        CalculateAlpha(ref f.AlphaHue, 0xFF);
                    }
                }
            }

            UpdateTextServerEntities(_world.Mobiles.Values, true);
            UpdateTextServerEntities(_world.Items.Values, false);

            UpdateDrawPosition = false;
        }

        private void UpdateTextServerEntities<T>(IEnumerable<T> entities, bool force)
            where T : Entity
        {
            foreach (T e in entities)
            {
                if (
                    e.TextContainer != null
                    && !e.TextContainer.IsEmpty
                    && (force || e.Graphic == 0x2006)
                )
                {
                    e.UpdateRealScreenPosition(_offset.X, _offset.Y);
                }
            }
        }

        public override void Update()
        {
            Profile currentProfile = _world.Profile.CurrentProfile;

            SelectedObject.TranslatedMousePositionByViewport = Camera.MouseToWorldPosition();

            base.Update();

            if (_time_cleanup < Time.Ticks)
            {
                _world.Map?.ClearUnusedBlocks();
                _time_cleanup = Time.Ticks + 500;
            }

            PacketHandlers.SendMegaClilocRequests(_world);

            if (_forceStopScene)
            {
                LoginScene loginScene = new LoginScene(_world);
                _world.Context.Game.SetScene(loginScene);
                loginScene.Reconnect = true;

                return;
            }

            if (!_world.InGame)
            {
                return;
            }

            if (Time.Ticks > _timePing)
            {
                _world.Network.Statistics.SendPing();
                _timePing = (long)Time.Ticks + 1000;
            }

            _world.Update();
            _animatedStaticsManager.Process();
            _world.BoatMovingManager.Update();
            _world.Player.Pathfinder.ProcessAutoWalk();
            _world.DelayedObjectClickManager.Update();

            if (!MoveCharacterByMouseInput() && !currentProfile.DisableArrowBtn)
            {
                Direction dir = DirectionHelper.DirectionFromKeyboardArrows(
                    _flags[0],
                    _flags[2],
                    _flags[1],
                    _flags[3]
                );

                if (_world.InGame && !_world.Player.Pathfinder.AutoWalking && dir != Direction.NONE)
                {
                    _world.Player.Walk(dir, currentProfile.AlwaysRun);
                }
            }

            if (
                _followingMode && SerialHelper.IsMobile(_followingTarget) && !_world.Player.Pathfinder.AutoWalking
            )
            {
                Mobile follow = _world.Mobiles.Get(_followingTarget);

                if (follow != null)
                {
                    int distance = follow.Distance;

                    if (distance > _world.ClientViewRange)
                    {
                        StopFollowing();
                    }
                    else if (distance > 3)
                    {
                        _world.Player.Pathfinder.WalkTo(follow.X, follow.Y, follow.Z, 1);
                    }
                }
                else
                {
                    StopFollowing();
                }
            }

            _world.Macros.Update();

            if (
                (currentProfile.CorpseOpenOptions == 1 || currentProfile.CorpseOpenOptions == 3)
                    && _world.TargetManager.IsTargeting
                || (currentProfile.CorpseOpenOptions == 2 || currentProfile.CorpseOpenOptions == 3)
                    && _world.Player.IsHidden
            )
            {
                _useItemQueue.ClearCorpses();
            }

            _useItemQueue.Update();

            if (!_world.Context.UI.IsMouseOverWorld)
            {
                SelectedObject.Object = null;
            }

            if (
                _world.TargetManager.IsTargeting
                && _world.TargetManager.TargetingState == CursorTarget.MultiPlacement
                && _world.CustomHouseManager == null
                && _world.TargetManager.MultiTargetInfo != null
            )
            {
                if (_multi == null)
                {
                    _multi = Item.Create(_world, 0);
                    _multi.Graphic = _world.TargetManager.MultiTargetInfo.Model;
                    _multi.Hue = _world.TargetManager.MultiTargetInfo.Hue;
                    _multi.IsMulti = true;
                }

                if (SelectedObject.Object is GameObject gobj)
                {
                    ushort x,
                        y;
                    sbyte z;

                    int cellX = gobj.X % 8;
                    int cellY = gobj.Y % 8;

                    GameObject o = _world.Map.GetChunk(gobj.X, gobj.Y)?.Tiles[cellX, cellY];

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

                    _world.Map.GetMapZ(x, y, out sbyte groundZ, out sbyte _);

                    if (gobj is Static st && st.ItemData.IsWet)
                    {
                        groundZ = gobj.Z;
                    }

                    x = (ushort)(x - _world.TargetManager.MultiTargetInfo.XOff);
                    y = (ushort)(y - _world.TargetManager.MultiTargetInfo.YOff);
                    z = (sbyte)(groundZ - _world.TargetManager.MultiTargetInfo.ZOff);

                    _multi.SetInWorldTile(x, y, z);
                    _multi.CheckGraphicChange();

                    _world.HouseManager.TryGetHouse(_multi.Serial, out House house);

                    foreach (Multi s in house.Components)
                    {
                        s.IsHousePreview = true;
                        s.SetInWorldTile(
                            (ushort)(_multi.X + s.MultiOffsetX),
                            (ushort)(_multi.Y + s.MultiOffsetY),
                            (sbyte)(_multi.Z + s.MultiOffsetZ)
                        );
                    }
                }
            }
            else if (_multi != null)
            {
                _world.HouseManager.RemoveMultiTargetHouse();
                _multi.Destroy();
                _multi = null;
            }

            if (_isMouseLeftDown && !_world.Context.Game.UO.GameCursor.ItemHold.Enabled)
            {
                if (
                    _world.CustomHouseManager != null
                    && _world.CustomHouseManager.SelectedGraphic != 0
                    && !_world.CustomHouseManager.SeekTile
                    && !_world.CustomHouseManager.Erasing
                    && Time.Ticks > _timeToPlaceMultiInHouseCustomization
                )
                {
                    if (
                        SelectedObject.Object is GameObject obj
                        && (
                            obj.X != _lastSelectedMultiPositionInHouseCustomization.X
                            || obj.Y != _lastSelectedMultiPositionInHouseCustomization.Y
                        )
                    )
                    {
                        _world.CustomHouseManager.OnTargetWorld(obj);
                        _timeToPlaceMultiInHouseCustomization = Time.Ticks + 50;
                        _lastSelectedMultiPositionInHouseCustomization.X = obj.X;
                        _lastSelectedMultiPositionInHouseCustomization.Y = obj.Y;
                    }
                }
                else if (Time.Ticks - _holdMouse2secOverItemTime >= 1000)
                {
                    if (SelectedObject.Object is Item it && GameActions.PickUp(_world, it.Serial, 0, 0))
                    {
                        _isMouseLeftDown = false;
                        _holdMouse2secOverItemTime = 0;
                    }
                }
            }
        }

        private readonly Passes.LightsPass _lightsPass;
        private readonly Passes.WorldPass _worldPass;
        private readonly Passes.DeathScreenPass _deathScreenPass;
        private readonly Passes.ClearPass _lightsClearPass;

        public override void BuildRenderPasses(RenderPipeline pipeline, RenderTargets renderTargets)
        {
            if (!_world.InGame)
            {
                return;
            }

            InitializeRenderTargets(renderTargets);

            // Death screen — skip world/lights, just show death text
            if (ShouldShowDeathScreen())
            {
                pipeline.Add(_deathScreenPass);
                return;
            }

            // Lights pass
            bool drawLights = (UseLights || UseAltLights)
                && !(_world.Player.IsDead && _world.Profile.CurrentProfile.EnableBlackWhiteEffect);

            if (drawLights)
            {
                if (!UseAltLights)
                {
                    float lightColor = _world.Light.IsometricLevel;
                    if (_world.Profile.CurrentProfile.UseDarkNights)
                    {
                        lightColor -= 0.04f;
                    }
                    _lightsPass.ClearColor = new Color(new Vector4(lightColor, lightColor, lightColor, 1));
                }
                else
                {
                    _lightsPass.ClearColor = Color.Black;
                }

                pipeline.Add(_lightsPass);
            }
            else
            {
                _lightsClearPass.Target = renderTargets.LightRenderTarget;
                _lightsClearPass.ClearColor = Color.Transparent;
                pipeline.Add(_lightsClearPass);
            }

            // World pass
            pipeline.Add(_worldPass);
        }

        internal bool ShouldShowDeathScreen()
        {
            return _world.Profile.CurrentProfile != null
                && _world.Profile.CurrentProfile.EnableDeathScreen
                && _world.InGame
                && _world.Player.IsDead
                && _world.Player.DeathScreenTimer > Time.Ticks;
        }

        internal void DrawDeathScreen(UltimaBatcher2D batcher)
        {
            _youAreDeadText.Draw(
                batcher,
                Camera.Bounds.X + (Camera.Bounds.Width / 2 - _youAreDeadText.Width / 2),
                Camera.Bounds.Bottom / 2,
                0f
            );
        }

        internal void DrawLightSprites(UltimaBatcher2D batcher)
        {
            Vector3 hue = Vector3.Zero;
            hue.Z = 1f;

            for (int i = 0; i < _lightCount; i++)
            {
                ref LightData l = ref _lights[i];
                ref readonly var lightInfo = ref _world.Context.Game.UO.Lights.GetLight(l.ID);

                if (lightInfo.Texture == null)
                {
                    continue;
                }

                hue.X = l.Color;
                hue.Y =
                    hue.X > 1.0f
                        ? l.IsHue
                            ? ShaderHueTranslator.SHADER_HUED
                            : ShaderHueTranslator.SHADER_LIGHTS
                        : ShaderHueTranslator.SHADER_NONE;

                batcher.Draw(
                    lightInfo.Texture,
                    new Vector2(
                        l.DrawX - lightInfo.UV.Width * 0.5f,
                        l.DrawY - lightInfo.UV.Height * 0.5f
                    ),
                    lightInfo.UV,
                    hue,
                    0f
                );
            }

            _lightCount = 0;
        }

        internal void DrawWorldContent(UltimaBatcher2D batcher)
        {
            SelectedObject.Object = null;
            Profiler.EnterContext(Profiler.ProfilerContext.RENDER_FRAME_WORLD_PREPARE);
            FillGameObjectList();
            Profiler.ExitContext(Profiler.ProfilerContext.RENDER_FRAME_WORLD_PREPARE);
            Profiler.EnterContext(Profiler.ProfilerContext.RENDER_FRAME_WORLD);

            batcher.SetBrightlight(_world.Profile.CurrentProfile.TerrainShadowsLevel * 0.1f);

            if (_world.Profile.CurrentProfile.UseCircleOfTransparency
                && _world.Profile.CurrentProfile.CircleOfTransparencyType != 1)
            {
                batcher.SetCircleOfTransparencyRadius(
                    (float)_world.Profile.CurrentProfile.CircleOfTransparencyRadius / Camera.Zoom
                );
            }
            else
            {
                batcher.SetCircleOfTransparencyRadius(0f);
            }

            RenderedObjectsCount = _renderLists.DrawRenderLists(
                batcher,
                _maxGroundZ,
                _visibleChunks,
                _offset.X,
                _offset.Y
            );

            if (
                _multi != null
                && _world.TargetManager.IsTargeting
                && _world.TargetManager.TargetingState == CursorTarget.MultiPlacement
            )
            {
                _multi.Draw(
                    batcher,
                    _multi.RealScreenPosition.X,
                    _multi.RealScreenPosition.Y,
                    _multi.CalculateDepthZ()
                );
            }

            _world.Weather.Draw(batcher, 0, 0, MAX_LAYER_DEPTH - 1);
            DrawSelection(batcher, MAX_LAYER_DEPTH);

            batcher.SetCircleOfTransparencyRadius(0f);

            Profiler.ExitContext(Profiler.ProfilerContext.RENDER_FRAME_WORLD);
        }

        private void InitializeRenderTargets(RenderTargets renderTargets)
        {
            renderTargets.SetLightsConfiguration(
                UseAltLights ? _altLightsBlend : (UseLights ? _darknessBlend : () => null),
                () =>
                {
                    Vector3 v = Vector3.Zero;
                    v.Z = UseAltLights ? 0.5f : 1f;
                    return v;
                }
            );
        }

        public override void DrawUI(UltimaBatcher2D batcher)
        {
            _healthLinesManager.Draw(batcher, 0f);

            if (!_world.Context.UI.IsMouseOverWorld)
            {
                SelectedObject.Object = null;
            }

            _world.WorldTextManager.ProcessWorldText(true);
            _world.WorldTextManager.Draw(batcher, Camera.Bounds.X, Camera.Bounds.Y, 0);
        }

        public void DrawSelection(UltimaBatcher2D batcher, float layerDepth)
        {
            if (_isSelectionActive)
            {
                Vector3 selectionHue = new()
                {
                    Z = 0.7f
                };

                Point upperLeftInWorld = Camera.ScreenToWorld(new Point(
                    Math.Min(_selectionStart.X, Mouse.Position.X) - Camera.Bounds.X,
                    Math.Min(_selectionStart.Y, Mouse.Position.Y) - Camera.Bounds.Y
                ));

                Point lowerRightInWorld = Camera.ScreenToWorld(new Point(
                    Math.Max(_selectionStart.X, Mouse.Position.X) - Camera.Bounds.X,
                    Math.Max(_selectionStart.Y, Mouse.Position.Y) - Camera.Bounds.Y
                ));

                Rectangle selectionRect = new Rectangle(
                    upperLeftInWorld.X,
                    upperLeftInWorld.Y,
                    lowerRightInWorld.X - upperLeftInWorld.X,
                    lowerRightInWorld.Y - upperLeftInWorld.Y
                );

                batcher.Draw(
                    SolidColorTextureCache.GetTexture(Color.Black),
                    selectionRect,
                    selectionHue,
                    layerDepth
                );

                selectionHue.Z = 0.3f;

                batcher.DrawRectangle(
                    SolidColorTextureCache.GetTexture(Color.DeepSkyBlue),
                    selectionRect.X,
                    selectionRect.Y,
                    selectionRect.Width,
                    selectionRect.Height,
                    selectionHue,
                    layerDepth
                );
            }
        }

        private RenderedText _youAreDeadTextInstance;
        private RenderedText _youAreDeadText => _youAreDeadTextInstance ??= RenderedText.Create(
            _world.Context.Game.UO,
            ResGeneral.YouAreDead,
            0xFFFF,
            3,
            false,
            FontStyle.BlackBorder,
            TEXT_ALIGN_TYPE.TS_LEFT
        );

        private void StopFollowing()
        {
            if (_followingMode)
            {
                _followingMode = false;
                _followingTarget = 0;
                _world.Player.Pathfinder.StopAutoWalk();

                _world.MessageManager.HandleMessage(
                    _world.Player,
                    ResGeneral.StoppedFollowing,
                    string.Empty,
                    0,
                    MessageType.Regular,
                    3,
                    TextType.CLIENT
                );
            }
        }

        private struct LightData
        {
            public byte ID;
            public ushort Color;
            public bool IsHue;
            public int DrawX,
                DrawY;
        }
    }
}
