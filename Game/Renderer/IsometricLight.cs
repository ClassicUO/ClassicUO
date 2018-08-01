using System;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.Renderer
{
    public sealed class IsometricLight
    {
        private float _direction = 4.12f;
        private float _height = -0.75f;
        private int _overall = 9;
        private int _personal = 9;

        public int Personal
        {
            get => _personal;
            set
            {
                _personal = value;
                Recalculate();
            }
        }

        public int Overall
        {
            get => _overall;
            set
            {
                _overall = value;
                Recalculate();
            }
        }

        public float Direction
        {
            get => _direction;
            set
            {
                _direction = value;
                Recalculate();
            }
        }

        public float Height
        {
            get => _height;
            set
            {
                _height = value;
                Recalculate();
            }
        }

        public float IsometricLevel { get; private set; }
        public Vector3 IsometricDirection { get; private set; }

        private void Recalculate()
        {
            float light = Math.Min(30 - Overall + Personal, 30f);
            light = Math.Max(light, 0);
            IsometricLevel = light / 30;

            _direction = 1.2f;
            IsometricDirection = Vector3.Normalize(new Vector3((float) Math.Cos(_direction), (float) Math.Sin(_direction), 1f));
        }
    }
}