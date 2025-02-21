using Newtonsoft.Json;

namespace GraphicsLib.Types.GltfTypes
{
    public class GltfMesh
    {
        [JsonProperty("primitives")]
        public required GtlfMeshPrimitive[] Primitives { get; set; }
        [JsonProperty("weights")]
        public float[]? Weights { get; set; }
        [JsonProperty("name")]
        public string? Name { get; set; }
        [JsonProperty("extensions")]
        public Dictionary<string, object>? Extensions { get; set; }
        [JsonProperty("extras")]
        public object? Extras { get; set; }
    }

    public class GtlfMeshPrimitive
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