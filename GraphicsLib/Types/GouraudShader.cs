using GraphicsLib.Primitives;
using System.Numerics;
using static GraphicsLib.Types.GouraudShader;

namespace GraphicsLib.Types
{
    public class GouraudShader : IShader<Vertex>
    {
        private Scene scene;

        public Scene Scene { get => scene; set => SetSceneParams(value); }
        private Vector3 lightPosition;
        private Vector3 baseColor = new Vector3(1, 1, 1);
        private Matrix4x4 worldTransform;
        private Matrix4x4 worldNormalTransform;
        private void SetSceneParams(Scene value)
        {
            scene = value;
            worldTransform = scene.Obj.Transformation.Matrix;
            worldNormalTransform = scene.Obj.Transformation.NormalMatrix;
            lightPosition = scene.LightPosition;
        }

        public GouraudShader()
        {
        }
        public GouraudShader(Scene scene)
        {
            Scene = scene;
        }
        public Vertex GetVertexWithWorldPositionFromFace(Obj obj, int faceIndex, int vertexIndex)
        {
            Face face = obj.faces[faceIndex];
            Vertex vertex = default;
            vertex.Position = Vector4.Transform(new Vector4(obj.vertices[face.vIndices[vertexIndex]], 1), worldTransform);
            if (face.nIndices == null)
                throw new ArgumentException("Face has no normal indices, BRUUUUUUUUUUUUUUUUUUUUUUUUUUUUUUUUUUUUUUUUH");
            Vector3 normal = Vector3.TransformNormal(obj.normals[face.nIndices[vertexIndex]], worldNormalTransform);
            float illumination = Vector3.Dot(normal, Vector3.Normalize(lightPosition - vertex.Position.AsVector3()));
            Vector3 color = baseColor * illumination;
            vertex.Color = Vector3.Clamp(color, Vector3.Zero, new Vector3(1, 1, 1));
            return vertex;  
        }

        public Vertex GetVertexWithWorldPositionFromTriangle(Obj obj, int triangleIndex, int vertexIndex)
        {
            StaticTriangle triangle = obj.triangles[triangleIndex];
            Vertex vertex = default;
            Vector3 normal;
            switch (vertexIndex)
            {
                case 0:
                    vertex.Position = Vector4.Transform(new Vector4(triangle.position0, 1), worldTransform);
                    normal = Vector3.TransformNormal(triangle.normal0, worldNormalTransform);
                    break;
                case 1:
                    vertex.Position = Vector4.Transform(new Vector4(triangle.position1, 1), worldTransform);
                    normal = Vector3.TransformNormal(triangle.normal1, worldNormalTransform);
                    break;
                case 2:
                    vertex.Position = Vector4.Transform(new Vector4(triangle.position2, 1), worldTransform);
                    normal = Vector3.TransformNormal(triangle.normal2, worldNormalTransform);
                    break;
                default:
                    throw new ArgumentException("Invalid vertex index");
            }
            float illumination = Vector3.Dot(normal, Vector3.Normalize(lightPosition - vertex.Position.AsVector3()));
            Vector3 color = baseColor * illumination;
            vertex.Color = Vector3.Clamp(color, Vector3.Zero, new Vector3(1, 1, 1));
            return vertex;
        }

        public uint PixelShader(Vertex input)
        {
            Vector3 finalColor = input.Color;
            return (uint)(0xFF) << 24
                         | (uint)(finalColor.X * 0xFF) << 16
                         | (uint)(finalColor.Y * 0xFF) << 8
                         | (uint)(finalColor.Z * 0xFF);
        }

        public struct Vertex : IVertex<Vertex>
        {
            public Vector4 Position { get; set; }

            public Vector3 Color { get; set; }
            public static Vertex Lerp(Vertex a, Vertex b, float t)
            {
                return new Vertex
                {
                    Position = Vector4.Lerp(a.Position, b.Position, t),
                    Color = Vector3.Lerp(a.Color, b.Color, t)
                };
            }
            public static Vertex operator +(Vertex lhs, Vertex rhs)
            {
                return new Vertex
                {
                    Position = lhs.Position + rhs.Position,
                    Color = lhs.Color + rhs.Color
                };
            }
            public static Vertex operator -(Vertex lhs, Vertex rhs)
            {
                return new Vertex
                {
                    Position = lhs.Position - rhs.Position,
                    Color = lhs.Color - rhs.Color
                };
            }
            public static Vertex operator *(Vertex lhs, float scalar)
            {
                return new Vertex
                {
                    Position = lhs.Position * scalar,
                    Color = lhs.Color * scalar
                };
            }
            public static Vertex operator *(float scalar, Vertex rhs)
            {
                return new Vertex
                {
                    Position = rhs.Position * scalar,
                    Color = rhs.Color * scalar
                };
            }
            public static Vertex operator /(Vertex lhs, float scalar)
            {
                return new Vertex
                {
                    Position = lhs.Position / scalar,
                    Color = lhs.Color / scalar
                };
            }
        }
    }
}
