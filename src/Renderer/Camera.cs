using System.Runtime.CompilerServices;
using ClassicUO.Input;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO.Renderer
{
    internal class Camera
    {
        private bool _updateMatrixes = true, _updateProjection = true;
        private Matrix _transform = Matrix.Identity, _inverseTransform = Matrix.Identity;
        private Matrix _projection;
        private float[] _cameraZoomValues = new float[1] { 1f };
        private int _zoomIndex;


        public Point Position;
        public Vector2 Origin;
        public Rectangle Bounds;


        public Matrix ViewTransformMatrix => TransformMatrix * ProjectionMatrix;

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
                    //float zoom = MathHelper.Clamp(value, _cameraZoomValues[0], _cameraZoomValues[_cameraZoomValues.Length - 1]);

                    //if (zoom == 0)
                    //{

                    //}

                    for (_zoomIndex = 0; _zoomIndex < _cameraZoomValues.Length; ++_zoomIndex)
                    {
                        if (_cameraZoomValues[_zoomIndex] == value)
                        {
                            _updateMatrixes = true;
                            break;
                        }
                    }

                    //ZoomIndex = (int) (value * _cameraZoomValues.Length) - _cameraZoomValues.Length / 2 - 1;
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

            point.X -= Bounds.X;
            point.Y -= Bounds.Y;

            Transform(ref point, ref _inverseTransform, out point);

            return point;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Point WorldToScreen(Point point)
        {
            UpdateMatrices();

            Transform(ref point, ref _transform, out point);

            return point;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Transform(ref Point position, ref Matrix matrix, out Point result)
        {
            float x = (position.X * matrix.M11) + (position.Y * matrix.M21) + matrix.M41;
            float y = (position.X * matrix.M12) + (position.Y * matrix.M22) + matrix.M42;
            result.X = (int) x;
            result.Y = (int) y;
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
