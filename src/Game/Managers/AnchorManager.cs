#region license
// Copyright (C) 2020 ClassicUO Development Community on Github
// 
// This project is an alternative client for the game Ultima Online.
// The goal of this is to develop a lightweight client considering
// new technologies.
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
using System.Collections.Generic;
using System.IO;
using System.Linq;

using ClassicUO.Game.UI.Controls;
using ClassicUO.Game.UI.Gumps;

using Microsoft.Xna.Framework;

namespace ClassicUO.Game.Managers
{
    internal sealed class AnchorManager
    {
        enum AnchorDirection
        {
            Left,
            Top,
            Right,
            Bottom
        }

        private static readonly Vector2[][] _anchorTriangles =
        {
            new[] {new Vector2(0f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0f, 1f)},
            new[] {new Vector2(0f, 0f), new Vector2(0.5f, 0.5f), new Vector2(1f, 0f)},
            new[] {new Vector2(1f, 0f), new Vector2(0.5f, 0.5f), new Vector2(1f, 1f)},
            new[] {new Vector2(0f, 1f), new Vector2(0.5f, 0.5f), new Vector2(1f, 1f)}
        };

        private static readonly Point[] _anchorDirectionMatrix =
        {
            new Point(-1, 0),
            new Point(0, -1),
            new Point(1, 0),
            new Point(0, 1)
        };

        private static readonly Point[] _anchorMultiplierMatrix =
        {
            new Point(0, 0),
            new Point(0, 0),
            new Point(1, 0),
            new Point(0, 1)
        };

        private readonly Dictionary<AnchorableGump, AnchorGroup> reverseMap = new Dictionary<AnchorableGump, AnchorGroup>();

        public AnchorGroup this[AnchorableGump control]
        {
            get
            {
                reverseMap.TryGetValue(control, out AnchorGroup group);

                return group;
            }

            private set
            {
                if (reverseMap.ContainsKey(control) && value == null)
                    reverseMap.Remove(control);
                else
                    reverseMap.Add(control, value);
            }
        }

        public void DropControl(AnchorableGump draggedControl, AnchorableGump host)
        {
            if (host.AnchorGroupName == draggedControl.AnchorGroupName && this[draggedControl] == null)
            {
                (Point? relativePosition, _) = GetAnchorDirection(draggedControl, host);

                if (relativePosition.HasValue)
                {
                    if (this[host] == null)
                        this[host] = new AnchorGroup(host);

                    if (this[host].IsEmptyDirection(draggedControl, host, relativePosition.Value))
                    {
                        this[host].AnchorControlAt(draggedControl, host, relativePosition.Value);
                        this[draggedControl] = this[host];
                    }
                }
            }
        }

        public Point GetCandidateDropLocation(AnchorableGump draggedControl, AnchorableGump host)
        {
            if (host.AnchorGroupName == draggedControl.AnchorGroupName && this[draggedControl] == null)
            {
                (Point? relativePosition, AnchorableGump g) = GetAnchorDirection(draggedControl, host);

                if (relativePosition.HasValue)
                {
                    if (this[host] == null || this[host].IsEmptyDirection(draggedControl, host, relativePosition.Value))
                    {
                        var offset = relativePosition.Value * new Point(g.GroupMatrixWidth, g.GroupMatrixHeight);

                        return new Point(host.X + offset.X, host.Y + offset.Y);
                    }
                }
            }

            return draggedControl.Location;
        }

        public AnchorableGump GetAnchorableControlUnder(AnchorableGump draggedControl)
        {
            return ClosestOverlappingControl(draggedControl);
        }

        public void DetachControl(AnchorableGump control)
        {
            if (this[control] != null)
            {
                var group = reverseMap.Where(o => o.Value == this[control]).Select(o => o.Key).ToList();

                if (group.Count == 2) // if detach 1+1 - need destroy all group
                {
                    foreach (var ctrl in group)
                    {
                        this[ctrl].DetachControl(ctrl);
                        this[ctrl] = null;
                    }
                }
                else
                {
                    this[control].DetachControl(control);
                    this[control] = null;
                }
            }
        }

        public void DisposeAllControls(AnchorableGump control)
        {
            if (this[control] != null)
            {
                foreach (var ctrl in reverseMap.Where(o => o.Value == this[control]).Select(o => o.Key).ToList())
                {
                    this[ctrl] = null;
                    ctrl.Dispose();
                }
            }
        }

        public void Save(BinaryWriter writer)
        {
            const int VERSION = 1;
            var groups = reverseMap.Values.Distinct().ToList();

            writer.Write(VERSION);
            writer.Write(groups.Count);

            foreach (var group in groups)
                group.Save(writer);
        }

        public void Restore(BinaryReader reader, List<Gump> gumps)
        {
            var version = reader.ReadInt32();
            var count = reader.ReadInt32();

            for (int i = 0; i < count; i++)
            {
                var group = new AnchorGroup();
                var groupGumps = group.Restore(reader, gumps);

                // Rebuild reverse map, skip if group count is <= 1
                if (groupGumps.Count > 1)
                {
                    foreach (var g in groupGumps)
                        this[g] = group;
                }
            }
        }

        private (Point?, AnchorableGump) GetAnchorDirection(AnchorableGump draggedControl, AnchorableGump host)
        {
            int xdistancescale = Math.Abs(draggedControl.X - host.X)*100 / host.Width;
            int ydistancescale = Math.Abs(draggedControl.Y - host.Y)*100 / host.Height;

            if (xdistancescale > ydistancescale)
            {
                if (draggedControl.X > host.X)
                    return (new Point(host.WidthMultiplier, 0), host);
                else
                    return (new Point(-draggedControl.WidthMultiplier, 0), draggedControl);
            }
            else
            {
                if (draggedControl.Y > host.Y)
                    return (new Point(0, host.HeightMultiplier), host);
                else
                    return (new Point(0, -draggedControl.HeightMultiplier), draggedControl);
            }
        }

        private bool IsPointInPolygon(Vector2[] polygon, Vector2 point)
        {
            bool isInside = false;

            for (int i = 0, j = polygon.Length - 1; i < polygon.Length; j = i++)
            {
                if (polygon[i].Y > point.Y != polygon[j].Y > point.Y &&
                    point.X < (polygon[j].X - polygon[i].X) * (point.Y - polygon[i].Y) / (polygon[j].Y - polygon[i].Y) + polygon[i].X)
                    isInside = !isInside;
            }

            return isInside;
        }

        public AnchorableGump ClosestOverlappingControl(AnchorableGump control)
        {
            AnchorableGump closestControl = null;
            int closestDistance = 99999;

            var hosts = UIManager.Gumps.OfType<AnchorableGump>().Where(s => s.AnchorGroupName == control.AnchorGroupName);
            foreach (AnchorableGump host in hosts)
            {
                if (IsOverlapping(control, host))
                {
                    int dirtyDistance = Math.Abs(control.X - host.X) + Math.Abs(control.Y - host.Y);
                    if (dirtyDistance < closestDistance)
                    {
                        closestDistance = dirtyDistance;
                        closestControl = host;
                    }
                }
            }

            return closestControl;
        }

        private bool IsOverlapping(AnchorableGump control, AnchorableGump host)
        {
            if (control == host)
                return false;

            if (control.Bounds.Top > host.Bounds.Bottom || control.Bounds.Bottom < host.Bounds.Top)
                return false;

            if (control.Bounds.Right < host.Bounds.Left || control.Bounds.Left > host.Bounds.Right)
                return false;

            return true;
        }

        public class AnchorGroup
        {
            private AnchorableGump[,] controlMatrix;
            private int updateCount;

            public AnchorGroup(AnchorableGump initial)
            {
                controlMatrix = new AnchorableGump[initial.WidthMultiplier, initial.HeightMultiplier];
                AddControlToMatrix(0, 0, initial);
            }

            public AnchorGroup()
            {
                controlMatrix = new AnchorableGump[0, 0];
            }

            private void AddControlToMatrix(int xinit, int yInit, AnchorableGump control)
            {
                for (int x = 0; x < control.WidthMultiplier; x++)
                {
                    for (int y = 0; y < control.HeightMultiplier; y++) controlMatrix[x + xinit, y + yInit] = control;
                }
            }

            public void Save(BinaryWriter writer)
            {
                writer.Write(controlMatrix.GetLength(0));
                writer.Write(controlMatrix.GetLength(1));

                for (int x = 0; x < controlMatrix.GetLength(0); x++)
                {
                    for (int y = 0; y < controlMatrix.GetLength(1); y++)
                    {
                        if (controlMatrix[x, y] != null)
                            writer.Write(controlMatrix[x, y].LocalSerial);
                        else
                            writer.Write(0);
                    }
                }
            }

            public void MakeTopMost()
            {
                for (int x = 0; x < controlMatrix.GetLength(0); x++)
                {
                    for (int y = 0; y < controlMatrix.GetLength(1); y++)
                    {
                        if (controlMatrix[x, y] != null)
                            UIManager.MakeTopMostGump(controlMatrix[x, y]);
                    }
                }
            }

            public void DetachControl(AnchorableGump control)
            {
                for (int x = 0; x < controlMatrix.GetLength(0); x++)
                {
                    for (int y = 0; y < controlMatrix.GetLength(1); y++)
                    {
                        if (controlMatrix[x, y] == control)
                            controlMatrix[x, y] = null;
                    }
                }
            }

            public List<AnchorableGump> Restore(BinaryReader reader, List<Gump> gumps)
            {
                HashSet<AnchorableGump> groupGumps = new HashSet<AnchorableGump>();

                uint xCount = reader.ReadUInt32();
                uint yCount = reader.ReadUInt32();

                ResizeMatrix((int) xCount, (int) yCount, 0, 0);

                for (int x = 0; x < controlMatrix.GetLength(0); x++)
                {
                    for (int y = 0; y < controlMatrix.GetLength(1); y++)
                    {
                        uint serial = reader.ReadUInt32();

                        if (serial != 0)
                        {
                            var gump = gumps.Where(o => o.LocalSerial == serial)
                                            .OfType<AnchorableGump>().SingleOrDefault();

                            if (gump != null)
                            {
                                groupGumps.Add(gump);
                                controlMatrix[x, y] = gump;
                            }
                        }
                    }
                }

                return groupGumps.ToList();
            }

            public void UpdateLocation(Control control, int deltaX, int deltaY)
            {
                if (updateCount == 0)
                {
                    updateCount++;

                    HashSet<Control> visited = new HashSet<Control>();

                    for (int x = 0; x < controlMatrix.GetLength(0); x++)
                    {
                        for (int y = 0; y < controlMatrix.GetLength(1); y++)
                        {
                            if (controlMatrix[x, y] != null && controlMatrix[x, y] != control)
                            {
                                if (!visited.Contains(controlMatrix[x, y]))
                                {
                                    controlMatrix[x, y].X += deltaX;
                                    controlMatrix[x, y].Y += deltaY;
                                    visited.Add(controlMatrix[x, y]);
                                }
                            }
                        }
                    }

                    updateCount--;
                }
            }

            public void AnchorControlAt(AnchorableGump control, AnchorableGump host, Point relativePosition)
            {
                Point? hostPosition = GetControlCoordinates(host);

                if (hostPosition.HasValue)
                {
                    var targetX = hostPosition.Value.X + relativePosition.X;
                    var targetY = hostPosition.Value.Y + relativePosition.Y;

                    if (IsEmptyDirection(targetX, targetY))
                    {
                        if (targetX < 0) // Create new column left
                            ResizeMatrix(controlMatrix.GetLength(0) + control.WidthMultiplier, controlMatrix.GetLength(1), control.WidthMultiplier, 0);
                        else if (targetX > controlMatrix.GetLength(0) - control.WidthMultiplier) // Create new column right
                            ResizeMatrix(controlMatrix.GetLength(0) + control.WidthMultiplier, controlMatrix.GetLength(1), 0, 0);

                        if (targetY < 0) //Create new row top
                            ResizeMatrix(controlMatrix.GetLength(0), controlMatrix.GetLength(1) + control.HeightMultiplier, 0, control.HeightMultiplier);
                        else if (targetY > controlMatrix.GetLength(1) - 1) // Create new row bottom
                            ResizeMatrix(controlMatrix.GetLength(0), controlMatrix.GetLength(1) + control.HeightMultiplier, 0, 0);


                        hostPosition = GetControlCoordinates(host);

                        if (hostPosition.HasValue)
                        {
                            targetX = hostPosition.Value.X + relativePosition.X;
                            targetY = hostPosition.Value.Y + relativePosition.Y;

                            AddControlToMatrix(targetX, targetY, control);
                        }
                    }
                }
            }

            public bool IsEmptyDirection(AnchorableGump draggedControl, AnchorableGump host, Point relativePosition)
            {
                Point? hostPosition = GetControlCoordinates(host);

                bool isEmpty = true;

                if (hostPosition.HasValue)
                {
                    var targetInitPosition = hostPosition.Value + relativePosition;

                    for (int xOffset = 0; xOffset < draggedControl.WidthMultiplier; xOffset++)
                    {
                        for (int yOffset = 0; yOffset < draggedControl.HeightMultiplier; yOffset++) isEmpty &= IsEmptyDirection(targetInitPosition.X + xOffset, targetInitPosition.Y + yOffset);
                    }

                    //// TODO: loop through
                    //var targetX = hostPosition.Value.X + relativePosition.X;
                    //var targetY = hostPosition.Value.Y + relativePosition.Y;

                    //return IsEmptyDirection(targetX, targetY);
                }

                return isEmpty;
            }

            public bool IsEmptyDirection(int x, int y)
            {
                if (x < 0 || x > controlMatrix.GetLength(0) - 1
                          || y < 0 || y > controlMatrix.GetLength(1) - 1)
                    return true;

                return controlMatrix[x, y] == null;
            }

            private Point? GetControlCoordinates(AnchorableGump control)
            {
                for (int x = 0; x < controlMatrix.GetLength(0); x++)
                {
                    for (int y = 0; y < controlMatrix.GetLength(1); y++)
                    {
                        if (controlMatrix[x, y] == control)
                            return new Point(x, y);
                    }
                }

                return null;
            }

            private void ResizeMatrix(int xCount, int yCount, int xInitial, int yInitial)
            {
                AnchorableGump[,] newMatrix = new AnchorableGump[xCount, yCount];

                for (int x = 0; x < controlMatrix.GetLength(0); x++)
                {
                    for (int y = 0; y < controlMatrix.GetLength(1); y++)
                        newMatrix[x + xInitial, y + yInitial] = controlMatrix[x, y];
                }

                controlMatrix = newMatrix;
            }

            private void printMatrix()
            {
                Console.WriteLine();
                Console.WriteLine();
                Console.WriteLine();

                for (int y = 0; y < controlMatrix.GetLength(1); y++)
                {
                    for (int x = 0; x < controlMatrix.GetLength(0); x++)
                    {
                        if (controlMatrix[x, y] != null)
                            Console.Write(" " + controlMatrix[x, y].LocalSerial + " ");
                        else
                            Console.Write(" ---------- ");
                    }

                    Console.WriteLine();
                }
            }
        }
    }
}