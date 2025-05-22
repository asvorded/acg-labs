using GraphicsLib.Types;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using static GraphicsLib.Types2.Shaders.PhongShader;

namespace GraphicsLib.Types2.Shaders
{
    public unsafe class PhongShader : IModelShader<PhongVertex>
    {
        static Material? currentMaterial = null;
        static ModelSkin? currentSkin = null;
        static LightSource[]? lightSources = null;
        static Matrix4x4 worldTransformation = Matrix4x4.Identity;
        static Matrix4x4 normalTransformation = Matrix4x4.Identity;
        static Vector3 cameraPosition;
        static Vector3 ambientLightColor;
        static float ambientLightIntensity;
        static Vector3 lightColor;

        static Vector3* positionsArray = null;
        static Vector3* normalsArray = null;
        static ushort* jointsArray = null;
        static float* weightsArray = null;

        public static void BindScene(in ModelScene scene)
        {
            cameraPosition = scene.Camera!.Position;
            ambientLightColor = new Vector3(1f);
            ambientLightIntensity = 0.5f;
            lightSources = scene.LightSources;
        }
        public static void UnbindScene()
        {
            lightSources = null;
        }
        public static void BindSkin(in ModelSkin skin)
        {
            currentSkin = skin;
        }
        public static void UnbindSkin()
        {
            currentSkin = null;
        }
        public static void BindPrimitive(in ModelPrimitive primitive, in Matrix4x4 transformation)
        {
            currentMaterial = primitive.Material;
            worldTransformation = transformation;
            Matrix4x4.Invert(transformation, out Matrix4x4 inverse);
            normalTransformation = Matrix4x4.Transpose(inverse);
            BindAttributes(primitive);
        }
        public static void UnbindPrimitive()
        {
            UnbindAttributes();
            currentMaterial = null;
        }
        private static void BindAttributes(in ModelPrimitive primitive)
        {
            unsafe
            {
                positionsArray = ModelShaderUtils.GetAttributePointer<Vector3>(primitive, "POSITION");
                normalsArray = ModelShaderUtils.GetAttributePointer<Vector3>(primitive, "NORMAL");
                jointsArray = ModelShaderUtils.GetJointsPointer(primitive);
                weightsArray = ModelShaderUtils.GetAttributePointer<float>(primitive, "WEIGHTS_0");
            }
        }
        private static void UnbindAttributes()
        {
            positionsArray = null;
            normalsArray = null;
            jointsArray = null;
            weightsArray = null;
        }

        public static Vector4 PixelShader(in PhongVertex input)
        {
            Vector3 diffuseColor = new Vector3(0,0.6f,1);
            Vector3 normal = Vector3.Normalize(input.Normal);
            float specularPower = 100;
            Vector3 viewDir = Vector3.Normalize(cameraPosition - input.WorldPosition);
            Vector3 ambient = Vector3.Clamp(diffuseColor * ambientLightColor * ambientLightIntensity
                            , Vector3.Zero
                            , new Vector3(1));
            Vector3 finalColor = ambient;
            //calculate all lighting related vectors
            if (lightSources != null)
            {
                foreach (var lightSource in lightSources)
                {
                    lightSource.CalculateLightDirAndIntensity(input.WorldPosition, out Vector3 lightDir, out float intensity);
                    float nDotL = Math.Max(Vector3.Dot(normal, -lightDir), 0);
                    Vector3 reflectDir = Vector3.Reflect(lightDir, normal);
                    if (nDotL <= 0 || intensity < 0.00001f)
                    {
                        continue;
                    }
                    float specularFactor = MathF.Pow(Math.Max(Vector3.Dot(reflectDir, viewDir), 0), specularPower);
                    finalColor += (diffuseColor * lightSource.Color + lightSource.Color * specularFactor) * (nDotL * intensity);
                }
            }
            return new Vector4(Vector3.Clamp(finalColor, Vector3.Zero, new Vector3(1)), 1);
        }
        public static PhongVertex VertexShader(in ModelPrimitive primitive, in int vertexDataIndex)
        {
            Vector4 initialPosition = new Vector4(positionsArray[vertexDataIndex], 1);
            Vector3 initialNormal = normalsArray == null ? Vector3.Zero : normalsArray[vertexDataIndex];
            if (currentSkin != null)
            {
                initialPosition = weightsArray[vertexDataIndex * 4] * Vector4.Transform(initialPosition, GetInversedBoneTransform(jointsArray[vertexDataIndex * 4])) +
                    weightsArray[vertexDataIndex * 4 + 1] * Vector4.Transform(initialPosition, GetInversedBoneTransform(jointsArray[vertexDataIndex * 4 + 1])) +
                    weightsArray[vertexDataIndex * 4 + 2] * Vector4.Transform(initialPosition, GetInversedBoneTransform(jointsArray[vertexDataIndex * 4 + 2])) +
                    weightsArray[vertexDataIndex * 4 + 3] * Vector4.Transform(initialPosition, GetInversedBoneTransform(jointsArray[vertexDataIndex * 4 + 3]));
                initialNormal = Vector3.Normalize(Vector3.TransformNormal(initialNormal, GetInversedBoneTransform(jointsArray[vertexDataIndex * 4])) * weightsArray[vertexDataIndex * 4] +
                    Vector3.TransformNormal(initialNormal, GetInversedBoneTransform(jointsArray[vertexDataIndex * 4 + 1])) * weightsArray[vertexDataIndex * 4 + 1] +
                    Vector3.TransformNormal(initialNormal, GetInversedBoneTransform(jointsArray[vertexDataIndex * 4 + 2])) * weightsArray[vertexDataIndex * 4 + 2] +
                    Vector3.TransformNormal(initialNormal, GetInversedBoneTransform(jointsArray[vertexDataIndex * 4 + 3])) * weightsArray[vertexDataIndex * 4 + 3]);
            }
            Vector4 worldPosition = Vector4.Transform(initialPosition, worldTransformation);
            Vector3 worldNormal = Vector3.Transform(initialNormal, normalTransformation);
            return new PhongVertex()
            {
                Position = worldPosition,
                Normal = worldNormal,
                WorldPosition = worldPosition.AsVector3(),
            };
        }
        private static Matrix4x4 GetInversedBoneTransform(in int jointIndex)
        {
            return currentSkin!.CurrentFrameJointMatrices?[jointIndex] ?? Matrix4x4.Identity;
        }
        public struct PhongVertex : IModelVertex<PhongVertex>
        {
            public Vector4 Position { readonly get => position; set => position = value; }
            public Vector3 Normal { readonly get => normal; set => normal = value; }
            public Vector3 WorldPosition { readonly get => worldPosition; set => worldPosition = value; }

            private Vector4 position;
            private Vector3 normal;
            private Vector3 worldPosition;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static PhongVertex Lerp(PhongVertex a, PhongVertex b, float t)
            {
                return a * (1 - t) + b * t;
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static PhongVertex operator +(PhongVertex lhs, PhongVertex rhs)
            {
                return new PhongVertex
                {
                    Position = lhs.Position + rhs.Position,
                    normal = lhs.normal + rhs.normal,
                    worldPosition = lhs.worldPosition + rhs.worldPosition,
                };
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static PhongVertex operator -(PhongVertex lhs, PhongVertex rhs)
            {                    
            return new PhongVertex
                {
                    Position = lhs.Position - rhs.Position,
                    normal = lhs.normal - rhs.normal,
                    worldPosition = lhs.worldPosition - rhs.worldPosition,
                };
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static PhongVertex operator *(PhongVertex lhs, float scalar)
            {
                return new PhongVertex
                {
                    Position = lhs.Position * scalar,
                    normal = lhs.normal * scalar,
                    worldPosition = lhs.worldPosition * scalar,
                };
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static PhongVertex operator *(float scalar, PhongVertex rhs)
            {
                return rhs * scalar;
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static PhongVertex operator /(PhongVertex lhs, float scalar)
            {
                return lhs * (1 / scalar);
            }
        }
    }
}
