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

using System.Runtime.CompilerServices;
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


            return IsStretched ? Draw3DStretched(batcher, posX, posY) : base.Draw(batcher, posX, posY);
        }


        private bool Draw3DStretched(UltimaBatcher2D batcher, int posX, int posY)
        {
            Texture.Ticks = Engine.Ticks;

            if (batcher.DrawSpriteLand(Texture, posX, posY + (Z << 2), ref Rectangle, ref Normals, ref HueVector))
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
                if (SelectedObject.IsPointInStretchedLand(ref Rectangle, x, y + (Z << 2)))
                    SelectedObject.Object = this;
            }
            else
            {
                if (SelectedObject.IsPointInLand(Texture, x - Bounds.X, y - Bounds.Y))
                    SelectedObject.Object = this;
            }
        }

        public void ApplyStrech(int x, int y, sbyte z)
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
                UpdateZ(
                    map.GetTileZ(x, y + 1),
                    map.GetTileZ(x + 1, y + 1),
                    map.GetTileZ(x + 1, y),
                    z);

                Vector3[,,] vec = new Vector3[3, 3, 4];
                int i;
                int j;

                if (Normals == null || Normals.Length != 4)
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
                            v0.Z = (currentZ - rightZ) << 2;
                            MergeAndNormalize(ref v0, -22.0f, -22.0f, (leftZ - currentZ) << 2);


                            ref var v1 = ref vec[curI, curJ, 1];
                            v1.X = 22;
                            v1.Y = 22;
                            v1.Z = (rightZ - bottomZ) << 2;
                            MergeAndNormalize(ref v1, -22.0f, 22.0f, (currentZ - rightZ) << 2);

                            ref var v2 = ref vec[curI, curJ, 2];
                            v2.X = 22;
                            v2.Y = -22;
                            v2.Z = (bottomZ - leftZ) << 2;
                            MergeAndNormalize(ref v2, 22.0f, 22.0f, (rightZ - bottomZ) << 2);

                            ref var v3 = ref vec[curI, curJ, 3];
                            v3.X = -22;
                            v3.Y = -22;
                            v3.Z = (leftZ - currentZ) << 2;
                            MergeAndNormalize(ref v3, 22.0f, -22.0f, (bottomZ - leftZ) << 2);
                        }
                    }
                }

                i = 1;
                j = 1;

                // 0
                SumAndNormalize(
                     ref vec,
                     i - 1, j - 1, 2,
                     i - 1, j, 1,
                     i, j - 1, 3,
                     i, j, 0,
                     out Normals[0]);

                // 1
                SumAndNormalize(
                    ref vec,
                    i, j - 1, 2,
                    i, j, 1,
                    i + 1, j - 1, 3,
                    i + 1, j, 0,
                    out Normals[1]);

                // 2
                SumAndNormalize(
                    ref vec,
                    i, j, 2,
                    i, j + 1, 1,
                    i + 1, j, 3,
                    i + 1, j + 1, 0,
                    out Normals[2]);

                // 3
                SumAndNormalize(
                    ref vec,
                    i - 1, j, 2,
                    i - 1, j + 1, 1,
                    i, j, 3,
                    i, j + 1, 0,
                    out Normals[3]);
            }
        }


        [MethodImpl(256)]
        private static void SumAndNormalize(
            ref Vector3[,,] vec,
            int index0_x, int index0_y, int index0_z,
            int index1_x, int index1_y, int index1_z,
            int index2_x, int index2_y, int index2_z,
            int index3_x, int index3_y, int index3_z,
            out Vector3 result)
        {
            Vector3.Add(ref vec[index0_x, index0_y, index0_z], ref vec[index1_x, index1_y, index1_z], out var v0Result);
            Vector3.Add(ref vec[index2_x, index2_y, index2_z], ref vec[index3_x, index3_y, index3_z], out var v1Result);
            Vector3.Add(ref v0Result, ref v1Result, out result);
            Vector3.Normalize(ref result, out result);
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

        [MethodImpl(256)]
        private static void MergeAndNormalize(ref Vector3 v, float x, float y, float z)
        {
            float newX = v.Y * z - v.Z * y;
            float newY = v.Z * x - v.X * z;
            float newZ = v.X * y - v.Y * x;
            v.X = newX;
            v.Y = newY;
            v.Z = newZ;

            Vector3.Normalize(ref v, out v);
        }
    }
}