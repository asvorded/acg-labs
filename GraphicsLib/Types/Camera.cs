using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace GraphicsLib.Types
{
    public class Camera
    {
        public Vector3 Position { get; set; }
        public Quaternion Rotation { get; set; }
    }
}
