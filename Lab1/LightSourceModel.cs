using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;
using System.Windows.Media;

namespace Lab1
{
    public class LightSourceModel : INotifyPropertyChanged
    {
        private string _name;
        private Color _color;
        private string _type;
        private double _intensity;
        private Point3D _position;
        private Vector3D _direction;

        public string Name
        {
            get => _name;
            set
            {
                _name = value;
                OnPropertyChanged(nameof(Name));
            }
        }

        public Color Color
        {
            get => _color;
            set
            {
                _color = value;
                OnPropertyChanged(nameof(Color));
            }
        }

        public string Type
        {
            get => _type;
            set
            {
                _type = value;
                OnPropertyChanged(nameof(Type));
            }
        }

        public double Intensity
        {
            get => _intensity;
            set
            {
                _intensity = value;
                OnPropertyChanged(nameof(Intensity));
            }
        }

        public Point3D Position
        {
            get => _position;
            set
            {
                _position = value;
                OnPropertyChanged(nameof(Position));
            }
        }

        public Vector3D Direction
        {
            get => _direction;
            set
            {
                _direction = value;
                OnPropertyChanged(nameof(Direction));
            }
        }

        public bool IsExpanded { get; set; }
        public bool IsSelected { get; set; }

        public LightSourceModel()
        {
            Color = Colors.White;
            Name = "New light";
            Type = "Точечный";
            Intensity = 1.0;
            Position = new Point3D(0, 0, 0);
            Direction = new Vector3D(0, 0, 0);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
