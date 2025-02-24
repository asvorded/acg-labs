using GraphicsLib;
using GraphicsLib.Types;
using Microsoft.Win32;
using Newtonsoft.Json;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Numerics;
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
    public partial class MainWindow : Window {
        private readonly OpenFileDialog ofd;

        private static Obj? obj;
        private static Camera camera;
        private Renderer renderer;
        private Point oldPos;

        static MainWindow() {
            camera = new Camera();
        }

        public MainWindow() {
            InitializeComponent();

            ofd = new OpenFileDialog {
                CheckFileExists = true,
                CheckPathExists = true,
                Multiselect = false,
                ValidateNames = true,
                Filter = "GLTF files(*.gltf)|*.gltf|OBJ files (*.obj)|*.obj|All files (*.*)|*.*"
            };
            ofd.FileOk += OnFileOpened;
            Height = SystemParameters.PrimaryScreenHeight / 1.25;
            Width = SystemParameters.PrimaryScreenWidth / 1.25;
            renderer = new Renderer(camera);
#if DEBUG
            DebugPanel.Visibility = Visibility.Visible;
#else
                DebugPanel.Visibility = Visibility.Visible;
#endif
        }

        private void OnFileOpened(object? sender, CancelEventArgs e) {
            fileName.Text = string.Join(' ', Resources["fileString"].ToString(), ofd.FileName);
            try {
                if (System.IO.Path.GetExtension(ofd.FileName).Equals(".obj"))
                    obj = Parser.ParseObjFile(ofd.FileName);
                else
                    obj = Parser.ParseGltfFile(ofd.FileName);
                obj.Transformation.Reset();
                Draw();
            } catch (Exception ex) {
                MessageBox.Show(ex.Message);
            }
        }

        private void Draw() {
            WriteableBitmap bitmap = new WriteableBitmap(
                ((int)canvas.ActualWidth), ((int)canvas.ActualHeight), 96, 96, PixelFormats.Bgra32, null);
            renderer.Bitmap = bitmap;
            Stopwatch stopwatch = new();
            stopwatch.Start();
            bitmap.Lock();
            if (obj != null) {
                renderer.RenderSolid(obj);
                //renderer.RenderCarcass(obj);
                
            } else {
                
            }
            bitmap.AddDirtyRect(new Int32Rect(0, 0, bitmap.PixelWidth, bitmap.PixelHeight));
            bitmap.Unlock();
            stopwatch.Stop();
            DebugPanel.Text = stopwatch.ElapsedMilliseconds.ToString();
            canvas.Child = new Image { Source = bitmap };
            renderer.Bitmap = null;
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
            } else {
                oldPos = new Point(-1, -1);
            }
        }

        private void Window_MouseUp(object sender, MouseButtonEventArgs e) {
            Mouse.Capture(canvas, CaptureMode.None);
            oldPos = new Point(-1, -1);
        }

        private void Window_MouseWheel(object sender, MouseWheelEventArgs e) {
            float dz = (float)e.Delta;
            float step = Keyboard.Modifiers == ModifierKeys.Control ? 0.002f : 0.0005f;
            camera.MoveTowardTarget(dz * step * camera.Distance);
            Draw();
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e) {
            Draw();
        }

        private void canvas_GotMouseCapture(object sender, MouseEventArgs e) { }

        private void canvas_LostMouseCapture(object sender, MouseEventArgs e) { }

        private string mode = "Move";

        private void RadioButton_Checked(object sender, RoutedEventArgs e) {
            mode = ((RadioButton)sender).Content.ToString()!;
        }

        private static float speed = 0.5f;
        private Dictionary<Key, Action> moveActions = new() {
            {
                Key.Left, () => { obj!.Transformation.Offset.X += speed; } 
            },
            {
                Key.Right, () => { obj!.Transformation.Offset.X -= speed; }
            },
            { 
                Key.Up, () => {
                    if ((Keyboard.Modifiers & ModifierKeys.Shift) != 0)
                        obj!.Transformation.Offset.Z += speed;
                    else
                        obj!.Transformation.Offset.Y += speed;
                }
            },
            {
                Key.Down, () => {
                    if ((Keyboard.Modifiers & ModifierKeys.Shift) != 0)
                        obj!.Transformation.Offset.Z -= speed;
                    else
                        obj!.Transformation.Offset.Y -= speed;
                }
            },
            {
                Key.OemPlus, MakeLarger
            },
            {
                Key.OemMinus, MakeSmaller
            }
        };

        private Dictionary<Key, Action> rotateActions = new() {
            {
                Key.Up, () => { obj!.Transformation.AngleX += speed; }
            },
            {
                Key.Down, () => { obj!.Transformation.AngleX -= speed; }
            },
            {
                Key.Left, () => {
                    if ((Keyboard.Modifiers & ModifierKeys.Shift) != 0)
                        obj!.Transformation.AngleY += speed;
                    else
                        obj!.Transformation.AngleZ += speed;
                }
            },
            {
                Key.Right, () => {
                    if ((Keyboard.Modifiers & ModifierKeys.Shift) != 0)
                        obj!.Transformation.AngleY -= speed;
                    else
                        obj!.Transformation.AngleZ -= speed;
                }
            },
            {
                Key.OemPlus, MakeLarger
            },
            {
                Key.OemMinus, MakeSmaller
            }
        };

        private static void MakeLarger() {
            obj!.Transformation.Scale += speed / 10.0f;
        }

        private static void MakeSmaller() {
            obj!.Transformation.Scale -= speed / 10.0f;
        }

        private void canvas_KeyDown(object sender, KeyEventArgs e) {
            Dictionary<Key, Action> handlers = moveActions;
            if (mode == "Rotate") {
                handlers = rotateActions;
            }

            if (handlers.TryGetValue(e.Key, out Action? action)) {
                speed = (Keyboard.Modifiers & ModifierKeys.Control) != 0 ? 1f : 0.25f;
                if (mode == "Move") {
                    speed *= camera.Distance / 500;
                }
                action();
                Draw();
            }
        }
    }
}