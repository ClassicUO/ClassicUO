// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.UI.Gumps;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.Containers
{
    /// <summary>
    /// Picks where a container gump should open. Walks the cached
    /// gump-position table first, then falls back to the active profile's
    /// override mode (near object, top-right, fixed point) or the
    /// auto-tiling grid. Reads <see cref="World"/> for item / mobile lookups
    /// and the active scene camera; mutates only its own <c>X</c>/<c>Y</c>.
    /// </summary>
    internal sealed class ContainerPositionCalculator : IContainerPositionCalculator
    {
        private readonly World _world;

        public ContainerPositionCalculator(World world)
        {
            _world = world;
        }

        public int DefaultX { get; } = 40;
        public int DefaultY { get; } = 40;

        public int X { get; private set; } = 40;
        public int Y { get; private set; } = 40;

        public void CalculateContainerPosition(uint serial, ushort g)
        {
            if (UIManager.GetGumpCachePosition(serial, out Point location))
            {
                X = location.X;
                Y = location.Y;
            }
            else
            {
                ref readonly var gumpInfo = ref Client.Game.UO.Gumps.GetGump(g);

                if (gumpInfo.Texture != null)
                {
                    float scale = UIManager.ContainerScale;

                    int width = (int)(gumpInfo.UV.Width * scale);
                    int height = (int)(gumpInfo.UV.Height * scale);

                    if (ProfileManager.CurrentProfile.OverrideContainerLocation)
                    {
                        switch (ProfileManager.CurrentProfile.OverrideContainerLocationSetting)
                        {
                            case 0:
                                SetPositionNearGameObject(g, serial, width, height);

                                break;

                            case 1:
                                X = Client.Game.ClientBounds.Width - width;
                                Y = 0;

                                break;

                            case 2:
                            case 3:
                                X =
                                    ProfileManager
                                        .CurrentProfile
                                        .OverrideContainerLocationPosition
                                        .X - (width >> 1);
                                Y =
                                    ProfileManager
                                        .CurrentProfile
                                        .OverrideContainerLocationPosition
                                        .Y - (height >> 1);

                                break;
                        }

                        if (X + width > Client.Game.ClientBounds.Width)
                        {
                            X -= width;
                        }

                        if (Y + height > Client.Game.ClientBounds.Height)
                        {
                            Y -= height;
                        }
                    }
                    else
                    {
                        int passed = 0;

                        for (int i = 0; i < 4 && passed == 0; i++)
                        {
                            if (
                                X + width + Constants.CONTAINER_RECT_STEP
                                > Client.Game.ClientBounds.Width
                            )
                            {
                                X = Constants.CONTAINER_RECT_DEFAULT_POSITION;

                                if (
                                    Y + height + Constants.CONTAINER_RECT_LINESTEP
                                    > Client.Game.ClientBounds.Height
                                )
                                {
                                    Y = Constants.CONTAINER_RECT_DEFAULT_POSITION;
                                }
                                else
                                {
                                    Y += Constants.CONTAINER_RECT_LINESTEP;
                                }
                            }
                            else if (
                                Y + height + Constants.CONTAINER_RECT_STEP
                                > Client.Game.ClientBounds.Height
                            )
                            {
                                if (
                                    X + width + Constants.CONTAINER_RECT_LINESTEP
                                    > Client.Game.ClientBounds.Width
                                )
                                {
                                    X = Constants.CONTAINER_RECT_DEFAULT_POSITION;
                                }
                                else
                                {
                                    X += Constants.CONTAINER_RECT_LINESTEP;
                                }

                                Y = Constants.CONTAINER_RECT_DEFAULT_POSITION;
                            }
                            else
                            {
                                passed = i + 1;
                            }
                        }

                        if (passed == 0)
                        {
                            X = DefaultX;
                            Y = DefaultY;
                        }
                        else if (passed == 1)
                        {
                            X += Constants.CONTAINER_RECT_STEP;
                            Y += Constants.CONTAINER_RECT_STEP;
                        }
                    }
                }
            }
        }

        private void SetPositionNearGameObject(ushort g, uint serial, int width, int height)
        {
            Item item = _world.Items.Get(serial);

            if (item == null)
            {
                return;
            }

            Item bank = _world.Player.FindItemByLayer(Layer.Bank);
            var camera = Client.Game.Scene.Camera;

            if (bank != null && serial == bank)
            {
                // open bank near player
                X = _world.Player.RealScreenPosition.X + camera.Bounds.X + 40;
                Y = _world.Player.RealScreenPosition.Y + camera.Bounds.Y - (height >> 1);
            }
            else if (item.OnGround)
            {
                // item is in world
                X = item.RealScreenPosition.X + camera.Bounds.X + 40;
                Y = item.RealScreenPosition.Y + camera.Bounds.Y - (height >> 1);
            }
            else if (SerialHelper.IsMobile(item.Container))
            {
                // pack animal, snooped player, npc vendor
                Mobile mobile = _world.Mobiles.Get(item.Container);

                if (mobile != null)
                {
                    X = mobile.RealScreenPosition.X + camera.Bounds.X + 40;
                    Y = mobile.RealScreenPosition.Y + camera.Bounds.Y - (height >> 1);
                }
            }
            else
            {
                // in a container, open near the container
                ContainerGump parentContainer = UIManager.GetGump<ContainerGump>(item.Container);

                if (parentContainer != null)
                {
                    X = parentContainer.X + (width >> 1);
                    Y = parentContainer.Y;
                }
            }
        }
    }
}
