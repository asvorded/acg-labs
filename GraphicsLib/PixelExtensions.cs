using System.Windows;
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
                ptr[y*bitmap.PixelWidth + x] = argb;
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
            defaultRect.X = x;
            defaultRect.Y = y;
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
        public static void DrawLine(this WriteableBitmap bitmap, System.Drawing.Point start, System.Drawing.Point finish, uint color)
        {
            try
            {

                int width = bitmap.PixelWidth;
                int height = bitmap.PixelHeight;
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
            catch (Exception ex)
            {
                int z = 0;
            }

        }
    }
}
