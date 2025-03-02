// SPDX-License-Identifier: BSD-2-Clause

using System;
using System.Runtime.CompilerServices;
using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.Managers;
using ClassicUO.Game.Map;
using ClassicUO.Assets;
using ClassicUO.Renderer;
using ClassicUO.Utility;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.GameObjects
{
    internal abstract class BaseGameObject : LinkedObject
    {
        protected BaseGameObject(World world) => World = world;

        public Point RealScreenPosition;

        public World World { get; }
    }

    internal abstract partial class GameObject : BaseGameObject
    {
        protected GameObject(World world) : base(world) { }

        public bool IsDestroyed { get; protected set; }
        public bool IsPositionChanged { get; protected set; }
        public TextContainer TextContainer { get; private set; }
        private AverageOverTime _averageOverTime;

        public int Distance
        {
            get
            {
                if (
                    World.Player == null /*|| IsDestroyed*/
                )
                {
                    return ushort.MaxValue;
                }

                if (ReferenceEquals(this, World.Player))
                {
                    return 0;
                }

                int x = X,
                    y = Y;

                if (this is Mobile mobile && mobile.Steps.Count != 0)
                {
                    ref Mobile.Step step = ref mobile.Steps.Back();
                    x = step.X;
                    y = step.Y;
                }

                int fx = World.RangeSize.X;
                int fy = World.RangeSize.Y;

                return Math.Max(Math.Abs(x - fx), Math.Abs(y - fy));
            }
        }

        public virtual void Update() { }

        public abstract bool CheckMouseSelection();

        // FIXME: remove it
        public sbyte FoliageIndex = -1;
        public ushort Graphic;
        public ushort Hue;
        public Vector3 Offset;
        public short PriorityZ;
        public GameObject TNext;
        public GameObject TPrevious;
        public ushort X,
            Y;
        public sbyte Z;

        public void AddDamage(int damage)
        {
            _averageOverTime ??= new AverageOverTime(TimeSpan.FromSeconds(15));

            _averageOverTime.AddValue(Time.Ticks, damage);
        }
        public double GetCurrentDPS()
        {
            if (_averageOverTime == null)
            {
                return 0;
            }
            return Math.Round(_averageOverTime.LastAveragePerSecond, 1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector2 GetScreenPosition()
        {
            return new Vector2(
                RealScreenPosition.X + Offset.X,
                RealScreenPosition.Y + (Offset.Y - Offset.Z)
            );
        }

        public void AddToTile()
        {
            AddToTile(X, Y);
        }

        public void AddToTile(int x, int y)
        {
            AddToTile(World.Map?.GetChunk(x, y), x % 8, y % 8);
        }

        public void AddToTile(Chunk chunk, int chunkX, int chunkY)
        {
            RemoveFromTile();

            if (!IsDestroyed && chunk != null)
            {
                chunk.AddGameObject(this, chunkX, chunkY);
            }
        }

        public void RemoveFromTile()
        {
            if (TPrevious != null)
            {
                TPrevious.TNext = TNext;
            }

            if (TNext != null)
            {
                TNext.TPrevious = TPrevious;
            }

            TNext = null;
            TPrevious = null;
        }

        public virtual void UpdateGraphicBySeason() { }

        public void UpdateScreenPosition()
        {
            IsPositionChanged = true;
            OnPositionChanged();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void UpdateRealScreenPosition(int offsetX, int offsetY)
        {
            RealScreenPosition.X = ((X - Y) * 22) - offsetX - 22;
            RealScreenPosition.Y = ((X + Y) * 22 - (Z << 2)) - offsetY - 22;
            IsPositionChanged = false;

            UpdateTextCoordsV();
        }

        public void SetInWorldTile(ushort x, ushort y, sbyte z)
        {
            X = x;
            Y = y;
            Z = z;
            UpdateScreenPosition();
            AddToTile(x, y);
        }

        public void AddMessage(MessageType type, string message, TextType text_type)
        {
            AddMessage(
                type,
                message,
                ProfileManager.CurrentProfile.ChatFont,
                ProfileManager.CurrentProfile.SpeechHue,
                true,
                text_type
            );
        }

        public virtual void UpdateTextCoordsV()
        {
            if (TextContainer == null)
            {
                return;
            }

            TextObject last = (TextObject)TextContainer.Items;

            while (last?.Next != null)
            {
                last = (TextObject)last.Next;
            }

            if (last == null)
            {
                return;
            }

            int offY = 0;

            Point p = RealScreenPosition;

            var bounds = Client.Game.UO.Arts.GetRealArtBounds(Graphic);

            p.Y -= bounds.Height >> 1;

            p.X += (int)Offset.X + 22;
            p.Y += (int)(Offset.Y - Offset.Z) + 44;

            p = Client.Game.Scene.Camera.WorldToScreen(p);

            for (; last != null; last = (TextObject)last.Previous)
            {
                if (last.RenderedText != null && !last.RenderedText.IsDestroyed)
                {
                    if (offY == 0 && last.Time < Time.Ticks)
                    {
                        continue;
                    }

                    last.OffsetY = offY;
                    offY += last.RenderedText.Height;

                    last.RealScreenPosition.X = p.X - (last.RenderedText.Width >> 1);
                    last.RealScreenPosition.Y = p.Y - offY;
                }
            }

            FixTextCoordinatesInScreen();
        }

        protected void FixTextCoordinatesInScreen()
        {
            if (this is Item it && SerialHelper.IsValid(it.Container))
            {
                return;
            }

            int offsetY = 0;

            int minX = 6;
            int maxX = minX + Client.Game.Scene.Camera.Bounds.Width - 6;
            int minY = 0;
            //int maxY = minY + ProfileManager.CurrentProfile.GameWindowSize.Y - 6;

            for (
                TextObject item = (TextObject)TextContainer.Items;
                item != null;
                item = (TextObject)item.Next
            )
            {
                if (
                    item.RenderedText == null
                    || item.RenderedText.IsDestroyed
                    || item.RenderedText.Texture == null
                    || item.Time < Time.Ticks
                )
                {
                    continue;
                }

                int startX = item.RealScreenPosition.X;
                int endX = startX + item.RenderedText.Width;

                if (startX < minX)
                {
                    item.RealScreenPosition.X += minX - startX;
                }

                if (endX > maxX)
                {
                    item.RealScreenPosition.X -= endX - maxX;
                }

                int startY = item.RealScreenPosition.Y;

                if (startY < minY && offsetY == 0)
                {
                    offsetY = minY - startY;
                }

                //int endY = startY + item.RenderedText.Height;

                //if (endY > maxY)
                //    UseInRender = 0xFF;
                //    //item.RealScreenPosition.Y -= endY - maxY;

                if (offsetY != 0)
                {
                    item.RealScreenPosition.Y += offsetY;
                }
            }
        }

        public void AddMessage(
            MessageType type,
            string text,
            byte font,
            ushort hue,
            bool isunicode,
            TextType text_type
        )
        {
            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            TextObject msg = World.MessageManager.CreateMessage(
                text,
                hue,
                font,
                isunicode,
                type,
                text_type
            );

            AddMessage(msg);
        }

        public void AddMessage(TextObject msg)
        {
            if (TextContainer == null)
            {
                TextContainer = new TextContainer();
            }

            msg.Owner = this;
            TextContainer.Add(msg);

            if (this is Item it && SerialHelper.IsValid(it.Container))
            {
                UpdateTextCoordsV();
            }
            else
            {
                IsPositionChanged = true;
                World.WorldTextManager.AddMessage(msg);
            }
        }

        protected virtual void OnPositionChanged() { }

        protected virtual void OnDirectionChanged() { }

        public virtual void Destroy()
        {
            if (IsDestroyed)
            {
                return;
            }

            Next = null;
            Previous = null;

            Clear();
            RemoveFromTile();
            TextContainer?.Clear();
            _averageOverTime = null;
            IsDestroyed = true;
            PriorityZ = 0;
            IsPositionChanged = false;
            Hue = 0;
            Offset = Vector3.Zero;
            RealScreenPosition = Point.Zero;
            IsFlipped = false;
            Graphic = 0;
            ObjectHandlesStatus = ObjectHandlesStatus.NONE;
            FrameInfo = Rectangle.Empty;
        }

        public static bool CanBeDrawn(World world, ushort g)
        {
            if (Client.Game == null)
                return true;

            switch (g)
            {
                case 0x0001:
                case 0x21BC:
                case 0xA1FE:
                case 0xA1FF:
                case 0xA200:
                case 0xA201:
                    //case 0x5690:
                    return false;

                case 0x9E4C:
                case 0x9E64:
                case 0x9E65:
                case 0x9E7D:
                    ref var data = ref Client.Game.UO.FileManager.TileData.StaticData[g];

                    return !data.IsBackground && !data.IsSurface;
            }

            if (g != 0x63D3)
            {
                if (g >= 0x2198 && g <= 0x21A4)
                {
                    return false;
                }

                // Easel fix.
                // In older clients the tiledata flag for this
                // item contains NoDiagonal for some reason.
                // So the next check will make the item invisible.
                if (g == 0x0F65 && Client.Game.UO.Version < ClientVersion.CV_60144)
                {
                    return true;
                }

                if (g < Client.Game.UO.FileManager.TileData.StaticData.Length)
                {
                    ref var data = ref Client.Game.UO.FileManager.TileData.StaticData[g];

                    // Hacky way to do not render "nodraw"
                    if (!string.IsNullOrEmpty(data.Name) && data.Name.StartsWith("nodraw", StringComparison.OrdinalIgnoreCase))
                        return false;

                    if (
                        !data.IsNoDiagonal
                        || data.IsAnimated
                            && world.Player != null
                            && world.Player.Race == RaceType.GARGOYLE
                    )
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
