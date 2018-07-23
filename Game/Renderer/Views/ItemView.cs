using ClassicUO.AssetsLoader;
using ClassicUO.Game.WorldObjects;
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
            {
                AllowedToDraw = item.Graphic > 2 && item.DisplayedGraphic > 2 && !IsNoDrawable(item.Graphic);
            }
            else
            {
                item.AnimIndex = 99;
                if ((item.Direction & Direction.Running) != 0)
                {
                    item.UsedLayer = true;
                    item.Direction &= Direction.Running;
                }
                else
                {
                    item.UsedLayer = false;
                }

                item.Layer = (Layer) item.Direction;

                AllowedToDraw = true;
                item.DisplayedGraphic = item.Amount;
            }
        }

        public new Item WorldObject => (Item) base.WorldObject;


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
                Bounds = new Rectangle(Texture.Width / 2 - 22, Texture.Height - 44 + WorldObject.Position.Z * 4,
                    Texture.Width, Texture.Height);
            }

            if (_hue != WorldObject.Hue)
            {
                _hue = WorldObject.Hue;
                HueVector = RenderExtentions.GetHueVector(_hue,
                    TileData.IsPartialHue((long) WorldObject.ItemData.Flags), false, false);
            }


            if (WorldObject.Amount > 1 && TileData.IsStackable((long) WorldObject.ItemData.Flags) &&
                WorldObject.DisplayedGraphic == WorldObject.Graphic)
            {
                var offsetDrawPosition = new Vector3(position.X - 5, position.Y - 5, 0);
                base.Draw(spriteBatch, offsetDrawPosition);
            }

            return base.Draw(spriteBatch, position);
        }


        private bool DrawCorpse(in SpriteBatch3D spriteBatch, in Vector3 position)
        {
            spriteBatch.GetZ();

            var dir = (byte) ((byte) WorldObject.Layer & 0x7F & 7);
            var mirror = false;

            Animations.GetAnimDirection(ref dir, ref mirror);

            IsFlipped = mirror;

            Animations.Direction = dir;

            var animIndex = (byte) WorldObject.AnimIndex;
            Graphic graphic = 0;
            EquipConvData? convertedItem = null;
            Hue color = 0;

            for (var i = 0; i < LayerOrder.USED_LAYER_COUNT; i++)
            {
                var layer = LayerOrder.UsedLayers[dir, i];

                if (layer == Layer.Mount) continue;

                if (layer == Layer.Invalid)
                {
                    graphic = WorldObject.DisplayedGraphic;
                    Animations.AnimGroup =
                        Animations.GetDieGroupIndex(WorldObject.GetMountAnimation(), WorldObject.UsedLayer);
                    color = WorldObject.Hue;
                }
                else
                {
                    var item = WorldObject.Equipment[(int) layer];
                    if (item == null)
                        continue;

                    graphic = item.ItemData.AnimID;

                    if (Animations.EquipConversions.TryGetValue(item.Graphic, out var map))
                        if (map.TryGetValue(item.ItemData.AnimID, out var data))
                        {
                            convertedItem = data;
                            graphic = data.Graphic;
                        }

                    color = item.Hue;
                }

                Animations.AnimID = graphic;

                ref var direction = ref Animations.DataIndex[Animations.AnimID].Groups[Animations.AnimGroup]
                    .Direction[Animations.Direction];
                if (direction.FrameCount == 0 && !Animations.LoadDirectionGroup(ref direction))
                    return false;

                int fc = direction.FrameCount;
                if (fc > 0 && animIndex >= fc) animIndex = (byte) (fc - 1);

                if (animIndex < direction.FrameCount)
                {
                    var frame = direction.Frames[animIndex];

                    if (frame.Pixels == null || frame.Pixels.Length <= 0)
                        return false;

                    int drawCenterY = frame.CenterY;
                    int drawX;
                    var drawY = drawCenterY + WorldObject.Position.Z * 4 - 22 - 3;

                    if (IsFlipped)
                        drawX = -22;
                    else
                        drawX = -22;

                    var x = drawX + frame.CenterX;
                    var y = -drawY - (frame.Heigth + frame.CenterY) + drawCenterY;

                    Texture = TextureManager.GetOrCreateAnimTexture(WorldObject.DisplayedGraphic, Animations.AnimGroup,
                        dir, animIndex, direction.Frames);
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