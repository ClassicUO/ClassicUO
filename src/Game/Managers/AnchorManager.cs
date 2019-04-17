using ClassicUO.Game.UI.Controls;
using ClassicUO.Game.UI.Gumps;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClassicUO.Game.Managers
{
    internal sealed class AnchorManager
    {
        public enum AnchorDirection
        {
            Left, Top, Right, Bottom
        }

        private static readonly Vector2[][] _anchorTriangles =
        {
             new Vector2[] { new Vector2(0f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0f, 1f) },
             new Vector2[] { new Vector2(0f, 0f), new Vector2(0.5f, 0.5f), new Vector2(1f, 0f) },
             new Vector2[] { new Vector2(1f, 0f), new Vector2(0.5f, 0.5f), new Vector2(1f, 1f) },
             new Vector2[] { new Vector2(0f, 1f), new Vector2(0.5f, 0.5f), new Vector2(1f, 1f) }
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

        public void DropControl(AnchorableGump draggedControl, AnchorableGump host, int x, int y)
        {
            if (host.AnchorGroupName == draggedControl.AnchorGroupName && this[draggedControl] == null)
            {
                Point? relativePosition = GetAnchorDirection(draggedControl, host, x, y);
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

        public Point GetCandidateDropLocation(AnchorableGump draggedControl, AnchorableGump host, int x, int y)
        {
            if (host.AnchorGroupName == draggedControl.AnchorGroupName && this[draggedControl] == null)
            {
                Point? relativePosition = GetAnchorDirection(draggedControl, host, x, y);
                if (relativePosition.HasValue)
                    if (this[host] == null || this[host].IsEmptyDirection(draggedControl, host, relativePosition.Value))
                    {
                        var offset = relativePosition.Value * new Point(host.GroupMatrixWidth, host.GroupMatrixHeight);
                        return new Point(host.X + offset.X, host.Y + offset.Y);
                    }
            }

            return draggedControl.Location;
        }

        public AnchorableGump GetAnchorableControlOver(Control draggedControl, int x, int y)
        {
            return Engine.UI.GetMouseOverControls(new Point(draggedControl.ScreenCoordinateX + x, draggedControl.ScreenCoordinateY + y))
                                .Where(o => o != draggedControl)
                                .OfType<AnchorableGump>()
                                .FirstOrDefault();
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

        public AnchorGroup this[AnchorableGump control]
        {
            get
            {
                reverseMap.TryGetValue(control, out AnchorGroup @group);

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

        public void Save(BinaryWriter writer)
        {
            int VERSION = 1;
            var groups = reverseMap.Values.Distinct().ToList();

            writer.Write(VERSION);
            writer.Write(groups.Count);
            foreach(var group in groups)
                group.Save(writer);
        }

        public void Restore(BinaryReader reader, List<Gump> gumps)
        {
            var version = reader.ReadInt32();
            var count = reader.ReadInt32();
            for(int i = 0; i < count; i++)
            {
                var group = new AnchorGroup();
                var groupGumps = group.Restore(reader, gumps);

                // Rebuild reverse map
                foreach (var g in groupGumps)
                    this[g] = group;
            }
        }

        private Point? GetAnchorDirection(AnchorableGump draggedControl, AnchorableGump host, int x, int y)
        {
            for (int xMult = 0; xMult < host.WidthMultiplier; xMult++)
            {
                for (int yMult = 0; yMult < host.HeightMultiplier; yMult++)
                {
                    var snapX = x - (host.GroupMatrixWidth * xMult);
                    var snapY = y - (host.GroupMatrixHeight * yMult);

                    var anchorPoint = new Vector2((float)snapX / host.GroupMatrixWidth, (float)snapY / host.GroupMatrixHeight);

                    if (xMult == 0)
                        if (IsPointInPolygon(_anchorTriangles[(int)AnchorDirection.Left], anchorPoint))
                            return new Point(-draggedControl.WidthMultiplier, yMult);

                    if (yMult == 0)
                        if (IsPointInPolygon(_anchorTriangles[(int)AnchorDirection.Top], anchorPoint))
                            return new Point(xMult, -draggedControl.HeightMultiplier);

                    if (xMult == host.WidthMultiplier - 1)
                        if (IsPointInPolygon(_anchorTriangles[(int)AnchorDirection.Right], anchorPoint))
                            return new Point(1 + xMult, yMult);

                    if (yMult == host.HeightMultiplier - 1)
                        if (IsPointInPolygon(_anchorTriangles[(int)AnchorDirection.Bottom], anchorPoint))
                            return new Point(xMult, 1 + yMult);
                }
            }
            
            return null;
        }

        private bool IsPointInPolygon(Vector2[] polygon, Vector2 point)
        {
            bool isInside = false;
            for (int i = 0, j = polygon.Length - 1; i < polygon.Length; j = i++)
            {
                if (((polygon[i].Y > point.Y) != (polygon[j].Y > point.Y)) &&
                (point.X < (polygon[j].X - polygon[i].X) * (point.Y - polygon[i].Y) / (polygon[j].Y - polygon[i].Y) + polygon[i].X))
                {
                    isInside = !isInside;
                }
            }
            return isInside;
        }

        public class AnchorGroup
        {
            private AnchorableGump[,] controlMatrix;
            private int updateCount = 0;

            public AnchorGroup(AnchorableGump initial)
            {
                controlMatrix = new AnchorableGump[initial.WidthMultiplier, initial.HeightMultiplier];
                AddControlToMatrix(0, 0, initial);
            }

            private void AddControlToMatrix(int xinit, int yInit, AnchorableGump control)
            {
                for (int x = 0; x < control.WidthMultiplier; x++)
                {
                    for (int y = 0; y < control.HeightMultiplier; y++)
                    {
                        controlMatrix[x + xinit, y + yInit] = control;
                    }
                }
            }

            public AnchorGroup()
            {
                controlMatrix = new AnchorableGump[0, 0];
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
                            writer.Write(Serial.INVALID);
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
                        {
                            Engine.UI.MakeTopMostGump(controlMatrix[x, y]);
                        }
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
                        {
                            controlMatrix[x, y] = null;
                        }
                    }
                }
            }

            public List<AnchorableGump> Restore(BinaryReader reader, List<Gump> gumps)
            {
                HashSet<AnchorableGump> groupGumps = new HashSet<AnchorableGump>();

                uint xCount = reader.ReadUInt32();
                uint yCount = reader.ReadUInt32();

                ResizeMatrix((int)xCount, (int)yCount, 0, 0);

                for (int x = 0; x < controlMatrix.GetLength(0); x++)
                {
                    for (int y = 0; y < controlMatrix.GetLength(1); y++)
                    {
                        var serial = (Serial)reader.ReadUInt32();
                        if (serial != Serial.INVALID)
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
                        for(int yOffset = 0; yOffset < draggedControl.HeightMultiplier; yOffset++)
                        {
                            isEmpty &= IsEmptyDirection(targetInitPosition.X + xOffset, targetInitPosition.Y + yOffset);
                        }
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
                for(int x = 0; x < controlMatrix.GetLength(0); x++)
                {
                    for (int y = 0; y < controlMatrix.GetLength(1); y++)
                    {
                        if (controlMatrix[x,y] == control)
                        {
                            return new Point(x, y);
                        }
                    }
                }

                return null;
            }
            
            private void ResizeMatrix(int xCount, int yCount, int xInitial, int yInitial)
            {
                AnchorableGump[,] newMatrix = new AnchorableGump[xCount, yCount];
                for (int x = 0; x < controlMatrix.GetLength(0); x++)
                    for (int y = 0; y < controlMatrix.GetLength(1); y++)
                        newMatrix[x + xInitial, y + yInitial] = controlMatrix[x, y];

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
