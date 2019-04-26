#region license
//  Copyright (C) 2019 ClassicUO Development Community on Github
//
//	This project is an alternative client for the game Ultima Online.
//	The goal of this is to develop a lightweight client considering 
//	new technologies.  
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

using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Map;
using ClassicUO.Input;
using ClassicUO.IO;
using ClassicUO.IO.Resources;
using ClassicUO.Renderer;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO.Game.GameObjects
{
    internal partial class Land
    {
        private readonly Vector3[] _normals = new Vector3[4];
        private readonly SpriteVertex[] _vertex = new SpriteVertex[4]
        {
            new SpriteVertex(new Vector3(), new Vector3(), new Vector3(0, 0, 0)), new SpriteVertex(new Vector3(), new Vector3(), new Vector3(1, 0, 0)), new SpriteVertex(new Vector3(), new Vector3(), new Vector3(0, 1, 0)), new SpriteVertex(new Vector3(), new Vector3(), new Vector3(1, 1, 0))
        };
        private Vector3 _storedHue;
        private Vector3 _vertex0_yOffset, _vertex1_yOffset, _vertex2_yOffset, _vertex3_yOffset;


        public override bool Draw(Batcher2D batcher, Vector3 position, MouseOverList objectList)
        {
            if (!AllowedToDraw || IsDestroyed)
                return false;

            Engine.DebugInfo.LandsRendered++;
            if (Texture == null || Texture.IsDisposed)
            {
                if (IsStretched)
                    Texture = FileManager.Textmaps.GetTexture(TileData.TexID);
                else
                {
                    Texture = FileManager.Art.GetLandTexture(Graphic);
                    Bounds = new Rectangle(0, 0, 44, 44);
                }
            }

            if (Engine.Profile.Current.NoColorObjectsOutOfRange && Distance > World.ViewRange)
            {
                HueVector.X = Constants.OUT_RANGE_COLOR;
                HueVector.Y = 1;
            }
            else if (World.Player.IsDead && Engine.Profile.Current.EnableBlackWhiteEffect)
            {
                HueVector.X = Constants.DEAD_RANGE_COLOR;
                HueVector.Y = 1;
            }
            else
            {
                HueVector.X = Hue;

                if (Hue != 0)
                {
                    HueVector.Y = IsStretched ? ShaderHuesTraslator.SHADER_LAND_HUED : ShaderHuesTraslator.SHADER_HUED;
                }
                else
                {
                    HueVector.Y = IsStretched ? ShaderHuesTraslator.SHADER_LAND : ShaderHuesTraslator.SHADER_NONE;
                }
            }

            //if (IsStretched)
            //{

            //    Vector3.Add(ref position, ref _vertex0_yOffset, out _vertex[0].Position);
            //    Vector3.Add(ref position, ref _vertex1_yOffset, out _vertex[1].Position);
            //    Vector3.Add(ref position, ref _vertex2_yOffset, out _vertex[2].Position);
            //    Vector3.Add(ref position, ref _vertex3_yOffset, out _vertex[3].Position);
            //    int z = Z * 4;

            //    _vertex[0].Position.Y += z;
            //    _vertex[1].Position.Y += z;
            //    _vertex[2].Position.Y += z;
            //    _vertex[3].Position.Y += z;

            //    SpriteRenderer.DrawLand(this, Hue, (int)position.X, (int)position.Y, _vertex);
            //    return true;

            //}
            //else
            //{
            //    SpriteRenderer.DrawLandArt(Graphic, Hue, (int) position.X, (int) position.Y);
            //    return true;
            //}

            return IsStretched ? Draw3DStretched(batcher, position, objectList) : base.Draw(batcher, position, objectList);
        }


        private bool Draw3DStretched(Batcher2D batcher, Vector3 position, MouseOverList objectList)
        {
            Texture.Ticks = Engine.Ticks;

            int z = Z * 4;

            if (Engine.Profile.Current.HighlightGameObjects)
            {
                if (IsSelected)
                {
                    if (_storedHue == Vector3.Zero)
                        _storedHue = HueVector;
                    HueVector = ShaderHuesTraslator.SelectedHue;
                }
                else if (_storedHue != Vector3.Zero)
                {
                    HueVector = _storedHue;
                    _storedHue = Vector3.Zero;
                }
            }

            Vector3.Add(ref position, ref _vertex0_yOffset, out _vertex[0].Position);
            Vector3.Add(ref position, ref _vertex1_yOffset, out _vertex[1].Position);
            Vector3.Add(ref position, ref _vertex2_yOffset, out _vertex[2].Position);
            Vector3.Add(ref position, ref _vertex3_yOffset, out _vertex[3].Position);

            _vertex[0].Position.Y += z;
            _vertex[1].Position.Y += z;
            _vertex[2].Position.Y += z;
            _vertex[3].Position.Y += z;

            //HueVector.Z = 1f - (AlphaHue / 255f);

            if (HueVector != _vertex[0].Hue)
            {
                _vertex[0].Hue = HueVector;
                _vertex[1].Hue = HueVector;
                _vertex[2].Hue = HueVector;
                _vertex[3].Hue = HueVector;
            }     

            if (!batcher.DrawSprite(Texture, _vertex))
                return false;

            if (objectList.IsMouseInObjectIsometric(_vertex))
                objectList.Add(this, _vertex[0].Position);

            return true;
        }

        protected override void MousePick(MouseOverList list, SpriteVertex[] vertex, bool istransparent)
        {
            int x = list.MousePosition.X - (int)vertex[0].Position.X;
            int y = list.MousePosition.Y - (int)vertex[0].Position.Y;

            if (Texture.Contains(x, y))
                list.Add(this, vertex[0].Position);
        }

        private void UpdateStreched(int x, int y, sbyte z)
        {
            Map.Map map = World.Map;

            if (IsStretched || FileManager.Textmaps.GetTexture(TileData.TexID) == null || !TestStretched(x, y, z, true))
            {
                IsStretched = false;
                MinZ = z;
            }
            else
            {
                IsStretched = true;
                UpdateZ(map.GetTileZ(x, y + 1), map.GetTileZ(x + 1, y + 1), map.GetTileZ(x + 1, y), z);

                Vector3[,,] vec = new Vector3[3, 3, 4];
                int i;
                int j;

                for (i = -1; i < 2; i++)
                {
                    int curX = x + i;
                    int curI = i + 1;

                    for (j = -1; j < 2; j++)
                    {
                        int curY = y + j;
                        int curJ = j + 1;
                        sbyte currentZ = map.GetTileZ(curX, curY);
                        sbyte leftZ = map.GetTileZ(curX, curY + 1);
                        sbyte rightZ = map.GetTileZ(curX + 1, curY);
                        sbyte bottomZ = map.GetTileZ(curX + 1, curY + 1);

                        if (currentZ == leftZ && currentZ == rightZ && currentZ == bottomZ)
                        {
                            for (int k = 0; k < 4; k++)
                            {
                                ref var v = ref vec[ curI, curJ, k];
                                v.X = 0;
                                v.Y = 0;
                                v.Z = 1;
                            }
                        }
                        else
                        {
                            ref var v0 = ref vec[curI, curJ, 0];
                            v0.X = -22;
                            v0.Y = 22;
                            v0.Z = (currentZ - rightZ) * 4;
                            Merge(ref vec[curI, curJ, 0], -22.0f, -22.0f, (leftZ - currentZ) * 4);
                            vec[curI, curJ, 0].Normalize();


                            ref var v1 = ref vec[curI, curJ, 1];
                            v1.X = 22;
                            v1.Y = 22;
                            v1.Z = (rightZ - bottomZ) * 4;
                            Merge(ref vec[curI, curJ, 1], -22.0f, 22.0f, (currentZ - rightZ) * 4);
                            vec[curI, curJ, 1].Normalize();

                            ref var v2 = ref vec[curI, curJ, 2];
                            v2.X = 22;
                            v2.Y = -22;
                            v2.Z = (bottomZ - leftZ) * 4;
                            Merge(ref vec[curI, curJ, 2], 22.0f, 22.0f, (rightZ - bottomZ) * 4);
                            vec[curI, curJ, 2].Normalize();

                            ref var v3 = ref vec[curI, curJ, 3];
                            v3.X = -22;
                            v3.Y = -22;
                            v3.Z = (leftZ - currentZ) * 4;
                            Merge(ref vec[curI, curJ, 3], 22.0f, -22.0f, (bottomZ - leftZ) * 4);
                            vec[curI, curJ, 3].Normalize();
                        }
                    }
                }

                i = 1;
                j = 1;
                _normals[0] = vec[i - 1, j - 1, 2] + vec[i - 1, j, 1] + vec[i, j - 1, 3] + vec[i, j, 0];
                _normals[0].Normalize();
                _normals[1] = vec[i, j - 1, 2] + vec[i, j, 1] + vec[i + 1, j - 1, 3] + vec[i + 1, j, 0];
                _normals[1].Normalize();
                _normals[2] = vec[i, j, 2] + vec[i, j + 1, 1] + vec[i + 1, j, 3] + vec[i + 1, j + 1, 0];
                _normals[2].Normalize();
                _normals[3] = vec[i - 1, j, 2] + vec[i - 1, j + 1, 1] + vec[i, j, 3] + vec[i, j + 1, 0];
                _normals[3].Normalize();
                _vertex[0].Normal = _normals[0];
                _vertex[1].Normal = _normals[1];
                _vertex[3].Normal = _normals[2];
                _vertex[2].Normal = _normals[3];


                _vertex0_yOffset.X = 22;
                _vertex0_yOffset.Y = -Rectangle.Left;
                _vertex0_yOffset.Z = 0;

                _vertex1_yOffset.X = 44;
                _vertex1_yOffset.Y = 22 - Rectangle.Bottom;
                _vertex1_yOffset.Z = 0;

                _vertex2_yOffset.X = 0;
                _vertex2_yOffset.Y = 22 - Rectangle.Top;
                _vertex2_yOffset.Z = 0;

                _vertex3_yOffset.X = 22;
                _vertex3_yOffset.Y = 44 - Rectangle.Right;
                _vertex3_yOffset.Z = 0;
            }

            Vector3 hue = Vector3.Zero;
            ShaderHuesTraslator.GetHueVector(ref hue, Hue);

            if (_vertex[0].Hue != hue)
                _vertex[0].Hue = _vertex[1].Hue = _vertex[2].Hue = _vertex[3].Hue = hue;
        }

        private static bool TestStretched(int x, int y, sbyte z, bool recurse)
        {
            bool result = false;

            for (int i = -1; i < 2 && !result; i++)
            {
                for (int j = -1; j < 2 && !result; j++)
                {
                    if (recurse)
                        result = TestStretched(x + i, y + j, z, false);
                    else
                    {
                        sbyte testZ = World.Map.GetTileZ(x + i, y + j);
                        result = testZ != z && testZ != -125;
                    }
                }
            }

            return result;
        }

        private static void Merge(ref Vector3 v, float x, float y, float z)
        {
            float newX = v.Y * z - v.Z * y;
            float newY = v.Z * x - v.X * z;
            float newZ = v.X * y - v.Y * x;
            v.X = newX;
            v.Y = newY;
            v.Z = newZ;
        }
    }
}