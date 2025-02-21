using GraphicsLib.Types.GltfTypes;
using Newtonsoft.Json;
using System.Reflection.PortableExecutable;

namespace GraphicsLib.Types.JsonConverters
{
    class GltfBufferConverter : JsonConverter<GltfBuffer>
    {
        private string? sourceDirectory = null;
        private byte[]? glbBuffer = null;
        public GltfBufferConverter() { }
        public GltfBufferConverter(string sourceDirectory)
        {
            this.sourceDirectory = sourceDirectory;
        }

        public override GltfBuffer? ReadJson(JsonReader reader, Type objectType, GltfBuffer? existingValue, bool hasExistingValue, Newtonsoft.Json.JsonSerializer serializer)
        {
            JsonSerializerSettings settings = new JsonSerializerSettings
            {
                Converters = [.. serializer.Converters.Where(c => c != this)]
            };
            GltfBuffer? gltfBuffer = JsonSerializer.CreateDefault(settings).Deserialize<GltfBuffer>(reader);
            if(gltfBuffer != null && sourceDirectory != null)
            {
                gltfBuffer.SourceDirectory = sourceDirectory;
            }
            if (gltfBuffer != null && glbBuffer != null)
            {
                gltfBuffer.Data = glbBuffer;
            }
            return gltfBuffer;
        }

        public override void WriteJson(JsonWriter writer, GltfBuffer? value, Newtonsoft.Json.JsonSerializer serializer)
        {
            JsonSerializerSettings settings = new JsonSerializerSettings
            {
                Converters = [.. serializer.Converters.Where(c => c != this)]
            };
            JsonSerializer.CreateDefault(settings).Serialize(writer, value);
        }
    }
}
