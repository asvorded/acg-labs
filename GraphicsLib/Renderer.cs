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
            List<Vector3> buffer = obj.vertices.ToList();
            for (int i = 0; i < buffer.Count; i++)
            {
                Vector3 v = buffer[i];
                Matrix4x4 view = Matrix4x4.CreateLookAt(new Vector3(0, 2000, 2000), Vector3.Zero, -Vector3.UnitY);
                v = Vector3.Transform(v, view);
                buffer[i] = v;
            }
            for (int i = 0; i < buffer.Count; i++)
            {
                Vector3 v = buffer[i];
                Matrix4x4 projection = Matrix4x4.CreatePerspectiveFieldOfView((float)(Math.PI / 4), 16 / 9, 0.1f, 10000);
                v = Vector3.Transform(v, projection);
                buffer[i] = v;
            }
            for (int i = 0; i < buffer.Count; i++)
            {
                Vector3 v = buffer[i];
                Matrix4x4 viewPort = Matrix4x4.CreateViewport(0, 0, 1920, 1080, 0, 200);
                v = Vector3.Transform(v, viewPort);
                buffer[i] = v;
            }
            List<Face> faces = obj.faces;
            for (int i = 0; i < faces.Count; i++)
            {
                Face face = faces[i];
                int[] vIndices = face.vIndices;
                for(int j = 0; j < vIndices.Length; j++)
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
