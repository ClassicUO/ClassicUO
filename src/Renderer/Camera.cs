#region license

// Copyright (c) 2021, andreakarasho
// All rights reserved.
// 
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are met:
// 1. Redistributions of source code must retain the above copyright
//    notice, this list of conditions and the following disclaimer.
// 2. Redistributions in binary form must reproduce the above copyright
//    notice, this list of conditions and the following disclaimer in the
//    documentation and/or other materials provided with the distribution.
// 3. All advertising materials mentioning features or use of this software
//    must display the following acknowledgement:
//    This product includes software developed by andreakarasho - https://github.com/andreakarasho
// 4. Neither the name of the copyright holder nor the
//    names of its contributors may be used to endorse or promote products
//    derived from this software without specific prior written permission.
// 
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS ''AS IS'' AND ANY
// EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
// WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
// DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER BE LIABLE FOR ANY
// DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
// (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
// LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
// ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
// (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
// SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

#endregion

using System.Runtime.CompilerServices;
using ClassicUO.Input;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO.Renderer
{
    internal class Camera
    {
        private float[] _cameraZoomValues = new float[1] { 1f };
        private Matrix _projection;
        private Matrix _transform = Matrix.Identity, _inverseTransform = Matrix.Identity;
        private bool _updateMatrixes = true, _updateProjection = true;
        private int _zoomIndex;


        public Matrix ViewTransformMatrix => TransformMatrix /** ProjectionMatrix*/;

        public Matrix ProjectionMatrix
        {
            get
            {
                if (_updateProjection)
                {
                    Matrix.CreateOrthographicOffCenter
                    (
                        0,
                        Bounds.Width,
                        Bounds.Height,
                        0,
                        0,
                        -1,
                        out _projection
                    );

                    _updateProjection = false;
                }

                return _projection;
            }
        }

        public Matrix TransformMatrix
        {
            get
            {
                UpdateMatrices();

                return _transform;
            }
        }

        public Matrix InverseTransformMatrix
        {
            get
            {
                UpdateMatrices();

                return _inverseTransform;
            }
        }

        public float Zoom
        {
            get => _cameraZoomValues[_zoomIndex];
            set
            {
                if (_cameraZoomValues[_zoomIndex] != value)
                {
                    // TODO: coding a better way to set zoom
                    for (_zoomIndex = 0; _zoomIndex < _cameraZoomValues.Length; ++_zoomIndex)
                    {
                        if (_cameraZoomValues[_zoomIndex] == value)
                        {
                            break;
                        }
                    }

                    // hack to trigger the bounds check and update matrices
                    ZoomIndex = _zoomIndex;
                }
            }
        }

        public int ZoomIndex
        {
            get => _zoomIndex;
            set
            {
                _updateMatrixes = true;
                _zoomIndex = value;

                if (_zoomIndex < 0)
                {
                    _zoomIndex = 0;
                }
                else if (_zoomIndex >= _cameraZoomValues.Length)
                {
                    _zoomIndex = _cameraZoomValues.Length - 1;
                }
            }
        }

        public int ZoomValuesCount => _cameraZoomValues.Length;
        public Rectangle Bounds;
        public Vector2 Origin;


        public Point Position;

        public void SetZoomValues(float[] values)
        {
            _cameraZoomValues = values;
        }

        public void SetGameWindowBounds(int x, int y, int width, int height)
        {
            if (Bounds.X != x || Bounds.Y != y || Bounds.Width != width || Bounds.Height != height)
            {
                Bounds.X = x;
                Bounds.Y = y;
                Bounds.Width = width;
                Bounds.Height = height;

                Origin.X = width / 2f;
                Origin.Y = height / 2f;

                //Position = Origin;

                _updateMatrixes = true;
                _updateProjection = true;
            }
        }

        public void SetPosition(int x, int y)
        {
            if (Position.X != x || Position.Y != y)
            {
                Position.X = x;
                Position.Y = y;

                _updateMatrixes = true;
            }
        }

        public void SetPositionOffset(int x, int y)
        {
            SetPosition(Position.X + x, Position.Y + y);
        }

        public Viewport GetViewport()
        {
            return new Viewport(Bounds.X, Bounds.Y, Bounds.Width, Bounds.Height);
        }

        public void Update()
        {
            UpdateMatrices();
        }

        public Point ScreenToWorld(Point point)
        {
            UpdateMatrices();

            Transform(ref point, ref _inverseTransform, out point);

            return point;
        }

        public Point WorldToScreen(Point point)
        {
            UpdateMatrices();

            Transform(ref point, ref _transform, out point);

            return point;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Transform(ref Point position, ref Matrix matrix, out Point result)
        {
            float x = position.X * matrix.M11 + position.Y * matrix.M21 + matrix.M41;
            float y = position.X * matrix.M12 + position.Y * matrix.M22 + matrix.M42;
            result.X = (int) x;
            result.Y = (int) y;
        }

        public Point MouseToWorldPosition()
        {
            Point mouse = Mouse.Position;

            mouse.X -= Bounds.X;
            mouse.Y -= Bounds.Y;

            return ScreenToWorld(mouse);
        }

        private void UpdateMatrices()
        {
            if (!_updateMatrixes)
            {
                return;
            }


            Matrix temp;

            Matrix.CreateTranslation(-Origin.X, -Origin.Y, 0f, out _transform);

            float zoom = 1f / Zoom;

            if (zoom != 1f)
            {
                Matrix.CreateScale(zoom, zoom, 1f, out temp);
                Matrix.Multiply(ref _transform, ref temp, out _transform);
            }

            Matrix.CreateTranslation(Origin.X, Origin.Y, 0f, out temp);
            Matrix.Multiply(ref _transform, ref temp, out _transform);


            Matrix.Invert(ref _transform, out _inverseTransform);

            _updateMatrixes = false;
        }
    }
}