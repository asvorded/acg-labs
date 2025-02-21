using Newtonsoft.Json;

namespace GraphicsLib.Types.JsonConverters
{
    public static class GltfSerializerSettings
    {
        public static JsonSerializerSettings Settings { get; } = new JsonSerializerSettings
        {
            Converters = {new QuaternionConverter(),
                new QuaternioNullableConverter(),
                    new GltfAccessorTypeConverter(),
                    new Matrix4x4Converter(),
                    new Matrix4x4NullableConverter(),
                    new Vector3Converter(),
                    new Vector3NullableConverter(),
                    new Vector2Converter(),
                    new Vector2NullableConverter()
            },
            
        };
    }
}
