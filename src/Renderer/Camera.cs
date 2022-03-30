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

using System;
using System.Runtime.CompilerServices;
using ClassicUO.Input;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO.Renderer
{
    class Camera
    {
        private Matrix _transform = Matrix.Identity;
        private Matrix _inverseTransform = Matrix.Identity;
        private bool _updateMatrixes = true;
        private float _lerpZoom;
        private float _zoom;


        public Camera(float minZoomValue = 1f, float maxZoomValue = 1f, float zoomStep = 0.1f)
        {
            ZoomMin = minZoomValue;
            ZoomMax = maxZoomValue;
            ZoomStep = zoomStep;
            Zoom = _lerpZoom = 1f;
        }


        public Rectangle Bounds;

        public Matrix ViewTransformMatrix
        {
            get
            {
                UpdateMatrices();

                return _transform;
            }
        }

        public float ZoomStep { get; private set; }
        public float ZoomMin { get; private set; }
        public float ZoomMax { get; private set; }
        public float Zoom
        {
            get => _zoom;
            set
            {
                _zoom = MathHelper.Clamp(value, ZoomMin, ZoomMax);
                _updateMatrixes = true;
            }
        }



        public void ZoomIn() => Zoom += ZoomStep;

        public void ZoomOut() => Zoom -= ZoomStep;

        public Viewport GetViewport() => new Viewport(Bounds.X, Bounds.Y, Bounds.Width, Bounds.Height);

        public void Update(bool force)
        {
            if (force)
            {
                _updateMatrixes = true;
            }

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

            var origin = new Vector2(Bounds.Width * 0.5f, Bounds.Height * 0.5f);

            Matrix.CreateTranslation(-origin.X, -origin.Y, 0f, out _transform);

            CalculateLerpZoom();

            Matrix.CreateScale(_lerpZoom, _lerpZoom, 1f, out temp);
            Matrix.Multiply(ref _transform, ref temp, out _transform);

            Matrix.CreateTranslation(origin.X, origin.Y, 0f, out temp);
            Matrix.Multiply(ref _transform, ref temp, out _transform);


            Matrix.Invert(ref _transform, out _inverseTransform);

            _updateMatrixes = false;
        }

        private void CalculateLerpZoom()
        {
            float zoom = 1f / Zoom;

            const float FADE_TIME = 12.0f;
            const float SMOOTHING_FACTOR = (1.0f / FADE_TIME) * 60.0f;

            _lerpZoom = zoom; // MathHelper.Lerp(_lerpZoom, zoom, SMOOTHING_FACTOR * Time.Delta);
        }
    }
}