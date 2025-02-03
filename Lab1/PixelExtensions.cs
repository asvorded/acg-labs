using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using System.Windows;
using System.Windows.Media;
using System.Runtime.CompilerServices;

namespace Lab1 {
    public static class PixelExtensions {

        private static Int32Rect defaultRect = new Int32Rect(0, 0, 1, 1);

        public static void BeginLock(this WriteableBitmap bitmap) {
            bitmap.Lock();
        }

        public static unsafe void SetPixelLocked(this WriteableBitmap bitmap, int x, int y, uint argb) {
            unsafe {
                uint* ptr = (uint*)bitmap.BackBuffer;
                ptr[y * 4 + x] = argb;
            }
            defaultRect.X = x;
            defaultRect.Y = y;
            bitmap.AddDirtyRect(defaultRect);
        }
        
        public static void EndLock(this WriteableBitmap bitmap) {
            bitmap.Unlock();
        }

        public static void SetPixel(this WriteableBitmap bitmap, int x, int y, uint color) {
            defaultRect.X = x;
            defaultRect.Y = y;
            bitmap.WritePixels(defaultRect, BitConverter.GetBytes(color), 4, 0);
        }
    }
}
