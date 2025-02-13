using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace GraphicsLib.Types {
    public struct ObjTransformation {

        public float AngleX;
        public float AngleY;
        public float AngleZ;
        public Vector3 Offset;
        public Matrix4x4 Matrix { get => GetTransformationMatrix(); }

        public Matrix4x4 GetTransformationMatrix() {
            Matrix4x4 matrix;
            Matrix4x4 offsetMatrix = new Matrix4x4(
                1, 0, 0, 0,
                0, 1, 0, 0,
                0, 0, 1, 0,
                Offset.X, Offset.Y, Offset.Z, 1
            );
            matrix = offsetMatrix;
            return matrix;
        }

        public void Reset() {
            AngleX = 0;
            AngleY = 0;
            AngleZ = 0;
            Offset = new Vector3();
        }
    }
}
