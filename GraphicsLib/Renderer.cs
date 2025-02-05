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

        public Renderer(Camera camera, WriteableBitmap bitmap)
        {
            Camera = camera;
            Bitmap = bitmap;
        }

        public void RenderCarcass(Obj obj)
        {
            if (obj == null)
                return;
            List<Vector3> buffer = obj.vertices.ToList();
            BitArray cullingArray = new BitArray(buffer.Count);
            //Преобразование в пространство камеры
            Matrix4x4 view = Camera.ViewMatrix;
            for (int i = 0; i < buffer.Count; i++)
            {
                Vector3 v = buffer[i];
                v = Vector3.Transform(v, view);
                buffer[i] = v;
            }
            //Преобразование в пространство проекции
            float width = Bitmap.PixelWidth;
            float height = Bitmap.PixelHeight;
            float aspectRatio = width / height;
            float fovVertical = MathF.PI / 3 / aspectRatio;
            float nearPlaneDistance = 0.1f;
            float farPlaneDistance = 3000f;
            float zCoeff = (float.IsPositiveInfinity(farPlaneDistance) ? -1f : farPlaneDistance / (nearPlaneDistance - farPlaneDistance));
            Matrix4x4 projection = new Matrix4x4(1/MathF.Tan(fovVertical * 0.5f) / aspectRatio, 0, 0, 0,
                                                 0, 1 / MathF.Tan(fovVertical * 0.5f), 0, 0,
                                                 0, 0, zCoeff, -1,
                                                 0, 0, zCoeff * nearPlaneDistance, 0);
            //Matrix4x4 projection = Matrix4x4.CreatePerspectiveFieldOfView(fovVertical, aspectRatio, nearPlaneDistance, farPlaneDistance);
            for (int i = 0; i < buffer.Count; i++)
            {
                Vector4 v = new Vector4(buffer[i], 1);
                v = Vector4.Transform(v, projection);
                if (v.W < nearPlaneDistance || v.W > farPlaneDistance)
                    cullingArray[i] = true;
                v /=  v.W;
                buffer[i] = v.AsVector3();
            }
            //Преобразование в пространство окна
            float leftCornerX = 0;
            float leftCornerY = 0;
            Matrix4x4 viewPort = new Matrix4x4(width / 2, 0, 0, 0,
                                               0, -height / 2, 0, 0,
                                               0, 0, 1, 0,
                                               leftCornerX + width / 2, leftCornerY + height / 2, 0, 1);
            //Matrix4x4 viewPort = Matrix4x4.CreateViewport(leftCornerX, leftCornerY, width, height, 0, 1);
            for (int i = 0; i < buffer.Count; i++)
            {
                Vector3 v = buffer[i];
                v = Vector3.Transform(v, viewPort);
                if ((v.X < 0f || v.X > width) || (v.Y < 0f || v.Y > height)) 
                    cullingArray[i] = true;
                buffer[i] = v;
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
                        Bitmap.DrawLine(new System.Drawing.Point((int)buffer[p1].X, (int)buffer[p1].Y),
                            new System.Drawing.Point((int)buffer[p2].X, (int)buffer[p2].Y), 0xFFFFFFFF);
                }
            }
        }
    }
}
