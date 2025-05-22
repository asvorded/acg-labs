using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Lab1
{
    public abstract class LightSourceModel : INotifyPropertyChanged
    {
        private Vector3Model _color = new Vector3Model();
        private float _intensity;
        private int shadowMapSize;

        public int ShadowMapSize
        {
            get => shadowMapSize;
            set { shadowMapSize = value; OnPropertyChanged(nameof(ShadowMapSize)); }
        }
        public Vector3Model Color
        {
            get => _color;
            set
            {
                if (_color != null)
                    _color.VectorChanged -= ColorChangedHandler;

                _color = value;

                if (_color != null)
                    _color.VectorChanged += ColorChangedHandler;

                OnPropertyChanged(nameof(Color));
            }
        }
        public float Intensity
        {
            get => _intensity;
            set { _intensity = value; OnPropertyChanged(nameof(Intensity)); }
        }
        protected LightSourceModel()
        {
            Color = new Vector3Model(1,1,1);
            Intensity = 1;
            ShadowMapSize = 1024;
        }
        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        private void ColorChangedHandler(object? sender, EventArgs e) =>
                OnPropertyChanged(nameof(Color));
    }

    public class PointLightSourceModel : LightSourceModel
    {
        private Vector3Model _position = new Vector3Model();

        public Vector3Model Position
        {
            get => _position;
            set
            {
                if (_position != null)
                    _position.VectorChanged -= PositionChangedHandler;

                _position = value;

                if (_position != null)
                    _position.VectorChanged += PositionChangedHandler;

                OnPropertyChanged(nameof(Position));
            }
        }
        public PointLightSourceModel()
        {
            Position = new Vector3Model();
        }
        private void PositionChangedHandler(object? sender, EventArgs e) =>
            OnPropertyChanged(nameof(Position));
    }

    public class DirectionalLightSourceModel : LightSourceModel
    {
        private Vector3Model _direction = new Vector3Model();

        public Vector3Model Direction
        {
            get => _direction;
            set
            {
                if (_direction != null)
                    _direction.VectorChanged -= DirectionChangedHandler;

                _direction = value;

                if (_direction != null)
                    _direction.VectorChanged += DirectionChangedHandler;

                OnPropertyChanged(nameof(Direction));
            }
        }
        private float _coverSize;

        public float CoverSize
        {
            get => _coverSize;
            set { _coverSize = value; OnPropertyChanged(nameof(CoverSize)); }
        }
        public DirectionalLightSourceModel()
        {
            Direction = new Vector3Model(1,0,0);
            CoverSize = 100f;
        }
        private void DirectionChangedHandler(object? sender, EventArgs e) =>
            OnPropertyChanged(nameof(Direction));
    }

    public class SpotLightSourceModel : LightSourceModel
    {
        private Vector3Model _position = new Vector3Model();
        private Vector3Model _direction = new Vector3Model();
        private float _cutOff;
        private float _outerCutOff;

        public Vector3Model Position
        {
            get => _position;
            set
            {
                if (_position != null)
                    _position.VectorChanged -= PositionChangedHandler;

                _position = value;

                if (_position != null)
                    _position.VectorChanged += PositionChangedHandler;

                OnPropertyChanged(nameof(Position));
            }
        }
        public SpotLightSourceModel()
        {
            Direction = new Vector3Model(1, 0, 0);
            Position = new Vector3Model();
            CutOff = 60;
            OuterCutOff = 80;
        }

        public Vector3Model Direction
        {
            get => _direction;
            set
            {
                if (_direction != null)
                    _direction.VectorChanged -= DirectionChangedHandler;

                _direction = value;

                if (_direction != null)
                    _direction.VectorChanged += DirectionChangedHandler;

                OnPropertyChanged(nameof(Direction));
            }
        }

        public float CutOff
        {
            get => _cutOff;
            set { _cutOff = value; OnPropertyChanged(nameof(CutOff)); }
        }

        public float OuterCutOff
        {
            get => _outerCutOff;
            set { _outerCutOff = value; OnPropertyChanged(nameof(OuterCutOff)); }
        }

        private void PositionChangedHandler(object? sender, EventArgs e) =>
            OnPropertyChanged(nameof(Position));

        private void DirectionChangedHandler(object? sender, EventArgs e) =>
            OnPropertyChanged(nameof(Direction));
    }

}
