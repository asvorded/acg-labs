using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace GraphicsLib.Types
{
    public interface IShader<Vertex> where Vertex : IVertex<Vertex>
    {
        public Scene Scene { get; set; }
        public uint PixelShader(Vertex input);
        public Vertex GetFromFace(Obj obj, int faceIndex, int vertexIndex);
    }
}
