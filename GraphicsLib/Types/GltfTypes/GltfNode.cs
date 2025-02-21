using Newtonsoft.Json;
using System.Numerics;

namespace GraphicsLib.Types.GltfTypes
{
    public class GltfNode
    {
        [JsonProperty("camera")]
        public int? Camera { get; set; }
        [JsonProperty("children")]
        public int[]? Children { get; set; }
        [JsonProperty("skin")]
        public int? Skin { get; set; }
        [JsonProperty("matrix")]
        public Matrix4x4? Matrix { get; set; }
        [JsonProperty("mesh")]
        public int? Mesh { get; set; }
        [JsonProperty("rotation")]
        public Quaternion? Rotation { get; set; }
        [JsonProperty("scale")]
        public Vector3? Scale { get; set; }
        [JsonProperty("translation")]
        public Vector3? Translation { get; set; }
        [JsonProperty("weights")]
        public float[]? Weights { get; set; }
        [JsonProperty("name")]
        public string? Name { get; set; }
        [JsonProperty("extensions")]
        public Dictionary<string, object>? Extensions { get; set; }
        [JsonProperty("extras")]
        public object? Extras { get; set; }
    }
}