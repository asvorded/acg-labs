using GraphicsLib.Types;
using Lab1;
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
            if( obj == null ) 
                return;
            List<Vector3> buffer = obj.vertices.ToList();
            Matrix4x4 view = Camera.ViewMatrix;
            for (int i = 0; i < buffer.Count; i++)
            {
                Vector3 v = buffer[i];                
                v = Vector3.Transform(v, view);
                buffer[i] = v;
            }
            Matrix4x4 projection2 = Matrix4x4.CreatePerspective(Bitmap.PixelWidth, Bitmap.PixelHeight, 1f, 1000f);
            Matrix4x4 projection = Matrix4x4.CreateOrthographic((float)Bitmap.Width, (float)Bitmap.Height, 1f, 10000f);
            for (int i = 0; i < buffer.Count; i++)
            {
                Vector3 v = buffer[i];
                //Matrix4x4 projection = Matrix4x4.CreatePerspectiveFieldOfView(MathF.PI / 2, (float)(Bitmap.Width / Bitmap.Height), 0.1f, 10000);
                //Matrix4x4 x2 = Matrix4x4.CreatePerspectiveFieldOfView(40 * MathF.PI / 180, 1, 0.01f, 100);
                v = Vector3.Transform(v, projection);
                buffer[i] = v;
            }
            Matrix4x4 viewPort = Matrix4x4.CreateViewport(0, 0, Bitmap.PixelWidth, Bitmap.PixelHeight, 0.01f, 200);
            for (int i = 0; i < buffer.Count; i++)
            {
                Vector3 v = buffer[i];               
                v = Vector3.Transform(v, viewPort);
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
                    int p2 = vIndices[(j + 1) % 3];
                    Bitmap.DrawLine(new System.Drawing.Point((int)buffer[p1].X, (int)buffer[p1].Y),
                        new System.Drawing.Point((int)buffer[p2].X, (int)buffer[p2].Y), 0xFFFFFFFF);
                }
            }
        }
    }
}
