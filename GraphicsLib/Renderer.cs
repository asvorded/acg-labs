using GraphicsLib.Types;
using Lab1;
using System.Collections;
using System.Diagnostics;
using System.Numerics;
using System.Windows.Ink;
using System.Windows.Media.Imaging;

namespace GraphicsLib
{
    public class Renderer
    {
        public Camera Camera { get; set; }
        public WriteableBitmap Bitmap { get; set; }

        public Renderer(Camera camera, WriteableBitmap bitmap)
        {
            Camera = camera;
            Bitmap = bitmap;
        }

        public void RenderCarcass(Obj obj)
        {
            if (obj == null)
                return;
            Vector3[] buffer = [.. obj.vertices];
            BitArray cullingArray = new BitArray(obj.vertices.Count);
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
            //Matrix4x4 viewPort = Matrix4x4.CreateViewport(leftCornerX, leftCornerY, width, height, 0, 1);
            Matrix4x4 finalTransform =  view * projection * viewPort;
            for (int i = 0; i < buffer.Length; i++)
            {
                Vector4 v = new Vector4(buffer[i], 1);
                v = Vector4.Transform(v, finalTransform);
                if (v.W < nearPlaneDistance || v.W > farPlaneDistance)
                    cullingArray[i] = true;
                v *= (1 / v.W);
                if ((v.X < 0f || v.X > width) || (v.Y < 0f || v.Y > height))
                    cullingArray[i] = true;
                buffer[i] = v.AsVector3();
            }
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
                        Bitmap.DrawLine(width, height, new System.Drawing.Point((int)buffer[p1].X, (int)buffer[p1].Y),
                            new System.Drawing.Point((int)buffer[p2].X, (int)buffer[p2].Y), 0xFFFFFFFF);
                }
            }
        }
    
    }
}
