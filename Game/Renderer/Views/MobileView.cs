using ClassicUO.AssetsLoader;
using ClassicUO.Game.WorldObjects;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.Renderer.Views
{
    public class MobileView : View
    {
        public MobileView(in Mobile mobile) : base(mobile)
        {
        }

        public new Mobile WorldObject => (Mobile) base.WorldObject;

        public override bool Draw(in SpriteBatch3D spriteBatch, in Vector3 position)
        {
            if (WorldObject.IsDisposed)
                return false;

            spriteBatch.GetZ();

            var mirror = false;
            var dir = (byte) WorldObject.GetAnimationDirection();
            Animations.GetAnimDirection(ref dir, ref mirror);
            IsFlipped = mirror;

            byte animGroup = 0;
            Hue color = 0;
            Graphic graphic = 0;
            EquipConvData? convertedItem = null;

            for (var i = 0; i < LayerOrder.USED_LAYER_COUNT; i++)
            {
                var layer = LayerOrder.UsedLayers[dir, i];

                if (layer == Layer.Mount)
                {
                    if (WorldObject.IsHuman)
                    {
                        var mount = WorldObject.Equipment[(int) Layer.Mount];
                        if (mount != null)
                        {
                            graphic = mount.GetMountAnimation();
                            var mountedHeightOffset = 0;

                            if (graphic < Animations.MAX_ANIMATIONS_DATA_INDEX_COUNT)
                                mountedHeightOffset = Animations.DataIndex[graphic].MountedHeightOffset;

                            animGroup = WorldObject.GetAnimationGroup(graphic);
                            color = mount.Hue;
                        }
                        else
                        {
                            continue;
                        }
                    }
                    else
                    {
                        continue;
                    }
                }
                else if (layer == Layer.Invalid)
                {
                    graphic = WorldObject.GetMountAnimation();
                    animGroup = WorldObject.GetAnimationGroup();
                    color = WorldObject.Hue;
                }
                else
                {
                    if (!WorldObject.IsHuman)
                        continue;

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


                var animIndex = WorldObject.AnimIndex;

                Animations.AnimID = graphic;
                Animations.AnimGroup = animGroup;
                Animations.Direction = dir;

                ref var direction = ref Animations.DataIndex[Animations.AnimID].Groups[Animations.AnimGroup]
                    .Direction[Animations.Direction];

                if (direction.FrameCount == 0 && !Animations.LoadDirectionGroup(ref direction))
                    continue;

                int fc = direction.FrameCount;
                if (fc > 0 && animIndex >= fc) animIndex = 0;

                if (animIndex < direction.FrameCount)
                {
                    var frame = direction.Frames[animIndex];

                    if (frame.Pixels == null || frame.Pixels.Length <= 0)
                        return false;


                    int drawCenterY = frame.CenterY;
                    int drawX;
                    var drawY = drawCenterY + WorldObject.Position.Z * 4 - 22 -
                                (int) (WorldObject.Offset.Y - WorldObject.Offset.Z - 3);

                    if (IsFlipped)
                        drawX = -22 + (int) WorldObject.Offset.X;
                    else
                        drawX = -22 - (int) WorldObject.Offset.X;


                    var x = drawX + frame.CenterX;
                    var y = -drawY - (frame.Heigth + frame.CenterY) + drawCenterY;

                    if (color <= 0)
                    {
                        if (direction.Address != direction.PatchedAddress)
                            color = Animations.DataIndex[Animations.AnimID].Color;

                        if (color <= 0 && convertedItem.HasValue)
                            color = convertedItem.Value.Color;
                    }

                    Texture = TextureManager.GetOrCreateAnimTexture(graphic, Animations.AnimGroup, dir, animIndex,
                        direction.Frames);
                    Bounds = new Rectangle(x, -y, frame.Width, frame.Heigth);
                    HueVector = RenderExtentions.GetHueVector(color);

                    base.Draw(spriteBatch, position);
                }
            }

            return true;
        }

        public override void Update(in double frameMS)
        {
            WorldObject.ProcessAnimation();

            base.Update(frameMS);
        }
    }
}