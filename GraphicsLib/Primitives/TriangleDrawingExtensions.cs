using GraphicsLib.Types;
using System.Numerics;
using System.Windows.Media.Imaging;

namespace GraphicsLib.Primitives
{
    public static class TriangleDrawingExtensions
    {
        public static void DrawTriangleWithZBuffer(
            this WriteableBitmap bitmap, int bitmapWidth, int bitmapHeight,
            Vector4 min, Vector4 mid, Vector4 max,
            uint color, Zbuffer zbuffer
        )
        {
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
                if (mid.X < min.X)
                {
                    (min, mid) = (mid, min);
                }
                bitmap.DrawFlatTopTriangleWithZBuffer(bitmapWidth, bitmapHeight, min, mid, max, color, zbuffer);
            }
            else if (max.Y == mid.Y)
            {
                //flat bottom
                if (max.X < mid.X)
                {
                    (mid, max) = (max, mid);
                }
                bitmap.DrawFlatBottomTriangleWithZBuffer(bitmapWidth, bitmapHeight, min, mid, max, color, zbuffer);
            }
            else
            {
                float c = (mid.Y - min.Y) / (max.Y - min.Y);
                Vector4 interpolant = Vector4.Lerp(min, max, c);
                if (interpolant.X > mid.X)
                {
                    //right major
                    bitmap.DrawFlatBottomTriangleWithZBuffer(bitmapWidth, bitmapHeight, min, mid, interpolant, color, zbuffer);
                    bitmap.DrawFlatTopTriangleWithZBuffer(bitmapWidth, bitmapHeight, mid, interpolant, max, color, zbuffer);
                }
                else
                {
                    //left major
                    bitmap.DrawFlatBottomTriangleWithZBuffer(bitmapWidth, bitmapHeight, min, interpolant, mid, color, zbuffer);
                    bitmap.DrawFlatTopTriangleWithZBuffer(bitmapWidth, bitmapHeight, interpolant, mid, max, color, zbuffer);
                }
            }

        }

        private static void DrawFlatTopTriangleWithZBuffer(this WriteableBitmap bitmap, int bitmapWidth, int bitmapHeight,
            Vector4 p0, Vector4 p1, Vector4 p2, uint color, Zbuffer zbuffer)
        {
            float dy = p2.Y - p0.Y;
            Vector4 dp0 = (p2 - p0) / dy;
            Vector4 dp1 = (p2 - p1) / dy;
            Vector4 rightPoint = p1;
            bitmap.DrawFlatTriangleWithZBuffer(bitmapWidth, bitmapHeight, p0, rightPoint, p2, dp0, dp1, color, zbuffer);
        }
        private static void DrawFlatBottomTriangleWithZBuffer(this WriteableBitmap bitmap, int bitmapWidth, int bitmapHeight,
            Vector4 p0, Vector4 p1, Vector4 p2, uint color, Zbuffer zbuffer)
        {
            float dy = p2.Y - p0.Y;
            Vector4 dp0 = (p1 - p0) / dy;
            Vector4 dp1 = (p2 - p0) / dy;
            Vector4 rightPoint = p0;
            bitmap.DrawFlatTriangleWithZBuffer(bitmapWidth, bitmapHeight, p0, rightPoint, p2, dp0, dp1, color, zbuffer);
        }

        private static void DrawFlatTriangleWithZBuffer(this WriteableBitmap bitmap, int bitmapWidth, int bitmapHeight,
            Vector4 leftPoint, Vector4 rightPoint, Vector4 p2, Vector4 dp0, Vector4 dp1, uint color, Zbuffer zbuffer)
        {
            int yStart = Math.Max((int)Math.Ceiling(leftPoint.Y), 0);
            int yEnd = Math.Min((int)Math.Ceiling(p2.Y), bitmapHeight - 1);
            float p0y = leftPoint.Y;
            leftPoint += dp0 * (yStart - p0y);
            rightPoint += dp1 * (yStart - p0y);
            for (int y = yStart; y < yEnd; y++)
            {
                int xStart = Math.Max((int)Math.Ceiling(leftPoint.X), 0);
                int xEnd = Math.Min((int)Math.Ceiling(rightPoint.X), bitmapWidth - 1);
                Vector4 iLine = leftPoint;
                float dx = rightPoint.X - leftPoint.X;
                Vector4 diLine = (rightPoint - leftPoint) / dx;
                iLine += diLine * (xStart - leftPoint.X);
                for (int x = xStart; x < xEnd; x++)
                {
                    float z = iLine.W;
                    if (zbuffer.TestAndSet(x, y, z))
                    {
                        unsafe
                        {
                            uint* ptr = (uint*)bitmap.BackBuffer;
                            ptr[y * bitmapWidth + x] = color;
                        }
                    }
                    iLine += diLine;
                }
                leftPoint += dp0;
                rightPoint += dp1;
            }
        }
    }
}
