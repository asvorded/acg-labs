using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace GraphicsLib.Types.JsonConverters
{
    public static class GltfSerializerSettings
    {
        public static JsonSerializerSettings GetSettings(string sourceDirectory)  => new JsonSerializerSettings
        {
            Converters = {new GtlfRootConverter(),
                          new QuaternionConverter(),
                          new QuaternioNullableConverter(),
                          new GltfAccessorTypeConverter(),
                          new Matrix4x4Converter(),
                          new Matrix4x4NullableConverter(),
                          new Vector3Converter(),
                          new Vector3NullableConverter(),
                          new Vector2Converter(),
                          new Vector2NullableConverter(),
                          new GltfBufferConverter(sourceDirectory)
            },
            ContractResolver = new DefaultContractResolver {
                NamingStrategy = new CamelCaseNamingStrategy {
                    OverrideSpecifiedNames = true
                }
            }
        };
    }
}
