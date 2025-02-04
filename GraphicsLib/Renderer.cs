using GraphicsLib.Types;
using Lab1;
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
            Matrix4x4 view = Camera.ViewMatrix;
            for (int i = 0; i < buffer.Count; i++)
            {
                Vector3 v = buffer[i];
                v = Vector3.Transform(v, view);
                buffer[i] = v;
            }
            Debug.WriteLine($"Point 1 = {buffer[0]}");
            float width = Bitmap.PixelWidth;
            float height = Bitmap.PixelHeight;
            //Matrix4x4 projection = Matrix4x4.CreatePerspective(Bitmap.PixelWidth, Bitmap.PixelHeight, 1f, float.PositiveInfinity);
            Matrix4x4 projection = new Matrix4x4(2 * 1 / width, 0, 0, 0,
                                                 0, 2 * 1 / height, 0, 0,
                                                 0, 0, 1000f / (1f - 1000f), -1,
                                                 0, 0, (1000f - 1f) / (1f - 1000f), 1);
            //projection = Matrix4x4.Transpose(projection);
            //Matrix4x4 projection = Matrix4x4.CreateOrthographic((float)Bitmap.Width, (float)Bitmap.Height, -1f, -1000f);
            for (int i = 0; i < buffer.Count; i++)
            {
                Vector3 v = buffer[i];
                v = Vector3.Transform(v, projection);
                buffer[i] = v;
            }
            Debug.WriteLine($"Point 1 = {buffer[0]}");
            //Matrix4x4 viewPort = Matrix4x4.CreateViewport(0, 0, Bitmap.PixelWidth, Bitmap.PixelHeight, 0, 0);
            Matrix4x4 viewPort = new Matrix4x4(width / 2, 0, 0, 0,
                                               0, height / 2, 0, 0,
                                               0, 0, 1, 0,
                                               width / 2, height / 2, 0, 1);
            for (int i = 0; i < buffer.Count; i++)
            {
                Vector3 v = buffer[i];
                v = Vector3.Transform(v, viewPort);
                buffer[i] = v;
            }
            Debug.WriteLine($"Point 1 = {buffer[0]}");
            List<Face> faces = obj.faces;
            for (int i = 0; i < faces.Count; i++)
            {
                Face face = faces[i];
                int[] vIndices = face.vIndices;
                for (int j = 0; j < vIndices.Length; j++)
                {
                    int p1 = vIndices[j];
                    int p2 = vIndices[(j + 1) % vIndices.Length];
                    Bitmap.DrawLine(new System.Drawing.Point((int)buffer[p1].X, (int)buffer[p1].Y),
                        new System.Drawing.Point((int)buffer[p2].X, (int)buffer[p2].Y), 0xFFFFFFFF);
                }
            }
        }
    }
}
