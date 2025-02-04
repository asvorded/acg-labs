using System.Numerics;

namespace GraphicsLib.Types
{
    public class Camera
    {
        private float azimuth;
        private float polar;
        private float distance;
        private static readonly Vector3 up = Vector3.UnitY;

        public float Azimuth { get => azimuth; set => SetAzimuth(value); }
        public float Polar { get => polar; set => SetPolar(value); }
        public Vector3 Position { get => GetPosition(); }
        public float Distance { get => distance; set => distance = value >= 0 ? value : 0; }
        public Vector3 Target { get; set; }
        public Matrix4x4 ViewMatrix { get => GetViewMatrix(); }

        private Matrix4x4 GetViewMatrix()
        {
            Vector3 position = Position;
            Vector3 zAxis = Vector3.Normalize(position - Target);
            Vector3 xAxis = Vector3.Normalize(Vector3.Cross(Camera.up, zAxis));
            Vector3 yAxis = -Vector3.Cross(xAxis, zAxis);
            Matrix4x4 view = new Matrix4x4(xAxis.X, yAxis.X, zAxis.X, 0,
                                           xAxis.Y, yAxis.Y, zAxis.Y, 0,
                                           xAxis.Z, yAxis.Z, zAxis.Z, 0,
                                           -Vector3.Dot(xAxis, position),
                                           -Vector3.Dot(yAxis, position),
                                           -Vector3.Dot(zAxis, position),
                                           1);
            //Matrix4x4 view = new Matrix4x4(xAxis.X, xAxis.Y, xAxis.Z, -Vector3.Dot(xAxis, position),
                                           //yAxis.X, yAxis.Y, yAxis.Z, -Vector3.Dot(yAxis, position),
                                           //zAxis.X, zAxis.Y, zAxis.Z, -Vector3.Dot(zAxis, position),
                                           //0, 0, 0, 1);
            return Matrix4x4.CreateLookAt(position, Target, up);
            return view;
        }

        public Camera()
        {
            Azimuth = 0;
            Polar = 0;// MathF.PI / 4;
            Distance = 1000;
            Target = Vector3.Zero;
        }
        public Camera(float azimuth, float polar, float distance, Vector3 target)
        {
            Azimuth = azimuth;
            Polar = polar;
            Distance = distance;
            Target = target;
        }
        private void SetAzimuth(float value)
        {
            azimuth = value;
            int fullcircles = (int)(Azimuth / MathF.Tau);
            if (azimuth < 0)
                fullcircles--;
            azimuth -= fullcircles * MathF.Tau;
        }
        private void SetPolar(float value)
        {
            polar = value;
            if (polar > MathF.PI - 0.01f)
                polar = MathF.PI - 0.01f;
            if (polar < 0.01f)
                polar = 0.01f;
        }
        private Vector3 GetPosition()
        {
            (float sinPolar, float cosPolar) = MathF.SinCos(Polar);
            (float sinAzim, float cosAzim) = MathF.SinCos(Azimuth);
            Vector3 position = default;
            position.Z = Distance * cosAzim * sinPolar;
            position.X = Distance * sinAzim * sinPolar;
            position.Y = Distance * cosPolar;
            return position;
        }
        /// <summary>
        /// Вращает вокруг цели по горизонтали
        /// </summary>
        /// <param name="angleRadians"></param>
        public void RotateAroundTargetHorizontal(float angleRadians)
        {
            Azimuth += angleRadians;
        }
        /// <summary>
        /// Вращает по вертикали на заданный
        /// угол в радианах положительное число - вниз, отрицательное - вверх
        /// </summary>
        /// <param name="angleRadians"> </param>
        public void RotateAroundTargetVertical(float angleRadians)
        {
            Polar += angleRadians;
        }
        public void MoveTowardTarget(float distance)
        {
            Distance -= distance;
        }
    }
}
