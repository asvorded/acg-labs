﻿using GraphicsLib;
using GraphicsLib.Types;
using Microsoft.Win32;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Lab1
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly OpenFileDialog ofd;

        private static Obj? obj;
        private static Camera camera;
        private static Scene scene;
        private Renderer renderer;
        private Point oldPos;

        static MainWindow()
        {
            camera = new Camera();
            scene = new Scene(camera);
        }
        public MainWindow()
        {
            InitializeComponent();

            ofd = new OpenFileDialog
            {
                CheckFileExists = true,
                CheckPathExists = true,
                Multiselect = false,
                ValidateNames = true,
                Filter = "GLTF files(*.gltf)|*.gltf|OBJ files (*.obj)|*.obj|All files (*.*)|*.*"
            };
            ofd.FileOk += OnFileOpened;
            Height = SystemParameters.PrimaryScreenHeight / 1.25;
            Width = SystemParameters.PrimaryScreenWidth / 1.25;

            renderer = new Renderer(scene);
#if DEBUG
            DebugPanel.Visibility = Visibility.Visible;
#else
            DebugPanel.Visibility = Visibility.Visible;
#endif
        }

        private void OnFileOpened(object? sender, CancelEventArgs e)
        {
            fileName.Text = string.Join(' ', Resources["fileString"].ToString(), ofd.SafeFileName);

            if (System.IO.Path.GetExtension(ofd.FileName).Equals(".obj"))
                obj = Parser.ParseObjFile(ofd.FileName);
            else
                obj = Parser.ParseGltfFile(ofd.FileName);
            obj.Transformation.Reset();
            scene.Obj = obj;
            Draw();

        }

        private void Draw()
        {
            if (renderer == null)
                return;
            try
            {
                Stopwatch stopwatch = Stopwatch.StartNew();
                WriteableBitmap bitmap = new WriteableBitmap(
                ((int)canvas.ActualWidth), ((int)canvas.ActualHeight), 96, 96, PixelFormats.Bgra32, null);
                renderer.Bitmap = bitmap;

                if (obj != null)
                {
                    if (renderMode == "Flat")
                        renderer.RenderSolid();
                    else if (renderMode == "Smooth")
                        renderer.Render<PhongShader, PhongShader.Vertex>();
                    else
                        renderer.RenderCarcass();

                }
                else
                {

                }
                bitmap.Lock();
                bitmap.AddDirtyRect(new Int32Rect(0, 0, bitmap.PixelWidth, bitmap.PixelHeight));
                bitmap.Unlock();
                stopwatch.Stop();
                DebugPanel.Text = (TimeSpan.TicksPerSecond / stopwatch.ElapsedTicks).ToString() + " fps";
                canvas.Child = new Image { Source = bitmap };
                renderer.Bitmap = null;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void ButtonOpenFile_Click(object sender, RoutedEventArgs e)
        {
            ofd.ShowDialog();
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Mouse.Capture(canvas);
            oldPos = Mouse.GetPosition(canvas);
        }

        private void Window_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed && Mouse.Captured == canvas && oldPos.X != -1)
            {
                Point newPos = Mouse.GetPosition(canvas);
                float dx = (float)(newPos.X - oldPos.X);
                float dy = (float)(newPos.Y - oldPos.Y);
                if ((Keyboard.Modifiers & ModifierKeys.Shift) != 0) {
                    // ....
                } else {
                    camera.RotateAroundTargetHorizontal((float)(-dx * MathF.PI / canvas.ActualWidth));
                    camera.RotateAroundTargetVertical((float)(-dy * MathF.PI / canvas.ActualHeight));
                }
                Draw();
                oldPos = newPos;
            }
            else
            {
                oldPos = new Point(-1, -1);
            }
        }

        private void Window_MouseUp(object sender, MouseButtonEventArgs e)
        {
            Mouse.Capture(canvas, CaptureMode.None);
            oldPos = new Point(-1, -1);
        }

        private void Window_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            float dz = (float)e.Delta;
            float step = Keyboard.Modifiers == ModifierKeys.Control ? 0.002f : 0.0005f;
            camera.MoveTowardTarget(dz * step * camera.Distance);
            Draw();
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Draw();
        }

        private void canvas_GotMouseCapture(object sender, MouseEventArgs e) { }

        private void canvas_LostMouseCapture(object sender, MouseEventArgs e) { }

        private string renderMode = "Flat";

        private void RadioButton_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is RadioButton radioButton && radioButton.IsChecked == true)
            {
                switch (radioButton.GroupName)
                {
                    case "RenderingMode":
                        renderMode = ((RadioButton)sender).Content.ToString()!;
                        Draw();
                        break;
                }
            }

        }

        private static float speed = 0.5f;
        private Dictionary<Key, Action> moveActions = new() {
            {
                Key.Left, () => { if (obj != null) obj.Transformation.Offset.X += speed; }
            },
            {
                Key.Right, () => { if (obj != null) obj!.Transformation.Offset.X -= speed; }
            },
            {
                Key.Up, () => {
                    if ((Keyboard.Modifiers & ModifierKeys.Shift) != 0)
                    {
                        if (obj != null)
                            obj.Transformation.Offset.Z += speed;
                    }
                    else
                    {
                        if (obj != null)
                            obj.Transformation.Offset.Y += speed;
                    }

                }
            },
            {
                Key.Down, () => {
                    if ((Keyboard.Modifiers & ModifierKeys.Shift) != 0)
                    {
                        if (obj != null)
                            obj.Transformation.Offset.Z -= speed;
                    }

                    else
                    {
                        if (obj != null)
                            obj.Transformation.Offset.Y -= speed;
                    }
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

        private static void MakeLarger()
        {
            if(obj != null)
                obj.Transformation.Scale += speed / 10.0f;
        }

        private static void MakeSmaller()
        {
            if (obj != null)
                obj.Transformation.Scale -= speed / 10.0f;
        }

        private void canvas_KeyDown(object sender, KeyEventArgs e)
        {
            if (rotateActions.TryGetValue(e.Key, out Action? action))
            {
                speed = (Keyboard.Modifiers & ModifierKeys.Control) != 0 ? 1f : 0.25f;
                action();
                Draw();
            }
        }

        private void Reset_Click(object sender, RoutedEventArgs e) {
            if (obj == null) {
                return;
            }

            obj.Transformation.Reset();
            Draw();
        }
    }
}