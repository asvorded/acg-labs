using GraphicsLib.Types.GltfTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace GraphicsLib.Types2
{
    public class ModelAnimation
    {
        public required float[] Timestamps { get; set; }
        public required float[] Outputs { get; set; }
        public required GltfInterpolationType InterpolationType { get; set; }
        public required GltfAnimationPathType PathType { get; set; }

        public static ModelAnimation FromGltfAnimation(GltfAnimationChannel gltfAnimation)
        {
            return new ModelAnimation()
            {
                InterpolationType = gltfAnimation.GltfAnimationSampler.Interpolation,
                PathType = gltfAnimation.Target.Path,
                Timestamps = gltfAnimation.GltfAnimationSampler.GetInput(),
                Outputs = gltfAnimation.GltfAnimationSampler.GetOutput(),
            };
        }

        public Matrix4x4 Apply(Matrix4x4 currentTransformation, float timeElapsed)
        {
            if (timeElapsed > Timestamps[^1])
            {
                timeElapsed -= ((int)(timeElapsed / Timestamps[^1])) * Timestamps[^1];
            }
            int tIndex = Array.BinarySearch(Timestamps, timeElapsed);
            if (tIndex < 0)
            {
                tIndex = (int)(~(ulong)tIndex);
            }
            if (tIndex < 0)
            {
                tIndex = 0;
            }
            if(tIndex > Timestamps.Length - 1)
            {
                tIndex = Timestamps.Length - 1;
            }
            Matrix4x4.Decompose(currentTransformation, out Vector3 scale, out Quaternion rotation, out Vector3 translation);
            switch (PathType)
            {
                case GltfAnimationPathType.TRANSLATION:
                    {
                        return Matrix4x4.CreateScale(scale)
                            * Matrix4x4.CreateFromQuaternion(rotation)
                            * GetTranslationMatrix(timeElapsed, tIndex);
                    }
                case GltfAnimationPathType.ROTATION:
                    {
                        return Matrix4x4.CreateScale(scale)
                            * GetRotationMatrix(timeElapsed, tIndex)
                            * Matrix4x4.CreateTranslation(translation);
                    }
                case GltfAnimationPathType.SCALE:
                    {
                        return GetScaleMatrix(timeElapsed, tIndex)
                            * Matrix4x4.CreateFromQuaternion(rotation)
                            * Matrix4x4.CreateTranslation(translation);
                    }

                case GltfAnimationPathType.WEIGHTS:
                    return currentTransformation;
                default:
                    return currentTransformation;
            }

        }
        private Matrix4x4 GetTranslationMatrix(float timeElapsed, int tIndex)
        {
            if (tIndex == 0)
            {
                return Matrix4x4.CreateTranslation(new Vector3(Outputs[0], Outputs[1], Outputs[2]));
            }

            float t0 = Timestamps[tIndex - 1];
            float t1 = Timestamps[tIndex];
            float t = (timeElapsed - t0) / (t1 - t0);

            Vector3 p1 = new Vector3(Outputs[tIndex * 3], Outputs[tIndex * 3 + 1], Outputs[tIndex * 3 + 2]);
            Vector3 p0 = new Vector3(Outputs[(tIndex - 1) * 3], Outputs[(tIndex - 1) * 3 + 1], Outputs[(tIndex - 1) * 3 + 2]);
            return InterpolationType switch
            {
                GltfInterpolationType.LINEAR => Matrix4x4.CreateTranslation(Vector3.Lerp(p0, p1, t)),
                GltfInterpolationType.STEP => Matrix4x4.CreateTranslation(p0),
                GltfInterpolationType.CUBICSPLINE => Matrix4x4.Identity,
                _ => Matrix4x4.Identity,
            };
        }
        private Matrix4x4 GetRotationMatrix(float timeElapsed, int tIndex)
        {
            if (tIndex == 0)
            {
                return Matrix4x4.CreateFromQuaternion(new Quaternion(Outputs[0], Outputs[1], Outputs[2], Outputs[3]));
            }

            float t0 = Timestamps[tIndex - 1];
            float t1 = Timestamps[tIndex];
            float t = (timeElapsed - t0) / (t1 - t0);
            Quaternion q1 = new Quaternion(Outputs[tIndex * 4], Outputs[tIndex * 4 + 1], Outputs[tIndex * 4 + 2], Outputs[tIndex * 4 + 3]);
            Quaternion q0 = new Quaternion(Outputs[(tIndex - 1) * 4], Outputs[(tIndex - 1) * 4 + 1], Outputs[(tIndex - 1) * 4 + 2], Outputs[(tIndex - 1) * 4 + 3]);
            return InterpolationType switch
            {
                GltfInterpolationType.LINEAR => Matrix4x4.CreateFromQuaternion(Quaternion.Slerp(q0, q1, t)),
                GltfInterpolationType.STEP => Matrix4x4.CreateFromQuaternion(q0),
                GltfInterpolationType.CUBICSPLINE => Matrix4x4.Identity,
                _ => Matrix4x4.Identity,
            };
        }
        private Matrix4x4 GetScaleMatrix(float timeElapsed, int tIndex)
        {
            if (tIndex == 0)
            {
                return Matrix4x4.CreateScale(new Vector3(Outputs[0], Outputs[1], Outputs[2]));
            }

            float t0 = Timestamps[tIndex - 1];
            float t1 = Timestamps[tIndex];
            float t = (timeElapsed - t0) / (t1 - t0);

            Vector3 s1 = new Vector3(Outputs[tIndex * 3], Outputs[tIndex * 3 + 1], Outputs[tIndex * 3 + 2]);
            Vector3 s0 = new Vector3(Outputs[(tIndex - 1) * 3], Outputs[(tIndex - 1) * 3 + 1], Outputs[(tIndex - 1) * 3 + 2]);
            return InterpolationType switch
            {
                GltfInterpolationType.LINEAR => Matrix4x4.CreateScale(Vector3.Lerp(s0, s1, t)),
                GltfInterpolationType.STEP => Matrix4x4.CreateScale(s0),
                GltfInterpolationType.CUBICSPLINE => Matrix4x4.Identity,
                _ => Matrix4x4.Identity,
            };
        }
    }
}
