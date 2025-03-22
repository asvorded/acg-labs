using GraphicsLib.Primitives;
using GraphicsLib.Shaders;
using GraphicsLib.Types;
using System.Numerics;
using System.Windows.Media.Imaging;

namespace GraphicsLib
{
    public class Renderer
    {
        public Scene Scene { get; set; }
        public WriteableBitmap? Bitmap { get; set; }
        private Vector4[] projectionSpaceBuffer;
        private int bufferLength;
        private Zbuffer? zBuffer;
        private ZbufferV2? zBufferV2;
        private GBuffer? zBufferWithIndices;

        public Renderer(Scene scene)
        {
            Scene = scene;
            projectionSpaceBuffer = [];
            bufferLength = 0;
            Bitmap = default;
            zBuffer = default;
            zBufferV2 = default;
            zBufferWithIndices = default;
        }
        private void ResizeBuffer(Obj obj)
        {
            int vertexCount = obj.vertices.Length;
            if (projectionSpaceBuffer.Length < vertexCount)
            {
                projectionSpaceBuffer = new Vector4[vertexCount];
            }
            bufferLength = vertexCount;
        }
        private void ResizeAndClearZBuffer()
        {
            if (Bitmap == null)
            {
                return;
            }
            if (zBuffer == null)
            {
                zBuffer = new Zbuffer(Bitmap.PixelWidth, Bitmap.PixelHeight);
            }
            else
            {
                int width = Bitmap.PixelWidth;
                int height = Bitmap.PixelHeight;
                if (zBuffer.Width != width && zBuffer.Height != height)
                {
                    zBuffer = new Zbuffer(Bitmap.PixelWidth, Bitmap.PixelHeight);
                }
                else
                {
                    zBuffer.Clear();
                }
            }
        }
        private void ResizeAndClearZBufferV2()
        {
            if (Bitmap == null)
            {
                return;
            }
            if (zBufferV2 == null)
            {
                zBufferV2 = new ZbufferV2(Bitmap.PixelWidth, Bitmap.PixelHeight);
            }
            else
            {
                int width = Bitmap.PixelWidth;
                int height = Bitmap.PixelHeight;
                if (zBufferV2.Width != width && zBufferV2.Height != height)
                {
                    zBufferV2 = new ZbufferV2(Bitmap.PixelWidth, Bitmap.PixelHeight);
                }
                else
                {
                    zBufferV2.Clear();
                }
            }

        }
        private void ResizeAndClearZBufferWithIndicies()
        {
            if (Bitmap == null)
            {
                return;
            }
            if (zBufferWithIndices == null)
            {
                zBufferWithIndices = new(Bitmap.PixelWidth, Bitmap.PixelHeight);
            }
            else
            {
                int width = Bitmap.PixelWidth;
                int height = Bitmap.PixelHeight;
                if (zBufferWithIndices.Width != width && zBufferWithIndices.Height != height)
                {
                    zBufferWithIndices = new(Bitmap.PixelWidth, Bitmap.PixelHeight);
                }
                else
                {
                    zBufferWithIndices.Clear();
                }
            }

        }

        public void RenderDeferred()
        {
            if (Bitmap == null)
                return;
            if (Scene.Obj == null)
                return;
            Obj obj = Scene.Obj;
            Camera mainCamera = Scene.Camera;
            mainCamera.ScreenHeight = Bitmap.PixelHeight;
            mainCamera.ScreenWidth = Bitmap.PixelWidth;
            int bitmapHeight = Bitmap.PixelHeight;
            int bitmapWidth = Bitmap.PixelWidth;

            ResizeAndClearZBufferWithIndicies();
            // transform from model space to world space
            Matrix4x4 worldTransform = obj.transformation.Matrix;
            // transform from world space to camera space
            Matrix4x4 cameraTransform = mainCamera.ViewMatrix;

            Matrix4x4 modelToCamera = worldTransform * cameraTransform;
            // transform from camera space to clipping space (divide by W to get to NDC space)
            Matrix4x4 projectionTransform = mainCamera.ProjectionMatrix;

            // transform from NDC space to viewport space
            Matrix4x4 viewPortTransform = mainCamera.ViewPortMatrix;
            /*#if DEBUG
                        for (int triangleIndex = 0; triangleIndex < obj.faces.Length; triangleIndex++)
            #else
                        Parallel.For(0, obj.faces.Length, triangleIndex =>
            #endif
                        {
                            Face triangle = obj.faces[triangleIndex];
                            Vector4 p0 = new Vector4(obj.vertices[triangle.vIndices[0]], 1);
                            Vector4 p1 = new Vector4(obj.vertices[triangle.vIndices[1]], 1);
                            Vector4 p2 = new Vector4(obj.vertices[triangle.vIndices[2]], 1);*/

#if DEBUG
            for (int triangleIndex = 0; triangleIndex < obj.triangles.Length; triangleIndex++)
#else
            Parallel.For(0, obj.triangles.Length, triangleIndex =>
#endif
            {
                StaticTriangle triangle = obj.triangles[triangleIndex];
                Vector4 p0 = new Vector4(triangle.position0, 1);
                Vector4 p1 = new Vector4(triangle.position1, 1);
                Vector4 p2 = new Vector4(triangle.position2, 1);
                p0 = Vector4.Transform(p0, modelToCamera);
                p1 = Vector4.Transform(p1, modelToCamera);
                p2 = Vector4.Transform(p2, modelToCamera);
                Vector4 normal = Vector3.Cross((p2 - p0).AsVector3(), (p1 - p0).AsVector3()).AsVector4();
                float orientation = Vector4.Dot(normal, p0);

                //Cull triangle if its orientation is facing away from the camera
                if (orientation <= 0)
#if DEBUG
                    continue;
#else
                    return;
#endif
                p0 = Vector4.Transform(p0, projectionTransform);
                p1 = Vector4.Transform(p1, projectionTransform);
                p2 = Vector4.Transform(p2, projectionTransform);

                //Cull triangle if it is not in frustum and all points are on the same side from it
                if (p0.X > p0.W && p1.X > p1.W && p2.X > p2.W)
#if DEBUG
                    continue;
#else
                    return;
#endif
                if (p0.X < -p0.W && p1.X < -p1.W && p2.X < -p2.W)
#if DEBUG
                    continue;
#else
                    return;
#endif
                if (p0.Y > p0.W && p1.Y > p1.W && p2.Y > p2.W)
#if DEBUG
                    continue;
#else
                    return;
#endif
                if (p0.Y < -p0.W && p1.Y < -p1.W && p2.Y < -p2.W)
#if DEBUG
                    continue;
#else
                    return;
#endif
                if (p0.Z > p0.W && p1.Z > p1.W && p2.Z > p2.W)
#if DEBUG
                    continue;
#else
                    return;
#endif
                if (p0.Z < 0 && p1.Z < 0 && p2.Z < 0)
#if DEBUG
                    continue;
#else
                    return;
#endif
                //Clipping triangle if it intersects near plane
                if (p0.Z < 0)
                {
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
                    Vector4 leftInterpolant = Vector4.Lerp(leftPointBehind, p2, c0);
                    Vector4 rightInterpolant = Vector4.Lerp(rightPointBehind, p2, c1);
                    ProcessTriangle(leftInterpolant, p2, rightInterpolant);
                }
                void ProcessTriangle(Vector4 p0, Vector4 p1, Vector4 p2)
                {
                    Transform(ref p0);
                    Transform(ref p1);
                    Transform(ref p2);
                    void Transform(ref Vector4 vertex)
                    {
                        float invZ = (1 / vertex.W);
                        vertex *= invZ;
                        vertex = Vector4.Transform(vertex, viewPortTransform);
                    }
                    MapTriangle(p0, p1, p2);


                    void MapTriangle(Vector4 p0, Vector4 p1, Vector4 p2)
                    {
                        Vector4 min = p0;
                        Vector4 mid = p1;
                        Vector4 max = p2;
                        // Correct min, mid and max
                        if (mid.Y < min.Y)
                        {
                            (min, mid) = (mid, min);
                        }
                        if (max.Y < min.Y)
                        {
                            (min, max) = (max, min);
                        }
                        if (max.Y < mid.Y)
                        {
                            (mid, max) = (max, mid);
                        }

                        if (min.Y == mid.Y)
                        {
                            //flat top
                            if (mid.X < min.X)
                            {
                                (min, mid) = (mid, min);
                            }
                            MapFlatTopTriangle(min, mid, max);
                        }
                        else if (max.Y == mid.Y)
                        {
                            //flat bottom
                            if (max.X > mid.X)
                            {
                                (mid, max) = (max, mid);
                            }
                            MapFlatBottomTriangle(min, mid, max);
                        }
                        else
                        {
                            float c = (mid.Y - min.Y) / (max.Y - min.Y);
                            Vector4 interpolant = Vector4.Lerp(min, max, c);
                            if (interpolant.X > mid.X)
                            {
                                //right major
                                MapFlatBottomTriangle(min, interpolant, mid);
                                MapFlatTopTriangle(mid, interpolant, max);
                            }
                            else
                            {
                                //left major 
                                MapFlatBottomTriangle(min, mid, interpolant);
                                MapFlatTopTriangle(interpolant, mid, max);
                            }
                        }
                    }

                    void MapFlatTopTriangle(Vector4 leftTopPoint, Vector4 rightTopPoint, Vector4 bottomPoint)
                    {
                        float dy = bottomPoint.Y - leftTopPoint.Y;
                        Vector4 dLeftPoint = (bottomPoint - leftTopPoint) / dy;
                        Vector4 dRightPoint = (bottomPoint - rightTopPoint) / dy;
                        float dzdx = (rightTopPoint.Z - leftTopPoint.Z) / (rightTopPoint.X - leftTopPoint.X);
                        Vector4 rightPoint = rightTopPoint;
                        MapFlatTriangle(leftTopPoint, rightPoint, bottomPoint.Y,
                                                                dLeftPoint, dRightPoint, dzdx);
                    }
                    void MapFlatBottomTriangle(Vector4 topPoint, Vector4 rightBottomPoint, Vector4 leftBottomPoint)
                    {
                        float dy = rightBottomPoint.Y - topPoint.Y;
                        Vector4 dRightPoint = (rightBottomPoint - topPoint) / dy;
                        Vector4 dLeftPoint = (leftBottomPoint - topPoint) / dy;
                        float dzdx = (rightBottomPoint.Z - leftBottomPoint.Z) / (rightBottomPoint.X - leftBottomPoint.X);
                        Vector4 rightPoint = topPoint;
                        MapFlatTriangle(topPoint, rightPoint,
                                rightBottomPoint.Y, dLeftPoint, dRightPoint, dzdx);
                    }
                    void MapFlatTriangle(Vector4 leftPoint, Vector4 rightPoint, float yMax, Vector4 dLeftPoint, Vector4 dRightPoint, float dzdx)
                    {
                        int yStart = Math.Max((int)Math.Ceiling(leftPoint.Y), 0);
                        int yEnd = Math.Min((int)Math.Ceiling(yMax), bitmapHeight);
                        float yPrestep = yStart - leftPoint.Y;
                        leftPoint += dLeftPoint * yPrestep;
                        rightPoint += dRightPoint * yPrestep;
                        for (int y = yStart; y < yEnd; y++)
                        {
                            int xStart = Math.Max((int)Math.Ceiling(leftPoint.X), 0);
                            int xEnd = Math.Min((int)Math.Ceiling(rightPoint.X), bitmapWidth);
                            float xPrestep = xStart - leftPoint.X;
                            float z = leftPoint.Z + dzdx * xPrestep;
                            for (int x = xStart; x < xEnd; x++)
                            {
                                zBufferWithIndices!.TestAndSet(x, y, z, triangleIndex);
                                z += dzdx;
                            }
                            leftPoint += dLeftPoint;
                            rightPoint += dRightPoint;
                        }

                    }
                }
#if DEBUG
            }
#else
            });
#endif
            for (int y = 0; y < bitmapHeight; y++)
                for (int x = 0; x < bitmapWidth; x++)
                {
                    if (zBufferWithIndices!.TriangleIndex(x,y) != -1)
                    {
                        Bitmap.SetPixelLockedNoDirty(x, y, 0xFFFFFFFF);
                    }
                }
        }
        public void Render<Shader, Vertex>() where Shader : IShader<Vertex>, new() where Vertex : struct, IVertex<Vertex>
        {
            if (Bitmap == null)
                return;
            if (Scene.Obj == null)
                return;
            Obj obj = Scene.Obj;
            Types.Camera mainCamera = Scene.Camera;
            mainCamera.ScreenHeight = Bitmap.PixelHeight;
            mainCamera.ScreenWidth = Bitmap.PixelWidth;
            Shader shader = new()
            {
                Scene = Scene
            };
            // prepare zbuffer
            ResizeAndClearZBufferV2();


            // transform from world space to camera space
            Matrix4x4 cameraTransform = mainCamera.ViewMatrix;
            // transform from camera space to clipping space (divide by W to get to NDC space)

            Matrix4x4 projectionTransform = mainCamera.ProjectionMatrix;

            // transform from NDC space to viewport space
            Matrix4x4 viewPortTransform = mainCamera.ViewPortMatrix;
            //For debugging purpose multithreading is disabled
            /*#if DEBUG
                        for (int i = 0; i < obj.faces.Length; i++)
            #else
                        Parallel.For(0, obj.faces.Length, i =>
            #endif
                        {
                            Face triangle = obj.faces[i];
                            Vertex p0 = shader.GetVertexWithWorldPositionFromFace(obj, i, 0);
                            Vertex p1 = shader.GetVertexWithWorldPositionFromFace(obj, i, 1);
                            Vertex p2 = shader.GetVertexWithWorldPositionFromFace(obj, i, 2);*/
#if DEBUG
            for (int i = 0; i < obj.triangles.Length; i++)
#else
            Parallel.For(0, obj.triangles.Length, i =>
#endif
                {
                    Face triangle = obj.faces[i];
                    Vertex p0 = shader.GetVertexWithWorldPositionFromTriangle(obj, i, 0);
                    Vertex p1 = shader.GetVertexWithWorldPositionFromTriangle(obj, i, 1);
                    Vertex p2 = shader.GetVertexWithWorldPositionFromTriangle(obj, i, 2);
                    p0.Position = Vector4.Transform(p0.Position, cameraTransform);
                    p1.Position = Vector4.Transform(p1.Position, cameraTransform);
                    p2.Position = Vector4.Transform(p2.Position, cameraTransform);
                    //cache vertex positions
                    Vector4 p0Position = p0.Position;
                    Vector4 p1Position = p1.Position;
                    Vector4 p2Position = p2.Position;
                    Vector4 normal = Vector3.Cross((p2Position - p0Position).AsVector3(), (p1Position - p0Position).AsVector3()).AsVector4();
                    float orientation = Vector4.Dot(normal, p0Position);
                    //Cull triangle if its orientation is facing away from the camera
                    if (orientation <= 0)
#if DEBUG
                    continue;
#else
                        return;
#endif
                    p0.Position = Vector4.Transform(p0Position, projectionTransform);
                    p1.Position = Vector4.Transform(p1Position, projectionTransform);
                    p2.Position = Vector4.Transform(p2Position, projectionTransform);
                    //cache vertex positions
                    p0Position = p0.Position;
                    p1Position = p1.Position;
                    p2Position = p2.Position;
                    //Cull triangle if it is not in frustum and all points are on the same side from it
                    if (p0Position.X > p0Position.W && p1Position.X > p1Position.W && p2Position.X > p2Position.W)
#if DEBUG
                    continue;
#else
                        return;
#endif
                    if (p0Position.X < -p0Position.W && p1Position.X < -p1Position.W && p2Position.X < -p2Position.W)
#if DEBUG
                    continue;
#else
                        return;
#endif
                    if (p0Position.Y > p0Position.W && p1Position.Y > p1Position.W && p2Position.Y > p2Position.W)
#if DEBUG
                    continue;
#else
                        return;
#endif
                    if (p0Position.Y < -p0Position.W && p1Position.Y < -p1Position.W && p2Position.Y < -p2Position.W)
#if DEBUG
                    continue;
#else
                        return;
#endif
                    if (p0Position.Z > p0Position.W && p1Position.Z > p1Position.W && p2Position.Z > p2Position.W)
#if DEBUG
                    continue;
#else
                        return;
#endif
                    if (p0Position.Z < 0 && p1Position.Z < 0 && p2Position.Z < 0)
#if DEBUG
                    continue;
#else
                        return;
#endif
                    //Clipping triangle if it intersects near plane
                    if (p0Position.Z < 0)
                    {
                        if (p1Position.Z < 0)
                        {
                            ClipTriangleIntoOne(p0, p1, p2);
                        }
                        else if (p2Position.Z < 0)
                        {
                            ClipTriangleIntoOne(p0, p2, p1);
                        }
                        else
                        {
                            ClipTriangleIntoTwo(p0, p1, p2);
                        }
                    }
                    else if (p1Position.Z < 0)
                    {
                        if (p2Position.Z < 0)
                        {
                            ClipTriangleIntoOne(p1, p2, p0);
                        }
                        else
                        {
                            ClipTriangleIntoTwo(p1, p0, p2);
                        }
                    }
                    else if (p2Position.Z < 0)
                    {
                        ClipTriangleIntoTwo(p2, p0, p1);
                    }
                    else
                    {
                        ProcessTriangle(p0, p1, p2);
                    }
                    void ClipTriangleIntoTwo(Vertex pointBehind, Vertex p1, Vertex p2)
                    {
                        float c0 = (-pointBehind.Position.Z) / (p1.Position.Z - pointBehind.Position.Z);
                        float c1 = (-pointBehind.Position.Z) / (p2.Position.Z - pointBehind.Position.Z);
                        Vertex leftInterpolant = Vertex.Lerp(pointBehind, p1, c0);
                        Vertex rightInterpolant = Vertex.Lerp(pointBehind, p2, c1);
                        ProcessTriangle(leftInterpolant, p1, p2);
                        ProcessTriangle(rightInterpolant, leftInterpolant, p2);
                    }
                    void ClipTriangleIntoOne(Vertex leftPointBehind, Vertex rightPointBehind, Vertex p2)
                    {
                        float c0 = (-leftPointBehind.Position.Z) / (p2.Position.Z - leftPointBehind.Position.Z);
                        float c1 = (-rightPointBehind.Position.Z) / (p2.Position.Z - rightPointBehind.Position.Z);
                        Vertex leftInterpolant = Vertex.Lerp(leftPointBehind, p2, c0);
                        Vertex rightInterpolant = Vertex.Lerp(rightPointBehind, p2, c1);
                        ProcessTriangle(leftInterpolant, p2, rightInterpolant);
                    }
                    void ProcessTriangle(Vertex p0, Vertex p1, Vertex p2)
                    {
                        TransformToViewPort(ref p0);
                        TransformToViewPort(ref p1);
                        TransformToViewPort(ref p2);
                        
                        void TransformToViewPort(ref Vertex vertex)
                        {
                            float invZ = (1 / vertex.Position.W);
                            vertex *= invZ;
                            Vector4 ndcPosition = Vector4.Transform(vertex.Position, viewPortTransform);
                            // save 1/z to use it in interpolation correction
                            ndcPosition.W = invZ;
                            vertex.Position = ndcPosition;
                        }
                        MapTriangle(p0, p1, p2);


                        void MapTriangle(Vertex p0, Vertex p1, Vertex p2)
                        {
                            Vertex min = p0;
                            Vertex mid = p1;
                            Vertex max = p2;
                            // Correct min, mid and max
                            if (mid.Position.Y < min.Position.Y)
                            {
                                (min, mid) = (mid, min);
                            }
                            if (max.Position.Y < min.Position.Y)
                            {
                                (min, max) = (max, min);
                            }
                            if (max.Position.Y < mid.Position.Y)
                            {
                                (mid, max) = (max, mid);
                            }

                            if (min.Position.Y == mid.Position.Y)
                            {
                                //flat top
                                if (mid.Position.X < min.Position.X)
                                {
                                    (min, mid) = (mid, min);
                                }
                                MapFlatTopTriangle(min, mid, max);
                            }
                            else if (max.Position.Y == mid.Position.Y)
                            {
                                //flat bottom
                                if (max.Position.X > mid.Position.X)
                                {
                                    (mid, max) = (max, mid);
                                }
                                MapFlatBottomTriangle(min, mid, max);
                            }
                            else
                            {
                                float c = (mid.Position.Y - min.Position.Y) / (max.Position.Y - min.Position.Y);
                                Vertex interpolant = Vertex.Lerp(min, max, c);
                                if (interpolant.Position.X > mid.Position.X)
                                {
                                    //right major
                                    MapFlatBottomTriangle(min, interpolant, mid);
                                    MapFlatTopTriangle(mid, interpolant, max);
                                }
                                else
                                {
                                    //left major
                                    MapFlatBottomTriangle(min, mid, interpolant);
                                    MapFlatTopTriangle(interpolant, mid, max);
                                }
                            }
                        }
                        void MapFlatTopTriangle(Vertex leftTopPoint, Vertex rightTopPoint, Vertex bottomPoint)
                        {
                            float dy = bottomPoint.Position.Y - leftTopPoint.Position.Y;
                            Vertex dLeftPoint = (bottomPoint - leftTopPoint) / dy;
                            Vertex dRightPoint = (bottomPoint - rightTopPoint) / dy;
                            Vertex dLineInterpolant = (rightTopPoint - leftTopPoint) / (rightTopPoint.Position.X - leftTopPoint.Position.X);
                            Vertex rightPoint = rightTopPoint;
                            MapFlatTriangle(leftTopPoint, rightPoint, bottomPoint.Position.Y, dLeftPoint, dRightPoint, dLineInterpolant);
                        }
                        void MapFlatBottomTriangle(Vertex topPoint, Vertex rightBottomPoint, Vertex leftBottomPoint)
                        {
                            float dy = rightBottomPoint.Position.Y - topPoint.Position.Y;
                            Vertex dRightPoint = (rightBottomPoint - topPoint) / dy;
                            Vertex dLeftPoint = (leftBottomPoint - topPoint) / dy;
                            Vertex rightPoint = topPoint;
                            Vertex DLineInterpolant = (rightBottomPoint - leftBottomPoint) / (rightBottomPoint.Position.X - leftBottomPoint.Position.X);
                            MapFlatTriangle(topPoint, rightPoint, rightBottomPoint.Position.Y, dLeftPoint, dRightPoint, DLineInterpolant);
                        }
                        void MapFlatTriangle(Vertex leftPoint, Vertex rightPoint, float yMax, Vertex dLeftPoint, Vertex dRightPoint, Vertex dLineInterpolant)
                        {
                            int yStart = Math.Max((int)Math.Ceiling(leftPoint.Position.Y), 0);
                            int yEnd = Math.Min((int)Math.Ceiling(yMax), (int)mainCamera.ScreenHeight);
                            float yPrestep = yStart - leftPoint.Position.Y;
                            leftPoint += dLeftPoint * yPrestep;
                            rightPoint += dRightPoint * yPrestep;
                            for (int y = yStart; y < yEnd; y++)
                            {
                                int xStart = Math.Max((int)Math.Ceiling(leftPoint.Position.X), 0);
                                int xEnd = Math.Min((int)Math.Ceiling(rightPoint.Position.X), (int)mainCamera.ScreenWidth);
                                float xPrestep = xStart - leftPoint.Position.X;
                                Vertex lineInterpolant = leftPoint + xPrestep * dLineInterpolant;
                                for (int x = xStart; x < xEnd; x++)
                                {
                                    if (zBufferV2!.Test(x, y, lineInterpolant.Position.Z))
                                    {
                                        Vertex correctedPoint = lineInterpolant * (1 / lineInterpolant.Position.W);
                                        zBufferV2.TestAndSet(x, y, lineInterpolant.Position.Z, shader.PixelShader(correctedPoint));
                                    }
                                        
                                    lineInterpolant += dLineInterpolant;
                                }
                                leftPoint += dLeftPoint;
                                rightPoint += dRightPoint;
                            }

                        }
                    }
#if DEBUG
            }
#else
                });
#endif
            Bitmap.FlushZBufferV2(zBufferV2!);
        }
        public void RenderSolid()
        {
            if (Bitmap == null)
                return;
            if (Scene.Obj == null)
                return;
            Obj obj = Scene.Obj;
            // prepare zbuffer
            ResizeAndClearZBuffer();

            // transform from model space to world space
            Matrix4x4 worldTransform = obj.transformation.Matrix;


            Camera mainCamera = Scene.Camera;
            // transform from world space to camera space
            Matrix4x4 cameraTransform = mainCamera.ViewMatrix;

            Matrix4x4 modelToCamera = worldTransform * cameraTransform;

            // transform from camera space to clipping space (divide by W to get to NDC space)
            mainCamera.ScreenHeight = Bitmap.PixelHeight;
            mainCamera.ScreenWidth = Bitmap.PixelWidth;
            Matrix4x4 projectionTransform = mainCamera.ProjectionMatrix;

            // transform from NDC space to viewport space
            Matrix4x4 viewPortTransform = mainCamera.ViewPortMatrix;

            for (int i = 0; i < obj.faces.Length; i++)
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
                            Vector3 lightDir = -(Vector3.Transform(obj.vertices[triangle.vIndices[i]], worldTransform) - Scene.Camera.Position);
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
                            Vector3 lightDir = -(Vector3.Transform(obj.vertices[triangle.vIndices[i]], worldTransform) - Scene.Camera.Position);
                            float vertexIllumination = Vector3.Dot(lightDir, translated) / (lightDir.Length() * translated.Length());
                            illumination += vertexIllumination;
                        }
                    }
                    illumination /= 3;
                    uint rgb = (uint)(illumination * 0xFF);
                    color &= (uint)((0xFF << 24) | (rgb << 16) | (rgb << 8) | rgb);
                    Bitmap.DrawTriangleWithZBuffer((int)mainCamera.ScreenWidth, (int)mainCamera.ScreenHeight, p0, p1, p2, color, zBuffer!);
                }
            }
        }
        public void RenderCarcass()
        {
            if (Bitmap == null)
                return;
            if (Scene.Obj == null)
                return;
            Obj obj = Scene.Obj;
            ResizeBuffer(obj);

            // transform from model space to world space
            Matrix4x4 worldTransform = obj.transformation.Matrix;

            // transform from world space to camera space
            Matrix4x4 cameraTransform = Scene.Camera.ViewMatrix;


            // transform from camera space to clipping space (divide by W to get to NDC space)
            int width = Bitmap.PixelWidth;
            int height = Bitmap.PixelHeight;
            float aspectRatio = (float)width / height;
            float fovVertical = MathF.PI / 3;
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

            Face[] faces = obj.faces;
            int facesCount = faces.Length;
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
