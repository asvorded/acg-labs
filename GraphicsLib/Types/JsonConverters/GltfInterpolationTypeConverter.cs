using GraphicsLib.Types.GltfTypes;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GraphicsLib.Types.JsonConverters
{
    internal class GltfInterpolationTypeConverter : JsonConverter<GltfInterpolationType>
    {
        public override GltfInterpolationType ReadJson(JsonReader reader, Type objectType, GltfInterpolationType existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            var interpolationType = reader.Value?.ToString()
                ?? throw new JsonSerializationException("Invalid accessor type");
            return Enum.Parse<GltfInterpolationType>(interpolationType, true);
        }

        public override void WriteJson(JsonWriter writer, GltfInterpolationType value, JsonSerializer serializer)
        {
            writer.WriteValue(value.ToString());
        }
    }
}
