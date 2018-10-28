#region license

//  Copyright (C) 2018 ClassicUO Development Community on Github
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

using ClassicUO.Game.Map;
using ClassicUO.Input;
using ClassicUO.IO.Resources;
using ClassicUO.Renderer;
using ClassicUO.Utility;

using Microsoft.Xna.Framework;

namespace ClassicUO.Game.Views
{
    public class TileView : View
    {
        private readonly Vector3[] _normals = new Vector3[4];
        private readonly SpriteVertex[] _vertex = new SpriteVertex[4]
        {
            new SpriteVertex(new Vector3(), new Vector3(), new Vector3(0, 0, 0)),
            new SpriteVertex(new Vector3(), new Vector3(), new Vector3(1, 0, 0)),
            new SpriteVertex(new Vector3(), new Vector3(), new Vector3(0, 1, 0)),
            new SpriteVertex(new Vector3(), new Vector3(), new Vector3(1, 1, 0))
        };
        private Vector3 _storedHue;
        private Vector3 _vertex0_yOffset, _vertex1_yOffset, _vertex2_yOffset, _vertex3_yOffset;

        public TileView(Tile tile) : base(tile)
        {
            //tile.IsStretched = tile.TileData.TexID > 0 && !TileData.IsWet((long)tile.TileData.Flags);

            
            AllowedToDraw = !tile.IsIgnored;

            if (tile.IsStretched)
                UpdateStreched(World.Map);
        }

        public override bool Draw(SpriteBatch3D spriteBatch, Vector3 position, MouseOverList objectList)
        {
            if (!AllowedToDraw || GameObject.IsDisposed)
                return false;
            Tile tile = (Tile) GameObject;


            if (Texture == null || Texture.IsDisposed)
            {
                if (tile.IsStretched)
                {
                    Texture = TextmapTextures.GetTextmapTexture(tile.TileData.TexID);
                }
                else
                {
                    Texture = Art.GetLandTexture(GameObject.Graphic);
                    Bounds = new Rectangle(0, GameObject.Position.Z * 4, 44, 44);

                }

            }



            HueVector = RenderExtentions.GetHueVector(GameObject.Hue);

            return !tile.IsStretched ? base.Draw(spriteBatch, position, objectList) : Draw3DStretched(spriteBatch, position, objectList);
        }

        private bool Draw3DStretched(SpriteBatch3D spriteBatch, Vector3 position, MouseOverList objectList)
        {
            Texture.Ticks = CoreGame.Ticks;

            _vertex[0].Position = position + _vertex0_yOffset;
            _vertex[1].Position = position + _vertex1_yOffset;
            _vertex[2].Position = position + _vertex2_yOffset;
            _vertex[3].Position = position + _vertex3_yOffset;

            if (IsSelected)
            {
                if (_storedHue == Vector3.Zero)
                    _storedHue = HueVector;
                HueVector = RenderExtentions.SelectedHue;
            }
            else if (_storedHue != Vector3.Zero)
            {
                HueVector = _storedHue;
                _storedHue = Vector3.Zero;
            }

            if (HueVector != _vertex[0].Hue)
                _vertex[0].Hue = _vertex[1].Hue = _vertex[2].Hue = _vertex[3].Hue = HueVector;

            if (!spriteBatch.DrawSprite(Texture, _vertex))
                return false;

            if (objectList.IsMouseInObjectIsometric(_vertex))
                objectList.Add(GameObject, _vertex[0].Position);

            return true;
        }

        protected override void MousePick(MouseOverList list, SpriteVertex[] vertex)
        {
            int x = list.MousePosition.X - (int) vertex[0].Position.X;
            int y = list.MousePosition.Y - (int) vertex[0].Position.Y;

            if (Art.Contains(GameObject.Graphic, x, y))
                list.Add(GameObject, vertex[0].Position);
        }

        private void UpdateStreched(Facet map)
        {
            Tile tile = (Tile) GameObject;

            if (/*tile.IsStretched && !TestStretched(tile.Position.X, tile.Position.Y, tile.Position.Z, true)*/ TextmapTextures.GetTextmapTexture(tile.TileData.TexID) == null)
            {
                tile.IsStretched = false;
                tile.MinZ = tile.Position.Z;
            }
            else
            {
                //tile.IsStretched = true;

                tile.UpdateZ(map.GetTileZ(tile.Position.X, tile.Position.Y + 1), map.GetTileZ(tile.Position.X + 1, tile.Position.Y + 1), map.GetTileZ(tile.Position.X + 1, tile.Position.Y));
                tile.PriorityZ = (short) (tile.AverageZ - 1);


                Vector3[,,] vec = new Vector3[3, 3, 4];
                int i;
                int j;

                for (i = -1; i < 2; i++)
                {
                    int curX = GameObject.Position.X + i;
                    int curI = i + 1;

                    for (j = -1; j < 2; j++)
                    {
                        int curY = GameObject.Position.Y + j;
                        int curJ = j + 1;


                        sbyte currentZ = map.GetTileZ(curX, curY);
                        sbyte leftZ = map.GetTileZ(curX, curY + 1);
                        sbyte rightZ = map.GetTileZ(curX + 1, curY);
                        sbyte bottomZ = map.GetTileZ(curX + 1, curY + 1);


                        if (currentZ == leftZ && currentZ == rightZ && currentZ == bottomZ)
                        {
                            for (int k = 0; k < 4; k++)
                                vec[curI, curJ, k] = new Vector3(0, 0, 1);
                        }
                        else
                        {
                            vec[curI, curJ, 0] = new Vector3(-22.0f, 22.0f, (currentZ - rightZ) * 4);
                            Merge(ref vec[curI, curJ, 0], -22.0f, -22.0f, (leftZ - currentZ) * 4);
                            vec[curI, curJ, 0].Normalize();

                            vec[curI, curJ, 1] = new Vector3(22.0f, 22.0f, (rightZ - bottomZ) * 4);
                            Merge(ref vec[curI, curJ, 1], -22.0f, 22.0f, (currentZ - rightZ) * 4);
                            vec[curI, curJ, 1].Normalize();

                            vec[curI, curJ, 2] = new Vector3(22.0f, -22.0f, (bottomZ - leftZ) * 4);
                            Merge(ref vec[curI, curJ, 2], 22.0f, 22.0f, (rightZ - bottomZ) * 4);
                            vec[curI, curJ, 2].Normalize();

                            vec[curI, curJ, 3] = new Vector3(-22.0f, -22.0f, (leftZ - currentZ) * 4);
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

                _vertex0_yOffset = new Vector3(22, -tile.Rectangle.Left, 0);
                _vertex1_yOffset = new Vector3(44, 22 - tile.Rectangle.Bottom, 0);
                _vertex2_yOffset = new Vector3(0, 22 - tile.Rectangle.Top, 0);
                _vertex3_yOffset = new Vector3(22, 44 - tile.Rectangle.Right, 0);          
            }

            Vector3 hue = RenderExtentions.GetHueVector(GameObject.Hue);
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

                        result = (testZ != z && testZ != -125);
                    }
                }
            }

            return result;
        }

        private static Vector3 CalculateNormal(float a, float b, float c, float d)
        {
            return Vector3.Normalize(new Vector3(a - b, 1f, c - d));
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