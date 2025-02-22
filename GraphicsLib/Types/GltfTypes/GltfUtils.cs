using System.Configuration;
using System.Numerics;

namespace GraphicsLib.Types.GltfTypes
{
    public static class GltfUtils
    {
        public static void PreprocessGltfRoot(GltfRoot gltfRoot)
        {
            if (gltfRoot.BufferViews != null)
            {
                foreach (var bufferView in gltfRoot.BufferViews)
                {
                    bufferView.Data = new ArraySegment<byte>(gltfRoot.Buffers![bufferView.Buffer].Data, bufferView.ByteOffset, bufferView.ByteLength);
                }
                if (gltfRoot.Accessors != null)
                {
                    foreach (var accessor in gltfRoot.Accessors)
                    {
                        if (accessor.BufferView.HasValue)
                        {
                            accessor.BufferViewObject = gltfRoot.BufferViews[accessor.BufferView.Value];
                        }
                    }
                }
            }
            if (gltfRoot.Nodes != null)
            {
                foreach (var node in gltfRoot.Nodes)
                {
                    if (node.Children != null)
                    {
                        foreach (var index in node.Children)
                        {
                            gltfRoot.Nodes[index].Parent = node;
                        }
                    }
                }
            }
            if (gltfRoot.Meshes != null)
            {
                foreach (var mesh in gltfRoot.Meshes)
                {
                    if (mesh.Primitives != null)
                    {
                        foreach (var primitive in mesh.Primitives)
                            primitive.Root = gltfRoot;
                    }
                }
            }
        }

        private static object[] ReadFromBufferView(GltfAccessor accessor, Func<ArraySegment<byte>, int, object> byteConverter, Func<object[], object> outConverter)
        {
            if (accessor.BufferViewObject == null)
                throw new ConfigurationErrorsException("accessor has no buffer view object. Make sure to preprocess root");
            var bufferView = accessor.BufferViewObject;
            ArraySegment<byte> data = bufferView.Data;
            int startOffset = accessor.ByteOffset;
            int componentByteCount = accessor.ComponentType.GetBytesCount();
            int componentCount = accessor.Type.GetComponentCount();
            int byteStride = bufferView.ByteStride ?? componentByteCount * componentCount;
            object[] values = new object[accessor.Count];
            object[] subBuffer = new object[componentCount];

            for (int i = 0; i < accessor.Count; i++)
            {
                for (int j = 0; j < componentCount; j++)
                {
                    subBuffer[j] = byteConverter(data, startOffset + i * byteStride + j * componentByteCount);
                }
                values[i] = outConverter(subBuffer);
            }

            return values;
        }
        public static int GetComponentCount(this GltfAccessorType accessorType) => accessorType switch
        {
            GltfAccessorType.SCALAR => 1,
            GltfAccessorType.VEC2 => 2,
            GltfAccessorType.VEC3 => 3,
            GltfAccessorType.VEC4 => 4,
            GltfAccessorType.MAT2 => 4,
            GltfAccessorType.MAT3 => 9,
            GltfAccessorType.MAT4 => 16,
            _ => throw new ArgumentOutOfRangeException(nameof(accessorType), accessorType, null)
        };
        public static int GetBytesCount(this GltfComponentType componentType) => componentType switch
        {
            GltfComponentType.BYTE => 1,
            GltfComponentType.UNSIGNED_BYTE => 1,
            GltfComponentType.SHORT => 2,
            GltfComponentType.UNSIGNED_SHORT => 2,
            GltfComponentType.INT => 4,
            GltfComponentType.UNSIGNED_INT => 4,
            GltfComponentType.FLOAT => 4,
            _ => throw new ArgumentOutOfRangeException(nameof(componentType), componentType, null),
        };

        public static object[] GetAccessorData(GltfAccessor accessor)
        {
            Func<ArraySegment<byte>, int, object> byteConverter = ResolveByteConverter(accessor.ComponentType);
            Func<object[], object> outConverter = ResolveOutConverter(accessor.Type);
            return ReadFromBufferView(accessor, byteConverter, outConverter);
        }

        private static Func<object[], object> ResolveOutConverter(GltfAccessorType outType) => outType switch
        {
            GltfAccessorType.SCALAR => (components) => Convert.ToInt32(components[0]),
            GltfAccessorType.VEC2 => (components) => new Vector2(Convert.ToSingle(components[0]), Convert.ToSingle(components[1])),
            GltfAccessorType.VEC3 => (components) => new Vector3(Convert.ToSingle(components[0]), Convert.ToSingle(components[1]), Convert.ToSingle(components[2])),
            GltfAccessorType.VEC4 => (components) => new Vector4(Convert.ToSingle(components[0]), Convert.ToSingle(components[1]), Convert.ToSingle(components[2]), Convert.ToSingle(components[3])),
            GltfAccessorType.MAT4 => (components) => new Matrix4x4(
                Convert.ToSingle(components[0]), Convert.ToSingle(components[1]), Convert.ToSingle(components[2]), Convert.ToSingle(components[3]),
                Convert.ToSingle(components[4]), Convert.ToSingle(components[5]), Convert.ToSingle(components[6]), Convert.ToSingle(components[7]),
                Convert.ToSingle(components[8]), Convert.ToSingle(components[9]), Convert.ToSingle(components[10]), Convert.ToSingle(components[11]),
                Convert.ToSingle(components[12]), Convert.ToSingle(components[13]), Convert.ToSingle(components[14]), Convert.ToSingle(components[15])),
            _ => throw new ArgumentOutOfRangeException(nameof(outType), outType, null)
        };
        private static Func<ArraySegment<byte>, int, object> ResolveByteConverter(GltfComponentType inType) => inType switch
        {
            GltfComponentType.BYTE => (data, offset) => data.Array![data.Offset + offset],
            GltfComponentType.UNSIGNED_BYTE => (data, offset) => data.Array![data.Offset + offset],
            GltfComponentType.SHORT => (data, offset) => BitConverter.ToInt16(data.Array!, data.Offset + offset),
            GltfComponentType.UNSIGNED_SHORT => (data, offset) => BitConverter.ToUInt16(data.Array!, data.Offset + offset),
            GltfComponentType.INT => (data, offset) => BitConverter.ToInt32(data.Array!, data.Offset + offset),
            GltfComponentType.UNSIGNED_INT => (data, offset) => BitConverter.ToUInt32(data.Array!, data.Offset + offset),
            GltfComponentType.FLOAT => (data, offset) => BitConverter.ToSingle(data.Array!, data.Offset + offset),
            _ => throw new ArgumentOutOfRangeException(nameof(inType), inType, null),

        };
        public static Type GetComponentType(this GltfComponentType componentType) => componentType switch
        {
            GltfComponentType.BYTE => typeof(sbyte),
            GltfComponentType.UNSIGNED_BYTE => typeof(byte),
            GltfComponentType.SHORT => typeof(short),
            GltfComponentType.UNSIGNED_SHORT => typeof(ushort),
            GltfComponentType.INT => typeof(int),
            GltfComponentType.UNSIGNED_INT => typeof(uint),
            GltfComponentType.FLOAT => typeof(float),
            _ => throw new ArgumentOutOfRangeException(nameof(componentType), componentType, null)
        };
        public static Type GetAccessorType(this GltfAccessorType accessorType) => accessorType switch
        {
            GltfAccessorType.SCALAR => typeof(int),
            GltfAccessorType.VEC2 => typeof(Vector2),
            GltfAccessorType.VEC3 => typeof(Vector3),
            GltfAccessorType.VEC4 => typeof(Vector4),
            GltfAccessorType.MAT4 => typeof(Matrix4x4),
            _ => throw new ArgumentOutOfRangeException(nameof(accessorType), accessorType, null)
        };
    }
}
