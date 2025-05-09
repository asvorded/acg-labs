using Newtonsoft.Json;
using System.Configuration;
using System.Numerics;

namespace GraphicsLib.Types.GltfTypes
{
    public class GltfMesh
    {
        [JsonProperty("primitives")]
        public required GltfMeshPrimitive[] Primitives { get; set; }
        [JsonProperty("weights")]
        public float[]? Weights { get; set; }
        [JsonProperty("name")]
        public string? Name { get; set; }
        [JsonProperty("extensions")]
        public Dictionary<string, object>? Extensions { get; set; }
        [JsonProperty("extras")]
        public object? Extras { get; set; }
    }

    public class GltfMeshPrimitive
    {
        [JsonProperty("attributes")]
        public required Dictionary<string, int> Attributes { get; set; }
        [JsonProperty("indices")]
        public int? Indices { get; set; }
        [JsonProperty("material")]
        public int? Material { get; set; }
        [JsonProperty("mode")]
        public GltfMeshMode Mode { get; set; } = GltfMeshMode.TRIANGLES;
        [JsonProperty("targets")]
        public object[]? Targets { get; set; }
        [JsonProperty("extensions")]
        public Dictionary<string, object>? Extensions { get; set; }
        [JsonProperty("extras")]
        public object? Extras { get; set; }

        [JsonIgnore]
        public GltfRoot? Root { get; set; }
        [JsonIgnore]
        public Vector3[]? Position { get => GetPosition(); }
        [JsonIgnore]
        public Vector3[]? Normal { get => GetNormals(); }
        [JsonIgnore]
        public int[]? PointIndices { get => GetPointIndices(); }
        [JsonIgnore]
        public Vector4[]? Tangent { get => GetTangents(); }
        public Vector2[]? GetTextureCoords(int v)
        {
            return GetVector2($"TEXCOORD_{v}");           
        }
        public Vector4[]? GetTangents()
        {
            return GetVector4("TANGENT");
        }
        public Vector3[]? GetPosition()
        {
            return GetVector3("POSITION");
        }
        public Vector3[]? GetNormals()
        {
            return GetVector3("NORMAL");
        }
        private int[]? GetPointIndices()
        {
            if (Indices.HasValue)
            {
                if (Root == null)
                {
                    throw new ConfigurationErrorsException("Root is null.");
                }
                var accessor = Root.Accessors![Indices.Value];
                return GltfUtils.GetAccessorData<int>(accessor);
            }
            return null;
        }
        private TTarget[]? GetScalar<TTarget>(string attribute) where TTarget : unmanaged
        {
            if (Attributes.TryGetValue(attribute, out int index))
            {
                if (Root == null)
                {
                    throw new ConfigurationErrorsException("Root is null.");
                }
                var accessor = Root.Accessors![index];
                return GltfUtils.GetAccessorData<TTarget>(accessor);
            }
            else
            {
                return null;
            }
        }
        private Vector2[]? GetVector2(string attribute)
        {
            if (Attributes.TryGetValue(attribute, out int index))
            {
                if (Root == null)
                {
                    throw new ConfigurationErrorsException("Root is null.");
                }
                var accessor = Root.Accessors![index];
                float[] floats = GltfUtils.GetAccessorData<float>(accessor);
                Vector2[] vectors = new Vector2[accessor.Count];
                for (int i = 0; i < accessor.Count; i++)
                {
                    vectors[i] = new Vector2(floats[i * 2], floats[i * 2 + 1]);
                }
                return vectors;
            }
            else
            {
                return null;
            }
        }
        private Vector3[]? GetVector3(string attribute)
        {
            if (Attributes.TryGetValue(attribute, out int index))
            {
                if (Root == null)
                {
                    throw new ConfigurationErrorsException("Root is null.");
                }
                var accessor = Root.Accessors![index];
                float[] floats = GltfUtils.GetAccessorData<float>(accessor);
                Vector3[] vectors = new Vector3[accessor.Count];
                for (int i = 0; i < accessor.Count; i++)
                {
                    vectors[i] = new Vector3(floats[i * 3], floats[i * 3 + 1], floats[i * 3 + 2]);
                }
                return vectors;
            }
            else
            {
                return null;
            }
        }
        private Vector4[]? GetVector4(string attribute)
        {
            if (Attributes.TryGetValue(attribute, out int index))
            {
                if (Root == null)
                {
                    throw new ConfigurationErrorsException("Root is null.");
                }
                var accessor = Root.Accessors![index];
                float[] floats = GltfUtils.GetAccessorData<float>(accessor);
                Vector4[] vectors = new Vector4[accessor.Count];
                for (int i = 0; i < accessor.Count; i++)
                {
                    vectors[i] = new Vector4(floats[i * 4], floats[i * 4 + 1], floats[i * 4 + 2], floats[i * 4 + 3]);
                }
                return vectors;
            }
            else
            {
                return null;
            }
        }
    }
    public enum GltfMeshMode
    {
        POINTS = 0,
        LINES = 1,
        LINE_LOOP = 2,
        LINE_STRIP = 3,
        TRIANGLES = 4,
        TRIANGLE_STRIP = 5,
        TRIANGLE_FAN = 6
    }
}