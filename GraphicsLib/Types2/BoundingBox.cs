using System.Numerics;

namespace GraphicsLib.Types2
{
    public record struct BoundingBox(Vector3 Min, Vector3 Max)
    {
        public readonly Vector3 Center => (Min + Max) / 2;
    }
}