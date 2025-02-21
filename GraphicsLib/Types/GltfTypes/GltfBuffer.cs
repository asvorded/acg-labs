using Newtonsoft.Json;
using System.Configuration;
using System.IO;

namespace GraphicsLib.Types.GltfTypes
{
    public class GltfBuffer
    {

        [JsonProperty("uri")]
        public Uri? Uri { get; set; }
        [JsonProperty("byteLength")]
        public required int ByteLength { get; set; }
        [JsonProperty("name")]
        public string? Name { get; set; }
        [JsonProperty("extensions")]
        public Dictionary<string, object>? Extensions { get; set; }
        [JsonProperty("extras")]
        public object? Extras { get; set; }

        private byte[]? _data;
        public byte[] Data
        {
            get
            {
                if (_data == null)
                {
                    if (Uri == null)
                    {
                        throw new ConfigurationErrorsException("Uri is null");
                    }
                    string dataType = Uri.GetComponents(UriComponents.Path | UriComponents.Query, UriFormat.UriEscaped);
                    string data = Uri.GetComponents(UriComponents.Fragment, UriFormat.UriEscaped);
                    if (dataType.StartsWith("data:application/octet-stream;base64,"))
                    {
                        string base64Data = data.Substring("data:application/octet-stream;base64,".Length);
                        _data = Convert.FromBase64String(base64Data);
                    }
                    else
                    {
                        File.ReadAllBytes(Uri.AbsolutePath);
                    }
                }
                return _data;
            }
        }
    }
}