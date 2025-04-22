using Newtonsoft.Json;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.Configuration;
using System.IO;
using Image = SixLabors.ImageSharp.Image;
namespace GraphicsLib.Types.GltfTypes
{
    public class GltfImage
    {
        [JsonProperty("uri")]
        public string? UriString { get; set; }

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
        [JsonIgnore]
        public GltfRoot? GltfRoot { get; set; }
        [JsonIgnore]
        private int? width;
        [JsonIgnore]
        private int? height;
        [JsonIgnore]
        private Rgba32[]? data;
        [JsonIgnore]
        private readonly Lock dataLock = new();
        [JsonIgnore]
        public int Width { get => GetWidth(); }

        private int GetWidth()
        {
            if (!width.HasValue)
            {
                GetImageData();
            }
            return width!.Value;
        }

        [JsonIgnore]
        public int Height { get=> GetHeight(); }

        private int GetHeight()
        {
            if(!height.HasValue)
            {
                GetImageData();
            }
            return height!.Value;
        }

        public Rgba32[] ImageData { get => GetData(); }

        private Rgba32[] GetData()
        {
            if (data == null)
            {
                lock (dataLock)
                {
                    if (data == null)
                    {
                        GetImageData();
                    }
                    
                }
            }
            return data!;
        }
        private void GetImageData()
        {
            Image<Rgba32> textureBitmap;
            if (UriString == null)
            {
                throw new ConfigurationErrorsException("Uri is null and Data is null");
            }
            if (UriString.StartsWith("data:"))
            {
                string base64Data = UriString.Split(',')[1];

                // Convert the base64 string into a byte array
                byte[] imageBytes = Convert.FromBase64String(base64Data);

                // Load the image from the byte array
                using var ms = new MemoryStream(imageBytes);
                textureBitmap = Image.Load<Rgba32>(ms);
            }
            else
            {
                if (GltfRoot?.SourcePath == null)
                    throw new ConfigurationErrorsException("SourcePath for buffer is null.");
                textureBitmap = Image.Load<Rgba32>(Path.Combine(GltfRoot.SourcePath, UriString));
            }
            width = textureBitmap.Width;
            height = textureBitmap.Height;
            data = new Rgba32[textureBitmap.Width * textureBitmap.Height];
            textureBitmap.CopyPixelDataTo(data);
            textureBitmap.Dispose();
        }
    }
}