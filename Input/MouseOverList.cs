using ClassicUO.Renderer;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Text;

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


    public class MouseOverList<T> where T: class
    {
        private readonly List<MouseOverItem<T>> _items;


        public MouseOverList()
        {
            _items = new List<MouseOverItem<T>>();
        }

        //public Point MousePosition { get; set; }




        public static bool IsMouseInObjectIsometric(SpriteVertex[] v, Point MousePosition)
        {
            if (v.Length != 4)
            {
                return false;
            }
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
                p[0] = new Point((int)v[0].Position.X, (int)v[0].Position.Y);
                p[1] = new Point((int)v[1].Position.X, (int)v[1].Position.Y);
                p[2] = new Point((int)v[3].Position.X, (int)v[3].Position.Y);
                p[3] = new Point((int)v[2].Position.X, (int)v[2].Position.Y);
                if (PointInPolygon(new Point((int)MousePosition.X, (int)MousePosition.Y), p))
                {
                    return true;
                }
            }
            return false;
        }

        private static bool PointInPolygon(Point p, Point[] poly)
        {
            // Taken from http://social.msdn.microsoft.com/forums/en-US/winforms/thread/95055cdc-60f8-4c22-8270-ab5f9870270a/
            Point p1, p2;
            bool inside = false;
            if (poly.Length < 3)
            {
                return inside;
            }
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

                if ((newPoint.X < p.X) == (p.X <= oldPoint.X)
                    && (p.Y - (long)p1.Y) * (p2.X - p1.X)
                    < (p2.Y - (long)p1.Y) * (p.X - p1.X))
                {
                    inside = !inside;
                }
                oldPoint = newPoint;
            }
            return inside;
        }
    }
}
