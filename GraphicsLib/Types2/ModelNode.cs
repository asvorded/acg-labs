using System.Numerics;

namespace GraphicsLib.Types2
{
    public class ModelNode
    {
        private Quaternion rotation = Quaternion.Identity;
        private Vector3 scale = new(1);
        private Vector3 translation = default;
        private Matrix4x4 transformationMatrix = Matrix4x4.Identity;
        private Matrix4x4 normalTransformationMatrix = Matrix4x4.Identity;

        public ModelNode[]? ChildNodes {  get; set; }
        public ModelMesh? Mesh { get; set; }
        public Quaternion Rotation { get => rotation; set { rotation = value; UpdateMatrix(); } }
        public Vector3 Scale { get => scale; set { scale = value; UpdateMatrix(); } }
        public Vector3 Translation { get => translation; set { translation = value; UpdateMatrix(); } }
        public Matrix4x4 TransformationMatrix { get => transformationMatrix; set { transformationMatrix = value; UpdateTransformationComponentsFromMatrix(); } }
        public Matrix4x4 NormalTransformationMatrix { get => normalTransformationMatrix; set { normalTransformationMatrix = value; UpdateTransformationComponentsFromNormalMatrix(); } }
        private void UpdateMatrix()
        {
            var transform = Matrix4x4.Identity;
            transform *= Matrix4x4.CreateScale(scale);
            transform *= Matrix4x4.CreateFromQuaternion(rotation);
            transform *= Matrix4x4.CreateTranslation(translation);
            TransformationMatrix = transform;
            Matrix4x4.Invert(transform, out Matrix4x4 normalTransform);
            normalTransformationMatrix = Matrix4x4.Transpose(normalTransform);
        }
        private void UpdateTransformationComponentsFromMatrix()
        {
            Matrix4x4.Decompose(transformationMatrix, out scale, out rotation, out translation);
            Matrix4x4.Invert(transformationMatrix, out Matrix4x4 normalTransform);
            normalTransformationMatrix = Matrix4x4.Transpose(normalTransform);
        }
        private void UpdateTransformationComponentsFromNormalMatrix()
        {

            Matrix4x4.Invert(Matrix4x4.Transpose(normalTransformationMatrix), out transformationMatrix);
            Matrix4x4.Decompose(transformationMatrix, out scale, out rotation, out translation);
        }
    }
}