﻿using System.Numerics;
using System.Windows.Media.Imaging;

namespace GraphicsLib.Types
{
    public class Material
    {
        public static Material defaultMaterial = new Material();


        public string name;
        public int baseColorTextureWidth;
        public int baseColorTextureHeight;
        public Vector4 baseColor = new(1, 1, 1, 1);
        public uint[] baseColorTexture;
        public float metallic = 1f;
        public float roughness = 1f;

        public Material()
        {
            name = string.Empty;
        }
    }
}