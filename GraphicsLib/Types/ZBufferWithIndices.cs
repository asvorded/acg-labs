using System.Runtime.InteropServices;

namespace GraphicsLib.Types
{
    class ZBufferWithIndices
    {
        [StructLayout(LayoutKind.Explicit)]
        public struct PixelData
        {
            [FieldOffset(0)]
            public UInt64 data;
            [FieldOffset(0)]
            public float depth = float.PositiveInfinity;
            [FieldOffset(4)]
            public int triangleIndex = -1;
            public PixelData()
            {
            }
            public PixelData(float depth, int triangleIndex)
            {
                this.depth = depth;
                this.triangleIndex = triangleIndex;
            }
            public override bool Equals(object? obj)
            {
                if (obj is PixelData other)
                {
                    return depth == other.depth && triangleIndex == other.triangleIndex;
                }
                return false;
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(depth, triangleIndex);
            }
        }
        public int Width { get => width; private set => width = value; }
        public int Height { get => height; private set => height = value; }
        private PixelData[] buffer;
        private int width;
        private int height;

        public ZBufferWithIndices(int width, int height)
        {
            this.Width = width;
            this.Height = height;
            buffer = new PixelData[width * height];
            Clear();
        }
        public void Clear()
        {
            Array.Fill<PixelData>(buffer, new PixelData());

        }
        public PixelData At(int pos)
        {
            return buffer[pos];
        }
        public PixelData this[int x, int y]
        {
            get
            {
                if (x < 0 || x >= width || y < 0 || y >= height)
                {
                    throw new ArgumentOutOfRangeException("x or y out of buffer range");
                }
                return buffer[y * width + x];
            }
            set
            {
                if (x < 0 || x >= width || y < 0 || y >= height)
                {
                    throw new ArgumentOutOfRangeException("x or y out of buffer range");
                }
                buffer[y * Width + x] = value;
            }
        }
        static int missCount = 0;
        static int interlockCount = 0;
        public bool TestAndSet(int x, int y, float depth, int triangleIndex)
        {

            if (x < 0 || x >= width || y < 0 || y >= height)
            {
                throw new ArgumentOutOfRangeException("x or y out of buffer range");
            }
            int pos = y * width + x;
            SpinWait spinWait = new();
            PixelData pixelData = default;
            pixelData.triangleIndex = triangleIndex;
            pixelData.depth = depth;
            while (true)
            {
                PixelData currentPixel = buffer[pos];
                if (currentPixel.depth < depth)
                {
                    Interlocked.Increment(ref missCount);
                    return false;
                }
                if (Interlocked.CompareExchange(ref buffer[pos].data, pixelData.data, currentPixel.data) == currentPixel.data)
                {
                    return true;
                }
                Interlocked.Increment(ref interlockCount);
                spinWait.SpinOnce();
            }
        }
    }
}
