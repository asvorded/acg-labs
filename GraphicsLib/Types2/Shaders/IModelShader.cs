using GraphicsLib.Types;
using System.Numerics;

namespace GraphicsLib.Types2.Shaders
{
    public interface IModelShader<Vertex> where Vertex : IModelVertex<Vertex>
    {
        public static abstract void BindScene(in ModelScene scene);
        public static abstract void UnbindScene();
        public static abstract void BindPrimitive(in ModelPrimitive primitive, in Matrix4x4 transformation);
        public static abstract void UnbindPrimitive();
        public static abstract void BindSkin(in ModelSkin skin);
        public static abstract void UnbindSkin();

        public static abstract Vector4 PixelShader(in Vertex input);
        public static abstract Vertex VertexShader(in ModelPrimitive primitive, in int vertexDataIndex);
        

    }
    public interface IModelVertex<T> where T : IModelVertex<T>
    {
        public Vector4 Position { get; set; }
        abstract static T Lerp(T a, T b, float t);
        public static abstract T operator +(T lhs, T rhs);

        public static abstract T operator -(T lhs, T rhs);

        public static abstract T operator *(T lhs, float scalar);
        public static abstract T operator *(float scalar, T rhs);
        public static abstract T operator /(T lhs, float scalar);
    }
    public static class ModelShaderUtils
    {
        public unsafe static T* GetAttributePointer<T>(in ModelPrimitive primitive, string attributeName) where T : unmanaged
        {
            if (!primitive.AttributesOffsets.TryGetValue(attributeName, out short offset))
            {
                return null;
            }
            if (offset == -1)
            {
                return null;
            }
            unsafe
            {
                float[] floatData = primitive.FloatData[offset];
                fixed (float* dataPtr = floatData)
                {
                    return (T*)dataPtr;
                }
            }
        }
        public unsafe static ushort* GetJointsPointer(in ModelPrimitive primitive)
        {
            if (primitive.Joints == null || primitive.Joints.Length == 0)
            {
                return null;
            }
            fixed (ushort* jointsPtr = primitive.Joints[0])
            {
                return jointsPtr;
            }
        }
    }
}
