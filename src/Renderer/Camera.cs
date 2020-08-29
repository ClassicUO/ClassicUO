using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using ClassicUO.Game.GameObjects;
using ClassicUO.Input;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO.Renderer
{
    internal class Camera
    {
        private bool _updateMatrixes = true;
        private Matrix _transform, _inverseTransform;
        private float[] _cameraZoomValues = new float[1] { 1f };
        private int _zoomIndex;


        public Point Position;
        public Point Origin;
        public Rectangle Bounds;

        
       
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
                    ZoomIndex = (int) (value * _cameraZoomValues.Length) - _cameraZoomValues.Length / 2 - 1;
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

                Origin.X = width / 2;
                Origin.Y = height / 2;

                _updateMatrixes = true;
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
            return new Viewport
            (
                Bounds.X,
                Bounds.Y,
                Bounds.Width,
                Bounds.Height
            );
        }
        public void Update()
        {
            UpdateMatrices();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Point ScreenToWorld(Point point)
        {
            UpdateMatrices();

            float x = (((point.X - Bounds.X) / ((float) Bounds.Width)) * 2f) - 1f;
            float y = -((((point.Y - Bounds.Y) / ((float) Bounds.Height)) * 2f) - 1f);

            point.X = (int) Math.Round((x * _inverseTransform.M11) + (y * _inverseTransform.M21) + _inverseTransform.M41);
            point.Y = (int) Math.Round((x * _inverseTransform.M12) + (y * _inverseTransform.M22) + _inverseTransform.M42);

            return point;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Point WorldToScreen(Point point)
        {
            UpdateMatrices();

            float x = ((point.X * _transform.M11) + (point.Y * _transform.M21) + _transform.M41);
            float y = ((point.X * _transform.M12) + (point.Y * _transform.M22) + _transform.M42);

            point.X = (int) Math.Round((((x + 1f) * 0.5f) * Bounds.Width) + Bounds.X);
            point.Y = (int) Math.Round((((-y + 1f) * 0.5f) * Bounds.Height) + Bounds.Y);

            return point;
        }

        public Point MouseToWorldPosition()
        {
            return ScreenToWorld(Mouse.Position);
        }

        private void UpdateMatrices()
        {
            if (!_updateMatrixes)
            {
                return;
            }

            float left = 0;
            float right = Bounds.Width + left;
            float top = 0;
            float bottom = Bounds.Height + top;

            float new_right = (right * Zoom);
            float new_bottom = (bottom * Zoom);

            left = -(new_right - right);
            top = -(new_bottom - bottom);

            Matrix.CreateOrthographicOffCenter
            (
                left,
                new_right,
                new_bottom,
                top,
                0,
                1,
                out _transform
            );


            Matrix.Invert(ref _transform, out _inverseTransform);
           
            _updateMatrixes = false;
        }
    }
}
