using System.Numerics;

namespace GraphicsLib.Types2
{
    public class ModelMesh
    {
        public BoundingBox? BoundingBox { get; set; }
        public required ModelPrimitive[] Primitives { get; set; }
    }
}