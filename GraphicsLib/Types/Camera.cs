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
        private Vector3 _lookAtPos;
        private Matrix4x4 _viewMatrix;

        public Vector3 Position { get => _position; set => SetPosition(value); }
        public Vector3 LookAtPos { get => _lookAtPos; set => _lookAtPos = value; }
        public Matrix4x4 ViewMatrix { get => _viewMatrix; private set => _viewMatrix = value; }

        public Camera(Vector3 position, Vector3 lookAtPos)
        {
            Position = position;
            LookAtPos = lookAtPos;
        }
        private void SetPosition(Vector3 newPos)
        {
            _position = newPos;
        }
        private void UpdateViewMatrix()
        {
            _viewMatrix = Matrix4x4.CreateLookAt(_position,_lookAtPos,)
        }
    }
}
