﻿using GraphicsLib.Primitives;
using GraphicsLib.Types;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using static GraphicsLib.Shaders.PbrShader;

namespace GraphicsLib.Shaders
{
    public class PbrShader : IShader<Vertex>
    {
        public Scene Scene { get => scene; set => SetSceneParams(value); }

        private Vector3 ambientLightColor;
        private float ambientLightIntensity;
        private Vector3 lightColor;
        private float lightIntensity;
        private Vector3 lightPosition;
        private void SetSceneParams(Scene value)
        {
            scene = value;
            if (scene.Obj == null)
                throw new ArgumentException("Scene object is null");
            //caching all values to avoid calling heavy properties
            worldTransform = scene.Obj.transformation.Matrix;
            worldNormalTransform = scene.Obj.transformation.NormalMatrix;
            cameraPos = scene.Camera.Position;
            ambientLightColor = scene.AmbientColor;
            ambientLightIntensity = scene.AmbientIntensity;
            lightColor = scene.LightColor;
            lightIntensity = scene.LightIntensity;
            lightPosition = scene.LightPosition;
        }

        private Matrix4x4 worldTransform;
        private Matrix4x4 worldNormalTransform;
        private Scene scene;
        private Vector3 cameraPos;
        public PbrShader()
        {
        }
        public PbrShader(Scene scene)
        {
            Scene = scene;
        }
        public uint PixelShader(in Vertex input)
        {
            Material material = input.Material;

            //calculate normal
            Vector3 normal = Vector3.Normalize(input.normal);
            if (material.normalTextureSampler != null)
            {
                float sign = input.tangent.W;
                Vector3 tangent = input.tangent.AsVector3();
                Vector3 tangentSpaceNormal = material.normalTextureSampler.Sample(input.normalUv).AsVector3();
                //decode
                tangentSpaceNormal = tangentSpaceNormal * 2 - new Vector3(1, 1, 1);
                Vector3 bitangent = sign * Vector3.Cross(normal, tangent);
                normal = Vector3.Normalize(tangent * tangentSpaceNormal.X + bitangent * tangentSpaceNormal.Y + normal * tangentSpaceNormal.Z);
            }
            //calculate all lighting related vectors
            Vector3 viewDir = Vector3.Normalize(cameraPos - input.worldPosition);
            Vector3 lightDir = Vector3.Normalize(lightPosition - input.worldPosition);
            Vector3 halfWayDir = Vector3.Normalize(lightDir + viewDir);
            //calculate roughness and metallic
            float roughness = material.roughness;
            float metallic = material.metallic;
            if (material.metallicRoughnessTextureSampler != null)
            {
                Vector4 metallicRoughness  = material.metallicRoughnessTextureSampler.Sample(input.roughnessUv);
                roughness *= metallicRoughness.Y;
                metallic *= metallicRoughness.Z;
            }
            //Calculate base color
            Vector4 diffuseColor = material.baseColor;
            if (material.baseColorTextureSampler != null)
            {
                diffuseColor *= material.baseColorTextureSampler.Sample(input.uv);
            }
            float nDotL = Math.Max(Vector3.Dot(normal, lightDir), 0);
            if (nDotL <= 0)
            {
                return (uint)(diffuseColor.W * 0xFF) << 24;
            }
            //calculate pbr lighting
            Vector3 baseReflectivity = Vector3.Lerp(new Vector3(1.0f), diffuseColor.AsVector3(), metallic);
            float nDotV = Math.Max(Vector3.Dot(normal, viewDir), 0);
            float oneMinusNDotV = 1 - nDotV;
            Vector3 fresnel = baseReflectivity + (new Vector3(1) - baseReflectivity) * (oneMinusNDotV * oneMinusNDotV * oneMinusNDotV * oneMinusNDotV * oneMinusNDotV);
            Vector3 kSpecular = fresnel;
            Vector3 kDiffuse = (new Vector3(1));/// - fresnel;
            float alpha = roughness;
            float alphaSqr = alpha * alpha;
            float nDotH = Math.Max(Vector3.Dot(normal, halfWayDir), 0);
            float denomPart = (alphaSqr - 1) * nDotH * nDotH + 1;
            float normalDistribution = alphaSqr / MathF.Max((MathF.PI * denomPart * denomPart), 0.0001f);
            float k = (alpha + 1)*(alpha + 1) * 0.125f;
            
            float gl = MathF.ReciprocalEstimate(Math.Max((nDotL * (1 - k) + k), 0.001f));
            float gv = MathF.ReciprocalEstimate(Math.Max((nDotV * (1 - k) + k), 0.001f));
            float geometryShading = gl * gv;
            Vector3 cookTorrance = kSpecular * (normalDistribution * geometryShading * 0.25f);
            Vector3 diffuse = diffuseColor.AsVector3();
            Vector3 bdfs = cookTorrance + (diffuse * kDiffuse);
            Vector3 finalColor = Vector3.Clamp(bdfs * lightColor * (nDotL * lightIntensity)
                            + diffuseColor.AsVector3() * ambientLightColor * ambientLightIntensity
                            , Vector3.Zero
                            , new Vector3(1));
            return (uint)(diffuseColor.W * 0xFF) << 24
                         | (uint)(finalColor.X * 0xFF) << 16
                         | (uint)(finalColor.Y * 0xFF) << 8
                         | (uint)(finalColor.Z * 0xFF);
        }

        public Vertex GetVertexWithWorldPositionFromFace(Obj obj, int faceIndex, int vertexIndex)
        {
            Face face = obj.faces[faceIndex];
            Vertex vertex = default;
            vertex.Position = Vector4.Transform(new Vector4(obj.vertices[face.vIndices[vertexIndex]], 1), worldTransform);
            if (face.nIndices == null)
                throw new ArgumentException("Face has no normal indices, BRUUUUUUUUUUUUUUUUUUUUUUUUUUUUUUUUUUUUUUUUH");
            vertex.normal = Vector3.TransformNormal(obj.normals[face.nIndices[vertexIndex]], worldNormalTransform);
            vertex.worldPosition = vertex.Position.AsVector3();
            if (face.tIndices == null)
                throw new ArgumentException("Face has no texture indices, BRUH");
            vertex.uv = obj.uvs[face.tIndices[vertexIndex]];
            if (face.tangentIndicies != null)
                vertex.tangent = obj.tangents[face.tangentIndicies[vertexIndex]];
            if (face.ntIndicies != null)
                vertex.normalUv = obj.normalUvs[face.ntIndicies[vertexIndex]];
            if (face.rtIndicies != null)
                vertex.roughnessUv = obj.roughnessUvs[face.rtIndicies[vertexIndex]];
            vertex.Material = obj.materials[face.MaterialIndex];
            return vertex;
        }
        public Vertex GetVertexWithWorldPositionFromTriangle(Obj obj, int triangleIndex, int vertexIndex)
        {
            StaticTriangle triangle = obj.triangles[triangleIndex];
            Vertex vertex = default;
            switch (vertexIndex)
            {
                case 0:
                    vertex.Position = Vector4.Transform(new Vector4(triangle.position0, 1), worldTransform);
                    vertex.normal = Vector3.TransformNormal(triangle.normal0, worldNormalTransform);
                    vertex.uv = triangle.uvCoordinate0;
                    vertex.tangent = triangle.tangent0;
                    vertex.normalUv = triangle.normalUvCoordinate0;
                    vertex.roughnessUv = triangle.roughnessUvCoordinate0;
                    break;
                case 1:
                    vertex.Position = Vector4.Transform(new Vector4(triangle.position1, 1), worldTransform);
                    vertex.normal = Vector3.TransformNormal(triangle.normal1, worldNormalTransform);
                    vertex.uv = triangle.uvCoordinate1;
                    vertex.tangent = triangle.tangent1;
                    vertex.normalUv = triangle.normalUvCoordinate1;
                    vertex.roughnessUv = triangle.roughnessUvCoordinate1;
                    break;
                case 2:
                    vertex.Position = Vector4.Transform(new Vector4(triangle.position2, 1), worldTransform);
                    vertex.normal = Vector3.TransformNormal(triangle.normal2, worldNormalTransform);
                    vertex.uv = triangle.uvCoordinate2;
                    vertex.tangent = triangle.tangent2;
                    vertex.normalUv = triangle.normalUvCoordinate2;
                    vertex.roughnessUv = triangle.roughnessUvCoordinate2;
                    break;
                default:
                    throw new ArgumentException("Invalid vertex index");
            }
            vertex.Material = triangle.material;
            vertex.worldPosition = vertex.Position.AsVector3();
            return vertex;
        }
        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct Vertex : IVertex<Vertex>
        {
            public Material Material { get; set; }
            public Vector4 Position { readonly get => position; set => position = value; }
            private Vector4 position;
            public Vector4 tangent;
            public Vector3 normal;
            public Vector3 worldPosition;
            public Vector2 uv;
            public Vector2 normalUv;
            public Vector2 roughnessUv;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Vertex Lerp(Vertex a, Vertex b, float t)
            {
                if (Avx2.IsSupported)
                {
                    return a * (1 - t) + b * t;
                }
                else
                {
                    return new Vertex
                    {
                        Position = Vector4.Lerp(a.Position, b.Position, t),
                        normal = Vector3.Lerp(a.normal, b.normal, t),
                        worldPosition = Vector3.Lerp(a.worldPosition, b.worldPosition, t),
                        uv = Vector2.Lerp(a.uv, b.uv, t),
                        tangent = Vector4.Lerp(a.tangent, b.tangent, t),
                        normalUv = Vector2.Lerp(a.normalUv, b.normalUv, t),
                        roughnessUv = Vector2.Lerp(a.roughnessUv, b.roughnessUv, t),
                        Material = a.Material
                    };
                }

            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Vertex operator +(Vertex lhs, Vertex rhs)
            {
                if (Avx2.IsSupported)
                {
                    unsafe
                    {
                        Vertex vertex = default;
                        Avx2.Store((float*)&vertex.position, Avx2.Add(Avx2.LoadVector256((float*)&lhs.position), Avx2.LoadVector256((float*)&rhs.position)));
                        Avx2.Store((float*)&vertex.normal, Avx2.Add(Avx2.LoadVector256((float*)&lhs.normal), Avx2.LoadVector256((float*)&rhs.normal)));
                        Avx2.Store((float*)&vertex.normalUv, Avx2.Add(Avx2.LoadVector128((float*)&lhs.normalUv), Avx2.LoadVector128((float*)&rhs.normalUv)));
                        vertex.Material = lhs.Material;
                        return vertex;
                    }
                }
                else
                {
                    return new Vertex
                    {
                        Position = lhs.Position + rhs.Position,
                        normal = lhs.normal + rhs.normal,
                        worldPosition = lhs.worldPosition + rhs.worldPosition,
                        uv = lhs.uv + rhs.uv,
                        tangent = lhs.tangent + rhs.tangent,
                        normalUv = lhs.normalUv + rhs.normalUv,
                        roughnessUv = lhs.roughnessUv + rhs.roughnessUv,
                        Material = lhs.Material
                    };

                }

            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Vertex operator -(Vertex lhs, Vertex rhs)
            {
                if (Avx2.IsSupported)
                {
                    unsafe
                    {
                        Vertex vertex = default;
                        Avx2.Store((float*)&vertex.position, Avx2.Subtract(Avx2.LoadVector256((float*)&lhs.position), Avx2.LoadVector256((float*)&rhs.position)));
                        Avx2.Store((float*)&vertex.normal, Avx2.Subtract(Avx2.LoadVector256((float*)&lhs.normal), Avx2.LoadVector256((float*)&rhs.normal)));
                        Avx2.Store((float*)&vertex.normalUv, Avx2.Subtract(Avx2.LoadVector128((float*)&lhs.normalUv), Avx2.LoadVector128((float*)&rhs.normalUv)));
                        vertex.Material = lhs.Material;
                        return vertex;
                    }
                }
                else
                {
                    return new Vertex
                    {
                        Position = lhs.Position - rhs.Position,
                        normal = lhs.normal - rhs.normal,
                        worldPosition = lhs.worldPosition - rhs.worldPosition,
                        uv = lhs.uv - rhs.uv,
                        tangent = lhs.tangent - rhs.tangent,
                        normalUv = lhs.normalUv - rhs.normalUv,
                        roughnessUv = lhs.roughnessUv - rhs.roughnessUv,
                        Material = lhs.Material
                    };
                }
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Vertex operator *(Vertex lhs, float scalar)
            {
                if (Avx2.IsSupported)
                {
                    unsafe
                    {
                        Vertex vertex = default;
                        Vector256<float> multiplier = Avx2.BroadcastScalarToVector256(&scalar);
                        Avx2.Store((float*)&vertex.position, Avx2.Multiply(Avx2.LoadVector256((float*)&lhs.position), multiplier));
                        Avx2.Store((float*)&vertex.normal, Avx2.Multiply(Avx2.LoadVector256((float*)&lhs.normal), multiplier));
                        Avx2.Store((float*)&vertex.normalUv, Avx2.Multiply(Avx2.LoadVector128((float*)&lhs.normalUv), multiplier.GetLower()));
                        vertex.Material = lhs.Material;
                        return vertex;
                    }
                }
                else
                {
                    return new Vertex
                    {
                        Position = lhs.Position * scalar,
                        normal = lhs.normal * scalar,
                        worldPosition = lhs.worldPosition * scalar,
                        uv = lhs.uv * scalar,
                        tangent = lhs.tangent * scalar,
                        normalUv = lhs.normalUv * scalar,
                        roughnessUv = lhs.roughnessUv * scalar,
                        Material = lhs.Material
                    };
                }

            }
            public static Vertex operator *(float scalar, Vertex rhs)
            {
                return rhs * scalar;
            }
            public static Vertex operator /(Vertex lhs, float scalar)
            {
                return lhs * (1 / scalar);
            }
        }
    }
}
