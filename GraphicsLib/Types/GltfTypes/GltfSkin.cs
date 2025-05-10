using Newtonsoft.Json;
using System.Configuration;
using System.Numerics;

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
        [JsonIgnore]
        public GltfRoot? Root { get; set; }
        public Matrix4x4[]? GetInverseBindMatricesArray()
        {
            if(Root == null)
            {
                throw new ConfigurationErrorsException("Root is null");
            }
            if (InverseBindMatrices == null)
            {
                return null;
            }
            var accessor = Root.Accessors![InverseBindMatrices.Value];
            float[]? floats = GltfUtils.GetAccessorData<float>(accessor);
            if(floats == null)
            {
                return null;
            }
            int count = floats.Length / 16;
            Matrix4x4[] matrices = new Matrix4x4[count];
            for (int i = 0; i < count; i++)
            {
                matrices[i] = new Matrix4x4(floats[i * 16], floats[i * 16 + 1], floats[i * 16 + 2], floats[i * 16 + 3],
                    floats[i * 16 + 4], floats[i * 16 + 5], floats[i * 16 + 6], floats[i * 16 + 7],
                    floats[i * 16 + 8], floats[i * 16 + 9], floats[i * 16 + 10], floats[i * 16 + 11],
                    floats[i * 16 + 12], floats[i * 16 + 13], floats[i * 16 + 14], floats[i * 16 + 15]);
            }
            return matrices;
        }
    }
}