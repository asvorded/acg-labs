using GraphicsLib.Types.GltfTypes;
using SixLabors.ImageSharp.PixelFormats;
using System.Configuration;
using System.Numerics;

namespace GraphicsLib.Types
{
    //TODO: bake more
    public class Sampler
    {
        private int width;
        private int height;
        private Rgba32[]? textureData;
        private readonly WrappingMode uWrappingMode;
        private readonly WrappingMode vWrappingMode;
        private readonly MagnificationFilterMode magnificationFilterMode;
        private readonly MinificationFilterMode minificationFilterMode;

        public Sampler(WrappingMode uWrappingMode,
            WrappingMode vWrappingMode,
            MagnificationFilterMode magnificationFilterMode,
            MinificationFilterMode minificationFilterMode)
        {
            this.uWrappingMode = uWrappingMode;
            this.vWrappingMode = vWrappingMode;
            this.magnificationFilterMode = magnificationFilterMode;
            this.minificationFilterMode = minificationFilterMode;
        }
        public void BindTexture(Rgba32[] textureData, int width, int height)
        {
            this.textureData = textureData;
            this.width = width;
            this.height = height;
        }
        public Vector4 Sample(Vector2 uv)
        {
            if(textureData == null)
            {
                throw new ConfigurationErrorsException("Texture data is not bound to sampler");
            }
            switch (uWrappingMode)
            {
                case WrappingMode.ClampToEdge:
                    uv.X = float.Clamp(uv.X, 0, 1);
                    break;
                case WrappingMode.Repeat:
                    uv.X = uv.X - MathF.Floor(uv.X);
                    break;
                case WrappingMode.MirroredRepeat:
                    uv.X = uv.X - uv.X % 2;
                    if (uv.X > 1)
                        uv.X = 2 - uv.X;
                    break;
            }
            switch (vWrappingMode)
            {
                case WrappingMode.ClampToEdge:
                    uv.Y = float.Clamp(uv.Y, 0, 1);
                    break;
                case WrappingMode.Repeat:
                    uv.Y = uv.Y - MathF.Floor(uv.Y);
                    break;
                case WrappingMode.MirroredRepeat:
                    uv.Y = uv.Y - uv.Y % 2;
                    if (uv.Y > 1)
                        uv.Y = 2 - uv.Y;
                    break;
            }
            switch (magnificationFilterMode)
            {
                case MagnificationFilterMode.Nearest:
                    {
                        int x = (int)(uv.X * (width - 1));
                        int y = (int)(uv.Y * (height - 1));
                        return textureData[y * width + x].ToScaledVector4();
                    }
                case MagnificationFilterMode.Linear:
                    {
                        int x0 = (int)(uv.X * (width - 1));
                        int x1 = int.Clamp(x0 + 1, 0, width - 1);
                        int y0 = (int)(uv.Y * (height - 1));
                        int y1 = int.Clamp(y0 + 1, 0, width - 1);
                        float dx = uv.X * (width - 1) - x0;
                        float dy = uv.Y * (height - 1) - y0;
                        Rgba32 sample = textureData[y0 * width + x0];
                        Vector4 c00 = textureData[y0 * width + x0].ToScaledVector4();
                        Vector4 c10 = textureData[y0 * width + x1].ToScaledVector4();
                        Vector4 c01 = textureData[y1 * width + x0].ToScaledVector4();
                        Vector4 c11 = textureData[y1 * width + x1].ToScaledVector4();
                        Vector4 c = (c00 * (1 - dx) + c10 * dx) * (1 - dy) + (c01 * (1 - dx) + c11 * dx) * dy;
                        return c;
                    }
            }
            return default;
        }
    }
}
