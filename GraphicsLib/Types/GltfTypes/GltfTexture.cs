using Newtonsoft.Json;

namespace GraphicsLib.Types.GltfTypes
{
    public class GltfTexture
    {
        [JsonProperty("sampler")]
        public int Sampler { get; set; }

        [JsonProperty("source")]
        public int Source { get; set; }
        [JsonProperty("name")]
        public string? Name { get; set; }
        [JsonProperty("extensions")]
        public Dictionary<string, object>? Extensions { get; set; }
        [JsonProperty("extras")]
        public object? Extras { get; set; }
    }
}