using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace GraphicsLib.Types {
    public struct ObjTransformation {

        public float AngleX { get; set; }
        public float AngleY { get; set; }
        public float AngleZ { get; set; }

        public Vector3 Offset { get; set; }
    }
}
