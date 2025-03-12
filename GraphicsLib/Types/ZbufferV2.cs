using System.Numerics;
using System.Runtime.InteropServices;

namespace GraphicsLib.Types
{
    [StructLayout(LayoutKind.Explicit)]
    public struct PixelData
    {
        [FieldOffset(0)]
        public UInt64 data;
        [FieldOffset(0)]
        public float depth = float.PositiveInfinity;
        [FieldOffset(4)]
        public uint color = 0;
        public PixelData()
        {
        }
        public PixelData(float depth, uint color)
        {
            this.depth = depth;
            this.color = color;
        }
        public override bool Equals(object? obj)
        {
            if (obj is PixelData other)
            {
                return depth == other.depth && color == other.color;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(depth, color);
        }
    }
    public class ZbufferV2
    {
        public int Width { get => width; private set => width = value; }
        public int Height { get => height; private set => height = value; }
        private PixelData[] buffer;
        private int width;
        private int height;

        public ZbufferV2(int width, int height)
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
        public bool TestAndSet(int x, int y, float depth, uint color)
        {
            if (x < 0 || x >= width || y < 0 || y >= height)
            {
                throw new ArgumentOutOfRangeException("x or y out of buffer range");
            }
            int pos = y * width + x;
            SpinWait spinWait = new();
            PixelData pixelData = default;
            pixelData.color = color;
            pixelData.depth = depth;
            while (true)
            {
                PixelData currentPixel = buffer[pos];
                if (currentPixel.depth < depth)
                {
                    return false;
                }
                if (Interlocked.CompareExchange(ref buffer[pos].data, pixelData.data, currentPixel.data) == currentPixel.data)
                {
                    return true;
                }
                spinWait.SpinOnce();
            }
        }
        public void MapTriangle(
            Vector4 p0, Vector4 p1, Vector4 p2,
            uint color
        )
        {
            Vector4 min = p0;
            Vector4 mid = p1;
            Vector4 max = p2;
            // Correct min, mid and max
            if (mid.Y < min.Y)
            {
                (min, mid) = (mid, min);
            }
            if (max.Y < min.Y)
            {
                (min, max) = (max, min);
            }
            if (max.Y < mid.Y)
            {
                (mid, max) = (max, mid);
            }

            if (min.Y == mid.Y)
            {
                //flat top
                //   ----
                //   \  /
                //    \/
                if (mid.X < min.X)
                {
                    (min, mid) = (mid, min);
                }
                MapFlatTopTriangle(min, mid, max, color);
            }
            else if (max.Y == mid.Y)
            {
                //flat bottom
                //    /\
                //   /  \
                //   ----
                if (max.X > mid.X)
                {
                    (mid, max) = (max, mid);
                }
                MapFlatBottomTriangle(min, mid, max, color);
            }
            else
            {
                float c = (mid.Y - min.Y) / (max.Y - min.Y);
                Vector4 interpolant = Vector4.Lerp(min, max, c);
                if (interpolant.X > mid.X)
                {
                    //right major
                    //    min
                    //       
                    // mid     interpolant
                    //                    
                    //  
                    //                       max

                    MapFlatBottomTriangle(min, interpolant, mid, color);
                    MapFlatTopTriangle(mid, interpolant, max, color);
                }
                else
                {
                    //left major
                    //                  min
                    //       
                    //      interpolant     mid
                    //                    
                    //  
                    // max                      
                    MapFlatBottomTriangle(min, mid, interpolant, color);
                    MapFlatTopTriangle(interpolant, mid, max, color);
                }
            }

        }

        private void MapFlatTopTriangle(Vector4 leftTopPoint, Vector4 rightTopPoint, Vector4 bottomPoint, uint color)
        {
            float dy = bottomPoint.Y - leftTopPoint.Y;
            Vector4 dLeftPoint = (bottomPoint - leftTopPoint) / dy;
            Vector4 dRightPoint = (bottomPoint - rightTopPoint) / dy;
            float dzdx = (rightTopPoint.W - leftTopPoint.W) / (rightTopPoint.X - leftTopPoint.X);
            Vector4 rightPoint = rightTopPoint;
            MapFlatTriangle(leftTopPoint, rightPoint, bottomPoint.Y, dLeftPoint, dRightPoint, dzdx, color);
        }
        private void MapFlatBottomTriangle(Vector4 topPoint, Vector4 rightBottomPoint, Vector4 leftBottomPoint, uint color)
        {
            float dy = rightBottomPoint.Y - topPoint.Y;
            Vector4 dRightPoint = (rightBottomPoint - topPoint) / dy;
            Vector4 dLeftPoint = (leftBottomPoint - topPoint) / dy;
            float dzdx = (rightBottomPoint.W - leftBottomPoint.W) / (rightBottomPoint.X - leftBottomPoint.X);
            Vector4 rightPoint = topPoint;
            MapFlatTriangle(topPoint, rightPoint, rightBottomPoint.Y, dLeftPoint, dRightPoint, dzdx, color);
        }
        private void MapFlatTriangle(Vector4 leftPoint, Vector4 rightPoint, float yMax, Vector4 dLeftPoint, Vector4 dRightPoint, float dzdx, uint color)
        {
            int yStart = Math.Max((int)Math.Ceiling(leftPoint.Y), 0);
            int yEnd = Math.Min((int)Math.Ceiling(yMax), height);
            float yPrestep = yStart - leftPoint.Y;
            leftPoint += dLeftPoint * yPrestep;
            rightPoint += dRightPoint * yPrestep;
            for (int y = yStart; y < yEnd; y++)
            {
                int xStart = Math.Max((int)Math.Ceiling(leftPoint.X), 0);
                int xEnd = Math.Min((int)Math.Ceiling(rightPoint.X), width);
                float xPrestep = xStart - leftPoint.X;
                float z = leftPoint.W + dzdx * xPrestep;
                for (int x = xStart; x < xEnd; x++)
                {
                    TestAndSet(x, y, z, color);
                    z += dzdx;
                }
                leftPoint += dLeftPoint;
                rightPoint += dRightPoint;
            }

        }
    }
}
