using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;

namespace GraphicsLib.Primitives {
    public static class TriangleDrawingExtensions {

        /// <summary>
        /// Calculates correct (1 / k) value for given two points
        /// </summary>
        /// <param name="v1">Start point</param>
        /// <param name="v2">End point</param>
        /// <returns>Correct (1 / k)</returns>
        /// <remarks>
        /// Function assumes that vectors contain integer points because they are screen coordinates
        /// </remarks>
        public static double CalculateKIncrement(int v1X, int v1Y, int v2X, int v2Y) {
            // Check for vertical
            if (v1X == v2X) {
                return 0.0;
            }
            if (v1Y == v2Y) {
                return v2X - v1X;
            }
            return (v1X - v2X) / (v1Y - v2Y);
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
        public static void DrawTriangle(
            this WriteableBitmap bitmap, int bitmapWidth, int bitmapHeight,
            Vector4 min, Vector4 mid, Vector4 max,
            uint color
        ) {
            // Correct min, mid and max
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
            (xMin, yMin) = ((int)min.X, (int)min.Y);
            (xMid, yMid) = ((int)mid.X, (int)mid.Y);
            (xMax, yMax) = ((int)max.X, (int)max.Y);

            // Define increase coefs and error values
            int errToMid, errToMax, errBetween;
            var kToMidIncrement = CalculateKIncrement(xMin, yMin, xMid, yMid);
            var kToMaxIncrement = CalculateKIncrement(xMin, yMin, xMax, yMax);
            var kBetweenIncrement = CalculateKIncrement(xMid, yMid, xMax, yMax);

            unsafe {
                uint* ptr = (uint*)bitmap.BackBuffer;

                double xToMid = xMin, xToMax = xMin;
                double x1, x2;
                int y = yMin;

                while (y <= yMid) {
                    xToMid += kToMidIncrement;
                    xToMax += kToMaxIncrement;

                    if (xToMid < xToMax) {
                        x1 = xToMid;
                        x2 = xToMax;
                    } else {
                        x2 = xToMid;
                        x1 = xToMax;
                    }
                    
                    for (int x = (int)x1; x <= x2; ++x) {
                        if (x >= 0 && x < bitmapWidth && y >= 0 && y < bitmapHeight) {
                            ptr[y * bitmapWidth + x] = color;
                        }
                    }
                    ++y;
                }

                yMax = Math.Min(yMin, bitmapHeight);
                while (y <= yMax) {
                    xToMid += kBetweenIncrement;
                    xToMax += kToMaxIncrement;

                    if (xToMid < xToMax) {
                        x1 = xToMid;
                        x2 = xToMax;
                    } else {
                        x2 = xToMid;
                        x1 = xToMax;
                    }

                    for (int x = (int)x1; x <= x2; ++x) {
                        if (x >= 0 && x <= bitmapWidth && y >= 0 && y <= bitmapHeight) {
                            ptr[y * bitmapWidth + x] = color;
                        }
                    }
                    ++y;
                }

            }
        }
    }
}
