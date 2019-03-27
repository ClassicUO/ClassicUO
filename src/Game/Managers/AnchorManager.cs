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
             new Point(-1, 0) ,
             new Point(0, -1) ,
             new Point(1, 0) ,
             new Point(0, 1)
        };

        private readonly Dictionary<AnchorableGump, AnchorGroup> reverseMap = new Dictionary<AnchorableGump, AnchorGroup>();

        public void DropControl(AnchorableGump draggedControl, AnchorableGump host, int x, int y)
        {
            if (host.AnchorGroupName == draggedControl.AnchorGroupName && this[draggedControl] == null)
            {
                AnchorDirection direction = GetAnchorDirection(host, x, y);

                if (this[host] == null)
                    this[host] = new AnchorGroup(host);

                if (this[host].IsEmptyDirection(host, direction))
                {
                    this[host].AnchorControlAt(draggedControl, host, direction);
                    this[draggedControl] = this[host];
                }
            }
        }

        public Point GetCandidateDropLocation(AnchorableGump draggedControl, AnchorableGump host, int x, int y)
        {
            if (host.AnchorGroupName == draggedControl.AnchorGroupName && this[draggedControl] == null)
            {
                AnchorDirection direction = GetAnchorDirection(host, x, y);

                if (this[host] == null || this[host].IsEmptyDirection(host, direction))
                {
                    var offset = _anchorDirectionMatrix[(int) direction] * new Point(draggedControl.Width, draggedControl.Height);
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

        private AnchorDirection GetAnchorDirection(AnchorableGump host, int x, int y)
        {
            var anchorPoint = new Vector2((float)x / host.Width, (float)y / host.Height);

            for (AnchorDirection anchorDirection = AnchorDirection.Left; anchorDirection <= AnchorDirection.Bottom; anchorDirection++)
            {
                if (IsPointInPolygon(_anchorTriangles[ (int) anchorDirection], anchorPoint))
                {
                    return anchorDirection;
                }
            }

            return AnchorDirection.Left;
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
            AnchorableGump[,] controlMatrix;
            int updateCount = 0;

            public AnchorGroup(AnchorableGump initial)
            {
                controlMatrix = new AnchorableGump[1, 1];
                controlMatrix[0, 0] = initial;
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
                var coords = GetControlCoordinates(control);
                controlMatrix[coords.Value.X, coords.Value.Y] = null;
            }

            public List<AnchorableGump> Restore(BinaryReader reader, List<Gump> gumps)
            {
                List<AnchorableGump> groupGumps = new List<AnchorableGump>();

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

                return groupGumps;
            }

            public void UpdateLocation(Control control, int deltaX, int deltaY)
            {
                if (updateCount == 0)
                {
                    updateCount++;

                    for (int x = 0; x < controlMatrix.GetLength(0); x++)
                    {
                        for (int y = 0; y < controlMatrix.GetLength(1); y++)
                        {
                            if (controlMatrix[x, y] != null && controlMatrix[x, y] != control)
                            {
                                controlMatrix[x, y].X += deltaX;
                                controlMatrix[x, y].Y += deltaY;
                            }
                        }
                    }

                    updateCount--;
                }
            }

            public void AnchorControlAt(AnchorableGump control, AnchorableGump host, AnchorDirection direction)
            {
                Point? hostDirection = GetControlCoordinates(host);
                if (hostDirection.HasValue)
                {
                    var targetX = hostDirection.Value.X + _anchorDirectionMatrix[(int)direction].X;
                    var targetY = hostDirection.Value.Y + _anchorDirectionMatrix[(int)direction].Y;

                    if (IsEmptyDirection(targetX, targetY))
                    {
                        if (targetX < 0) // Create new column left
                            ResizeMatrix(controlMatrix.GetLength(0) + 1, controlMatrix.GetLength(1), 1, 0);
                        else if (targetX > controlMatrix.GetLength(0) - 1) // Create new column right
                            ResizeMatrix(controlMatrix.GetLength(0) + 1, controlMatrix.GetLength(1), 0, 0);

                        if (targetY < 0) //Create new row top
                            ResizeMatrix(controlMatrix.GetLength(0), controlMatrix.GetLength(1) + 1, 0, 1);
                        else if (targetY > controlMatrix.GetLength(1) - 1) // Create new row bottom
                            ResizeMatrix(controlMatrix.GetLength(0), controlMatrix.GetLength(1) + 1, 0, 0);


                        hostDirection = GetControlCoordinates(host);

                        if (hostDirection.HasValue)
                        {
                            targetX = hostDirection.Value.X + _anchorDirectionMatrix[(int) direction].X;
                            targetY = hostDirection.Value.Y + _anchorDirectionMatrix[(int) direction].Y;
                            controlMatrix[targetX, targetY] = control;
                        }
                    }
                }
            }

            public bool IsEmptyDirection(AnchorableGump host, AnchorDirection direction)
            {
                Point? hostDirection = GetControlCoordinates(host);
                if (hostDirection.HasValue)
                {
                    var targetX = hostDirection.Value.X + _anchorDirectionMatrix[(int) direction].X;
                    var targetY = hostDirection.Value.Y + _anchorDirectionMatrix[(int) direction].Y;

                    return IsEmptyDirection(targetX, targetY);
                }

                return false;
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
        }
    }
}
