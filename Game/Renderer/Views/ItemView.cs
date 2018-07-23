using System;
using System.Collections.Generic;
using System.Text;
using ClassicUO.Game.Renderer;
using ClassicUO.Game.WorldObjects;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.Renderer.Views
{
    public class ItemView : View
    {
        private Graphic _originalGraphic;
        private Hue _hue;

        public ItemView(in Item item) : base(item)
        {
            if (AssetsLoader.TileData.IsWet((long)item.ItemData.Flags))
                SortZ++;

            if (!item.IsCorpse)
                AllowedToDraw = item.Graphic > 2 && item.DisplayedGraphic > 2 && !IsNoDrawable(item.Graphic);
            else
            {
                item.AnimIndex = 99;
                if ((item.Direction & Direction.Running) != 0)
                {
                    item.UsedLayer = true;
                    item.Direction &= Direction.Running;
                }
                else
                    item.UsedLayer = false;

                item.Layer = (Layer)item.Direction;

                AllowedToDraw = true;
                item.DisplayedGraphic = item.Amount;
            }
        }

        public new Item WorldObject => (Item)base.WorldObject;


        public override bool Draw(in SpriteBatch3D spriteBatch, in Vector3 position)
        {
            if (!AllowedToDraw || WorldObject.IsDisposed)
                return false;

            if (WorldObject.IsCorpse)
                return DrawCorpse(spriteBatch, position);

            if (_originalGraphic != WorldObject.DisplayedGraphic || Texture == null || Texture.IsDisposed)
            {
                _originalGraphic = WorldObject.DisplayedGraphic;
                Texture = TextureManager.GetOrCreateStaticTexture(_originalGraphic);
                Bounds = new Rectangle(Texture.Width / 2 - 22, Texture.Height - 44 + (WorldObject.Position.Z * 4), Texture.Width, Texture.Height);
            }

            if (_hue != WorldObject.Hue)
            {
                _hue = WorldObject.Hue;
                HueVector = RenderExtentions.GetHueVector(_hue, AssetsLoader.TileData.IsPartialHue((long)WorldObject.ItemData.Flags), false, false);
            }


            if (WorldObject.Amount > 1 &&  AssetsLoader.TileData.IsStackable((long)WorldObject.ItemData.Flags) && WorldObject.DisplayedGraphic == WorldObject.Graphic)
            {
                Vector3 offsetDrawPosition = new Vector3(position.X - 5, position.Y - 5, 0);
                base.Draw(spriteBatch, offsetDrawPosition);
            }

            return base.Draw(spriteBatch, position);
        }



        private bool DrawCorpse(in SpriteBatch3D spriteBatch, in Vector3 position)
        {
            spriteBatch.GetZ();

            byte dir = (byte)(((byte)WorldObject.Layer & 0x7F) & 7);
            bool mirror = false;

            AssetsLoader.Animations.GetAnimDirection(ref dir, ref mirror);

            IsFlipped = mirror;

            AssetsLoader.Animations.Direction = dir;

            byte animIndex = (byte)WorldObject.AnimIndex;
            Graphic graphic = 0;
            AssetsLoader.EquipConvData? convertedItem = null;
            Hue color = 0;

            for (int i = 0; i < LayerOrder.USED_LAYER_COUNT; i++)
            {
                Layer layer = LayerOrder.UsedLayers[dir, i];
                
                if (layer == Layer.Mount)
                    continue;
                else if (layer == Layer.Invalid)
                {
                    graphic = WorldObject.DisplayedGraphic;
                    AssetsLoader.Animations.AnimGroup = AssetsLoader.Animations.GetDieGroupIndex(WorldObject.GetMountAnimation(), WorldObject.UsedLayer);
                    color = WorldObject.Hue;
                }
                else
                {
                    Item item = WorldObject.Equipment[(int)layer];
                    if (item == null)
                        continue;

                    graphic = item.ItemData.AnimID;

                    if (AssetsLoader.Animations.EquipConversions.TryGetValue(item.Graphic, out var map))
                    {
                        if (map.TryGetValue(item.ItemData.AnimID, out var data))
                        {
                            convertedItem = data;
                            graphic = data.Graphic;
                        }
                    }
                    color = item.Hue;
                }

                AssetsLoader.Animations.AnimID = graphic;

                ref var direction = ref AssetsLoader.Animations.DataIndex[AssetsLoader.Animations.AnimID].Groups[AssetsLoader.Animations.AnimGroup].Direction[AssetsLoader.Animations.Direction];
                if (direction.FrameCount == 0 && !AssetsLoader.Animations.LoadDirectionGroup(ref direction))
                    return false;

                int fc = direction.FrameCount;
                if (fc > 0 && animIndex >= fc)
                {
                    animIndex = (byte)(fc - 1);
                }

                if (animIndex < direction.FrameCount)
                {
                    var frame = direction.Frames[animIndex];

                    if (frame.Pixels == null || frame.Pixels.Length <= 0)
                        return false;

                    int drawCenterY = frame.CenterY;
                    int drawX;
                    int drawY = drawCenterY + (WorldObject.Position.Z * 4) - 22 - 3;

                    if (IsFlipped)
                    {
                        drawX = -22;
                    }
                    else
                    {
                        drawX = -22;
                    }

                    int x = (drawX + frame.CenterX);
                    int y = -drawY - (frame.Heigth + frame.CenterY) + drawCenterY;

                    Texture = TextureManager.GetOrCreateAnimTexture(WorldObject.DisplayedGraphic, AssetsLoader.Animations.AnimGroup, dir, animIndex, direction.Frames);
                    Bounds = new Rectangle(x, -y, frame.Width, frame.Heigth);
                    HueVector = RenderExtentions.GetHueVector(color);
                    base.Draw(spriteBatch, position);
                }

            }

            return true;
        }



        protected override void MousePick(in SpriteVertex[] vertex)
        {
            base.MousePick(vertex);
        }
    }
}
