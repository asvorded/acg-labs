using GraphicsLib.Primitives;
using GraphicsLib.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using static GraphicsLib.Shaders.OpaqueShadowShader;

namespace GraphicsLib.Shaders
{
    public class OpaqueShadowShader : IShader<Vertex>
        {
            private Scene scene;
            public Scene Scene { get => scene; set => SetSceneParams(value); }
            private Matrix4x4 worldTransform;
            private void SetSceneParams(Scene value)
            {
                scene = value;
                worldTransform = scene.Obj!.transformation.Matrix;
            }
            public Vertex GetVertexWithWorldPositionFromFace(Obj obj, int faceIndex, int vertexIndex)
            {
                Face face = obj.faces[faceIndex];
                Vertex vertex = default;
                vertex.Position = Vector4.Transform(new Vector4(obj.vertices[face.vIndices[vertexIndex]], 1), worldTransform);
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
                        break;
                    case 1:
                        vertex.Position = Vector4.Transform(new Vector4(triangle.position1, 1), worldTransform);
                        break;
                    case 2:
                        vertex.Position = Vector4.Transform(new Vector4(triangle.position2, 1), worldTransform);
                        break;
                    default:
                        throw new ArgumentException("Invalid vertex index");
                }
                return vertex;
            }

            public uint PixelShader(in Vertex input) => 0xFFFFFFFF;

            public struct Vertex : IVertex<Vertex>
            {
                public Vector4 Position { get; set; }
                public static Vertex Lerp(Vertex a, Vertex b, float t)
                {
                    return new Vertex
                    {
                        Position = Vector4.Lerp(a.Position, b.Position, t),
                    };
                }
                public static Vertex operator +(Vertex lhs, Vertex rhs)
                {
                    return new Vertex
                    {
                        Position = lhs.Position + rhs.Position,
                    };
                }
                public static Vertex operator -(Vertex lhs, Vertex rhs)
                {
                    return new Vertex
                    {
                        Position = lhs.Position - rhs.Position,
                    };
                }
                public static Vertex operator *(Vertex lhs, float scalar)
                {
                    return new Vertex
                    {
                        Position = lhs.Position * scalar,
                    };
                }
                public static Vertex operator *(float scalar, Vertex rhs)
                {
                    return new Vertex
                    {
                        Position = rhs.Position * scalar,
                    };
                }
                public static Vertex operator /(Vertex lhs, float scalar)
                {
                    return new Vertex
                    {
                        Position = lhs.Position / scalar,
                    };
                }
            }
        }
    }
