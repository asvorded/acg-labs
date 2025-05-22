using GraphicsLib.Types.GltfTypes;
using System.Numerics;

namespace GraphicsLib.Types2
{
    public class ModelSkin
    {
        public Matrix4x4[]? InverseBindMatrices { get; set; }
        public Matrix4x4[]? CurrentFrameJointMatrices { get; set; }
        public ModelNode? Skeleton { get; set; }
        public static ModelSkin FromGltfSkin(GltfSkin gltfSkin)
        {
            var skin = new ModelSkin()
            {
                InverseBindMatrices = gltfSkin.GetInverseBindMatricesArray(),
            };
            if (skin.InverseBindMatrices != null)
            {
                skin.CurrentFrameJointMatrices = new Matrix4x4[skin.InverseBindMatrices.Length];
            }
            return skin;
        }
    }
}
