﻿using GraphicsLib.Primitives;
using GraphicsLib.Shaders;
using GraphicsLib.Types;
using System.Buffers;
using System.Collections.Concurrent;
using System.Numerics;
using System.Windows;
using System.Windows.Media.Imaging;

namespace GraphicsLib.Types2
{
    public class ModelRenderer
    {
        public ZBufferV2? Zbuffer { get; set; }
        private static readonly Queue<(Matrix4x4 Transform, ModelPrimitive Primitive)> nonOpaqueQueue = [];

        private static readonly ConcurrentDictionary<(Type, Type), object> pipelineCache = [];
        public static float TimeElapsed { get; set; } = 0;
        public void Render<Shader, Vertex>(in ModelScene scene,in WriteableBitmap Bitmap) where Shader : IModelShader<Vertex>, new() where Vertex : struct, IVertex<Vertex>
        {
            if (scene.RootModelNodes == null || scene.RootModelNodes.Length == 0 || scene.Camera == null)
                return;
            if(Zbuffer == null)
            {
                Zbuffer = new((int)scene.Camera.ScreenWidth, (int)scene.Camera.ScreenHeight);
                Zbuffer.ChangeDefaultColor(0xFF0080AA);
            }
            Zbuffer.ResizeAndClear((int)scene.Camera.ScreenWidth, (int)scene.Camera.ScreenHeight);
            var pipeline = GetPipeline<Shader, Vertex>();
            pipeline.BindScene(scene);
            pipeline.BindZBuffer(Zbuffer);
            Shader.BindScene(scene);
            for (int i = 0; i < scene.RootModelNodes.Length; i++)
            {
                RenderOpaqueRecursive<Shader, Vertex>(pipeline, scene.RootModelNodes[i], Matrix4x4.Identity);
            }
            Vector3 cameraPosition = scene.Camera.Position;
            foreach (var (Transform, Primitive) in nonOpaqueQueue.OrderBy(x =>  (Vector3.Transform(x.Primitive.BoundingBox!.Value.Center, x.Transform) - cameraPosition).LengthSquared()))
            {
                RenderNonOpaquePrimitive<Shader, Vertex>(pipeline, Primitive, Transform);
            }
            nonOpaqueQueue.Clear();
            pipeline.Unbind();
            Bitmap.FlushZBufferV2(Zbuffer);
        }
        private static void RenderNonOpaquePrimitive<Shader, Vertex>(in Pipeline<Shader, Vertex> pipeline, in ModelPrimitive primitive, in Matrix4x4 transform) where Shader : IModelShader<Vertex>, new() where Vertex : struct, IVertex<Vertex>
        {
            Shader.BindPrimitive(primitive, transform);
            pipeline.Render(primitive);
            Shader.UnbindPrimitive();
        }
        private static void RenderOpaqueRecursive<Shader, Vertex>(in Pipeline<Shader,Vertex> pipeline, in ModelNode node,in Matrix4x4 parentTransform) where Shader : IModelShader<Vertex>, new() where Vertex : struct, IVertex<Vertex>
        {
            Matrix4x4 currentTransformation = node.TransformationMatrix;
            if (node.Animations != null)
            {
                foreach (var animation in node.Animations)
                {
                    currentTransformation = animation.Apply(currentTransformation, TimeElapsed);
                }
            }
            currentTransformation *= parentTransform;
            if (node.ChildNodes != null)
            {
                foreach (var child in node.ChildNodes)
                {
                    RenderOpaqueRecursive<Shader, Vertex>(pipeline, child, currentTransformation);
                }
            }
            if(node.Mesh != null && pipeline.IsBoundingBoxWithinView(node.Mesh.BoundingBox!.Value, currentTransformation))
            {
                foreach (var primitive in node.Mesh.Primitives)
                {
                    RenderOpaquePrimitive(pipeline, primitive, currentTransformation);
                }
            }
        }
        private static void RenderOpaquePrimitive<Shader, Vertex>(in Pipeline<Shader, Vertex> pipeline, in ModelPrimitive primitive,in Matrix4x4 transform) where Shader : IModelShader<Vertex>, new() where Vertex : struct, IVertex<Vertex>
        {
            if (pipeline.IsBoundingBoxWithinView(primitive.BoundingBox!.Value, transform))
            {
                if (primitive.Material?.alphaMode == Types.GltfTypes.GltfMaterialAlphaMode.OPAQUE)
                {
                    Shader.BindPrimitive(primitive, transform);
                    pipeline.Render(primitive);
                    Shader.UnbindPrimitive();
                }
                else
                {
                    nonOpaqueQueue.Enqueue((transform, primitive));
                }

            }
        }

        private static Pipeline<Shader, Vertex> GetPipeline<Shader, Vertex>() where Shader : IModelShader<Vertex>, new() where Vertex : struct, IVertex<Vertex>
        {
            var key = (typeof(Shader), typeof(Vertex));
            return (Pipeline<Shader, Vertex>)pipelineCache.GetOrAdd(key, (key) => GeneratePipeline<Shader, Vertex>());
        }
        private static Pipeline<Shader, Vertex> GeneratePipeline<Shader, Vertex>() where Shader : IModelShader<Vertex>, new() where Vertex : struct, IVertex<Vertex>
        {
            return new Pipeline<Shader, Vertex>();
        }
        class Pipeline<Shader, Vertex> where Shader : IModelShader<Vertex>, new() where Vertex : struct, IVertex<Vertex>
        {
            public ModelScene? Scene { get; set; }
            private ModelPrimitive? currentPrimitive;
            private ZBufferV2? zBuffer;
            private Matrix4x4 cameraTransform;
            private Matrix4x4 projectionTransform;
            private Matrix4x4 viewPortTransform;
            private readonly Plane[] worldSpacevViewFrustumPlanes = new Plane[6];
            private int screenHeight;
            private int screenWidth;
            private Vertex[]? verticesBuffer = null;
            public void BindScene(ModelScene scene)
            {
                Scene = scene;
                cameraTransform = scene.Camera!.ViewMatrix;
                projectionTransform = scene.Camera.ProjectionMatrix;
                viewPortTransform = scene.Camera.ViewPortMatrix;
                screenHeight = (int)scene.Camera.ScreenHeight;
                screenWidth = (int)scene.Camera.ScreenWidth;
                ExtractFrustumPlanes();
            }
            private void ExtractFrustumPlanes()
            {
                Matrix4x4 viewProjectionMatrix = cameraTransform * projectionTransform;
                // Left plane
                worldSpacevViewFrustumPlanes[0] = new Plane(
                    viewProjectionMatrix.M14 + viewProjectionMatrix.M11,
                    viewProjectionMatrix.M24 + viewProjectionMatrix.M21,
                    viewProjectionMatrix.M34 + viewProjectionMatrix.M31,
                    viewProjectionMatrix.M44 + viewProjectionMatrix.M41
                );

                // Right plane
                worldSpacevViewFrustumPlanes[1] = new Plane(
                    viewProjectionMatrix.M14 - viewProjectionMatrix.M11,
                    viewProjectionMatrix.M24 - viewProjectionMatrix.M21,
                    viewProjectionMatrix.M34 - viewProjectionMatrix.M31,
                    viewProjectionMatrix.M44 - viewProjectionMatrix.M41
                );

                // Bottom plane
                worldSpacevViewFrustumPlanes[2] = new Plane(
                    viewProjectionMatrix.M14 + viewProjectionMatrix.M12,
                    viewProjectionMatrix.M24 + viewProjectionMatrix.M22,
                    viewProjectionMatrix.M34 + viewProjectionMatrix.M32,
                    viewProjectionMatrix.M44 + viewProjectionMatrix.M42
                );

                // Top plane
                worldSpacevViewFrustumPlanes[3] = new Plane(
                    viewProjectionMatrix.M14 - viewProjectionMatrix.M12,
                    viewProjectionMatrix.M24 - viewProjectionMatrix.M22,
                    viewProjectionMatrix.M34 - viewProjectionMatrix.M32,
                    viewProjectionMatrix.M44 - viewProjectionMatrix.M42
                );

                // Near plane
                worldSpacevViewFrustumPlanes[4] = new Plane(
                    viewProjectionMatrix.M14 + viewProjectionMatrix.M13,
                    viewProjectionMatrix.M24 + viewProjectionMatrix.M23,
                    viewProjectionMatrix.M34 + viewProjectionMatrix.M33,
                    viewProjectionMatrix.M44 + viewProjectionMatrix.M43
                );

                // Far plane
                worldSpacevViewFrustumPlanes[5] = new Plane(
                    viewProjectionMatrix.M14 - viewProjectionMatrix.M13,
                    viewProjectionMatrix.M24 - viewProjectionMatrix.M23,
                    viewProjectionMatrix.M34 - viewProjectionMatrix.M33,
                    viewProjectionMatrix.M44 - viewProjectionMatrix.M43
                );
                // Normalize all planes
                for (int i = 0; i < 6; i++)
                {
                    worldSpacevViewFrustumPlanes[i] = Plane.Normalize(worldSpacevViewFrustumPlanes[i]);
                }
            }
            public void BindZBuffer(ZBufferV2 zBuffer)
            {
                this.zBuffer = zBuffer;
            }
            public void Unbind()
            {
                Scene = null;
                zBuffer = null;
            }

            public static BoundingBox TransformBoundingBox(in BoundingBox localBox, in Matrix4x4 worldMatrix)
            {
                // Get all 8 corners in model space
                Span<Vector3> corners =
                [
                    new Vector3(localBox.Min.X, localBox.Min.Y, localBox.Min.Z),
                    new Vector3(localBox.Min.X, localBox.Min.Y, localBox.Max.Z),
                    new Vector3(localBox.Min.X, localBox.Max.Y, localBox.Min.Z),
                    new Vector3(localBox.Min.X, localBox.Max.Y, localBox.Max.Z),
                    new Vector3(localBox.Max.X, localBox.Min.Y, localBox.Min.Z),
                    new Vector3(localBox.Max.X, localBox.Min.Y, localBox.Max.Z),
                    new Vector3(localBox.Max.X, localBox.Max.Y, localBox.Min.Z),
                    new Vector3(localBox.Max.X, localBox.Max.Y, localBox.Max.Z),
                ];

                // Transform all corners to world space
                Vector3 min = new(float.MaxValue);
                Vector3 max = new(float.MinValue);

                for (int i = 0; i < 8; i++)
                {
                    Vector3 worldCorner = Vector3.Transform(corners[i], worldMatrix);
                    min = Vector3.Min(min, worldCorner);
                    max = Vector3.Max(max, worldCorner);
                }

                return new(min, max );
            }


            public bool IsBoundingBoxWithinView(in BoundingBox boundingBox, in Matrix4x4 transform)
            {
                var (min, max) = TransformBoundingBox(boundingBox, transform);
                for (int i = 0; i < 6; i++)
                {
                    ref readonly Plane plane = ref worldSpacevViewFrustumPlanes[i];

                    Vector3 pVertex;
                    pVertex.X = plane.Normal.X > 0 ? max.X : min.X;
                    pVertex.Y = plane.Normal.Y > 0 ? max.Y : min.Y;
                    pVertex.Z = plane.Normal.Z > 0 ? max.Z : min.Z;
                    if (Vector3.Dot(plane.Normal, pVertex) + plane.D < 0)
                        return false;
                }
                return true;
            }
            private ParallelOptions RenderLoopParallelOptions { get; set; } = new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount * 2};
            public void Render(ModelPrimitive primitive)
            {
                PreprocessVertices(primitive);
                switch (primitive.Mode)
                {
                    case Types.GltfTypes.GltfMeshMode.TRIANGLES:
                        {
                            Parallel.For(0, primitive.Indices!.Length / 3, RenderLoopParallelOptions, AssembleTriangle);
                        }
                        break;
                    case Types.GltfTypes.GltfMeshMode.TRIANGLE_STRIP:
                        {
                            Parallel.For(0, primitive.Indices!.Length - 2, RenderLoopParallelOptions, AssembleTriangleStrip);
                        }
                        break;
                    case Types.GltfTypes.GltfMeshMode.TRIANGLE_FAN:
                        {
                            Parallel.For(2, primitive.Indices!.Length - 2, RenderLoopParallelOptions, AssembleTriangleFan);
                        }
                        break;
                }
                CleanUpVertices();
            }
            private ParallelOptions PreprocessLoopParallelOptions { get; set; } = new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount };

            private void PreprocessVertices(ModelPrimitive primitive)
            {
                currentPrimitive = primitive;
                ArrayPool<Vertex> arrayPool = ArrayPool<Vertex>.Shared;
                int verticesCount = currentPrimitive.VertexCount;
                verticesBuffer = arrayPool.Rent(verticesCount);
                PreprocessLoopParallelOptions.MaxDegreeOfParallelism = Math.Min(verticesCount / 128 + 1, Environment.ProcessorCount);

                if (verticesCount > 128)
                {
                    Parallel.For(0, verticesCount, PreprocessLoopParallelOptions, i =>
                        verticesBuffer[i] = Shader.VertexShader(currentPrimitive, i)
                    );
                }
                else
                {
                    for (int i = 0; i < verticesCount; i++)
                    {
                        verticesBuffer[i] = Shader.VertexShader(currentPrimitive, i);
                    }
                }
        
            }
            private void CleanUpVertices()
            {
                currentPrimitive = null;
                ArrayPool<Vertex> arrayPool = ArrayPool<Vertex>.Shared;
                arrayPool.Return(verticesBuffer!);
                verticesBuffer = null;
            }
            private void AssembleTriangle(int i)
            {
                Vertex p0 = verticesBuffer![currentPrimitive!.Indices![3 * i]];
                Vertex p1 = verticesBuffer[currentPrimitive!.Indices![3 * i + 1]];
                Vertex p2 = verticesBuffer[currentPrimitive!.Indices![3 * i + 2]];
                MoveTriangleToCameraSpace(ref p0, ref p1, ref p2);
            }
            private void AssembleTriangleStrip(int i)
            {
                if(i % 2 == 1)
                {
                    Vertex p0 = verticesBuffer![currentPrimitive!.Indices![i]];
                    Vertex p1 = verticesBuffer[currentPrimitive!.Indices![i + 1]];
                    Vertex p2 = verticesBuffer[currentPrimitive!.Indices![i + 2]];
                    MoveTriangleToCameraSpace(ref p0, ref p1, ref p2);
                }
                else
                {
                    Vertex p1 = verticesBuffer![currentPrimitive!.Indices![i + 1]];
                    Vertex p0 = verticesBuffer[currentPrimitive!.Indices![i]];
                    Vertex p2 = verticesBuffer[currentPrimitive!.Indices![i + 2]];
                    MoveTriangleToCameraSpace(ref p0, ref p1, ref p2);
                }
            }
            private void AssembleTriangleFan(int i)
            {
                Vertex p1 = verticesBuffer![currentPrimitive!.Indices![0]];
                Vertex p0 = verticesBuffer[currentPrimitive!.Indices![i + 1]];
                Vertex p2 = verticesBuffer[currentPrimitive!.Indices![i + 2]];
                MoveTriangleToCameraSpace(ref p0, ref p1, ref p2);
            }
            private void MoveTriangleToCameraSpace(ref Vertex p0, ref Vertex p1, ref Vertex p2)
            {
                p0.Position = Vector4.Transform(p0.Position, cameraTransform);
                p1.Position = Vector4.Transform(p1.Position, cameraTransform);
                p2.Position = Vector4.Transform(p2.Position, cameraTransform);
                Vector4 normal = Vector3.Cross((p2.Position - p0.Position).AsVector3(), (p1.Position - p0.Position).AsVector3()).AsVector4();
                float orientation = Vector4.Dot(normal, p0.Position);
                //Cull triangle if its orientation is facing away from the camera
                if (orientation <= 0)
                    return;
                ProjectTriangle(p0, p1, p2);
            }
            private void ProjectTriangle(Vertex p0,Vertex p1, Vertex p2)
            {
                p0.Position = Vector4.Transform(p0.Position, projectionTransform);
                p1.Position = Vector4.Transform(p1.Position, projectionTransform);
                p2.Position = Vector4.Transform(p2.Position, projectionTransform);
                CullAndClipTriangle(p0, p1, p2);
            }
            private void CullAndClipTriangle(in Vertex p0, in Vertex p1, in Vertex p2)
            {
                //check if triangle is outside the view frustum
                if (p0.Position.X > p0.Position.W && p1.Position.X > p1.Position.W && p2.Position.X > p2.Position.W)
                    return;
                if (p0.Position.X < -p0.Position.W && p1.Position.X < -p1.Position.W && p2.Position.X < -p2.Position.W)
                    return;
                if (p0.Position.Y > p0.Position.W && p1.Position.Y > p1.Position.W && p2.Position.Y > p2.Position.W)
                    return;
                if (p0.Position.Y < -p0.Position.W && p1.Position.Y < -p1.Position.W && p2.Position.Y < -p2.Position.W)
                    return;
                if (p0.Position.Z > p0.Position.W && p1.Position.Z > p1.Position.W && p2.Position.Z > p2.Position.W)
                    return;
                if (p0.Position.Z < 0 && p1.Position.Z < 0 && p2.Position.Z < 0)
                    return;
                //Clipping triangle if it intersects near plane
                if (p0.Position.Z < 0)
                {
                    if (p1.Position.Z < 0)
                    {
                        ClipTriangleIntoOne(p0, p1, p2);
                    }
                    else if (p2.Position.Z < 0)
                    {
                        ClipTriangleIntoOne(p0, p2, p1);
                    }
                    else
                    {
                        ClipTriangleIntoTwo(p0, p1, p2);
                    }
                }
                else if (p1.Position.Z < 0)
                {
                    if (p2.Position.Z < 0)
                    {
                        ClipTriangleIntoOne(p1, p2, p0);
                    }
                    else
                    {
                        ClipTriangleIntoTwo(p1, p0, p2);
                    }
                }
                else if (p2.Position.Z < 0)
                {
                    ClipTriangleIntoTwo(p2, p0, p1);
                }
                else
                {
                    ProjectTriangleToViewPort(p0, p1, p2);
                }

            }
            private void ClipTriangleIntoTwo(in Vertex pointBehind, in Vertex p1, in Vertex p2)
            {
                float c0 = (-pointBehind.Position.Z) / (p1.Position.Z - pointBehind.Position.Z);
                float c1 = (-pointBehind.Position.Z) / (p2.Position.Z - pointBehind.Position.Z);
                Vertex leftInterpolant = Vertex.Lerp(pointBehind, p1, c0);
                Vertex rightInterpolant = Vertex.Lerp(pointBehind, p2, c1);
                ProjectTriangleToViewPort(leftInterpolant, p1, p2);
                ProjectTriangleToViewPort(rightInterpolant, leftInterpolant, p2);
            }
            private void ClipTriangleIntoOne(in Vertex leftPointBehind, in Vertex rightPointBehind, in Vertex p2)
            {
                float c0 = (-leftPointBehind.Position.Z) / (p2.Position.Z - leftPointBehind.Position.Z);
                float c1 = (-rightPointBehind.Position.Z) / (p2.Position.Z - rightPointBehind.Position.Z);
                Vertex leftInterpolant = Vertex.Lerp(leftPointBehind, p2, c0);
                Vertex rightInterpolant = Vertex.Lerp(rightPointBehind, p2, c1);
                ProjectTriangleToViewPort(leftInterpolant, p2, rightInterpolant);
            }
            private void ProjectTriangleToViewPort(Vertex p0, Vertex p1, Vertex p2)
            {
                TransformToViewPort(ref p0);
                TransformToViewPort(ref p1);
                TransformToViewPort(ref p2);
                DrawTriangle(p0, p1, p2);
            }
            private void TransformToViewPort(ref Vertex vertex)
            {
                float invZ = 1 / vertex.Position.W;
                // divide all vertex fields to apply projection correction later
                vertex *= invZ;
                Vector4 ndcPosition = Vector4.Transform(vertex.Position, viewPortTransform);
                // save 1/z to use it in projection correction
                ndcPosition.W = invZ;
                vertex.Position = ndcPosition;
            }
            private void DrawTriangle(in Vertex p0, in Vertex p1, in Vertex p2)
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
                    DrawFlatTopTriangle(min, mid, max);
                }
                else if (max.Position.Y == mid.Position.Y)
                {
                    //flat bottom
                    if (max.Position.X > mid.Position.X)
                    {
                        (mid, max) = (max, mid);
                    }
                    DrawFlatBottomTriangle(min, mid, max);
                }
                else
                {
                    float c = (mid.Position.Y - min.Position.Y) / (max.Position.Y - min.Position.Y);
                    Vertex interpolant = Vertex.Lerp(min, max, c);
                    if (interpolant.Position.X > mid.Position.X)
                    {
                        //right major
                        DrawFlatBottomTriangle(min, interpolant, mid);
                        DrawFlatTopTriangle(mid, interpolant, max);
                    }
                    else
                    {
                        //left major
                        DrawFlatBottomTriangle(min, mid, interpolant);
                        DrawFlatTopTriangle(interpolant, mid, max);
                    }
                }
            }
            void DrawFlatTopTriangle(in Vertex leftTopPoint, in Vertex rightTopPoint, in Vertex bottomPoint)
            {
                float dy = bottomPoint.Position.Y - leftTopPoint.Position.Y;
                Vertex dLeftPoint = (bottomPoint - leftTopPoint) / dy;
                Vertex dRightPoint = (bottomPoint - rightTopPoint) / dy;
                Vertex dLineInterpolant = (rightTopPoint - leftTopPoint) / (rightTopPoint.Position.X - leftTopPoint.Position.X);
                Vertex rightPoint = rightTopPoint;
                DrawFlatTriangle(leftTopPoint, rightPoint, bottomPoint.Position.Y, dLeftPoint, dRightPoint, dLineInterpolant);
            }
            void DrawFlatBottomTriangle(in Vertex topPoint, in Vertex rightBottomPoint, in Vertex leftBottomPoint)
            {
                float dy = rightBottomPoint.Position.Y - topPoint.Position.Y;
                Vertex dRightPoint = (rightBottomPoint - topPoint) / dy;
                Vertex dLeftPoint = (leftBottomPoint - topPoint) / dy;
                Vertex rightPoint = topPoint;
                Vertex DLineInterpolant = (rightBottomPoint - leftBottomPoint) / (rightBottomPoint.Position.X - leftBottomPoint.Position.X);
                DrawFlatTriangle(topPoint, rightPoint, rightBottomPoint.Position.Y, dLeftPoint, dRightPoint, DLineInterpolant);
            }
            void DrawFlatTriangle(Vertex leftPoint, Vertex rightPoint, float yMax, in Vertex dLeftPoint, in Vertex dRightPoint, in Vertex dLineInterpolant)
            {
                int yStart = Math.Max((int)MathF.Ceiling(leftPoint.Position.Y), 0);
                int yEnd = Math.Min((int)MathF.Ceiling(yMax), screenHeight);
                float yPrestep = yStart - leftPoint.Position.Y;
                leftPoint += dLeftPoint * yPrestep;
                rightPoint += dRightPoint * yPrestep;
                for (int y = yStart; y < yEnd; y++, leftPoint += dLeftPoint, rightPoint += dRightPoint)
                {
                    int xStart = Math.Max((int)MathF.Ceiling(leftPoint.Position.X), 0);
                    int xEnd = Math.Min((int)MathF.Ceiling(rightPoint.Position.X), screenWidth);
                    if (xStart >= xEnd)
                    {
                        continue;
                    }
                    float xPrestep = xStart - leftPoint.Position.X;
                    Vertex lineInterpolant = leftPoint + xPrestep * dLineInterpolant;
                    for (int x = xStart; x < xEnd; x++, lineInterpolant += dLineInterpolant)
                    {
                        if (zBuffer!.Test(x, y, -lineInterpolant.Position.W))
                        {
                            Vertex correctedPoint = lineInterpolant * (1 / lineInterpolant.Position.W);

                            Vector4 color = Shader.PixelShader(correctedPoint) * 0xFF;

                            if(currentPrimitive!.Material?.alphaMode == Types.GltfTypes.GltfMaterialAlphaMode.BLEND)
                            {
                                if(color.W <= 0)
                                {
                                    continue;
                                }
                                uint encodedPrevColor = zBuffer[x, y].color;
                                Vector4 prevColor = new((encodedPrevColor >> 16) & 0xFF,
                                                        (encodedPrevColor >> 8) & 0xFF,
                                                        encodedPrevColor & 0xFF,
                                                        (encodedPrevColor >> 24) & 0xFF);
                                var finalColor = Vector4.Lerp(prevColor, color, color.W / 0xFF);
                                uint colorUint = (uint)(finalColor.W) << 24
                                            | (uint)(finalColor.X) << 16
                                            | (uint)(finalColor.Y) << 8
                                            | (uint)(finalColor.Z);
                                zBuffer.TestAndSet(x, y, -lineInterpolant.Position.W, colorUint);
                            }
                            else
                            {
                                uint colorUint = (uint)(color.W) << 24
                                            | (uint)(color.X) << 16
                                            | (uint)(color.Y) << 8
                                            | (uint)(color.Z);
                                zBuffer.TestAndSet(x, y, -lineInterpolant.Position.W, colorUint);
                            }
                               
                        }
                    }
                }
            }
        }
    }
}
