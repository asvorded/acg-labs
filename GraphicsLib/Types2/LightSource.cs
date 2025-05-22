using System.Numerics;

namespace GraphicsLib.Types2
{
    public abstract class LightSource
    {
        public required Vector3 Color { get; set; }
        public required float Intensity { get; set; }
        public required float ShadowMapSize { get; set; }
        public abstract void CalculateLightDirAndIntensity(in Vector3 position, out Vector3 lightDir, out float intensity);
    }
    public class PointLightSource : LightSource
    {
        public required Vector3 Position {  get; set; }

        public override void CalculateLightDirAndIntensity(in Vector3 position, out Vector3 lightDir, out float intensity)
        {
            lightDir = Vector3.Normalize(position - Position);
            intensity = Intensity;
        }
    }
    public class AmbientLightSource : LightSource
    {
        public override void CalculateLightDirAndIntensity(in Vector3 position, out Vector3 lightDir, out float intensity)
        {
            lightDir = Vector3.Zero;
            intensity = Intensity;
        }
    }
    public class DirectionalLightSource : LightSource
    {
        public required Vector3 Direction { get; set; }
        public required float CoverSize { get; set; }

        public override void CalculateLightDirAndIntensity(in Vector3 position, out Vector3 lightDir, out float intensity)
        {
            lightDir = Direction;
            intensity = Intensity;
        }
    }
    public class SpotLightSource : LightSource
    {
        public required Vector3 Position { get; set; }
        public required Vector3 Direction { get; set; }
        public required float CutOffCos { get; set; }
        public required float OuterCutCos { get; set; }

        public override void CalculateLightDirAndIntensity(in Vector3 position, out Vector3 lightDir, out float intensity)
        {
            lightDir = Vector3.Normalize(position - Position);
            float theta = Vector3.Dot(Direction, lightDir);
            intensity = float.Clamp(float.Lerp(Intensity, 0, (theta - CutOffCos) / (OuterCutCos - CutOffCos)), 0, Intensity);
        }
    }
}
