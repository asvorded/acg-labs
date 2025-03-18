using GraphicsLib.Primitives;
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
        public Vector3[] vertices= [];
        public Vector3[] normals = [];
        public Vector2[] uvs = [];
        public StaticTriangle[] triangles = [];
        public Face[] faces = [];
        public ObjTransformation Transformation;
        public Material material;
        public Obj() { }
    }
}
