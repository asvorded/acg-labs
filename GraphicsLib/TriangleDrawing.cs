using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace GraphicsLib {
    public class TriangleDrawing {

        public static void DrawTriangle(Point p1, Point p2, Point p3) {
            // Define Ymax = Y1, Ymid = Y2 and Ymin = Y3
            if (p2.Y > p1.Y) {
                (p1, p2) = (p2, p1);
            }
            if (p3.Y > p1.Y) {
                (p1, p3) = (p3, p1);
            }
            if (p3.Y > p2.Y) { 
                (p2, p3) = (p3, p2);
            }

            var pMax = p1;
            var pMid = p2;
            var pMin = p3;

            
        }
    }
}
