using Microsoft.Win32;
using System.ComponentModel;
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
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly OpenFileDialog ofd;

        public MainWindow() {
            InitializeComponent();

            ofd = new OpenFileDialog {
                 CheckFileExists = true,
                 CheckPathExists = true,
                 Multiselect = false,
                 Filter = "OBJ files (*.obj)|*.obj"
             };
            ofd.FileOk += OnFileOpened;

            Height = SystemParameters.PrimaryScreenHeight / 1.25;
            Width = SystemParameters.PrimaryScreenWidth / 1.25;
        }

        private void OnFileOpened(object? sender, CancelEventArgs e) {
            fileName.Text = string.Join(' ', Resources["fileString"].ToString(), ofd.FileName);

            WriteableBitmap bitmap = new WriteableBitmap(
                ((int)canvas.ActualWidth), ((int)canvas.ActualHeight), 96, 96, PixelFormats.Bgra32, null);

            bitmap.SetPixel(100, 100, 0xFFFFFFFF);
            bitmap.SetPixel(101, 101, 0xFFFFFFFF);

            canvas.Child = new Image { Source = bitmap };
        }

        private void ButtonOpenFile_Click(object sender, RoutedEventArgs e) {
            ofd.ShowDialog();
        }


    }
}