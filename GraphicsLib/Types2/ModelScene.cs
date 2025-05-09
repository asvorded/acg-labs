using GraphicsLib.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GraphicsLib.Types2
{
    public class ModelScene
    {
        public ModelNode[] RootModelNodes { get; set; }

        public Camera? Camera { get; set; }
    }
}
