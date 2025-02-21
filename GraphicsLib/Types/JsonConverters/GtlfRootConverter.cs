using GraphicsLib.Types.GltfTypes;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GraphicsLib.Types.JsonConverters
{
    class GtlfRootConverter : JsonConverter<GltfRoot>
    {
        public override GltfRoot? ReadJson(JsonReader reader, Type objectType, GltfRoot? existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            GltfRoot? gltfRoot = serializer.Deserialize<GltfRoot>(reader);
            if (gltfRoot is not null)
            {
                
            }
            return gltfRoot;
        }

        public override void WriteJson(JsonWriter writer, GltfRoot? value, JsonSerializer serializer)
        {
            serializer.Serialize(writer, value);
        }
    }
}
