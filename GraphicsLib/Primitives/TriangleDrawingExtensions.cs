using GraphicsLib.Types;
using System.Numerics;
using System.Windows.Media.Imaging;

namespace GraphicsLib.Primitives
{
    public static class TriangleDrawingExtensions
    {

        /// <summary>
        /// Calculates correct (1 / k) value for given two points
        /// </summary>
        /// <param name="v1">Start point</param>
        /// <param name="v2">End point</param>
        /// <returns>Correct (1 / k)</returns>
        /// <remarks>
        /// Function assumes that vectors contain integer points because they are screen coordinates
        /// </remarks>
        public static double CalculateKIncrement(int v1X, int v1Y, int v2X, int v2Y)
        {
            // Check for vertical
            if (v1X == v2X)
            {
                return 0.0;
            }
            if (v1Y == v2Y)
            {
                return v2X - v1X;
            }
            return (double)(v1X - v2X) / (v1Y - v2Y);
        }

        /// <summary>
        /// Draws a triangle by points in screen coordinates
        /// </summary>
        /// <param name="min">First point (will be min)</param>
        /// <param name="mid">Second point (will be mid)</param>
        /// <param name="max">Third point (will be max)</param>
        /// <remarks>
        /// Function assumes that vectors contain integer points because they are screen coordinates
        /// </remarks>
        public static void DrawTriangle2(
            this WriteableBitmap bitmap, int bitmapWidth, int bitmapHeight,
            Vector4 min, Vector4 mid, Vector4 max,
            uint color
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

            int xMin, yMin, xMid, yMid, xMax, yMax;
            (xMin, yMin) = ((int)min.X, (int)min.Y);
            (xMid, yMid) = ((int)mid.X, (int)mid.Y);
            (xMax, yMax) = ((int)max.X, (int)max.Y);

            // Define increase coefs and error values
            int errToMid, errToMax, errBetween;
            var kToMidIncrement = CalculateKIncrement(xMin, yMin, xMid, yMid);
            var kToMaxIncrement = CalculateKIncrement(xMin, yMin, xMax, yMax);
            var kBetweenIncrement = CalculateKIncrement(xMid, yMid, xMax, yMax);

            unsafe
            {
                uint* ptr = (uint*)bitmap.BackBuffer;

                double xToMid = xMin, xToMax = xMin;
                double x1, x2;
                int y = yMin;

                while (y <= yMid)
                {
                    xToMid += kToMidIncrement;
                    xToMax += kToMaxIncrement;

                    if (xToMid < xToMax)
                    {
                        x1 = xToMid;
                        x2 = xToMax;
                    }
                    else
                    {
                        x2 = xToMid;
                        x1 = xToMax;
                    }

                    for (int x = (int)x1; x <= x2; ++x)
                    {
                        if (x >= 0 && x < bitmapWidth && y >= 0 && y < bitmapHeight)
                        {
                            ptr[y * bitmapWidth + x] = color;
                        }
                    }
                    ++y;
                }

                while (y <= yMax)
                {
                    xToMid += kBetweenIncrement;
                    xToMax += kToMaxIncrement;

                    if (xToMid < xToMax)
                    {
                        x1 = xToMid;
                        x2 = xToMax;
                    }
                    else
                    {
                        x2 = xToMid;
                        x1 = xToMax;
                    }

                    for (int x = (int)x1; x <= x2; ++x)
                    {
                        if (x >= 0 && x < bitmapWidth && y >= 0 && y < bitmapHeight)
                        {
                            ptr[y * bitmapWidth + x] = color;
                        }
                    }
                    ++y;
                }

            }
        }
        public static void DrawTriangle(
            this WriteableBitmap bitmap, int bitmapWidth, int bitmapHeight,
            Vector4 min, Vector4 mid, Vector4 max,
            uint color
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
                if (mid.X < min.X)
                {
                    (min, mid) = (mid, min);
                }
                bitmap.DrawFlatTopTriangle(bitmapWidth, bitmapHeight, min, mid, max, color);
            }
            else if (max.Y == mid.Y)
            {
                if (max.X < mid.X)
                {
                    (mid, max) = (max, mid);
                }
                bitmap.DrawFlatBottomTriangle(bitmapWidth, bitmapHeight, min, mid, max, color);
            }
            else
            {
                float c = (mid.Y - min.Y) / (max.Y - min.Y);
                Vector4 interpolant = Vector4.Lerp(min, max, c);
                if (interpolant.X > mid.X)
                {
                    //right major
                    bitmap.DrawFlatBottomTriangle(bitmapWidth, bitmapHeight, min, mid, interpolant, color);
                    bitmap.DrawFlatTopTriangle(bitmapWidth, bitmapHeight, mid, interpolant, max, color);
                }
                else
                {
                    //left major
                    bitmap.DrawFlatBottomTriangle(bitmapWidth, bitmapHeight, min, interpolant, mid, color);
                    bitmap.DrawFlatTopTriangle(bitmapWidth, bitmapHeight, interpolant, mid, max, color);
                }
            }

        }

        private static void DrawFlatTopTriangle(this WriteableBitmap bitmap, int bitmapWidth, int bitmapHeight, Vector4 p0, Vector4 p1, Vector4 p2, uint color)
        {
            float dy = p2.Y - p0.Y;
            Vector4 dp0 = (p2 - p0) / dy;
            Vector4 dp1 = (p2 - p1) / dy;
            Vector4 rightPoint = p1;
            bitmap.DrawFlatTriangle(bitmapWidth, bitmapHeight, p0, p1, p2, dp0, dp1, rightPoint, color);
        }
        private static void DrawFlatBottomTriangle(this WriteableBitmap bitmap, int bitmapWidth, int bitmapHeight, Vector4 p0, Vector4 p1, Vector4 p2, uint color)
        {
            float dy = p2.Y - p0.Y;
            Vector4 dp0 = (p1 - p0) / dy;
            Vector4 dp1 = (p2 - p0) / dy;
            Vector4 rightPoint = p0;
            bitmap.DrawFlatTriangle(bitmapWidth, bitmapHeight, p0, p1, p2, dp0, dp1, rightPoint, color);
        }
        private static void DrawFlatTriangle(this WriteableBitmap bitmap, int bitmapWidth, int bitmapHeight,
            Vector4 p0, Vector4 p1, Vector4 p2, Vector4 dp0, Vector4 dp1, Vector4 rightPoint, uint color)
        {
            int yStart = Math.Max((int)Math.Ceiling(p0.Y), 0);
            int yEnd = Math.Min((int)Math.Ceiling(p2.Y), bitmapHeight - 1);
            Vector4 leftPoint = p0 + dp0 * (yStart - p0.Y);
            rightPoint += dp1 * (yStart - p0.Y);
            for (int y = yStart; y < yEnd; y++)
            {
                int xStart = Math.Max((int)Math.Ceiling(leftPoint.X), 0);
                int xEnd = Math.Min((int)Math.Ceiling(rightPoint.X), bitmapWidth - 1);
                for (int x = xStart; x < xEnd; x++)
                {
                    unsafe
                    {
                        uint* ptr = (uint*)bitmap.BackBuffer;
                        ptr[y * bitmapWidth + x] = color;
                    }
                }
                leftPoint += dp0;
                rightPoint += dp1;
            }
        }
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
        private static void DrawFlatTriangleWithZBuffer2(this WriteableBitmap bitmap, int bitmapWidth, int bitmapHeight,
            Vector4 leftPoint, Vector4 rightPoint, Vector4 EndPoint, Vector4 dLeftPoint, Vector4 dRightPoint, uint color, Zbuffer zbuffer)
        {
            int yStart = Math.Max((int)Math.Ceiling(leftPoint.Y), 0);
            int yEnd = Math.Min((int)Math.Ceiling(EndPoint.Y), bitmapHeight - 1);
            leftPoint += dLeftPoint * (yStart - leftPoint.Y);
            rightPoint += dRightPoint * (yStart - leftPoint.Y);
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
                leftPoint += dLeftPoint;
                rightPoint += dRightPoint;
            }

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
