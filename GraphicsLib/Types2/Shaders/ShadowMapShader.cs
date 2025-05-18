using System.Numerics;
using System.Runtime.CompilerServices;
using static GraphicsLib.Types2.Shaders.ShadowMapShader;

namespace GraphicsLib.Types2.Shaders
{
    public unsafe class ShadowMapShader : IModelShader<ShadowMapVertex>
    {
        static ModelSkin? currentSkin = null;
        static Matrix4x4 worldTransformation = Matrix4x4.Identity;
        static Vector3* positionsArray = null;
        static ushort* jointsArray = null;
        static float* weightsArray = null;
        public static void BindScene(in ModelScene scene)
        {
            //no action needed
        }
        public static void UnbindScene()
        {
            // no action needed
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
            worldTransformation = transformation;
            BindAttributes(primitive);
        }
        public static void UnbindPrimitive()
        {
            UnbindAttributes();
        }
        private static void BindAttributes(in ModelPrimitive primitive)
        {
            unsafe
            {
                positionsArray = ModelShaderUtils.GetAttributePointer<Vector3>(primitive, "POSITION");
                jointsArray = ModelShaderUtils.GetJointsPointer(primitive);
                weightsArray = ModelShaderUtils.GetAttributePointer<float>(primitive, "WEIGHTS_0");
            }
        }
        private static void UnbindAttributes()
        {
            positionsArray = null;
            jointsArray = null;
            weightsArray = null;
        }
        public static Vector4 PixelShader(in ShadowMapVertex input)
        {
            return new Vector4(1);
        }

        public static ShadowMapVertex VertexShader(in ModelPrimitive primitive, in int vertexDataIndex)
        {
            Vector4 initialPosition = new Vector4(positionsArray[vertexDataIndex], 1);
            if (currentSkin != null)
            {
                initialPosition = weightsArray[vertexDataIndex * 4] * Vector4.Transform(initialPosition, GetInversedBoneTransform(jointsArray[vertexDataIndex * 4])) +
                    weightsArray[vertexDataIndex * 4 + 1] * Vector4.Transform(initialPosition, GetInversedBoneTransform(jointsArray[vertexDataIndex * 4 + 1])) +
                    weightsArray[vertexDataIndex * 4 + 2] * Vector4.Transform(initialPosition, GetInversedBoneTransform(jointsArray[vertexDataIndex * 4 + 2])) +
                    weightsArray[vertexDataIndex * 4 + 3] * Vector4.Transform(initialPosition, GetInversedBoneTransform(jointsArray[vertexDataIndex * 4 + 3]));
            }
            Vector4 worldPosition = Vector4.Transform(initialPosition, worldTransformation);
            return new ShadowMapVertex()
            {
                Position = worldPosition,               
            };
        }
        private static Matrix4x4 GetInversedBoneTransform(in int jointIndex)
        {
            return currentSkin!.CurrentFrameJointMatrices?[jointIndex] ?? Matrix4x4.Identity;
        }
        public struct ShadowMapVertex : IModelVertex<ShadowMapVertex>
        {
            public Vector4 Position { readonly get => position; set => position = value; }
            private Vector4 position;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static ShadowMapVertex Lerp(ShadowMapVertex a, ShadowMapVertex b, float t)
            {
                return new ShadowMapVertex
                {
                    Position = Vector4.Lerp(a.Position, b.Position, t),
                };
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static ShadowMapVertex operator +(ShadowMapVertex lhs, ShadowMapVertex rhs)
            {
                return new ShadowMapVertex
                {
                    Position = lhs.Position + rhs.Position,
                };
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static ShadowMapVertex operator -(ShadowMapVertex lhs, ShadowMapVertex rhs)
            {
                return new ShadowMapVertex
                {
                    Position = lhs.Position - rhs.Position,
                };
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static ShadowMapVertex operator *(ShadowMapVertex lhs, float scalar)
            {
                return new ShadowMapVertex
                {
                    Position = lhs.Position * scalar,
                };
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static ShadowMapVertex operator *(float scalar, ShadowMapVertex rhs)
            {
                return rhs * scalar;
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static ShadowMapVertex operator /(ShadowMapVertex lhs, float scalar)
            {
                return lhs * (1 / scalar);
            }
        }
    }
}
