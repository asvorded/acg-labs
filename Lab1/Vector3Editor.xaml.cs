using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Lab1
{
    public class Vector3Model : INotifyPropertyChanged
    {
        private float x, y, z;

        public float X { get => x; set { x = value; OnPropertyChanged(nameof(X)); OnVectorChanged(); } }
        public float Y { get => y; set { y = value; OnPropertyChanged(nameof(Y)); OnVectorChanged(); } }
        public float Z { get => z; set { z = value; OnPropertyChanged(nameof(Z)); OnVectorChanged(); } }
        public Vector3Model()
        {
            X = 0;
            Y = 0;
            Z = 0;
        }
        public Vector3Model(float x, float y, float z)
        {
            X = x;  
            Y = y;
            Z = z;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public event EventHandler? VectorChanged;

        protected void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        private void OnVectorChanged() => VectorChanged?.Invoke(this, EventArgs.Empty);
    }
    public partial class Vector3Editor : UserControl
    {
        public static readonly DependencyProperty VectorProperty =
        DependencyProperty.Register(
            "Vector",
            typeof(Vector3Model),
            typeof(Vector3Editor),
            new FrameworkPropertyMetadata(
                new Vector3Model(),
                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault
            )
        );

        public Vector3Model Vector
        {
            get => (Vector3Model)GetValue(VectorProperty);
            set => SetValue(VectorProperty, value);
        }

        public string Label
        {
            get => (string)GetValue(LabelProperty);
            set => SetValue(LabelProperty, value);
        }

        public static readonly DependencyProperty LabelProperty =
            DependencyProperty.Register(
                "Label",
                typeof(string),
                typeof(Vector3Editor),
                new PropertyMetadata("")
            );

        public Vector3Editor()
        {
            InitializeComponent();
        }
    }
}
