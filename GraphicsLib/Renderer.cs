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
        private ZbufferV2? zbufferV2;

        public Renderer(Camera camera)
        {
            Camera = camera;
            projectionSpaceBuffer = [];
            bufferLength = 0;
            Bitmap = default;
            zbuffer = default;
            zbufferV2 = default;
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
        private void ResizeAndClearZBufferV2()
        {
            if (Bitmap == null)
            {
                return;
            }
            if (zbufferV2 == null)
            {
                zbufferV2 = new ZbufferV2(Bitmap.PixelWidth, Bitmap.PixelHeight);
            }
            else
            {
                int width = Bitmap.PixelWidth;
                int height = Bitmap.PixelHeight;
                if (zbufferV2.Width != width && zbufferV2.Height != height)
                {
                    zbufferV2 = new ZbufferV2(Bitmap.PixelWidth, Bitmap.PixelHeight);
                }
                else
                {
                    zbufferV2.Clear();
                }
            }

        }
        public void RenderSolid2(Obj obj)
        {
            if (Bitmap == null)
                return;
            if (obj == null)
                return;

            // prepare zbuffer
            ResizeAndClearZBufferV2();

            // transform from model space to world space
            Matrix4x4 worldTransform = obj.Transformation.Matrix;

            // transform from world space to camera space
            Matrix4x4 cameraTransform = Camera.ViewMatrix;

            Matrix4x4 modelToCamera = worldTransform * cameraTransform;

            // transform from camera space to clipping space (divide by W to get to NDC space)
            int width = Bitmap.PixelWidth;
            int height = Bitmap.PixelHeight;
            float aspectRatio = (float)width / height;
            float fovVertical = MathF.PI / 3 / aspectRatio;
            float nearPlaneDistance = 0.1f;
            float farPlaneDistance = float.PositiveInfinity;
            Matrix4x4 projectionTransform = Matrix4x4.CreatePerspectiveFieldOfView(fovVertical, aspectRatio, nearPlaneDistance, farPlaneDistance);

            // transform from NDC space to viewport space
            float leftCornerX = 0;
            float leftCornerY = 0;
            Matrix4x4 viewPortTransform = Matrix4x4.CreateViewport(leftCornerX, leftCornerY, width, height, 0, 1);

            Parallel.For(0, obj.faces.Count, i =>
            {
                Face triangle = obj.faces[i];
                Vector4 p0 = new Vector4(obj.vertices[triangle.vIndices[0]], 1);
                Vector4 p1 = new Vector4(obj.vertices[triangle.vIndices[1]], 1);
                Vector4 p2 = new Vector4(obj.vertices[triangle.vIndices[2]], 1);
                p0 = Vector4.Transform(p0, modelToCamera);
                p1 = Vector4.Transform(p1, modelToCamera);
                p2 = Vector4.Transform(p2, modelToCamera);
                Vector4 normal = Vector3.Cross((p2 - p0).AsVector3(), (p1 - p0).AsVector3()).AsVector4();
                float orientation = Vector4.Dot(normal, p0);
                //Cull triangle if its orientation is facing away from the camera
                if (orientation <= 0)
                    return;
                p0 = Vector4.Transform(p0, projectionTransform);
                p1 = Vector4.Transform(p1, projectionTransform);
                p2 = Vector4.Transform(p2, projectionTransform);
                //Cull triangle if it is not in frustum and all points are on the same side from it
                if (p0.X > p0.W && p1.X > p1.W && p2.X > p2.W)
                    return;
                if (p0.X < -p0.W && p1.X < -p1.W && p2.X < -p2.W)
                    return;
                if (p0.Y > p0.W && p1.Y > p1.W && p2.Y > p2.W)
                    return;
                if (p0.Y < -p0.W && p1.Y < -p1.W && p2.Y < -p2.W)
                    return;
                if (p0.Z > p0.W && p1.Z > p1.W && p2.Z > p2.W)
                    return;
                if (p0.Z < 0 && p1.Z < 0 && p2.Z < 0)
                    return;
                uint color = 0xFFFFFFFF;
                //Clipping triangle if it intersects near plane
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


                //Clip triangle into two
                //            |    p1
                //            li    |
                //            | \   |
                //      pb    |  \  |
                //            ri  \ |
                //            |    p2
                //            |
                void ClipTriangleIntoTwo(Vector4 pointBehind, Vector4 p1, Vector4 p2)
                {
                    float c0 = (-pointBehind.Z) / (p1.Z - pointBehind.Z);
                    float c1 = (-pointBehind.Z) / (p2.Z - pointBehind.Z);
                    Vector4 leftInterpolant = Vector4.Lerp(pointBehind, p1, c0);
                    Vector4 rightInterpolant = Vector4.Lerp(pointBehind, p2, c1);
                    ProcessTriangle(leftInterpolant, p1, p2);
                    ProcessTriangle(rightInterpolant, leftInterpolant, p2);
                }
                //Clip triangle into one
                //     lpb    | 
                //            li    
                //            | 
                //            |     p2
                //            ri  
                //     rpb    |   
                //            |
                void ClipTriangleIntoOne(Vector4 leftPointBehind, Vector4 rightPointBehind, Vector4 p2)
                {
                    float c0 = (-leftPointBehind.Z) / (p2.Z - leftPointBehind.Z);
                    float c1 = (-rightPointBehind.Z) / (p2.Z - rightPointBehind.Z);
                    Vector4 leftInterpolant = Vector4.Lerp(leftPointBehind, p2, c0);
                    Vector4 rightInterpolant = Vector4.Lerp(rightPointBehind, p2, c1);
                    ProcessTriangle(leftInterpolant, p2, rightInterpolant);
                }
                void ProcessTriangle(Vector4 p0, Vector4 p1, Vector4 p2)
                {
                    Transform(ref p0);
                    Transform(ref p1);
                    Transform(ref p2);
                    // save z to use it in zbuffer
                    void Transform(ref Vector4 vertex)
                    {
                        float invZ = (1 / vertex.W);
                        float z = vertex.W;
                        vertex *= invZ;
                        vertex = Vector4.Transform(vertex, viewPortTransform);
                        vertex.W = z;
                    }

                    //calculate illumination
                    float illumination = 0f;
                    if (triangle.nIndices != null)
                    {
                        for (int i = 0; i < 3; i++)
                        {
                            Vector3 normal = obj.normals[triangle.nIndices[i]];
                            Vector3 translated = Vector3.TransformNormal(normal, worldTransform);
                            Vector3 lightDir = -(Vector3.Transform(obj.vertices[triangle.vIndices[i]], worldTransform) - Camera.Position);
                            float vertexIllumination = Vector3.Dot(lightDir, translated) / (lightDir.Length() * translated.Length());
                            illumination += vertexIllumination;
                        }
                    }
                    else
                    {
                        Span<Vector3> vectors = [obj.vertices[triangle.vIndices[0]], obj.vertices[triangle.vIndices[1]], obj.vertices[triangle.vIndices[2]]];
                        for (int i = 0; i < vectors.Length; i++)
                        {
                            vectors[i] = Vector3.Transform(vectors[i], worldTransform);
                        }
                        Vector3 normal = Vector3.Cross(vectors[2] - vectors[0], vectors[1] - vectors[0]);
                        Vector3 translated = Vector3.TransformNormal(normal, worldTransform);
                        for (int i = 0; i < 3; i++)
                        {
                            Vector3 lightDir = -(Vector3.Transform(obj.vertices[triangle.vIndices[i]], worldTransform) - Camera.Position);
                            float vertexIllumination = Vector3.Dot(lightDir, translated) / (lightDir.Length() * translated.Length());
                            illumination += vertexIllumination;
                        }
                    }
                    illumination /= 3;
                    uint rgb = (uint)(illumination * 0xFF);
                    color &= (uint)((0xFF << 24) | (rgb << 16) | (rgb << 8) | rgb);
                    zbufferV2!.MapTriangle(p0, p1, p2, color);
                }
            });
            Bitmap.FlushZBufferV2(zbufferV2!);
        }
        public void RenderSolid(Obj obj)
        {
            if (Bitmap == null)
                return;
            if (obj == null)
                return;
            
            // prepare zbuffer
            ResizeAndClearZBuffer();

            // transform from model space to world space
            Matrix4x4 worldTransform = obj.Transformation.Matrix;

            // transform from world space to camera space
            Matrix4x4 cameraTransform = Camera.ViewMatrix;

            Matrix4x4 modelToCamera = worldTransform * cameraTransform;

            // transform from camera space to clipping space (divide by W to get to NDC space)
            int width = Bitmap.PixelWidth;
            int height = Bitmap.PixelHeight;
            float aspectRatio = (float)width / height;
            float fovVertical = MathF.PI / 3 / aspectRatio;
            float nearPlaneDistance = 0.1f;
            float farPlaneDistance = float.PositiveInfinity;
            Matrix4x4 projectionTransform = Matrix4x4.CreatePerspectiveFieldOfView(fovVertical, aspectRatio, nearPlaneDistance, farPlaneDistance);

            // transform from NDC space to viewport space
            float leftCornerX = 0;
            float leftCornerY = 0;
            Matrix4x4 viewPortTransform = Matrix4x4.CreateViewport(leftCornerX, leftCornerY, width, height, 0, 1);


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
                float orientation = Vector4.Dot(normal, p0);
                //Cull triangle if its orientation is facing away from the camera
                if (orientation <= 0)
                    continue;
                p0 = Vector4.Transform(p0, projectionTransform);
                p1 = Vector4.Transform(p1, projectionTransform);
                p2 = Vector4.Transform(p2, projectionTransform);
                //Cull triangle if it is not in frustum and all points are on the same side from it
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
                //Clipping triangle if it intersects near plane
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


                //Clip triangle into two
                //            |    p1
                //            li    |
                //            | \   |
                //      pb    |  \  |
                //            ri  \ |
                //            |    p2
                //            |
                void ClipTriangleIntoTwo(Vector4 pointBehind, Vector4 p1, Vector4 p2)
                {
                    float c0 = (-pointBehind.Z) / (p1.Z - pointBehind.Z);
                    float c1 = (-pointBehind.Z) / (p2.Z - pointBehind.Z);
                    Vector4 leftInterpolant = Vector4.Lerp(pointBehind, p1, c0);
                    Vector4 rightInterpolant = Vector4.Lerp(pointBehind, p2, c1);
                    ProcessTriangle(leftInterpolant, p1, p2);
                    ProcessTriangle(rightInterpolant, leftInterpolant, p2);
                }
                //Clip triangle into one
                //     lpb    | 
                //            li    
                //            | 
                //            |     p2
                //            ri  
                //     rpb    |   
                //            |
                void ClipTriangleIntoOne(Vector4 leftPointBehind, Vector4 rightPointBehind, Vector4 p2)
                {
                    float c0 = (-leftPointBehind.Z) / (p2.Z - leftPointBehind.Z);
                    float c1 = (-rightPointBehind.Z) / (p2.Z - rightPointBehind.Z);
                    Vector4 leftInterpolant = Vector4.Lerp(leftPointBehind, p2, c0);
                    Vector4 rightInterpolant = Vector4.Lerp(rightPointBehind, p2, c1);
                    ProcessTriangle(leftInterpolant, p2, rightInterpolant);
                }
                void ProcessTriangle(Vector4 p0, Vector4 p1, Vector4 p2)
                {
                    Transform(ref p0);
                    Transform(ref p1);
                    Transform(ref p2);
                    // save z to use it in zbuffer
                    void Transform(ref Vector4 vertex)
                    {
                        float invZ = (1 / vertex.W);
                        float z = vertex.W;
                        vertex *= invZ;
                        vertex = Vector4.Transform(vertex, viewPortTransform);
                        vertex.W = z;
                    }

                    //calculate illumination
                    float illumination = 0f;
                    if (triangle.nIndices != null)
                    {
                        for (int i = 0; i < 3; i++)
                        {
                            Vector3 normal = obj.normals[triangle.nIndices[i]];
                            Vector3 translated = Vector3.TransformNormal(normal, worldTransform);
                            Vector3 lightDir = -(Vector3.Transform(obj.vertices[triangle.vIndices[i]], worldTransform) - Camera.Position);
                            float vertexIllumination = Vector3.Dot(lightDir, translated) / (lightDir.Length() * translated.Length());
                            illumination += vertexIllumination;
                        }
                    }
                    else
                    {
                        Span<Vector3> vectors = [obj.vertices[triangle.vIndices[0]], obj.vertices[triangle.vIndices[1]], obj.vertices[triangle.vIndices[2]]];
                        for(int i = 0; i < vectors.Length; i++)
                        {
                            vectors[i] = Vector3.Transform(vectors[i], worldTransform);
                        }
                        Vector3 normal = Vector3.Cross(vectors[2] - vectors[0], vectors[1] - vectors[0]);
                        Vector3 translated = Vector3.TransformNormal(normal, worldTransform);
                        for (int i = 0; i < 3; i++)
                        {
                            Vector3 lightDir = -(Vector3.Transform(obj.vertices[triangle.vIndices[i]], worldTransform) - Camera.Position);
                            float vertexIllumination = Vector3.Dot(lightDir, translated) / (lightDir.Length() * translated.Length());
                            illumination += vertexIllumination;
                        }
                    }
                    illumination /= 3;
                    uint rgb = (uint)(illumination * 0xFF);
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

            // transform from model space to world space
            Matrix4x4 worldTransform = obj.Transformation.Matrix;

            // transform from world space to camera space
            Matrix4x4 cameraTransform = Camera.ViewMatrix;


            // transform from camera space to clipping space (divide by W to get to NDC space)
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

            // transform from NDC space to viewport space
            float leftCornerX = 0;
            float leftCornerY = 0;
            Matrix4x4 viewPortTransform = new Matrix4x4(
                (float)width / 2, 0, 0, 0,
                0, -(float)height / 2, 0, 0,
                0, 0, 1, 0,
                leftCornerX + (float)width / 2, leftCornerY + (float)height / 2, 0, 1);


            Matrix4x4 modelToProjection = worldTransform * cameraTransform * projectionTransform;
            for (int i = 0; i < bufferLength; i++)
            {
                Vector4 v = new(obj.vertices[i], 1);
                v = Vector4.Transform(v, modelToProjection);
                //buffering to avoid recalculations for every face
                projectionSpaceBuffer[i] = v;
            }

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

                    //Cull edge if edge is not in frustum and both points are on the same side from it
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
                    
                    //Clip edge if one point is behind the camera
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

                    // find intersection between edge and near plane
                    //
                    //           |
                    //       +---+----+
                    //           |
                    //           |
                    //  Z -------0--------------->
                    static void InterpolateV0(ref Vector4 v0, ref Vector4 v1)
                    {
                        float coeff = (-v0.Z) / (v1.Z - v0.Z);
                        v0 = Vector4.Lerp(v0, v1, coeff);
                    }
                    //final transformations
                    v0 = Vector4.Transform(v0, viewPortTransform);
                    v0 *= (1 / v0.W);
                    v1 = Vector4.Transform(v1, viewPortTransform);
                    v1 *= (1 / v1.W);
                    //drawing
                    Bitmap.DrawLine(width, height, (int)v0.X, (int)v0.Y,
                       (int)v1.X, (int)v1.Y, color);
                }
            }
        }

    }
}
