using Newtonsoft.Json;
using System.Configuration;

namespace GraphicsLib.Types.GltfTypes
{
    public class GltfAnimation
    {
        [JsonProperty("channels")]
        public required List<GltfAnimationChannel> Channels { get; set; }

        [JsonProperty("samplers")]
        public required List<GltfAnimationSampler> Samplers { get; set; }

        [JsonProperty("name")]
        public string? Name { get; set; }
        [JsonProperty("extensions")]
        public Dictionary<string, object>? Extensions { get; set; }
        [JsonProperty("extras")]
        public object? Extras { get; set; }
    }

    public class GltfAnimationSampler
    {
        [JsonProperty("input")]
        public required int Input { get; set; }

        [JsonProperty("interpolation")]
        public GltfInterpolationType Interpolation { get; set; } = GltfInterpolationType.LINEAR;

        [JsonProperty("output")]
        public required int Output { get; set; }
        [JsonProperty("extensions")]
        public Dictionary<string, object>? Extensions { get; set; }
        [JsonProperty("extras")]
        public object? Extras { get; set; }
        [JsonIgnore]
        public GltfRoot? Root { get; set; }

        public float[] GetInput()
        {
            if(Root == null)
            {
                throw new ConfigurationErrorsException("Root is null");
            }
            return GltfUtils.GetAccessorData<float>(Root.Accessors![Input]);
        }
        public float[] GetOutput()
        {
            if (Root == null)
            {
                throw new ConfigurationErrorsException("Root is null");
            }
            return GltfUtils.GetAccessorData<float>(Root.Accessors![Output]);
        }
    }
    public enum GltfInterpolationType
    {
        LINEAR,
        STEP,
        CUBICSPLINE
    }
    public class GltfAnimationChannel
    {
        [JsonProperty("sampler")]
        public required int Sampler { get; set; }

        [JsonProperty("target")]
        public required GltfAnimationTarget Target { get; set; }

        [JsonProperty("extensions")]
        public Dictionary<string, object>? Extensions { get; set; }
        [JsonProperty("extras")]
        public object? Extras { get; set; }
        [JsonIgnore]
        public GltfAnimationSampler GltfAnimationSampler { get; set; }
    }

    public class GltfAnimationTarget
    {
        [JsonProperty("node")]
        public int? Node { get; set; }

        [JsonProperty("path")]
        public required GltfAnimationPathType Path { get; set; }

        [JsonProperty("extensions")]
        public Dictionary<string, object>? Extensions { get; set; }
        [JsonProperty("extras")]
        public object? Extras { get; set; }
    }
    public enum GltfAnimationPathType
    {
        TRANSLATION,
        ROTATION,
        SCALE,
        WEIGHTS
    }
}