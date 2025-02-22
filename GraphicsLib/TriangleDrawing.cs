using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;

namespace GraphicsLib {
    public static class TriangleDrawingExtensions {

        /// <summary>
        /// Draws a triangle by points in screen coordinates
        /// </summary>
        /// <param name="min">First point (will be min)</param>
        /// <param name="mid">Second point (will be mid)</param>
        /// <param name="max">Third point (will be max)</param>
        public static void DrawTriangle(
            this WriteableBitmap bitmap, int bitmapWidth, int bitmapHeight,
            Vector min, Vector mid, Vector max,
            uint color
        ) {
            // Define min = p1, mid = p2 and max = p3
            if (mid.Y < min.Y) {
                (min, mid) = (mid, min);
            }
            if (max.Y < min.Y) {
                (min, max) = (max, min);
            }
            if (max.Y < mid.Y) { 
                (mid, max) = (max, mid);
            }

            int xMin, yMin, xMid, yMid, xMax, yMax;
            (xMin, yMin) = ((int)Math.Round(min.X), (int)Math.Round(min.Y));
            (xMid, yMid) = ((int)Math.Round(mid.X), (int)Math.Round(mid.Y));
            (xMax, yMax) = ((int)Math.Round(max.X), (int)Math.Round(max.Y));

            var kToMid = (mid.Y - min.Y) / (mid.X - min.X);
            var kToMin = (max.Y - min.Y) / (max.X - min.X);
            var kBetween = (mid.Y - max.Y) / (mid.X - max.X);

            unsafe {
                uint* ptr = (uint*)bitmap.BackBuffer;

                double x1 = xMin, x2 = xMin;
                for (int y = yMin; y <= yMid; y++) {
                    x1 += 1 / kToMid;
                    x2 += 1 / kToMin;
                    if (x1 > x2) {
                        (x1, x2) = (x2, x1);
                    }
                    for (int x = (int)x1; x <= (int)x2; x++) {
                        ptr[y * bitmapWidth + x] = color;
                    }
                }
                x1 = xMid; x2 = xMax;
                for (int y = yMid; y <= yMax; y++) {
                    x1 += 1 / kBetween;
                    x2 += 1 / kToMin;
                    if (x1 > x2) {
                        (x1, x2) = (x2, x1);
                    }
                    for (int x = (int)x1; x <= (int)x2; x++) {
                        ptr[y * bitmapWidth + x] = color;
                    }
                }
            }
        }
    }
}
