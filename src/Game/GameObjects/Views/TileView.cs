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

using ClassicUO.IO;
using ClassicUO.Renderer;

using Microsoft.Xna.Framework;

namespace ClassicUO.Game.GameObjects
{
    internal sealed partial class Land
    {
        public override bool Draw(UltimaBatcher2D batcher, int posX, int posY)
        {
            if (!AllowedToDraw || IsDestroyed)
                return false;

            Engine.DebugInfo.LandsRendered++;

            ResetHueVector();

            if (Texture == null || Texture.IsDisposed)
            {
                if (IsStretched)
                    Texture = FileManager.Textmaps.GetTexture(TileData.TexID);
                else
                {
                    Texture = FileManager.Art.GetLandTexture(Graphic);
                    Bounds.Width = 44;
                    Bounds.Height = 44;
                }
            }


            if (Engine.Profile.Current.HighlightGameObjects && SelectedObject.LastObject == this)
            {
                HueVector.X = 0x0023;
                HueVector.Y = 1;
            }
            else if (Engine.Profile.Current.NoColorObjectsOutOfRange && Distance > World.ClientViewRange)
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
                    HueVector.Y = IsStretched ? ShaderHuesTraslator.SHADER_LAND_HUED : ShaderHuesTraslator.SHADER_HUED;
                else
                    HueVector.Y = IsStretched ? ShaderHuesTraslator.SHADER_LAND : ShaderHuesTraslator.SHADER_NONE;
            }


            if (IsStretched ? Draw3DStretched(batcher, posX, posY) : base.Draw(batcher, posX, posY)) return true;

            return false;
        }


        private bool Draw3DStretched(UltimaBatcher2D batcher, int posX, int posY)
        {
            Texture.Ticks = Engine.Ticks;

            if (batcher.DrawSpriteLand(Texture, posX, posY + Z * 4, ref Rectangle, ref Normals, ref HueVector))
            {
                Select(posX, posY);

                return true;
            }

            return false;
        }

        public override void Select(int x, int y)
        {
            if (SelectedObject.Object == this)
                return;

            if (IsStretched)
            {
                if (SelectedObject.IsPointInStretchedLand(Rectangle, x, y + Z * 4))
                    SelectedObject.Object = this;
            }
            else
            {
                if (SelectedObject.IsPointInLand(Graphic, x - Bounds.X, y - Bounds.Y))
                    SelectedObject.Object = this;
            }
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

                Normals = new Vector3[4];

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
                                ref var v = ref vec[curI, curJ, k];
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
                Normals[0] = vec[i - 1, j - 1, 2] + vec[i - 1, j, 1] + vec[i, j - 1, 3] + vec[i, j, 0];
                Normals[0].Normalize();
                Normals[1] = vec[i, j - 1, 2] + vec[i, j, 1] + vec[i + 1, j - 1, 3] + vec[i + 1, j, 0];
                Normals[1].Normalize();
                Normals[2] = vec[i, j, 2] + vec[i, j + 1, 1] + vec[i + 1, j, 3] + vec[i + 1, j + 1, 0];
                Normals[2].Normalize();
                Normals[3] = vec[i - 1, j, 2] + vec[i - 1, j + 1, 1] + vec[i, j, 3] + vec[i, j + 1, 0];
                Normals[3].Normalize();
            }
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