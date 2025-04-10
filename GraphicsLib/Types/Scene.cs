using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
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
        public Vector3 AmbientColor { get; set; } = new(1f, 1f, 1f);
        public float AmbientIntensity { get; set; } = 0f;
        public Vector3 baseDiffuseColor { get; set; } = new(1f, 1f, 1f);
        public float SpecularPower { get; set; } = 100f;

        public Vector3 LightColor { get; set; } = new(1f, 1f, 1f);
        public float LightIntensity { get; set; } = 1f;
        public Vector3 LightPosition { get; set; } = new(000f, 1000f, 1000f);
    }
}
