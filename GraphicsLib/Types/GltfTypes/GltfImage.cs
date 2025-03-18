using Newtonsoft.Json;

namespace GraphicsLib.Types.GltfTypes
{
    public class GltfImage
    {
        [JsonProperty("uri")]
        public string? Uri { get; set; }

        [JsonProperty("mimeType")]
        public string? MimeType { get; set; }

        [JsonProperty("bufferView")]
        public int? BufferView { get; set; }
        [JsonProperty("name")]
        public string? Name { get; set; }
        [JsonProperty("extensions")]
        public Dictionary<string, object>? Extensions { get; set; }
        [JsonProperty("extras")]
        public object? Extras { get; set; }
    }
}