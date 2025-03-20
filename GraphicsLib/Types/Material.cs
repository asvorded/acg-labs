using GraphicsLib.Types.GltfTypes;
using SixLabors.ImageSharp.PixelFormats;
using System.Numerics;
using System.Security.Cryptography;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;

namespace GraphicsLib.Types
{
    public class Material
    {
        public static Material defaultMaterial = new Material();


        public string name;
        public Vector4 baseColor = new(1, 1, 1, 1);
        public Sampler? baseColorTextureSampler;
        public float metallic = 1f;
        public float roughness = 1f;

        public Material()
        {
            name = string.Empty;
        }
        public static Material FromGltfMaterial(GltfMaterial material)
        {
            Material newMaterial = new();
            if (material.Name != null)
                newMaterial.name = material.Name;
            if (material.PbrMetallicRoughness != null)
            {
                var pbr = material.PbrMetallicRoughness;
                newMaterial.metallic = pbr.MetallicFactor;
                newMaterial.roughness = pbr.RoughnessFactor;
                newMaterial.baseColor = pbr.BaseColorFactor;
                if (pbr.BaseColorTexture != null)
                {
                    var gltfTexture = pbr.BaseColorTexture.Texture;
                    var samplerSettings = gltfTexture.Sampler;
                    var image = gltfTexture.Image;
                    var textureImage = image!.Texture;
                    var sampler = samplerSettings!.GetSampler();
                    Rgba32[] pixels = new Rgba32[textureImage.Height * textureImage.Width];
                    textureImage.CopyPixelDataTo(pixels);
                    sampler.BindTexture(pixels, textureImage.Width, textureImage.Height);
                    newMaterial.baseColorTextureSampler = sampler;
                }
                if (pbr.MetallicRoughnessTexture != null)
                {
                    //var texture = pbr.MetallicRoughnessTexture;
                    //var image = gltfRoot.Images![texture.Index];
                    //newMaterial.metallicRoughnessTexture = Path.Combine(sourceDirectory, image.UriString);
                }
            }
            return newMaterial;
        }
    }
}