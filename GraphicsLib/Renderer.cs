using GraphicsLib.Primitives;
using GraphicsLib.Types;
using System.Diagnostics;
using System.Numerics;
using System.Windows.Media.Imaging;

namespace GraphicsLib
{
    public class Renderer
    {
        public Camera Camera { get; set; }
        public WriteableBitmap? Bitmap { get; set; }
        private Vector4[] projectionSpaceBuffer;
        private int bufferLength;
        private Zbuffer? zbuffer;

        public Renderer(Camera camera)
        {
            Camera = camera;
            projectionSpaceBuffer = [];
            bufferLength = 0;
            Bitmap = default;
            zbuffer = default;
        }
        private void ResizeBuffer(Obj obj)
        {
            int vertexCount = obj.vertices.Count;
            if (projectionSpaceBuffer.Length < vertexCount)
            {
                projectionSpaceBuffer = new Vector4[vertexCount];
            }
            bufferLength = vertexCount;
        }
        private void ResizeAndClearZBuffer()
        {
            if(Bitmap == null)
            {
                return;
            }
            if (zbuffer == null)
            {
                zbuffer = new Zbuffer(Bitmap.PixelWidth, Bitmap.PixelHeight);
            }
            else
            {
                int width = Bitmap.PixelWidth;
                int height = Bitmap.PixelHeight;
                if (zbuffer.Width != width && zbuffer.Height != height)
                {
                    zbuffer = new Zbuffer(Bitmap.PixelWidth, Bitmap.PixelHeight);
                }
                else
                {
                    zbuffer.Clear();
                }
            }

        }
        public void RenderSolid(Obj obj)
        {
            if (Bitmap == null)
                return;
            if (obj == null)
                return;
            //ResizeBuffer(obj);
            ResizeAndClearZBuffer();
            // Преобразование в мировое пространство
            Matrix4x4 worldTransform = obj.Transformation.Matrix;

            // Преобразование в пространство камеры
            Matrix4x4 cameraTransform = Camera.ViewMatrix;

            Matrix4x4 modelToCamera = worldTransform * cameraTransform;

            // Преобразование в пространство проекции
            int width = Bitmap.PixelWidth;
            int height = Bitmap.PixelHeight;
            float aspectRatio = (float)width / height;
            float fovVertical = MathF.PI / 3 / aspectRatio;
            float nearPlaneDistance = 0.1f;
            float farPlaneDistance = float.PositiveInfinity;
            Matrix4x4 projectionTransform = Matrix4x4.CreatePerspectiveFieldOfView(fovVertical, aspectRatio, nearPlaneDistance, farPlaneDistance);

            // Преобразование в пространство окна
            float leftCornerX = 0;
            float leftCornerY = 0;
            Matrix4x4 viewPortTransform = Matrix4x4.CreateViewport(leftCornerX, leftCornerY, width, height, 0, 1);

            //Matrix4x4 modelToProjection = worldTransform * cameraTransform * projectionTransform;
            for (int i = 0; i < obj.faces.Count; i++)
            {
                Face triangle = obj.faces[i];
                Vector4 p0 = new Vector4(obj.vertices[triangle.vIndices[0]],1);
                Vector4 p1 = new Vector4(obj.vertices[triangle.vIndices[1]], 1);
                Vector4 p2 = new Vector4(obj.vertices[triangle.vIndices[2]], 1);
                p0 = Vector4.Transform(p0, modelToCamera);
                p1 = Vector4.Transform(p1, modelToCamera);
                p2 = Vector4.Transform(p2, modelToCamera);
                Vector4 normal = Vector3.Cross((p2 - p0).AsVector3(), (p1 - p0).AsVector3()).AsVector4();
                float illumination = Vector4.Dot(normal, p0);
                if (illumination <= 0)
                    continue;
                illumination /= normal.Length() * p0.Length();
                p0 = Vector4.Transform(p0, projectionTransform);
                p1 = Vector4.Transform(p1, projectionTransform);
                p2 = Vector4.Transform(p2, projectionTransform);
                if (p0.X > p0.W && p1.X > p1.W && p2.X > p2.W)
                    continue;
                if (p0.X < -p0.W && p1.X < -p1.W && p2.X < -p2.W)
                    continue;
                if (p0.Y > p0.W && p1.Y > p1.W && p2.Y > p2.W)
                    continue;
                if (p0.Y < -p0.W && p1.Y < -p1.W && p2.Y < -p2.W)
                    continue;
                if (p0.Z > p0.W && p1.Z > p1.W && p2.Z > p2.W)
                    continue;
                if (p0.Z < 0 && p1.Z < 0 && p2.Z < 0)
                    continue;
                uint color = 0xFFFFFFFF;
                if (p0.Z < 0)
                {
                    color = 0xFF00FF00;
                    if (p1.Z < 0)
                    {
                        ClipTriangleIntoOne(p0, p1, p2);
                    }
                    else if (p2.Z < 0)
                    {
                        ClipTriangleIntoOne(p0, p2, p1);
                    }
                    else
                    {
                        ClipTriangleIntoTwo(p0, p1, p2);
                    }
                }
                else if (p1.Z < 0)
                {
                    color = 0xFF00FF00;
                    if (p2.Z < 0)
                    {
                        ClipTriangleIntoOne(p1, p2, p0);
                    }
                    else
                    {
                        ClipTriangleIntoTwo(p1, p0, p2);
                    }
                }
                else if (p2.Z < 0)
                {
                    color = 0xFF00FF00;
                    ClipTriangleIntoTwo(p2, p0, p1);
                }
                else
                {
                    ProcessTriangle(p0, p1, p2);
                }


                void ClipTriangleIntoTwo(Vector4 pointBehind, Vector4 p1, Vector4 p2)
                {
                    float c0 = (-pointBehind.Z) / (p1.Z - pointBehind.Z);
                    float c1 = (-pointBehind.Z) / (p2.Z - pointBehind.Z);
                    Vector4 leftInterpolant = Vector4.Lerp(pointBehind, p1, c0);
                    Vector4 rightInterpolant = Vector4.Lerp(pointBehind, p2, c1);
                    ProcessTriangle(leftInterpolant, p1, p2);
                    ProcessTriangle(rightInterpolant, leftInterpolant, p2);
                }
                void ClipTriangleIntoOne(Vector4 leftPointBehind, Vector4 rightPointBehind, Vector4 p2)
                {
                    float c0 = (-leftPointBehind.Z) / (p2.Z - leftPointBehind.Z);
                    float c1 = (-rightPointBehind.Z) / (p2.Z - rightPointBehind.Z);
                    Vector4 p0a = Vector4.Lerp(leftPointBehind, p2, c0);
                    Vector4 p0b = Vector4.Lerp(rightPointBehind, p2, c1);
                    ProcessTriangle(p0a, p0b, p2);
                }
                void ProcessTriangle(Vector4 p0, Vector4 p1, Vector4 p2)
                {
                    Transform(ref p0);
                    Transform(ref p1);
                    Transform(ref p2);
                    void Transform(ref Vector4 vertex)
                    {
                        float invZ = (1 / vertex.W);
                        float z = vertex.W;
                        vertex *= invZ;
                        vertex = Vector4.Transform(vertex, viewPortTransform);
                        vertex.W = z;
                    }

                    uint rgb = (uint)((illumination / 1.5f + 1f / 3) * 0xFF);
                    color &= (uint)((0xFF << 24) | (rgb << 16) | (rgb << 8) | rgb);
                    Bitmap.DrawTriangleWithZBuffer(width, height, p0, p1, p2, color, zbuffer!);
                }
            }  
        }
        public void RenderCarcass(Obj obj)
        {
            if (Bitmap == null)
                return;
            if (obj == null)
                return;
            ResizeBuffer(obj);

            // Преобразование в мировое пространство
            Matrix4x4 worldTransform = obj.Transformation.Matrix;

            // Преобразование в пространство камеры
            Matrix4x4 cameraTransform = Camera.ViewMatrix;

            // Преобразование в пространство проекции
            int width = Bitmap.PixelWidth;
            int height = Bitmap.PixelHeight;
            float aspectRatio = (float)width / height;
            float fovVertical = MathF.PI / 3 / aspectRatio;
            float nearPlaneDistance = 0.01f;
            float farPlaneDistance = float.PositiveInfinity;
            float zCoeff = (float.IsPositiveInfinity(farPlaneDistance) ? -1f : farPlaneDistance / (nearPlaneDistance - farPlaneDistance));
            Matrix4x4 projectionTransform = new Matrix4x4(
                1 / MathF.Tan(fovVertical * 0.5f) / aspectRatio, 0, 0, 0,
                0, 1 / MathF.Tan(fovVertical * 0.5f), 0, 0,
                0, 0, zCoeff, -1,
                0, 0, zCoeff * nearPlaneDistance, 0
            );
            Matrix4x4.CreatePerspectiveFieldOfView(fovVertical, aspectRatio, nearPlaneDistance, farPlaneDistance);

            // Преобразование в пространство окна
            float leftCornerX = 0;
            float leftCornerY = 0;
            Matrix4x4 viewPortTransform = new Matrix4x4(
                (float)width / 2, 0, 0, 0,
                0, -(float)height / 2, 0, 0,
                0, 0, 1, 0,
                leftCornerX + (float)width / 2, leftCornerY + (float)height / 2, 0, 1);

            Stopwatch sw = Stopwatch.StartNew();

            // Creating final trasformation matrix
            Matrix4x4 modelToProjection = worldTransform * cameraTransform * projectionTransform;
            for (int i = 0; i < bufferLength; i++)
            {
                Vector4 v = new(obj.vertices[i], 1);
                v = Vector4.Transform(v, modelToProjection);
                projectionSpaceBuffer[i] = v;
            }
            sw.Stop();

            // Drawing
            Stopwatch stopwatch = Stopwatch.StartNew();
            List<Face> faces = obj.faces;
            int facesCount = faces.Count;
            uint color = 0xFFFF0000;

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

                    if (v0.Z < 0)
                    {
                        InterpolateV0(ref v0, ref v1);
                        color = 0xFF0000FF;
                    }
                    else if (v1.Z < 0)
                    {
                        InterpolateV0(ref v1, ref v0);
                        color = 0xFF0000FF;
                    }
                    static void InterpolateV0(ref Vector4 v0, ref Vector4 v1)
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
