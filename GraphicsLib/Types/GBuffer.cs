using System.Runtime.InteropServices;

namespace GraphicsLib.Types
{
    class GBuffer
    {
        public int Width { get => width; private set => width = value; }
        public int Height { get => height; private set => height = value; }
        private volatile float[] depths;
        private volatile int[] triangleIndices;
        private int width;
        private int height;

        public GBuffer(int width, int height)
        {
            this.Width = width;
            this.Height = height;
            depths = new float[width * height];
            triangleIndices = new int[width * height];
            Clear();
        }
        public void Clear()
        {
            Array.Fill<int>(triangleIndices, -1);
            Array.Fill<float>(depths, 1f);
        }
        public int TriangleIndex(int x, int y)
        {
            if (x < 0 || x >= width || y < 0 || y >= height)
            {
                throw new ArgumentOutOfRangeException("x or y out of buffer range");
            }
            return triangleIndices[y * width + x];
        }
        public bool TestAndSet(int x, int y, float depth, int triangleIndex)
        {

            if (x < 0 || x >= width || y < 0 || y >= height)
            {
                throw new ArgumentOutOfRangeException("x or y out of buffer range");
            }
            int pos = y * width + x;
            SpinWait spinWait = new();
            while (true)
            {
                float currentDepth = depths[pos];
                if (currentDepth < depth)
                {
                    return false;
                }
                if (Interlocked.CompareExchange(ref depths[pos], depth, currentDepth) == currentDepth)
                {
                    triangleIndices[pos] = triangleIndex;
                    return true;
                }
                spinWait.SpinOnce();
            }
        }
    }
}
