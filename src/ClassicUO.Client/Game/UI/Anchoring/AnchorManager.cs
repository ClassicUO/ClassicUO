// SPDX-License-Identifier: BSD-2-Clause

using System;
using System.Collections.Generic;
using System.Xml;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Game.UI.Gumps;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.UI.Anchoring
{
    /// <summary>
    /// Facade over the Anchoring collaborators: preserves the existing
    /// public surface (<c>UIManager.AnchorManager.X</c>) and the nested
    /// <see cref="AnchorGroup"/> matrix helper while delegating control-to-
    /// group bookkeeping to <see cref="IAnchorStore"/> and overlap / bounds
    /// math to <see cref="IAnchorGeometry"/>. The drop / detach / dispose
    /// orchestration stays on the facade because it spans both
    /// collaborators and the matrix.
    /// </summary>
    internal sealed class AnchorManager
    {
        private static readonly Vector2[][] _anchorTriangles =
        {
            new[] { new Vector2(0f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0f, 1f) },
            new[] { new Vector2(0f, 0f), new Vector2(0.5f, 0.5f), new Vector2(1f, 0f) },
            new[] { new Vector2(1f, 0f), new Vector2(0.5f, 0.5f), new Vector2(1f, 1f) },
            new[] { new Vector2(0f, 1f), new Vector2(0.5f, 0.5f), new Vector2(1f, 1f) }
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

        private readonly IAnchorStore _store;
        private readonly IAnchorGeometry _geometry;

        /// <summary>Production composition root. Defaults to concrete collaborators.</summary>
        public AnchorManager()
            : this(new AnchorStore(), new AnchorGeometry())
        {
        }

        /// <summary>Full DI seam — inject all collaborators.</summary>
        internal AnchorManager(IAnchorStore store, IAnchorGeometry geometry)
        {
            _store = store;
            _geometry = geometry;
        }

        public AnchorGroup this[AnchorableGump control]
        {
            get => _store.Get(control);
            set => _store.Set(control, value);
        }

        public void Save(XmlTextWriter writer)
        {
            foreach (AnchorGroup value in _store.DistinctGroups())
            {
                value.Save(writer);
            }
        }

        /*public void AttachControl(AnchorableGump host, AnchorableGump control)
        {
            if (host.AnchorType == control.AnchorType && this[control] == null)
            {
                if (this[host] == null)
                    this[host] = new AnchorGroup(host);

                this[host].AnchorControlAt(control, host, control.Location);
                this[control] = this[host];
            }
        }*/

        public void DropControl(AnchorableGump draggedControl, AnchorableGump host)
        {
            if (host.AnchorType == draggedControl.AnchorType && this[draggedControl] == null)
            {
                (Point? relativePosition, _) = GetAnchorDirection(draggedControl, host);

                if (relativePosition.HasValue)
                {
                    if (this[host] == null)
                    {
                        this[host] = new AnchorGroup(host);
                    }

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
            if (host.AnchorType == draggedControl.AnchorType && this[draggedControl] == null)
            {
                (Point? relativePosition, AnchorableGump g) = GetAnchorDirection(draggedControl, host);

                if (relativePosition.HasValue)
                {
                    if (this[host] == null || this[host].IsEmptyDirection(draggedControl, host, relativePosition.Value))
                    {
                        Point offset = relativePosition.Value * new Point(g.GroupMatrixWidth, g.GroupMatrixHeight);

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
                List<AnchorableGump> group = _store.GetControlsInGroup(this[control]);

                if (group.Count == 2) // if detach 1+1 - need destroy all group
                {
                    foreach (AnchorableGump ctrl in group)
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
                foreach (AnchorableGump ctrl in _store.GetControlsInGroup(this[control]))
                {
                    this[ctrl] = null;
                    ctrl.Dispose();
                }
            }
        }

        private (Point?, AnchorableGump) GetAnchorDirection(AnchorableGump draggedControl, AnchorableGump host)
        {
            int xdistancescale = Math.Abs(draggedControl.X - host.X) * 100 / host.Width;
            int ydistancescale = Math.Abs(draggedControl.Y - host.Y) * 100 / host.Height;

            if (xdistancescale > ydistancescale)
            {
                if (draggedControl.X > host.X)
                {
                    return (new Point(host.WidthMultiplier, 0), host);
                }

                return (new Point(-draggedControl.WidthMultiplier, 0), draggedControl);
            }

            if (draggedControl.Y > host.Y)
            {
                return (new Point(0, host.HeightMultiplier), host);
            }

            return (new Point(0, -draggedControl.HeightMultiplier), draggedControl);
        }

        private bool IsPointInPolygon(Vector2[] polygon, Vector2 point)
        {
            bool isInside = false;

            for (int i = 0, j = polygon.Length - 1; i < polygon.Length; j = i++)
            {
                if (polygon[i].Y > point.Y != polygon[j].Y > point.Y && point.X < (polygon[j].X - polygon[i].X) * (point.Y - polygon[i].Y) / (polygon[j].Y - polygon[i].Y) + polygon[i].X)
                {
                    isInside = !isInside;
                }
            }

            return isInside;
        }

        public AnchorableGump ClosestOverlappingControl(AnchorableGump control)
        {
            if (control == null || control.IsDisposed)
            {
                return null;
            }

            AnchorableGump closestControl = null;
            int closestDistance = 99999;

            foreach (Gump c in UIManager.Gumps)
            {
                if (!c.IsDisposed && c is AnchorableGump host && host.AnchorType == control.AnchorType)
                {
                    if (_geometry.IsOverlapping(control, host))
                    {
                        int dirtyDistance = Math.Abs(control.X - host.X) + Math.Abs(control.Y - host.Y);

                        if (dirtyDistance < closestDistance)
                        {
                            closestDistance = dirtyDistance;
                            closestControl = host;
                        }
                    }
                }
            }

            return closestControl;
        }

        private enum AnchorDirection
        {
            Left,
            Top,
            Right,
            Bottom
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

            public void AddControlToMatrix(int xinit, int yInit, AnchorableGump control)
            {
                for (int x = 0; x < control.WidthMultiplier; x++)
                {
                    for (int y = 0; y < control.HeightMultiplier; y++)
                    {
                        controlMatrix[x + xinit, y + yInit] = control;
                    }
                }
            }

            public void Save(XmlTextWriter writer)
            {
                writer.WriteStartElement("anchored_group_gump");

                writer.WriteAttributeString("matrix_w", controlMatrix.GetLength(0).ToString());

                writer.WriteAttributeString("matrix_h", controlMatrix.GetLength(1).ToString());

                for (int y = 0; y < controlMatrix.GetLength(1); y++)
                {
                    for (int x = 0; x < controlMatrix.GetLength(0); x++)
                    {
                        AnchorableGump gump = controlMatrix[x, y];

                        if (gump != null)
                        {
                            writer.WriteStartElement("gump");
                            gump.Save(writer);
                            writer.WriteAttributeString("matrix_x", x.ToString());
                            writer.WriteAttributeString("matrix_y", y.ToString());
                            writer.WriteEndElement();
                        }
                    }
                }

                writer.WriteEndElement();
            }

            public void MakeTopMost()
            {
                for (int x = 0; x < controlMatrix.GetLength(0); x++)
                {
                    for (int y = 0; y < controlMatrix.GetLength(1); y++)
                    {
                        if (controlMatrix[x, y] != null)
                        {
                            UIManager.MakeTopMostGump(controlMatrix[x, y]);
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
                    int targetX = hostPosition.Value.X + relativePosition.X;
                    int targetY = hostPosition.Value.Y + relativePosition.Y;

                    if (IsEmptyDirection(targetX, targetY))
                    {
                        if (targetX < 0) // Create new column left
                        {
                            ResizeMatrix(controlMatrix.GetLength(0) + control.WidthMultiplier, controlMatrix.GetLength(1), control.WidthMultiplier, 0);
                        }
                        else if (targetX > controlMatrix.GetLength(0) - control.WidthMultiplier) // Create new column right
                        {
                            ResizeMatrix(controlMatrix.GetLength(0) + control.WidthMultiplier, controlMatrix.GetLength(1), 0, 0);
                        }

                        if (targetY < 0) //Create new row top
                        {
                            ResizeMatrix(controlMatrix.GetLength(0), controlMatrix.GetLength(1) + control.HeightMultiplier, 0, control.HeightMultiplier);
                        }
                        else if (targetY > controlMatrix.GetLength(1) - 1) // Create new row bottom
                        {
                            ResizeMatrix(controlMatrix.GetLength(0), controlMatrix.GetLength(1) + control.HeightMultiplier, 0, 0);
                        }


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
                    Point targetInitPosition = hostPosition.Value + relativePosition;

                    for (int xOffset = 0; xOffset < draggedControl.WidthMultiplier; xOffset++)
                    {
                        for (int yOffset = 0; yOffset < draggedControl.HeightMultiplier; yOffset++)
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
                if (x < 0 || x > controlMatrix.GetLength(0) - 1 || y < 0 || y > controlMatrix.GetLength(1) - 1)
                {
                    return true;
                }

                return controlMatrix[x, y] == null;
            }

            private Point? GetControlCoordinates(AnchorableGump control)
            {
                for (int x = 0; x < controlMatrix.GetLength(0); x++)
                {
                    for (int y = 0; y < controlMatrix.GetLength(1); y++)
                    {
                        if (controlMatrix[x, y] == control)
                        {
                            return new Point(x, y);
                        }
                    }
                }

                return null;
            }

            public void ResizeMatrix(int xCount, int yCount, int xInitial, int yInitial)
            {
                AnchorableGump[,] newMatrix = new AnchorableGump[xCount, yCount];

                for (int x = 0; x < controlMatrix.GetLength(0); x++)
                {
                    for (int y = 0; y < controlMatrix.GetLength(1); y++)
                    {
                        newMatrix[x + xInitial, y + yInitial] = controlMatrix[x, y];
                    }
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
                        {
                            Console.Write(" " + controlMatrix[x, y].LocalSerial + " ");
                        }
                        else
                        {
                            Console.Write(" ---------- ");
                        }
                    }

                    Console.WriteLine();
                }
            }
        }
    }
}
