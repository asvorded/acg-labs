using GraphicsLib.Types;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace GraphicsLib.Shaders
{
    public interface IShader<Vertex> where Vertex : IVertex<Vertex>
    {
        public Scene Scene { get; set; }
        public uint PixelShader(in Vertex input);
        public Vertex GetVertexWithWorldPositionFromFace(Obj obj, int faceIndex, int vertexIndex);
        public Vertex GetVertexWithWorldPositionFromTriangle(Obj obj, int triangleIndex, int vertexIndex);
    }
}
