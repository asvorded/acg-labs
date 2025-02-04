using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace GraphicsLib.Types
{
    public class Camera
    {
        private Vector3 _position;
        private Vector3 _target;
        private Matrix4x4 _viewMatrix;

        public Vector3 Position { get => _position; set => SetPosition(value); }
        public Vector3 Target { get => _target; set => SetTarget(value); }
        public Matrix4x4 ViewMatrix { get => _viewMatrix; }

        public Camera(Vector3 position, Vector3 lookAtPos)
        {
            Position = position;
            Target = lookAtPos;
        }
        private void SetPosition(Vector3 newPos)
        {
            _position = newPos;
            UpdateViewMatrix();
        }
        private void SetTarget(Vector3 newTarget)
        {
            _target = newTarget;
            UpdateViewMatrix();
        }
        private void UpdateViewMatrix()
        {
            _viewMatrix = Matrix4x4.CreateLookAt(_position, _target, Vector3.UnitY);
        }
        public void RotateAroundTargetHorizontal(float angleRadians)
        {
            Matrix4x4 rotation = Matrix4x4.CreateRotationY(angleRadians, Target);
            Position = Vector3.Transform(Position, rotation);
        }
        public void RotateAroundTargetVertical(float angleRadians)
        {
            Vector3 axis = Vector3.Cross(Position - Target, Vector3.UnitY);
            Quaternion rotation = Quaternion.CreateFromAxisAngle(axis, angleRadians);
            Position = Vector3.Transform(Position, rotation);
        }
    }
}
