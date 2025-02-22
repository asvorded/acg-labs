using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace GraphicsLib.Primitives {
    public class StaticTriangle {

        public const int TriangleVerticesCount = 3;

        public Vector4[] Vertices = new Vector4[TriangleVerticesCount];
    }
}
