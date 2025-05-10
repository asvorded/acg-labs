using GraphicsLib.Types;
using System.Numerics;

namespace GraphicsLib.Types2
{
    public interface IModelShader<Vertex> where Vertex : IVertex<Vertex>
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
    public interface IVertex<T> where T : IVertex<T>
    {
        public Vector4 Position { get; set; }
        abstract static T Lerp(T a, T b, float t);
        public static abstract T operator +(T lhs, T rhs);

        public static abstract T operator -(T lhs, T rhs);

        public static abstract T operator *(T lhs, float scalar);
        public static abstract T operator *(float scalar, T rhs);
        public static abstract T operator /(T lhs, float scalar);
    }
}
