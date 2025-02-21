using System.Printing;

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
        }
        
        public static OUT[]? ReadFromBufferView<IN, OUT> (GltfAccessor accessor, Func<ArraySegment<byte>,int,IN> byteConverter, Func<IN[],OUT> outConverter)
        {
            if (accessor.BufferViewObject == null)
                return null;
            var bufferView = accessor.BufferViewObject;
            ArraySegment<byte> data = bufferView.Data;
            
            int startOffset = accessor.ByteOffset;
            int componentByteCount = accessor.Type.GetComponentCount();
            int componentCount = accessor.ComponentType.GetBytesCount();
            int byteStride = bufferView.ByteStride ?? componentByteCount * componentCount;
            OUT[] values = new OUT[accessor.Count];
            IN[] subBuffer = new IN[componentCount];
            for (int i = 0; i < accessor.Count; i++)
            {
                for(int j = 0; j < componentCount; j++)
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
    }
}
