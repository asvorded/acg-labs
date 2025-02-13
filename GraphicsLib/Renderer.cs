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
        private Vector4[] projectionSpaceBuffer;
        private int bufferLength;
        private Vector4[] projectionBuffer;

        public Renderer(Camera camera, WriteableBitmap bitmap)
        {
            Camera = camera;
            Bitmap = bitmap;
            projectionSpaceBuffer = [];
            bufferLength = 0;
            projectionBuffer = [];
        }
        private void ResizeBuffer(Obj obj)
        {
            int vertexCount = obj.vertices.Count;
            if (bufferLength < vertexCount)
            {
                bufferLength = vertexCount;
                projectionSpaceBuffer = new Vector4[bufferLength];
                projectionBuffer = new Vector4[bufferLength];
            }
        }
        public void RenderCarcass(Obj obj)
        {
            if (obj == null)
                return;
            ResizeBuffer(obj);
            //Преобразование в мировое пространство
            Matrix4x4 worldTransform = obj.transform;
            //Преобразование в пространство камеры
            Matrix4x4 cameraTransform = Camera.ViewMatrix;
            //Преобразование в пространство проекции
            int width = Bitmap.PixelWidth;
            int height = Bitmap.PixelHeight;
            float aspectRatio = (float)width / height;
            float fovVertical = MathF.PI / 3 / aspectRatio;
            float nearPlaneDistance = 1f;
            float farPlaneDistance = float.PositiveInfinity;
            float zCoeff = (float.IsPositiveInfinity(farPlaneDistance) ? -1f : farPlaneDistance / (nearPlaneDistance - farPlaneDistance));
            Matrix4x4 projectionTransform = new Matrix4x4(1 / MathF.Tan(fovVertical * 0.5f) / aspectRatio, 0, 0, 0,
                                                 0, 1 / MathF.Tan(fovVertical * 0.5f), 0, 0,
                                                 0, 0, zCoeff, -1,
                                                 0, 0, zCoeff * nearPlaneDistance, 0);
            //Matrix4x4 projection = Matrix4x4.CreatePerspectiveFieldOfView(fovVertical, aspectRatio, nearPlaneDistance, farPlaneDistance);
            //Преобразование в пространство окна
            float leftCornerX = 0;
            float leftCornerY = 0;
            Matrix4x4 viewPortTransform = new Matrix4x4((float)width / 2, 0, 0, 0,
                                               0, -(float)height / 2, 0, 0,
                                               0, 0, 1, 0,
                                               leftCornerX + (float)width / 2, leftCornerY + (float)height / 2, 0, 1);
            //Matrix4x4 viewPort1 = Matrix4x4.CreateViewport(leftCornerX, leftCornerY, width, height, 0, 1);
            Stopwatch sw = Stopwatch.StartNew();
            Matrix4x4 modelToProjection = worldTransform * cameraTransform * projectionTransform;
            for (int i = 0; i < bufferLength; i++)
            {
                Vector4 v = new(obj.vertices[i], 1);
                v = Vector4.Transform(v, modelToProjection);
                projectionSpaceBuffer[i] = v;
            }
            sw.Stop();
            Stopwatch stopwatch = Stopwatch.StartNew();
            List<Face> faces = obj.faces;
            int facesCount = faces.Count;
            for (int i = 0; i < facesCount; i++)
            {
                Face face = faces[i];
                int[] vIndices = face.vIndices;
                for (int j = 0; j < vIndices.Length; j++)
                {
                    int p0 = vIndices[j];
                    int p1 = vIndices[(j + 1) % vIndices.Length];
                    Vector4 v0 = projectionSpaceBuffer[p0];
                    Vector4 v1 = projectionSpaceBuffer[p1];
                    if (v0.X > v0.W && v1.X > v1.W)
                        continue;
                    if (v0.X < -v0.W && v1.X < -v1.W)
                        continue;
                    if (v0.Y > v0.W && v1.Y > v1.W)
                        continue;
                    if (v0.Y < -v0.W && v1.Y < -v1.W)
                        continue;
                    if (v0.Z > v0.W && v1.Z > v1.W)
                        continue;
                    if (v0.Z < 0 && v1.Z < 0)
                        continue;
                    uint color = 0xFFFFFFFF;
                    if (v0.Z < 0)
                    {
                        InterpolateV0(ref v0, ref v1);
                        color = 0xFF0000FF;
                    }
                    else if(v1.Z < 0)
                    {
                        InterpolateV0(ref v1, ref v0);
                        color = 0xFF0000FF;
                    }
                    void InterpolateV0(ref Vector4 v0,ref Vector4 v1)
                    {
                        float coeff = (-v0.Z) / (v1.Z - v0.Z);
                        v0 = Vector4.Lerp(v0, v1, coeff);
                    }
                    v0 = Vector4.Transform(v0, viewPortTransform);
                    v0 *= (1 / v0.W);
                    v1 = Vector4.Transform(v1, viewPortTransform);
                    v1 *= (1 / v1.W);
                    Bitmap.DrawLine(width, height, (int)v0.X, (int)v0.Y,
                       (int)v1.X, (int)v1.Y, color);
                }
            }
            stopwatch.Stop();
        }

    }
}
