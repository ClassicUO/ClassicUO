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
        private bool _updateProjection = true;


        private float _zoom = 1f;
        private Matrix _projection, _transform, _inverseTransform;


        public Point Position;
        public Point Origin;
        public Rectangle Bounds;


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
                UpdateMatrixes();
                return _transform;
            }
        }

        public Matrix InverseTransformMatrix
        {
            get
            {
                UpdateMatrixes();
                return _inverseTransform;
            }
        }

        public Matrix ViewProjectionMatrix => TransformMatrix /** ProjectionMatrix*/;

        public float Zoom
        {
            get => _zoom;
            set
            {
                if (_zoom != value)
                {
                    _zoom = 1f / value;
                    _updateMatrixes = true;
                }
            }
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

                _updateProjection = true;
                _updateMatrixes = true;
            }
        }

        public void LockCameraTo(GameObject obj)
        {

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
            UpdateMatrixes();
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Transform(ref Point position, ref Matrix matrix, out Point result)
        {
            float x = (position.X * matrix.M11) + (position.Y * matrix.M21) + matrix.M31;
            float y = (position.X * matrix.M12) + (position.Y * matrix.M22) + matrix.M32;
            result.X = (int) x;
            result.Y = (int) y;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Point ScreenToWorld(Point point)
        {
            UpdateMatrixes();

            //Transform(ref point, ref _inverseTransform, out point);

            float x = (((point.X - Bounds.X) / ((float) Bounds.Width)) * 2f) - 1f;
            float y = -((((point.Y - Bounds.Y) / ((float) Bounds.Height)) * 2f) - 1f);

            point.X = (int) Math.Round((x * _inverseTransform.M11) + (y * _inverseTransform.M21) + _inverseTransform.M41);
            point.Y = (int) Math.Round((x * _inverseTransform.M12) + (y * _inverseTransform.M22) + _inverseTransform.M42);

            return point;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Point WorldToScreen(Point point)
        {
            UpdateMatrixes();

            //Transform(ref point, ref _transform, out point);

            float x = ((point.X * _transform.M11) + (point.Y * _transform.M21) + _transform.M41);
            float y = ((point.X * _transform.M12) + (point.Y * _transform.M22) + _transform.M42);

            point.X = (int) Math.Round((((x + 1f) * 0.5f) * Bounds.Width) + Bounds.X);
            point.Y = (int) Math.Round((((-y + 1f) * 0.5f) * Bounds.Height) + Bounds.Y);

            return point;
        }

        public Point MouseToWorldPosition()
        {
            Point mouse = Mouse.Position;
          
            //mouse.X = (int) ((mouse.X - Bounds.X) / Zoom);
            //mouse.Y = (int) ((mouse.Y - Bounds.Y) / Zoom);


            return ScreenToWorld(mouse);
        }

        private void UpdateMatrixes()
        {
            if (!_updateMatrixes)
            {
                return;
            }


            float left = 0;
            float right = Bounds.Width + left;
            float top = 0;
            float bottom = Bounds.Height + top;

            float new_right = (right / _zoom);
            float new_bottom = (bottom / _zoom);

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


            //Matrix temp;

            //_transform = Matrix.CreateTranslation(-Position.X, -Position.Y, 0f);

            //if (_zoom != 1f)
            //{
            //    Matrix.CreateScale(_zoom, _zoom, 1f, out temp);
            //    Matrix.Multiply(ref _transform, ref temp, out _transform);
            //}

            //Matrix.CreateTranslation(Origin.X, Origin.Y, 0f, out temp);
            //Matrix.Multiply(ref _transform, ref temp, out _transform);





            Matrix.Invert(ref _transform, out _inverseTransform);
           
            _updateMatrixes = false;
        }
    }
}
