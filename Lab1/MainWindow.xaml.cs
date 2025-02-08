using GraphicsLib;
using GraphicsLib.Types;
using Microsoft.Win32;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
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

        private Obj? obj;
        private Camera camera;
        private Point oldPos;

        public MainWindow() {
            InitializeComponent();

            ofd = new OpenFileDialog {
                 CheckFileExists = true,
                 CheckPathExists = true,
                 Multiselect = false,
                 ValidateNames = true,
                 Filter = "OBJ files (*.obj)|*.obj|GLTF files(*.gltf)|*.gltf|All files (*.*)|*.*"
            };
            ofd.FileOk += OnFileOpened;
            Height = SystemParameters.PrimaryScreenHeight / 1.25;
            Width = SystemParameters.PrimaryScreenWidth / 1.25;
            camera = new Camera();
            #if DEBUG
                DebugPanel.Visibility = Visibility.Visible;
            #else
                DebugPanel.Visibility = Visibility.Collapsed;
            #endif
        }

        private void OnFileOpened(object? sender, CancelEventArgs e) {
            fileName.Text = string.Join(' ', Resources["fileString"].ToString(), ofd.FileName);
            try
            {
                using FileStream fileStream = new(ofd.FileName, FileMode.Open);
                if (System.IO.Path.GetExtension(ofd.FileName).Equals(".obj"))
                    obj = Parser.ParseObjFile(fileStream);
                else
                    obj = Parser.ParseGltfFile(fileStream, System.IO.Path.GetDirectoryName(ofd.FileName));
                Draw();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void Draw() {
            WriteableBitmap bitmap = new WriteableBitmap(
                ((int)canvas.ActualWidth), ((int)canvas.ActualHeight), 96, 96, PixelFormats.Bgra32, null);
            Renderer renderer = new Renderer(camera, bitmap);
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            bitmap.Lock();
            //for (int i = 0; i < 50; i++)
            //{
            if (obj != null)
            {
                renderer.RenderCarcass(obj);
            }
            //});
            bitmap.AddDirtyRect(new Int32Rect(0, 0, bitmap.PixelWidth, bitmap.PixelHeight));
            bitmap.Unlock();
            stopwatch.Stop();
            DebugPanel.Text = stopwatch.ElapsedMilliseconds.ToString();
            canvas.Child = new Image { Source = bitmap };
        }

        private void ButtonOpenFile_Click(object sender, RoutedEventArgs e) {
            ofd.ShowDialog();
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e) {
            Mouse.Capture(canvas);
            oldPos = Mouse.GetPosition(canvas);
        }

        private void Window_MouseMove(object sender, MouseEventArgs e) {
            if (e.LeftButton == MouseButtonState.Pressed && Mouse.Captured == canvas && oldPos.X != -1) {
                Point newPos = Mouse.GetPosition(canvas);
                float dx = (float)(newPos.X - oldPos.X);
                float dy = (float)(newPos.Y - oldPos.Y);
                camera.RotateAroundTargetHorizontal((float)(-dx * MathF.PI / canvas.ActualWidth));
                camera.RotateAroundTargetVertical((float)(-dy * MathF.PI / canvas.ActualHeight));
                Draw();
                oldPos = newPos;
            }
            else
            {
                oldPos = new Point(-1 , -1);
            }
        }

        private void Window_MouseUp(object sender, MouseButtonEventArgs e) {
            Mouse.Capture(canvas, CaptureMode.None);
            oldPos = new Point(-1, -1);
        }
        private void Window_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            float dz = (float)e.Delta;
            camera.MoveTowardTarget(dz * 0.0002f * camera.Distance);
            Draw();
        }
    }
}