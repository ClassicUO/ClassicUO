#region license
//  Copyright (C) 2018 ClassicUO Development Community on Github
//
//	This project is an alternative client for the game Ultima Online.
//	The goal of this is to develop a lightweight client considering 
//	new technologies.  
//  (Copyright (c) 2018 ClassicUO Development Team)
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
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Map;
using ClassicUO.Input;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.Views
{
    public class TileView : View
    {
        private static readonly Point[] _surroundingIndexes = { new Point(0, -1), new Point(1, -1), new Point(-1, 0), new Point(1, 0), new Point(2, 0), new Point(-1, 1), new Point(0, 1), new Point(1, 1), new Point(2, 1), new Point(0, 2), new Point(1, 2) };

        private readonly Vector3[] _normals = new Vector3[4];
        private readonly SpriteVertex[] _vertex = new SpriteVertex[4]
        {
            new SpriteVertex(new Vector3(), new Vector3(), new Vector3(0, 0, 0)),
            new SpriteVertex(new Vector3(), new Vector3(), new Vector3(1, 0, 0)),
            new SpriteVertex(new Vector3(), new Vector3(), new Vector3(0, 1, 0)),
            new SpriteVertex(new Vector3(), new Vector3(), new Vector3(1, 1, 0))
        };
        private bool _needUpdateStrechedTile = true;
        private Vector3 _vertex0_yOffset, _vertex1_yOffset, _vertex2_yOffset, _vertex3_yOffset;


        public TileView(Tile tile) : base(tile)
        {
            tile.IsStretched = !(tile.TileData.TexID <= 0 && IO.Resources.TileData.IsWet((long)tile.TileData.Flags));
            AllowedToDraw = !tile.IsIgnored;
            tile.AverageZ = SortZ;
            tile.MinZ = tile.Position.Z;
        }


        //public new Tile GameObject => (Tile)base.GameObject;

        public override bool Draw(SpriteBatch3D spriteBatch, Vector3 position, MouseOverList<GameObject> objectList)
        {
            if (!AllowedToDraw || GameObject.IsDisposed)
            {
                return false;
            }

            Tile tile = (Tile)GameObject;

            if (Texture == null || Texture.IsDisposed)
            {
                if (tile.IsStretched)
                {
                    Texture = IO.Resources.TextmapTextures.GetTextmapTexture(((Tile)GameObject).TileData.TexID);
                }
                else
                {
                    Texture = IO.Resources.Art.GetLandTexture(GameObject.Graphic);
                    Bounds = new Rectangle(0, GameObject.Position.Z * 4, 44, 44);
                }
            }

            if (_needUpdateStrechedTile)
            {
                UpdateStreched(World.Map);
                _needUpdateStrechedTile = false;
            }

            return !tile.IsStretched ? base.Draw(spriteBatch, position, objectList) : Draw3DStretched(spriteBatch, position, objectList);
        }


        private bool Draw3DStretched(SpriteBatch3D spriteBatch, Vector3 position, MouseOverList<GameObject> objectList)
        {
            Texture.Ticks = World.Ticks;

            _vertex[0].Position = position + _vertex0_yOffset;
            _vertex[1].Position = position + _vertex1_yOffset;
            _vertex[2].Position = position + _vertex2_yOffset;
            _vertex[3].Position = position + _vertex3_yOffset;

            _vertex[0].Hue =
            _vertex[1].Hue =
            _vertex[2].Hue =
            _vertex[3].Hue = RenderExtentions.GetHueVector(GameObject.Hue);


            if (!spriteBatch.DrawSprite(Texture, _vertex))
            {
                return false;
            }

            if (objectList.IsMouseInObjectIsometric(_vertex))
                objectList.Add(GameObject, _vertex[0].Position);

            return true;
        }


        protected override void MousePick(MouseOverList<GameObject> list, SpriteVertex[] vertex)
        {
            int x = list.MousePosition.X - (int)vertex[0].Position.X;
            int y = list.MousePosition.Y - (int)vertex[0].Position.Y;

            if (Texture.Contains(x, y))
                list.Add(GameObject, vertex[0].Position);
        }


        private void UpdateStreched(Facet map)
        {
            float[] surroundingTilesZ = new float[_surroundingIndexes.Length];
            for (int i = 0; i < _surroundingIndexes.Length; i++)
            {
                surroundingTilesZ[i] = map.GetTileZ((short)(GameObject.Position.X + _surroundingIndexes[i].X), (short)(GameObject.Position.Y + _surroundingIndexes[i].Y));
            }

            sbyte currentZ = GameObject.Position.Z;
            sbyte leftZ = (sbyte)surroundingTilesZ[6];
            sbyte rightZ = (sbyte)surroundingTilesZ[3];
            sbyte bottomZ = (sbyte)surroundingTilesZ[7];

            if (!(currentZ == leftZ && currentZ == rightZ && currentZ == bottomZ))
            {
                Tile tile = (Tile)GameObject;
                sbyte low = 0, high = 0;
                sbyte sort = (sbyte)map.GetAverageZ(GameObject.Position.Z, leftZ, rightZ, bottomZ, ref low, ref high);
                tile.AverageZ = sort;
                if (sort != SortZ)
                {
                    tile.MinZ = low;

                    SortZ = sort;
                    map.GetTile((short)GameObject.Position.X, (short)GameObject.Position.Y).ForceSort();
                }
            }

            _normals[0] = CalculateNormal(surroundingTilesZ[2], surroundingTilesZ[3], surroundingTilesZ[0], surroundingTilesZ[6]);
            _normals[1] = CalculateNormal(GameObject.Position.Z, surroundingTilesZ[4], surroundingTilesZ[1], surroundingTilesZ[7]);
            _normals[2] = CalculateNormal(surroundingTilesZ[5], surroundingTilesZ[7], GameObject.Position.Z, surroundingTilesZ[9]);
            _normals[3] = CalculateNormal(surroundingTilesZ[6], surroundingTilesZ[8], surroundingTilesZ[3], surroundingTilesZ[10]);

            _vertex0_yOffset = new Vector3(22, -(currentZ * 4), 0);
            _vertex1_yOffset = new Vector3(44f, 22 - rightZ * 4, 0);
            _vertex2_yOffset = new Vector3(0, 22 - leftZ * 4, 0);
            _vertex3_yOffset = new Vector3(22, 44f - bottomZ * 4, 0);

            _vertex[0].Normal = _normals[0];
            _vertex[1].Normal = _normals[1];
            _vertex[2].Normal = _normals[2];
            _vertex[3].Normal = _normals[3];

            Vector3 hue = RenderExtentions.GetHueVector(GameObject.Hue);
            if (_vertex[0].Hue != hue)
            {
                _vertex[0].Hue = _vertex[1].Hue = _vertex[2].Hue = _vertex[3].Hue = hue;
            }
        }

        private static Vector3 CalculateNormal(float a, float b, float c, float d)
        {
            return Vector3.Normalize(new Vector3(a - b, 1f, c - d));
        }
    }
}