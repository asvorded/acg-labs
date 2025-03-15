﻿using System.Numerics;
using System.Runtime.CompilerServices;
using System.Windows.Media.TextFormatting;
using static GraphicsLib.Types.PhongShader;

namespace GraphicsLib.Types
{
    public class PhongShader : IShader<Vertex>
    {
        public Scene Scene { get => scene; set => SetSceneParams(value); }

        private Vector3 ambient;
        private Vector3 diffuseColor;
        private float specularPower;

        private Vector3 lightColor;
        private float lightIntensity;
        private Vector3 lightPosition;
        private void SetSceneParams(Scene value)
        {
            this.scene = value;
            if (scene.Obj == null)
                throw new ArgumentException("Scene object is null");
            //caching all values to avoid calling heavy properties
            worldTransform = scene.Obj.Transformation.Matrix;
            worldNormalTransform = scene.Obj.Transformation.NormalMatrix;
            cameraPos = scene.Camera.Position;
            ambient = scene.AmbientColor * scene.AmbientIntensity;
            diffuseColor = scene.DiffuseColor;
            specularPower = scene.SpecularPower;
            lightColor = scene.LightColor * scene.LightIntensity;
            lightIntensity = scene.LightIntensity;
            lightPosition = scene.LightPosition;
        }

        private Matrix4x4 worldTransform;
        private Matrix4x4 worldNormalTransform;
        private Scene scene;
        private Vector3 cameraPos;
        public PhongShader()
        {
        }
        public PhongShader(Scene scene)
        {
            Scene = scene;
        }

        public struct Vertex : IVertex<Vertex>
        {
            public Vector4 Position { get; set; }

            public Vector3 Normal { get; set; }
            public Vector3 WorldPosition { get; set; }
            public static Vertex Lerp(Vertex a, Vertex b, float t)
            {
                return new Vertex
                {
                    Position = Vector4.Lerp(a.Position, b.Position, t),
                    Normal = Vector3.Lerp(a.Normal, b.Normal, t),
                    WorldPosition = Vector3.Lerp(a.WorldPosition, b.WorldPosition, t)
                };
            }
            public static Vertex operator +(Vertex lhs, Vertex rhs)
            {
                return new Vertex
                {
                    Position = lhs.Position + rhs.Position,
                    Normal = lhs.Normal + rhs.Normal,
                    WorldPosition = lhs.WorldPosition + rhs.WorldPosition
                };
            }
            public static Vertex operator -(Vertex lhs, Vertex rhs)
            {
                return new Vertex
                {
                    Position = lhs.Position - rhs.Position,
                    Normal = lhs.Normal - rhs.Normal,
                    WorldPosition = lhs.WorldPosition - rhs.WorldPosition
                };
            }
            public static Vertex operator *(Vertex lhs, float scalar)
            {
                return new Vertex
                {
                    Position = lhs.Position * scalar,
                    Normal = lhs.Normal * scalar,
                    WorldPosition = lhs.WorldPosition * scalar
                };
            }
            public static Vertex operator *(float scalar, Vertex rhs)
            {
                return new Vertex
                {
                    Position = rhs.Position * scalar,
                    Normal = rhs.Normal * scalar,
                    WorldPosition = rhs.WorldPosition * scalar
                };
            }
            public static Vertex operator /(Vertex lhs, float scalar)
            {
                return new Vertex
                {
                    Position = lhs.Position / scalar,
                    Normal = lhs.Normal / scalar,
                    WorldPosition = lhs.WorldPosition / scalar
                };
            }
        }

        public uint PixelShader(Vertex input)
        {
            Vector3 camDir = Vector3.Normalize(cameraPos - input.WorldPosition);
            Vector3 normal = Vector3.Normalize(input.Normal);
            Vector3 lightDir = Vector3.Normalize(lightPosition - input.WorldPosition);
            Vector3 reflectDir = Vector3.Reflect(-lightDir, normal);
            float diffuseFactor = Math.Max(Vector3.Dot(normal, lightDir), 0);
            Vector3 diffuse = diffuseColor * diffuseFactor * lightIntensity;
            float specularFactor = MathF.Pow(Math.Max(Vector3.Dot(reflectDir, camDir), 0), specularPower);
            Vector3 specular = lightColor * specularFactor * lightIntensity;
            Vector3 finalColor = ambient + diffuse + specular;
            uint color = (uint) (0xFF) << 24
                         | (uint)(Math.Min(finalColor.X, 1f) * 0xFF) << 16 
                         | (uint)(Math.Min(finalColor.Y, 1f) * 0xFF) << 8 
                         | (uint)(Math.Min(finalColor.Z, 1f) * 0xFF);
            return color;
        }

        public Vertex GetFromFace(Obj obj, int faceIndex, int vertexIndex)
        {
            Face face = obj.faces[faceIndex];
            Vertex vertex = default;
            vertex.Position = Vector4.Transform(new Vector4(obj.vertices[face.vIndices[vertexIndex]],1), worldTransform);
            if(face.nIndices == null)
                throw new ArgumentException("Face has no normal indices, BRUUUUUUUUUUUUUUUUUUUUUUUUUUUUUUUUUUUUUUUUH");
            vertex.Normal = Vector3.TransformNormal(obj.normals[face.nIndices[vertexIndex]], worldNormalTransform);
            vertex.WorldPosition = vertex.Position.AsVector3();
            return vertex;
        }
    }
}
