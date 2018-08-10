using System.Collections.Generic;
using ClassicUO.AssetsLoader;
using ClassicUO.Game.GameObjects;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.Renderer.Views
{
    public class ItemView : View
    {
        private Hue _hue;
        private Graphic _originalGraphic;

        public ItemView(in Item item) : base(item)
        {
            if (TileData.IsWet((long) item.ItemData.Flags))
                SortZ++;


            if (!item.IsCorpse)
                AllowedToDraw = item.Graphic > 2 && item.DisplayedGraphic > 2 && !IsNoDrawable(item.Graphic);
            else
            {
                item.AnimIndex = 99;
                if ((item.Direction & Direction.Running) != 0)
                {
                    item.UsedLayer = true;
                    item.Direction &= (Direction) 0x7F;
                }
                else
                    item.UsedLayer = false;

                item.Layer = (Layer) item.Direction;

                AllowedToDraw = true;
                item.DisplayedGraphic = item.Amount;

                //item.Deferred = new DeferredEntity(item, item.Position.Z, item.Tile);
            }
        }

        public Item WorldObject => (Item) GameObject;


        public override bool Draw(in SpriteBatch3D spriteBatch, in Vector3 position)
        {
            if (!AllowedToDraw || WorldObject.IsDisposed)
                return false;

            if (WorldObject.IsCorpse)
            {
                return !PreDraw(position) && DrawInternal(spriteBatch, position);
            }

            if (WorldObject.Effect == null)
            {
                if (_originalGraphic != WorldObject.DisplayedGraphic || Texture == null || Texture.IsDisposed)
                {
                    _originalGraphic = WorldObject.DisplayedGraphic;
                    Texture = TextureManager.GetOrCreateStaticTexture(_originalGraphic);
                    Bounds = new Rectangle(Texture.Width / 2 - 22, Texture.Height - 44 + WorldObject.Position.Z * 4, Texture.Width, Texture.Height);
                }

                if (_hue != WorldObject.Hue)
                {
                    _hue = WorldObject.Hue;
                    HueVector = RenderExtentions.GetHueVector(_hue, TileData.IsPartialHue((long) WorldObject.ItemData.Flags), false, false);
                }


                if (WorldObject.Amount > 1 && TileData.IsStackable((long) WorldObject.ItemData.Flags) && WorldObject.DisplayedGraphic == WorldObject.Graphic)
                {
                    Vector3 offsetDrawPosition = new Vector3(position.X - 5, position.Y - 5, 0);
                    base.Draw(spriteBatch, offsetDrawPosition);
                }

                //var vv = position;
                //vv.Z = position.X + position.Y;

                //if (AssetsLoader.TileData.IsBackground((long)GameObject.ItemData.Flags) &&
                //    AssetsLoader.TileData.IsSurface((long)GameObject.ItemData.Flags))
                //    vv.Z += 0.001f * (GameObject.IsometricPosition.Z + 0.4f);
                //else if (AssetsLoader.TileData.IsBackground((long)GameObject.ItemData.Flags))
                //    vv.Z += 0.001f * (GameObject.IsometricPosition.Z + 0.2f);
                //else if (AssetsLoader.TileData.IsSurface((long)GameObject.ItemData.Flags))
                //    vv.Z += 0.001f * (GameObject.IsometricPosition.Z + 0.5f);
                //else
                //    vv.Z += 0.001f * (GameObject.IsometricPosition.Z + 0.6f);


                //CalculateRenderDepth((sbyte)vv.Z, 20, GameObject.ItemData.Height, (byte)(GameObject.Serial & 0xFF));


                base.Draw(spriteBatch, position);
            }
            else
            {
                if (!WorldObject.Effect.IsDisposed)
                    WorldObject.Effect.View.Draw(spriteBatch, position);
            }

            return true;
        }

        protected override void MessageOverHead(in SpriteBatch3D spriteBatch, in Vector3 position)
        {
            base.MessageOverHead(in spriteBatch, in position);
        }


        public override bool DrawInternal(in SpriteBatch3D spriteBatch, in Vector3 position)
        {
            spriteBatch.GetZ();

            byte dir = (byte) ((byte) WorldObject.Layer & 0x7F & 7);
            bool mirror = false;

            Animations.GetAnimDirection(ref dir, ref mirror);

            IsFlipped = mirror;

            Animations.Direction = dir;

            byte animIndex = (byte) WorldObject.AnimIndex;
            Graphic graphic = 0;
            EquipConvData? convertedItem = null;
            Hue color = 0;

            for (int i = 0; i < LayerOrder.USED_LAYER_COUNT; i++)
            {
                Layer layer = LayerOrder.UsedLayers[dir, i];

                if (layer == Layer.Mount) continue;

                if (layer == Layer.Invalid)
                {
                    graphic = WorldObject.DisplayedGraphic;
                    Animations.AnimGroup = Animations.GetDieGroupIndex(WorldObject.GetMountAnimation(), WorldObject.UsedLayer);
                    color = WorldObject.Hue;
                }
                else
                {
                    Item item = WorldObject.Equipment[(int) layer];
                    if (item == null)
                        continue;

                    graphic = item.ItemData.AnimID;

                    if (Animations.EquipConversions.TryGetValue(item.Graphic, out Dictionary<ushort, EquipConvData> map))
                    {
                        if (map.TryGetValue(item.ItemData.AnimID, out EquipConvData data))
                        {
                            convertedItem = data;
                            graphic = data.Graphic;
                        }
                    }

                    color = item.Hue;
                }

                Animations.AnimID = graphic;

                ref AnimationDirection direction = ref Animations.DataIndex[Animations.AnimID].Groups[Animations.AnimGroup].Direction[Animations.Direction];
                if (direction.FrameCount == 0 && !Animations.LoadDirectionGroup(ref direction))
                    return false;

                int fc = direction.FrameCount;

                if (fc > 0 && animIndex >= fc)
                    animIndex = (byte) (fc - 1);

                if (animIndex < direction.FrameCount)
                {
                    AnimationFrame frame = direction.Frames[animIndex];

                    if (frame.Pixels == null || frame.Pixels.Length <= 0)
                        return false;

                    int drawCenterY = frame.CenterY;
                    int drawX;
                    int drawY = drawCenterY + WorldObject.Position.Z * 4 - 22 - 3;

                    if (IsFlipped)
                        drawX = -22;
                    else
                        drawX = -22;

                    int x = drawX + frame.CenterX;
                    int y = -drawY - (frame.Heigth + frame.CenterY) + drawCenterY;

                    Texture = TextureManager.GetOrCreateAnimTexture(frame);
                    Bounds = new Rectangle(x, -y, frame.Width, frame.Heigth);
                    HueVector = RenderExtentions.GetHueVector(color);
                    base.Draw(spriteBatch, position);
                }
            }

            return true;
        }

        public override void Update(in double frameMS)
        {
            if (WorldObject.IsCorpse)
                WorldObject.ProcessAnimation();

            if (WorldObject.Effect != null)
            {
                if (WorldObject.Effect.IsDisposed)
                    WorldObject.Effect = null;
                else
                    WorldObject.Effect.UpdateAnimation(frameMS);
            }
        }

        protected override void MousePick(in SpriteVertex[] vertex)
        {
            base.MousePick(vertex);
        }
    }
}