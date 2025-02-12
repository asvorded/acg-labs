using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace GraphicsLib.Types
{
    public class Obj
    {
        public List<Vector3> vertices = new();
        public List<Vector3> normals = new();
        public List<Face> faces = new();
        public Matrix4x4 transform = Matrix4x4.Identity;
        public Obj() { }
    }
}
