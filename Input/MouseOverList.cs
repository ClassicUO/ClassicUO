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
using System.Collections.Generic;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;

namespace ClassicUO.Input
{
    public struct MouseOverItem<T> where T : class
    {
        public MouseOverItem(T obj, Point pos)
        {
            Object = obj;
            InTexturePoint = pos;
        }

        public Point InTexturePoint;
        public T Object;
    }


    public class MouseOverList<T> where T : class
    {
        private readonly List<MouseOverItem<T>> _items;


        public MouseOverList(MousePicker<T> picker)
        {
            _items = new List<MouseOverItem<T>>();
            MousePosition = picker.Position;
            Picker = picker.PickOnly;
        }

        public Point MousePosition { get; set; }

        public PickerType Picker { get; set; }

        public MouseOverItem<T> GetItem(Point position)
        {
            if (_items.Count <= 0)
                return default;

            return _items[_items.Count - 1];
        }

        public void Add(T obj, Vector3 position)
        {
            Point p = new Point(MousePosition.X - (int) position.X, MousePosition.Y - (int) position.Y);
            _items.Add(new MouseOverItem<T>(obj, p));
        }

        public void Clear() => _items.Clear();

        public bool IsMouseInObjectIsometric(SpriteVertex[] v)
        {
            if (v.Length != 4) return false;
            float high = -50000, low = 50000;
            for (int i = 0; i < 4; i++)
            {
                if (v[i].Position.Y > high)
                    high = v[i].Position.Y;
                if (v[i].Position.Y < low)
                    low = v[i].Position.Y;
            }

            if (high < MousePosition.Y)
                return false;
            if (low > MousePosition.Y)
                return false;
            if (v[1].Position.X < MousePosition.X)
                return false;
            if (v[2].Position.X > MousePosition.X)
                return false;

            float minX = v[0].Position.X, maxX = v[0].Position.X;
            float minY = v[0].Position.Y, maxY = v[0].Position.Y;

            for (int i = 1; i < v.Length; i++)
            {
                if (v[i].Position.X < minX)
                    minX = v[i].Position.X;
                if (v[i].Position.X > maxX)
                    maxX = v[i].Position.X;
                if (v[i].Position.Y < minY)
                    minY = v[i].Position.Y;
                if (v[i].Position.Y > maxY)
                    maxY = v[i].Position.Y;
            }

            BoundingBox iBoundingBox = new BoundingBox(new Vector3(minX, minY, 0), new Vector3(maxX, maxY, 10));
            if (iBoundingBox.Contains(new Vector3(MousePosition.X, MousePosition.Y, 1)) == ContainmentType.Contains)
            {
                Point[] p = new Point[4];
                p[0] = new Point((int) v[0].Position.X, (int) v[0].Position.Y);
                p[1] = new Point((int) v[1].Position.X, (int) v[1].Position.Y);
                p[2] = new Point((int) v[3].Position.X, (int) v[3].Position.Y);
                p[3] = new Point((int) v[2].Position.X, (int) v[2].Position.Y);
                if (PointInPolygon(new Point(MousePosition.X, MousePosition.Y), p)) return true;
            }

            return false;
        }

        private static bool PointInPolygon(Point p, Point[] poly)
        {
            // Taken from http://social.msdn.microsoft.com/forums/en-US/winforms/thread/95055cdc-60f8-4c22-8270-ab5f9870270a/
            Point p1, p2;
            bool inside = false;
            if (poly.Length < 3) return inside;
            Point oldPoint = new Point(
                poly[poly.Length - 1].X, poly[poly.Length - 1].Y);

            for (int i = 0; i < poly.Length; i++)
            {
                Point newPoint = new Point(poly[i].X, poly[i].Y);
                if (newPoint.X > oldPoint.X)
                {
                    p1 = oldPoint;
                    p2 = newPoint;
                }
                else
                {
                    p1 = newPoint;
                    p2 = oldPoint;
                }

                if (newPoint.X < p.X == p.X <= oldPoint.X
                    && (p.Y - (long) p1.Y) * (p2.X - p1.X)
                    < (p2.Y - (long) p1.Y) * (p.X - p1.X))
                    inside = !inside;
                oldPoint = newPoint;
            }

            return inside;
        }
    }
}