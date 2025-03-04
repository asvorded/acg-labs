using GraphicsLib.Types;
using System.Numerics;
using System.Windows.Media.Imaging;

namespace GraphicsLib.Primitives
{
    public static class TriangleDrawingExtensions
    {

        /// <summary>
        ///  Draws triangle of any size
        ///  points order is not important
        ///  W-coordinate is used for Z-buffer
        /// </summary>
        public static void DrawTriangleWithZBuffer(
            this WriteableBitmap bitmap, int bitmapWidth, int bitmapHeight,
            Vector4 p0, Vector4 p1, Vector4 p2,
            uint color, Zbuffer zbuffer
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
                bitmap.DrawFlatTopTriangleWithZBuffer(bitmapWidth, bitmapHeight, min, mid, max, color, zbuffer);
            }
            else if (max.Y == mid.Y)
            {
                //flat bottom
                //    /\
                //   /  \
                //   ----
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
                    //    min
                    //       
                    // mid     interpolant
                    //                    
                    //  
                    //                       max

                    bitmap.DrawFlatBottomTriangleWithZBuffer(bitmapWidth, bitmapHeight, min, interpolant, mid, color, zbuffer);
                    bitmap.DrawFlatTopTriangleWithZBuffer(bitmapWidth, bitmapHeight, mid, interpolant, max, color, zbuffer);
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
                    bitmap.DrawFlatBottomTriangleWithZBuffer(bitmapWidth, bitmapHeight, min, mid, interpolant, color, zbuffer);
                    bitmap.DrawFlatTopTriangleWithZBuffer(bitmapWidth, bitmapHeight, interpolant, mid, max, color, zbuffer);
                }
            }

        }

        private static void DrawFlatTopTriangleWithZBuffer(this WriteableBitmap bitmap, int bitmapWidth, int bitmapHeight,
            Vector4 leftTopPoint, Vector4 rightTopPoint, Vector4 bottomPoint, uint color, Zbuffer zbuffer)
        {
            float dy = bottomPoint.Y - leftTopPoint.Y;
            Vector4 dLeftPoint = (bottomPoint - leftTopPoint) / dy;
            Vector4 dRightPoint = (bottomPoint - rightTopPoint) / dy;
            Vector4 rightPoint = rightTopPoint;
            bitmap.DrawFlatTriangleWithZBuffer(bitmapWidth, bitmapHeight, leftTopPoint, rightPoint, bottomPoint,
                                                dLeftPoint, dRightPoint, color, zbuffer);
        }
        private static void DrawFlatBottomTriangleWithZBuffer(this WriteableBitmap bitmap, int bitmapWidth, int bitmapHeight,
            Vector4 topPoint, Vector4 rightBottomPoint, Vector4 leftBottomPoint, uint color, Zbuffer zbuffer)
        {
            float dy = rightBottomPoint.Y - topPoint.Y;
            Vector4 dRightPoint = (rightBottomPoint - topPoint) / dy;
            Vector4 dLeftPoint = (leftBottomPoint - topPoint) / dy;
            Vector4 rightPoint = topPoint;
            bitmap.DrawFlatTriangleWithZBuffer(bitmapWidth, bitmapHeight, topPoint, rightPoint, rightBottomPoint, dLeftPoint, dRightPoint, color, zbuffer);
        }
        private static void DrawFlatTriangleWithZBuffer(this WriteableBitmap bitmap, int bitmapWidth, int bitmapHeight,
            Vector4 leftPoint, Vector4 rightPoint, Vector4 EndPoint, Vector4 dLeftPoint, Vector4 dRightPoint, uint color, Zbuffer zbuffer)
        {
            int yStart = Math.Max((int)Math.Ceiling(leftPoint.Y), 0);
            int yEnd = Math.Min((int)Math.Ceiling(EndPoint.Y), bitmapHeight - 1);
            float yTop = leftPoint.Y;
            leftPoint += dLeftPoint * (yStart - yTop);
            rightPoint += dRightPoint * (yStart - yTop);
            for (int y = yStart; y < yEnd; y++)
            {
                int xStart = Math.Max((int)Math.Ceiling(leftPoint.X), 0);
                int xEnd = Math.Min((int)Math.Ceiling(rightPoint.X), bitmapWidth - 1);
                Vector4 lineInterpolant = leftPoint;
                float dx = rightPoint.X - leftPoint.X;
                Vector4 dLine = (rightPoint - leftPoint) / dx;
                lineInterpolant += dLine * (xStart - leftPoint.X);
                for (int x = xStart; x < xEnd; x++)
                {
                    float z = lineInterpolant.W;
                    if (zbuffer.TestAndSet(x, y, z))
                    {
                        unsafe
                        {
                            uint* ptr = (uint*)bitmap.BackBuffer;
                            ptr[y * bitmapWidth + x] = color;
                        }
                    }
                    lineInterpolant += dLine;
                }
                leftPoint += dLeftPoint;
                rightPoint += dRightPoint;
            }

        }
        public static void FlushZBufferV2(this WriteableBitmap bitmap, ZbufferV2 zbufferV2)
        {
            unsafe
            {
                uint* ptr = (uint*)bitmap.BackBuffer;
                int x = bitmap.PixelWidth;
                int y = bitmap.PixelHeight;
                for (int i = 0; i < x * y; i++)
                {
                    ptr[i] = zbufferV2.At(i).color;
                }
            }
        }

    }
}
