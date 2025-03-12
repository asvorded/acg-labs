using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GraphicsLib.Types
{
    public class Scene
    {
        public Camera Camera { get; set; }
        public Obj? Obj { get; set; }
        public Scene(Camera? camera = null, Obj? obj = null)
        {
            Camera = camera ?? new Camera();
            Obj = obj;
        }
    }
}
