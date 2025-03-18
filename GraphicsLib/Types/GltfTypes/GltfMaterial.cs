using Newtonsoft.Json;
using System.Numerics;

namespace GraphicsLib.Types.GltfTypes
{
    public class GltfMaterial
    {
        [JsonProperty("name")]
        public string? Name { get; set; }
        [JsonProperty("pbrMetallicRoughness")]
        public GltfMaterialPbrMetallicRoughnessInfo PbrMetallicRoughness { get; set; } = new();
        [JsonProperty("normalTexture")]
        public GltfMaterialNormalTextureInfo? NormalTexture { get; set; }
        [JsonProperty("occlusionTexture")]
        public GltfMaterialOcclusionTextureInfo? OcclusionTexture { get; set; }
        [JsonProperty("emissiveTexture")]
        public GltfMaterialTextureInfo? EmissiveTexture { get; set; }
        [JsonProperty("emissiveFactor")]
        public float[] EmissiveFactor { get; set; } = { 0.0f, 0.0f, 0.0f };
        [JsonProperty("alphaMode")]
        public string AlphaMode { get; set; } = "OPAQUE";
        [JsonProperty("alphaCutoff")]
        public float AlphaCutoff { get; set; } = 0.5f;
        [JsonProperty("doubleSided")]
        public bool DoubleSided { get; set; } = false;
        [JsonProperty("extensions")]
        public Dictionary<string, object>? Extensions { get; set; }
        [JsonProperty("extras")]
        public object? Extras { get; set; }

    }

    public class GltfMaterialTextureInfo
    {
        [JsonProperty("index")]
        public required int Index { get; set; }
        [JsonProperty("texCoord")]
        public int TexCoord { get; set; } = 0;
        public Dictionary<string, object>? Extensions { get; set; }
        [JsonProperty("extras")]
        public object? Extras { get; set; }
    }

    public class GltfMaterialOcclusionTextureInfo
    {
        [JsonProperty("index")]
        public required int Index { get; set; }
        [JsonProperty("texCoord")]
        public int TexCoord { get; set; } = 0;
        [JsonProperty("strength")]
        public float Strength { get; set; } = 1.0f;
        public Dictionary<string, object>? Extensions { get; set; }
        [JsonProperty("extras")]
        public object? Extras { get; set; }
    }

    public class GltfMaterialNormalTextureInfo
    {
        [JsonProperty("index")]
        public required int Index { get; set; }
        [JsonProperty("texCoord")]
        public int TexCoord { get; set; } = 0;
        [JsonProperty("scale")]
        public float Scale { get; set; } = 1.0f;
        public Dictionary<string, object>? Extensions { get; set; }
        [JsonProperty("extras")]
        public object? Extras { get; set; }
    }

    public class GltfMaterialPbrMetallicRoughnessInfo
    {
        [JsonProperty("baseColorFactor")]
        public Vector4 BaseColorFactor { get; set; } = new Vector4(1.0f, 1.0f, 1.0f, 1.0f);
        [JsonProperty("baseColorTexture")]
        public GltfMaterialTextureInfo? BaseColorTexture { get; set; }
        [JsonProperty("metallicFactor")]
        public float MetallicFactor { get; set; } = 1.0f;
        [JsonProperty("roughnessFactor")]
        public float RoughnessFactor { get; set; } = 1.0f;
        [JsonProperty("metallicRoughnessTexture")]
        public GltfMaterialTextureInfo? MetallicRoughnessTexture { get; set; }
        public Dictionary<string, object>? Extensions { get; set; }
        [JsonProperty("extras")]
        public object? Extras { get; set; }
    }

}