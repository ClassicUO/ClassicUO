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

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Runtime.CompilerServices;

namespace ClassicUO.Renderer
{
    public class Camera
    {
        private const float MAX_PEEK_DISTANCE = 250f;
        private const float MIN_PEEK_SPEED = 0.01f;
        private const float PEEK_TIME_FACTOR = 5;

        private Matrix _transform = Matrix.Identity;
        private Matrix _inverseTransform = Matrix.Identity;
        private bool _updateMatrixes = true;
        private float _lerpZoom;
        private float _zoom;
        private Vector2 _lerpOffset;


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



        public void ZoomIn() => Zoom -= ZoomStep;

        public void ZoomOut() => Zoom += ZoomStep;

        public Viewport GetViewport() => new Viewport(Bounds.X, Bounds.Y, Bounds.Width, Bounds.Height);

        public Vector2 Offset => _lerpOffset;

        public bool PeekingToMouse;

        public bool PeekBackwards;

        private float _timeDelta = 0;
        private Point _mousePos;

        public void Update(bool force, float timeDelta, Point mousePos)
        {
            if (force)
            {
                _updateMatrixes = true;
            }

            _timeDelta = timeDelta;
            _mousePos = mousePos;

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
            result.X = (int)x;
            result.Y = (int)y;
        }

        public Point MouseToWorldPosition()
        {
            Point mouse = _mousePos;

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

            CalculatePeek(origin);

            Matrix.CreateTranslation(origin.X - _lerpOffset.X, origin.Y - _lerpOffset.Y, 0f, out temp);
            Matrix.Multiply(ref _transform, ref temp, out _transform);


            Matrix.Invert(ref _transform, out _inverseTransform);

            _updateMatrixes = false;
        }

        private void CalculateLerpZoom()
        {
            float zoom = 1f / Zoom;

            //const float FADE_TIME = 12.0f;
            //const float SMOOTHING_FACTOR = (1.0f / FADE_TIME) * 60.0f;

            _lerpZoom = zoom; // MathHelper.Lerp(_lerpZoom, zoom, SMOOTHING_FACTOR * Time.Delta);
        }

        private void CalculatePeek(Vector2 origin)
        {
            Vector2 target_offset = new Vector2();

            if (PeekingToMouse)
            {
                Vector2 target = new Vector2(_mousePos.X - Bounds.X, _mousePos.Y - Bounds.Y);

                if (PeekBackwards)
                {
                    target.X = 2 * origin.X - target.X;
                    target.Y = 2 * origin.Y - target.Y;
                }

                target_offset = target - origin;
                float length = target_offset.Length();

                if (length > 0)
                {
                    float length_factor = Math.Min(length / (Bounds.Height >> 1), 1f);
                    target_offset = Vector2.Normalize(target_offset) * Utility.Easings.OutQuad(length_factor) * MAX_PEEK_DISTANCE / Zoom;
                }
            }

            float dist = Vector2.Distance(target_offset, _lerpOffset);

            if (dist > 1f)
            {
                float time = Math.Max(Utility.Easings.OutQuart(dist / MAX_PEEK_DISTANCE) * _timeDelta * PEEK_TIME_FACTOR, MIN_PEEK_SPEED);
                _lerpOffset = Vector2.Lerp(_lerpOffset, target_offset, time);
            }
            else
            {
                _lerpOffset = target_offset;
            }
        }
    }
}