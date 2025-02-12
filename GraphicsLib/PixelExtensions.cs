using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Lab1
{
    public static class PixelExtensions
    {

        private static Int32Rect defaultRect = new Int32Rect(0, 0, 1, 1);

        public static void BeginLock(this WriteableBitmap bitmap)
        {
            bitmap.Lock();
        }

        public static unsafe void SetPixelLocked(this WriteableBitmap bitmap, int x, int y, uint argb)
        {
            unsafe
            {
                uint* ptr = (uint*)bitmap.BackBuffer;
                ptr[y * bitmap.PixelWidth + x] = argb;
            }
            defaultRect.X = x;
            defaultRect.Y = y;
            bitmap.AddDirtyRect(defaultRect);
        }
        public static unsafe void SetPixelLockedNoDirty(this WriteableBitmap bitmap, int x, int y, uint argb)
        {
            unsafe
            {
                uint* ptr = (uint*)bitmap.BackBuffer;
                ptr[y * bitmap.PixelWidth + x] = argb;
            }
        }

        public static void EndLock(this WriteableBitmap bitmap)
        {
            bitmap.Unlock();
        }

        public static void SetPixel(this WriteableBitmap bitmap, int x, int y, uint color)
        {
            defaultRect.X = x;
            defaultRect.Y = y;
            bitmap.WritePixels(defaultRect, BitConverter.GetBytes(color), 4, 0);
        }

        public static void DrawLine(this WriteableBitmap bitmap, int width, int height, System.Drawing.Point start, System.Drawing.Point finish, uint color)
        {
            if (bitmap == null)
                return;
            int dx = (finish.X > start.X) ? (finish.X - start.X) : (start.X - finish.X);
            int dy = (finish.Y > start.Y) ? (finish.Y - start.Y) : (start.Y - finish.Y);
            int gradX = (finish.X >= start.X) ? 1 : -1;
            int gradY = (finish.Y >= start.Y) ? 1 : -1;
            if (dy < dx)
            {
                if (finish.X < start.X)
                {
                    (start, finish) = (finish, start);
                    gradY = -gradY;
                }

                int error = 0;
                int deltaerr = dy + 1;
                int y = start.Y;
                for (int x = start.X; x < finish.X; x++)
                {
                    if (x >= 0 && x < width && y >= 0 && y < height)
                        bitmap.SetPixelLockedNoDirty(x, y, color);
                    error += deltaerr;
                    if (error > dx + 1)
                    {
                        y += gradY;
                        error -= dx + 1;
                    }
                }
            }
            else
            {
                if (finish.Y < start.Y)
                {
                    (start, finish) = (finish, start);
                    gradX = -gradX;
                }
                int error = 0;
                int deltaerr = dx + 1;
                int x = start.X;
                for (int y = start.Y; y < finish.Y; y++)
                {
                    if (x >= 0 && x < width && y >= 0 && y < height)
                        bitmap.SetPixelLockedNoDirty(x, y, color);
                    error += deltaerr;
                    if (error > dy + 1)
                    {
                        x += gradX;
                        error -= dy + 1;
                    }
                }
            }
        }
    
        public static void DrawLine(this WriteableBitmap bitmap, int width, int height, int x0, int y0, int x1, int y1, uint color)
        {
            if (bitmap == null)
                return;
            int dx = (x1 > x0) ? (x1 - x0) : (x0 - x1);
            int dy = (y1 > y0) ? (y1 - y0) : (y0 - y1);
            int gradX = (x1 >= x0) ? 1 : -1;
            int gradY = (y1 >= y0) ? 1 : -1;
            if (dy < dx)
            {
                if (x1 < x0)
                {
                    (x0, x1) = (x1, x0);
                    (y0, y1) = (y1, y0);
                    gradY = -gradY;
                }

                int error = 0;
                int deltaerr = dy + 1;
                int y = y0;
                for (int x = x0; x < x1; x++)
                {
                    if (x >= 0 && x < width && y >= 0 && y < height)
                        bitmap.SetPixelLockedNoDirty(x, y, color);
                    error += deltaerr;
                    if (error > dx + 1)
                    {
                        y += gradY;
                        error -= dx + 1;
                    }
                }
            }
            else
            {
                if (y1 < y0)
                {
                    (x0, x1) = (x1, x0);
                    (y0, y1) = (y1, y0);
                    gradX = -gradX;
                }
                int error = 0;
                int deltaerr = dx + 1;
                int x = x0;
                for (int y = y0; y < y1; y++)
                {
                    if (x >= 0 && x < width && y >= 0 && y < height)
                        bitmap.SetPixelLockedNoDirty(x, y, color);
                    error += deltaerr;
                    if (error > dy + 1)
                    {
                        x += gradX;
                        error -= dy + 1;
                    }
                }
            }
        }
    }
}
