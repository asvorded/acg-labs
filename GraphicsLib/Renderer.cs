using GraphicsLib.Types;
using Lab1;
using System.Collections;
using System.Diagnostics;
using System.Numerics;
using System.Windows.Media.Imaging;

namespace GraphicsLib
{
    public class Renderer
    {
        public Camera Camera { get; set; }
        public WriteableBitmap Bitmap { get; set; }
        private Vector4[] cameraSpaceBuffer;
        private int bufferLength;
        private Vector4[] projectionBuffer;
        private BitArray cullingArray;

        public Renderer(Camera camera, WriteableBitmap bitmap)
        {
            Camera = camera;
            Bitmap = bitmap;
            cameraSpaceBuffer = [];
            bufferLength = 0;
            projectionBuffer = [];
            cullingArray = new BitArray(0);
        }
        private void ResizeBuffer(Obj obj)
        {
            int vertexCount = obj.vertices.Count;
            if(bufferLength < vertexCount )
            {
                bufferLength = vertexCount;
                cameraSpaceBuffer = new Vector4[bufferLength];
                projectionBuffer = new Vector4[bufferLength];
                cullingArray = new BitArray(bufferLength);
            }
        }
        public void RenderCarcass(Obj obj)
        {
            if (obj == null)
                return;
            ResizeBuffer(obj);
            //Преобразование в пространство камеры
            Matrix4x4 view = Camera.ViewMatrix;
            //Преобразование в пространство проекции
            int width = Bitmap.PixelWidth;
            int height = Bitmap.PixelHeight;
            float aspectRatio = (float)width / height;
            float fovVertical = MathF.PI / 3 / aspectRatio;
            float nearPlaneDistance = 0.1f;
            float farPlaneDistance = float.PositiveInfinity;
            float zCoeff = (float.IsPositiveInfinity(farPlaneDistance) ? -1f : farPlaneDistance / (nearPlaneDistance - farPlaneDistance));
            Matrix4x4 projection = new Matrix4x4(1 / MathF.Tan(fovVertical * 0.5f) / aspectRatio, 0, 0, 0,
                                                 0, 1 / MathF.Tan(fovVertical * 0.5f), 0, 0,
                                                 0, 0, zCoeff, -1,
                                                 0, 0, zCoeff * nearPlaneDistance, 0);
            //Matrix4x4 projection = Matrix4x4.CreatePerspectiveFieldOfView(fovVertical, aspectRatio, nearPlaneDistance, farPlaneDistance);
            //Преобразование в пространство окна
            float leftCornerX = 0;
            float leftCornerY = 0;
            Matrix4x4 viewPort = new Matrix4x4((float)width / 2, 0, 0, 0,
                                               0, -(float)height / 2, 0, 0,
                                               0, 0, 1, 0,
                                               leftCornerX + (float)width / 2, leftCornerY + (float)height / 2, 0, 1);
            //Matrix4x4 viewPort1 = Matrix4x4.CreateViewport(leftCornerX, leftCornerY, width, height, 0, 1);
            Stopwatch sw = Stopwatch.StartNew();
            /*Matrix4x4 finalTransform = view * projection * viewPort;
            for (int i = 0; i < bufferLength; i++)
            {
                Vector4 v = new Vector4(obj.vertices[i], 1);
                v = Vector4.Transform(v, finalTransform);
                if (v.W < nearPlaneDistance || v.W > farPlaneDistance)
                    cullingArray[i] = true;
                v *= (1 / v.W);
                if ((v.X < 0f || v.X > width) || (v.Y < 0f || v.Y > height))
                   cullingArray[i] = true;
                projectionBuffer[i] = v;
            }
            sw.Stop();
            MessageBox.Show($"finalTransform{sw.ElapsedTicks}");
            sw.Restart();*/
            Matrix4x4 fullProjection = projection * viewPort;
            for (int i = 0; i < bufferLength; i++)
            {
                Vector4 v = new Vector4(obj.vertices[i], 1);
                v = Vector4.Transform(v, view);
                cameraSpaceBuffer[i] = v;
                v = Vector4.Transform(v, fullProjection);
                if (v.W < nearPlaneDistance || v.W > farPlaneDistance)
                    cullingArray[i] = true;
                v *= (1 / v.W);
                if ((v.X < 0f || v.X > width) || (v.Y < 0f || v.Y > height))
                    cullingArray[i] = true;
                projectionBuffer[i] = v;
            }
            sw.Stop();
            Stopwatch stopwatch = Stopwatch.StartNew();
            List<Face> faces = obj.faces;
            for (int i = 0; i < faces.Count; i++)
            {
                Face face = faces[i];
                int[] vIndices = face.vIndices;
                for (int j = 0; j < vIndices.Length; j++)
                {
                    int p1 = vIndices[j];
                    int p2 = vIndices[(j + 1) % vIndices.Length];
                    if (!(cullingArray[p1] && cullingArray[p2]))
                        Bitmap.DrawLine(width, height, new System.Drawing.Point((int)projectionBuffer[p1].X, (int)projectionBuffer[p1].Y),
                            new System.Drawing.Point((int)projectionBuffer[p2].X, (int)projectionBuffer[p2].Y), 0xFFFFFFFF);
                }
            }
            stopwatch.Stop();
        }

    }
}
