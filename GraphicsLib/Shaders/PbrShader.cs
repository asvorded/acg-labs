using GraphicsLib.Primitives;
using GraphicsLib.Types;
using System.Numerics;
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

        public struct Vertex : IVertex<Vertex>
        {
            public Material Material { get; set; }
            public Vector4 Position { get; set; }
            public Vector3 Normal { get; set; }
            public Vector3 WorldPosition { get; set; }
            public Vector2 Uv { get; set; }
            public Vector4 Tangent { get; set; }
            public Vector2 NormalUv { get; set; }
            public Vector2 RoughnessUv { get; set; }

            public static Vertex Lerp(Vertex a, Vertex b, float t)
            {
                return new Vertex
                {
                    Position = Vector4.Lerp(a.Position, b.Position, t),
                    Normal = Vector3.Lerp(a.Normal, b.Normal, t),
                    WorldPosition = Vector3.Lerp(a.WorldPosition, b.WorldPosition, t),
                    Uv = Vector2.Lerp(a.Uv, b.Uv, t),
                    Tangent = Vector4.Lerp(a.Tangent, b.Tangent, t),
                    NormalUv = Vector2.Lerp(a.NormalUv, b.NormalUv, t),
                    RoughnessUv = Vector2.Lerp(a.RoughnessUv, b.RoughnessUv, t),
                    Material = a.Material
                };
            }
            public static Vertex operator +(Vertex lhs, Vertex rhs)
            {
                return new Vertex
                {
                    Position = lhs.Position + rhs.Position,
                    Normal = lhs.Normal + rhs.Normal,
                    WorldPosition = lhs.WorldPosition + rhs.WorldPosition,
                    Uv = lhs.Uv + rhs.Uv,
                    Tangent = lhs.Tangent + rhs.Tangent,
                    NormalUv = lhs.NormalUv + rhs.NormalUv,
                    RoughnessUv = lhs.RoughnessUv + rhs.RoughnessUv,
                    Material = lhs.Material
                };
            }
            public static Vertex operator -(Vertex lhs, Vertex rhs)
            {
                return new Vertex
                {
                    Position = lhs.Position - rhs.Position,
                    Normal = lhs.Normal - rhs.Normal,
                    WorldPosition = lhs.WorldPosition - rhs.WorldPosition,
                    Uv = lhs.Uv - rhs.Uv,
                    Tangent = lhs.Tangent - rhs.Tangent,
                    NormalUv = lhs.NormalUv - rhs.NormalUv,
                    RoughnessUv = lhs.RoughnessUv - rhs.RoughnessUv,
                    Material = lhs.Material
                };
            }
            public static Vertex operator *(Vertex lhs, float scalar)
            {
                return new Vertex
                {
                    Position = lhs.Position * scalar,
                    Normal = lhs.Normal * scalar,
                    WorldPosition = lhs.WorldPosition * scalar,
                    Uv = lhs.Uv * scalar,
                    Tangent = lhs.Tangent * scalar,
                    NormalUv = lhs.NormalUv * scalar,
                    RoughnessUv = lhs.RoughnessUv * scalar,
                    Material = lhs.Material
                };
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

        public uint PixelShader(Vertex input)
        {
            Vector3 finalColor = default;
            Material material = input.Material;

            //calculate normal
            Vector3 normal = Vector3.Normalize(input.Normal);
            if (material.normalTextureSampler != null)
            {
                Vector3 tangent = input.Tangent.AsVector3();
                Vector3 tangentSpaceNormal = material.normalTextureSampler.Sample(input.NormalUv).AsVector3();
                //decode
                tangentSpaceNormal = tangentSpaceNormal * 2 - new Vector3(1, 1, 1);
                Vector3 bitangent = Vector3.Cross(normal, tangent);
                normal = Vector3.Normalize(tangent * tangentSpaceNormal.X + bitangent * tangentSpaceNormal.Y + normal * tangentSpaceNormal.Z);
            }
            //calculate all lighting related vectors
            Vector3 viewDir = Vector3.Normalize(cameraPos - input.WorldPosition);
            Vector3 lightDir = Vector3.Normalize(lightPosition - input.WorldPosition);
            Vector3 halfWayDir = Vector3.Normalize(lightDir + viewDir);
            //calculate roughness and metallic
            float roughness = material.roughness;
            float metallic = material.metallic;
            if (material.metallicRoughnessTextureSampler != null)
            {
                Vector4 metallicRoughness  = material.metallicRoughnessTextureSampler.Sample(input.RoughnessUv);
                roughness *= metallicRoughness.Y;
                metallic *= metallicRoughness.Z;
            }
            //Calculate base color
            Vector4 diffuseColor = material.baseColor;
            if (material.baseColorTextureSampler != null)
            {
                diffuseColor *= material.baseColorTextureSampler.Sample(input.Uv);
            }
            //calculate pbr lighting
            Vector3 baseReflectivity = Vector3.Lerp(new Vector3(0.04f), diffuseColor.AsVector3(), metallic);
            float nDotV = Math.Max(Vector3.Dot(normal, viewDir), 0);
            float oneMinusNDotV = 1 - nDotV;
            Vector3 fresnel = baseReflectivity + (new Vector3(1) - baseReflectivity) * (oneMinusNDotV * oneMinusNDotV * oneMinusNDotV * oneMinusNDotV * oneMinusNDotV);
            Vector3 kSpecular = fresnel;
            Vector3 kDiffuse = (new Vector3(1)); //- kSpecular);// * (1 - metallic);
            float alpha = roughness * roughness;
            float alphaSqr = alpha * alpha;
            float nDotH = Math.Max(Vector3.Dot(normal, halfWayDir), 0);
            float denomPart = (alphaSqr - 1) * nDotH * nDotH + 1;
            float normalDistribution = alphaSqr / MathF.Max((MathF.PI * denomPart * denomPart), 0.0001f);
            float k = alpha * 0.5f;
            float nDotL = Math.Max(Vector3.Dot(normal, lightDir), 0);

            //float gl = nDotL / Math.Max((nDotL * (1 - k) + k), 0.0001f);
            //float gv = nDotV / Math.Max((nDotV * (1 - k) + k), 0.0001f);
            float gl = MathF.ReciprocalEstimate(Math.Max((nDotL * (1 - k) + k), 0.0001f));
            float gv = MathF.ReciprocalEstimate(Math.Max((nDotV * (1 - k) + k), 0.0001f));
            float geometryShading = gl * gv;
            //Vector3 cookTorrance = kSpecular * (normalDistribution * geometryShading / MathF.Max((4 * nDotL * nDotV), 0.0001f));
            Vector3 cookTorrance = kSpecular * (normalDistribution * geometryShading * 0.25f);
            Vector3 diffuse = diffuseColor.AsVector3() / MathF.PI;
            Vector3 bdfs = cookTorrance + diffuse;// * kDiffuse;
            finalColor = Vector3.Clamp(bdfs * lightColor * (nDotL * lightIntensity) 
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
            vertex.Normal = Vector3.TransformNormal(obj.normals[face.nIndices[vertexIndex]], worldNormalTransform);
            vertex.WorldPosition = vertex.Position.AsVector3();
            if (face.tIndices == null)
                throw new ArgumentException("Face has no texture indices, BRUH");
            vertex.Uv = obj.uvs[face.tIndices[vertexIndex]];
            if (face.tangentIndicies != null)
                vertex.Tangent = obj.tangents[face.tangentIndicies[vertexIndex]];
            if (face.ntIndicies != null)
                vertex.NormalUv = obj.normalUvs[face.ntIndicies[vertexIndex]];
            if (face.rtIndicies != null)
                vertex.RoughnessUv = obj.roughnessUvs[face.rtIndicies[vertexIndex]];
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
                    vertex.Normal = Vector3.TransformNormal(triangle.normal0, worldNormalTransform);
                    vertex.Uv = triangle.uvCoordinate0;
                    vertex.Tangent = triangle.tangent0;
                    vertex.NormalUv = triangle.normalUvCoordinate0;
                    vertex.RoughnessUv = triangle.roughnessUvCoordinate0;
                    break;
                case 1:
                    vertex.Position = Vector4.Transform(new Vector4(triangle.position1, 1), worldTransform);
                    vertex.Normal = Vector3.TransformNormal(triangle.normal1, worldNormalTransform);
                    vertex.Uv = triangle.uvCoordinate1;
                    vertex.Tangent = triangle.tangent1;
                    vertex.NormalUv = triangle.normalUvCoordinate1;
                    vertex.RoughnessUv = triangle.roughnessUvCoordinate1;
                    break;
                case 2:
                    vertex.Position = Vector4.Transform(new Vector4(triangle.position2, 1), worldTransform);
                    vertex.Normal = Vector3.TransformNormal(triangle.normal2, worldNormalTransform);
                    vertex.Uv = triangle.uvCoordinate2;
                    vertex.Tangent = triangle.tangent2;
                    vertex.NormalUv = triangle.normalUvCoordinate2;
                    vertex.RoughnessUv = triangle.roughnessUvCoordinate2;
                    break;
                default:
                    throw new ArgumentException("Invalid vertex index");
            }
            vertex.Material = triangle.material;
            vertex.WorldPosition = vertex.Position.AsVector3();
            return vertex;
        }
    }
}
