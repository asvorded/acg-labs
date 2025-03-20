using Newtonsoft.Json;
using System.Configuration;

namespace GraphicsLib.Types.GltfTypes
{
    public class GltfTexture
    {
        [JsonProperty("sampler")]
        public int? SamplerIndex { get; set; }

        [JsonProperty("source")]
        public int? ImageSourceIndex { get; set; }
        [JsonProperty("name")]
        public string? Name { get; set; }
        [JsonProperty("extensions")]
        public Dictionary<string, object>? Extensions { get; set; }
        [JsonProperty("extras")]
        public object? Extras { get; set; }
        [JsonIgnore]
        public GltfImage? Image { get; set; }
        [JsonIgnore]
        public GltfSampler? Sampler { get; set; }
    }
}