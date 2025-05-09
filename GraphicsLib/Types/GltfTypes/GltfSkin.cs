using Newtonsoft.Json;

namespace GraphicsLib.Types.GltfTypes
{
    public class GltfSkin
    {
        [JsonProperty("inverseBindMatrices")]
        public int? InverseBindMatrices { get; set; }

        [JsonProperty("skeleton")]
        public int? Skeleton { get; set; }

        [JsonProperty("joints")]
        public required int[] Joints { get; set; }

        [JsonProperty("name")]
        public string? Name { get; set; }
        [JsonProperty("extensions")]
        public Dictionary<string, object>? Extensions { get; set; }
        [JsonProperty("extras")]
        public object? Extras { get; set; }
    }
}