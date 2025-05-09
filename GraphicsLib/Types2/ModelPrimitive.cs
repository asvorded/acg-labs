using GraphicsLib.Types;
using GraphicsLib.Types.GltfTypes;
using System.Numerics;

namespace GraphicsLib.Types2
{
    public class ModelPrimitive
    {
        public required Dictionary<string, short> AttributesOffsets { get; set; }
        public int[]? Indices { get; set; }
        public float[][] FloatData { get; set; } = [];
        public int VertexCount { get; set; }
        public Material? Material { get; set; }
        public GltfMeshMode Mode { get; set; } = GltfMeshMode.TRIANGLES;
        public BoundingBox? BoundingBox { get; set; }
    }
}