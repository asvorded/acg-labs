namespace GraphicsLib.Types.GltfTypes
{
    public static class GltfUtils
    {


        public static int GetBytesCount(GltfComponentType componentType) => componentType switch
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
        public static void PreprocessGltfRoot(GltfRoot gltfRoot)
        {
            if (gltfRoot.BufferViews != null)
            {
                foreach (var bufferView in gltfRoot.BufferViews)
                {
                    bufferView.Data = new ArraySegment<byte>(gltfRoot.Buffers![bufferView.Buffer].Data, bufferView.ByteOffset, bufferView.ByteLength);
                }
            }
        }
        
        public static T ReadFromBufferView<T> (GltfBufferView bufferView, int byteStride, int index)
        {
            int byteOffset = index * byteStride;
            byte[] elementBytes = new byte[byteStride];
            Array.Copy(bytes, byteOffset, elementBytes, 0, byteStride); 
            elementBytes.Aggregate
            switch (componentType)
            {
                case GltfComponentType.FLOAT:
                    return BitConverter.ToSingle(elementBytes, 0);
                case GltfComponentType.USHORT:
                    return BitConverter.ToUInt16(elementBytes, 0);
                case GltfComponentType.UINT:
                    return BitConverter.ToUInt32(elementBytes, 0);
                case GltfComponentType.BYTE:
                    return elementBytes[0]; 
                default:
                    throw new ArgumentOutOfRangeException(nameof(componentType), $"Unsupported component type: {componentType}");
            }
        }

    }
}
