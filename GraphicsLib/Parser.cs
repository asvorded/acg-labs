using GraphicsLib.Types;
using GraphicsLib.Types.GltfTypes;
using GraphicsLib.Types.JsonConverters;
using Newtonsoft.Json;
using System.Globalization;
using System.IO;
using System.Numerics;
namespace GraphicsLib
{
    public static class Parser
    {

        static readonly int GL_INT = 5124;
        static readonly int GL_UNSIGNED_INT = 5125;
        static readonly int GL_FLOAT = 5126;
        static readonly int GL_INT = 5124;
        static readonly int GL_UNSIGNED_INT = 5125;
        static readonly int GL_FLOAT = 5126;
        static readonly int GL_INT = 5124;
        static readonly int GL_UNSIGNED_INT = 5125;
        static readonly int GL_FLOAT = 5126;
        static readonly int GL_INT = 5124;
        static readonly int GL_UNSIGNED_INT = 5125;
        static readonly int GL_FLOAT = 5126;
        /// <summary>
        /// Загружает модель из текста формата Wavefront Obj
        /// </summary>
        /// <param name="source">Входной текст</param>
        /// <returns>Модель</returns>
        public static Obj ParseObjFile(string filePath)
        {
            Obj obj = new();
            using FileStream fileStream = new(filePath, FileMode.Open);
            using StreamReader sr = new(fileStream);
            while (!sr.EndOfStream)
            {
                string? line = sr.ReadLine();
                if (line == null || line.Length == 0)
                {
                    continue;
                }
                string[] parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                switch (parts[0])
                {
                    case "v":
                        {
                            Vector3 newVertex;
                            newVertex.X = Single.Parse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture);
                            newVertex.Y = Single.Parse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture);
                            newVertex.Z = Single.Parse(parts[3], NumberStyles.Float, CultureInfo.InvariantCulture);
                            if (parts.Length == 5)
                            {
                                //Нормализуем
                                Single w = Single.Parse(parts[4], NumberStyles.Float, CultureInfo.InvariantCulture);
                                newVertex /= w;
                            }
                            obj.vertices.Add(newVertex);
                        }
                        break;
                    case "vn":
                        {
                            Vector3 newNormal;
                            newNormal.X = Single.Parse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture);
                            newNormal.Y = Single.Parse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture);
                            newNormal.Z = Single.Parse(parts[3], NumberStyles.Float, CultureInfo.InvariantCulture);
                            //нормаль может быть не нормализована. нормализуем
                            newNormal = Vector3.Normalize(newNormal);
                            obj.normals.Add(newNormal);
                        }
                        break;
                    case "f":
                        {
                            int[,] indices = new int[parts.Length - 1, 3];
                            string[] faceParts = parts[1].Split('/');
                            bool hasTextureIndices = faceParts.Length > 1 && faceParts[1].Length > 0;
                            bool hasNormalIndices = faceParts.Length > 2 && faceParts[2].Length > 0;
                            int[] vertices = new int[(parts.Length - 1)];
                            int[]? textures = hasTextureIndices ? new int[(parts.Length - 1)] : null;
                            int[]? normals = hasNormalIndices ? new int[(parts.Length - 1)] : null;
                            for (int i = 1; i < parts.Length; i++)
                            {
                                faceParts = parts[i].Split('/');
                                vertices[i - 1] = int.Parse(faceParts[0], NumberStyles.Integer, CultureInfo.InvariantCulture) - 1;
                                if (textures != null)
                                {
                                    textures[i - 1] = int.Parse(faceParts[1], NumberStyles.Integer, CultureInfo.InvariantCulture) - 1;
                                }
                                if (normals != null)
                                {
                                    normals[i - 1] = int.Parse(faceParts[2], NumberStyles.Integer, CultureInfo.InvariantCulture) - 1;
                                }
                            }
                            Face newFace = new(vertices, textures, normals);
                            obj.faces.Add(newFace);
                        }
                        break;
                    case "#":
                    default:
                        //skip
                        break;
                }
            }
            foreach (var face in obj.faces)
            {
                for (int i = 0; i < face.vIndices.Length; i++)
                {
                    int p = face.vIndices[i];
                    if (p < 0)
                        p = obj.vertices.Count + p + 1;
                    face.vIndices[i] = p;
                }
            }
            return obj;
        }
        public static Obj ParseGltfFile(string filePath)
        {
            Obj obj = new();
            using FileStream fileStream = new(filePath, FileMode.Open);
            using StreamReader sr = new(fileStream);
            string data = sr.ReadToEnd();
            string sourceDirectory = Path.GetDirectoryName(filePath)!;
            GltfRoot gltfRoot = JsonConvert.DeserializeObject<GltfRoot>(data, GltfSerializerSettings.GetSettings(sourceDirectory))
                ?? throw new FormatException("invalid json gltf");
            if (gltfRoot.Nodes != null)
                foreach (var node in gltfRoot.Nodes)
                {
                    Matrix4x4 transform = node.GlobalTransform;
                    if (node.Mesh.HasValue)
                    {
                        var mesh = gltfRoot.Meshes![node.Mesh.Value];
                        foreach (var primitive in mesh.Primitives)
                        {
                            var mode = primitive.Mode;
                            var attributes = primitive.Attributes;
                            int vIndexOffset = obj.vertices.Count;
                            Vector3[]? vertices = primitive.Position;
                            if (vertices != null)
                            {
                                foreach (var v in vertices)
                                {
                                    Vector3 transformed = Vector3.Transform(v, transform);
                                    obj.vertices.Add(transformed);
                                }
                            }
                            int[]? indicies = primitive.PointIndices;
                            if (indicies != null)
                            {
                                if(mode == GltfMeshMode.TRIANGLES)
                                {
                                    for (int i = 0; i < indicies.Length; i += 3)
                                    {
                                        Face newFace = new([indicies[i] + vIndexOffset, indicies[i + 1] + vIndexOffset, indicies[i + 2] + vIndexOffset], null, null);
                                        obj.faces.Add(newFace);
                                    }
                                }else if (mode == GltfMeshMode.TRIANGLE_STRIP)
                                {
                                    for (int i = 0; i < indicies.Length - 2; i++)
                                    {
                                        Face newFace = new([indicies[i] + vIndexOffset, indicies[i + 1] + vIndexOffset, indicies[i + 2] + vIndexOffset], null, null);
                                        obj.faces.Add(newFace);
                                    }
                                }
                            else if (mode == GltfMeshMode.TRIANGLE_FAN)
                            {
                                for (int i = 0; i < indicies.Length - 2; i++)
                                {
                                    Face newFace = new([indicies[0] + vIndexOffset, indicies[i + 1] + vIndexOffset, indicies[i + 2] + vIndexOffset], null, null);
                                    obj.faces.Add(newFace);
                                }
                            }
                        }
                            /*if (attributes.ContainsKey("POSITION"))
                            {
                                var accessor = gltfRoot.Accessors![attributes["POSITION"]];
                                AddVectors(finalTransform, obj.vertices, accessor);
                            }
                            if (primitive.attributes.NORMAL != null)
                            {
                                var accessor = accessors[(int)primitive.attributes.NORMAL];
                                AddVectors(finalTransform, obj.normals, accessor);
                            }
                            int mode = primitive.mode ?? GL_TRIANGLES;
                            if (mode == GL_TRIANGLES || mode == GL_TRIANGLE_STRIP || mode == GL_TRIANGLE_FAN)
                            {
                                var accessor = accessors[(int)primitive.indices];
                                AddFaces(obj.faces, accessor, mode, vIndexOffset);
                                continue;
                            }*/
                            //throw new NotImplementedException($"wtf is this primitive: {mode}");
                        }
                    }
                }

            return obj;
        }
        /*
        private static void AddVectors(Matrix4x4 transform, List<Vector3> vectors, GltfRoot gltfRoot, int accessorIndex)
        {
            var accessor = gltfRoot.Accessors![accessorIndex];
            if (!accessor.BufferView.HasValue)
            {
            int offset = accessor.byteOffset ?? 0;
            int count = accessor.count;
            var bufferView = accessor.bufferView;
            int byteLength = bufferView.byteLength;
            if (!(byteLength >= offset + sizeof(Single) * count))
            {
                throw new Exception("bufferView is not long enough");
            }
            offset += (int)bufferView.byteOffset;
            var buffer = bufferView.buffer;
            for (int i = 0; i < count; i++)
            {
                Vector3 v = new(BitConverter.ToSingle(buffer, offset),
                                BitConverter.ToSingle(buffer, offset + sizeof(Single)),
                                BitConverter.ToSingle(buffer, offset + 2 * sizeof(Single)));
                vectors.Add(Vector3.Transform(v, transform));
                offset += 3 * sizeof(Single);
            }
        }
        dynamic gltfData = JsonConvert.DeserializeObject<dynamic>(data)
            ?? throw new FormatException("invalid json gltf");
        JsonConvert.DeserializeObject<dynamic>(data, )
        List<byte[]> buffers = [];
        List<dynamic> bufferViews = [];
        List<dynamic> accessors = [];
        List<dynamic> meshes = [];
        Dictionary<int, Matrix4x4> parentTransforms = [];
        //retreive binary data from buffer URI
        foreach (var buffer in gltfData.buffers)
        {
            String uri = buffer.uri;
            buffers.Add(GetBufferData(uri, sourceFileDirectory));
        }
        //get bufferViews with pointers to buffers
        foreach (var bufferView in gltfData.bufferViews)
        {
            bufferViews.Add(new
            {
                buffer = buffers[(int)bufferView.buffer],
                bufferView.byteLength,
                byteOffset = bufferView.byteOffset ?? 0,
                bufferView.target
            });
        }
        //get accessors with pointers to bufferViews
        foreach (var accessor in gltfData.accessors)
        {
            accessors.Add(new
            {
                bufferView = bufferViews[(int)accessor.bufferView],
                accessor.componentType,
                accessor.count,
                accessor.type,
                accessor.byteOffset
            });
        }
        foreach (var mesh in gltfData.meshes)
        {
            meshes.Add(new
            {
                mesh.primitives,
                mesh.name
            });
        }
        int nodeIndex = 0;
        foreach (var node in gltfData.nodes)
        {
            Matrix4x4 parentTransform = default;
            if (!parentTransforms.TryGetValue(nodeIndex, out parentTransform))
            {
                parentTransform = Matrix4x4.Identity;
            }
            Matrix4x4 transform = Matrix4x4.Identity;
            if (node.matrix != null)
            {
                transform = new Matrix4x4((Single)node.matrix[0], (Single)node.matrix[1], (Single)node.matrix[2],
                    (Single)node.matrix[3], (Single)node.matrix[4], (Single)node.matrix[5], (Single)node.matrix[6],
                    (Single)node.matrix[7], (Single)node.matrix[8], (Single)node.matrix[9], (Single)node.matrix[10],
                    (Single)node.matrix[11], (Single)node.matrix[12], (Single)node.matrix[13], (Single)node.matrix[14],
                    (Single)node.matrix[15]);
            }
            if (node.scale != null)
            {
                Vector3 scale = new((Single)(node.scale[0]),
                    (Single)(node.scale[1]),
                    (Single)(node.scale[2]));
                transform *= Matrix4x4.CreateScale(scale);
            }
            if (node.rotation != null)
            {
                Quaternion rotation = new((Single)(node.rotation[0]),
                                         (Single)(node.rotation[1]),
                                         (Single)(node.rotation[2]),
                                         (Single)(node.rotation[3]));
                transform *= Matrix4x4.CreateFromQuaternion(rotation);
            }
            if (node.translation != null)
            {
                Vector3 translation = new((Single)node.translation[0],
                                       (Single)(node.translation[1]),
                                        (Single)(node.translation[2]));
                transform *= Matrix4x4.CreateTranslation(translation);
            }
            Matrix4x4 finalTransform = transform * parentTransform;
            if (node.children != null)
            {
                foreach (var child in node.children)
                {
                    parentTransforms.Add((int)child, finalTransform);
                }
            }
            if (node.mesh != null)
            {
                var mesh = meshes[(int)node.mesh];
                foreach (var primitive in mesh.primitives)
                {
                    int vIndexOffset = obj.vertices.Count;
                    if (primitive.attributes.POSITION != null)
                    {
                        var accessor = accessors[(int)primitive.attributes.POSITION];
                        AddVectors(finalTransform, obj.vertices, accessor);
                    }
                    if (primitive.attributes.NORMAL != null)
                    {
                        var accessor = accessors[(int)primitive.attributes.NORMAL];
                        AddVectors(finalTransform, obj.normals, accessor);
                    }
                    int mode = primitive.mode ?? GL_TRIANGLES;
                    if (mode == GL_TRIANGLES || mode == GL_TRIANGLE_STRIP || mode == GL_TRIANGLE_FAN)
                    {
                        var accessor = accessors[(int)primitive.indices];
                        AddFaces(obj.faces, accessor, mode, vIndexOffset);
                        continue;
                    }
                    throw new NotImplementedException($"wtf is this primitive: {mode}");
                }
            }
            nodeIndex++;
        }
        return obj;
    }*/
        /*private static object ParseGlScalarFromBytes(Type type, byte[] bytes, int offset)
            return obj;
        }
        private static object ParseGlScalarFromBytes(Type type, byte[] bytes, int offset)
        return obj;
    }*/
        /*private static object ParseGlScalarFromBytes(Type type, byte[] bytes, int offset)
            return obj;
        }
        private static object ParseGlScalarFromBytes(Type type, byte[] bytes, int offset)
            return obj;
        }
        private static object ParseGlScalarFromBytes(Type type, byte[] bytes, int offset)
        {
            if (type == typeof(UInt16))
                return BitConverter.ToUInt16(bytes, offset);
            if (type == typeof(UInt32))
                return BitConverter.ToUInt32(bytes, offset);
            if (type == typeof(Int32))
                return BitConverter.ToInt32(bytes, offset);
            if (type == typeof(Single))
                return BitConverter.ToSingle(bytes, offset);
            throw new Exception($"parse unsupported type{type.FullName}");
        }
        private static Type? ResolveGlType(int componentType)
        {
            if (componentType == GL_UNSIGNED_SHORT)
                return typeof(UInt16);
            if (componentType == GL_UNSIGNED_INT)
                return typeof(UInt32);
            if (componentType == GL_INT)
                return typeof(Int32);
            if (componentType == GL_FLOAT)
                return typeof(Single);
            return null;
        }
        private static void AddFaces(List<Face> faces, dynamic accessor, int mode, int vIndexOffset)
        {
            string type = accessor.type;
            if (!type.Equals(GL_SCALAR_TYPE))
            {
                throw new Exception("accessor type missmatch");
            }
            int offset = accessor.byteOffset ?? 0;
            int count = accessor.count;
            int componentType = accessor.componentType;
            Type scalarType = ResolveGlType(componentType) ?? throw new Exception($"unresolved type {componentType}");
            int scalarTypeSize = Marshal.SizeOf(scalarType);
            var bufferView = accessor.bufferView;
            int byteLength = bufferView.byteLength;
            if (!(byteLength >= offset + scalarTypeSize * count))
            {
                throw new Exception("bufferView is not long enough");
            }
            offset += (int)bufferView.byteOffset;
            var buffer = bufferView.buffer;
            if (mode == PrimitiveModes.TRIANGLES)
            {
                for (int i = 0; i < count; i += 3)
                {
                    int[] vIndices = [ (int)ParseGlScalarFromBytes(scalarType, buffer, offset) + vIndexOffset,
                                       (int)ParseGlScalarFromBytes(scalarType, buffer, offset + 1 * scalarTypeSize) + vIndexOffset,
                                       (int)ParseGlScalarFromBytes(scalarType, buffer, offset + 2 * scalarTypeSize) + vIndexOffset ];
                    Face face = new(vIndices, null, null);
                    faces.Add(face);
                    offset += 3 * scalarTypeSize;
                }
            }
            else if (mode == PrimitiveModes.TRIANGLE_STRIP)//HACK {1 2 3}  drawing order is (2 1 3) to maintain proper winding
            {
                bool switchPoints = false;
                for (int i = 2; i < count; i++)
                {
                    int[] vIndices = [ (int)ParseGlScalarFromBytes(scalarType, buffer, offset) + vIndexOffset,
                                       (int)ParseGlScalarFromBytes(scalarType, buffer, offset + 1 * scalarTypeSize) + vIndexOffset,
                                       (int)ParseGlScalarFromBytes(scalarType, buffer, offset + 2 * scalarTypeSize) + vIndexOffset ];
                    if(switchPoints)
                        (vIndices[0], vIndices[1]) = (vIndices[1],  vIndices[0]);
                    switchPoints = !switchPoints;
                    Face face = new(vIndices, null, null);
                    faces.Add(face);
                    offset += scalarTypeSize;
                }
            }
            else if (mode == PrimitiveModes.TRIANGLE_FAN)
            {
                int startIndex = (int)ParseGlScalarFromBytes(scalarType, buffer, offset) + vIndexOffset;
                for (int i = 2; i < count; i++)
                {
                    int[] vIndices = [ startIndex,
                                       (int)ParseGlScalarFromBytes(scalarType, buffer, offset + 1 * scalarTypeSize) + vIndexOffset,
                                       (int)ParseGlScalarFromBytes(scalarType, buffer, offset + 2 * scalarTypeSize) + vIndexOffset ];
                    Face face = new(vIndices, null, null);
                    faces.Add(face);
                    offset += scalarTypeSize;
                }
            }
        }
        private static void AddVectors(Matrix4x4 transform, List<Vector3> vectors, dynamic accessor)
        {
            string type = accessor.type;
            if (!type.Equals(GL_VEC3_TYPE))
            {
                throw new Exception("accessor type missmatch");
            }
            int offset = accessor.byteOffset ?? 0;
            int count = accessor.count;
            var bufferView = accessor.bufferView;
            int byteLength = bufferView.byteLength;
            if (!(byteLength >= offset + sizeof(Single) * count))
            {
                throw new Exception("bufferView is not long enough");
            }
            offset += (int)bufferView.byteOffset;
            var buffer = bufferView.buffer;
            for (int i = 0; i < count; i++)
            {
                Vector3 v = new(BitConverter.ToSingle(buffer, offset),
                                BitConverter.ToSingle(buffer, offset + sizeof(Single)),
                                BitConverter.ToSingle(buffer, offset + 2 * sizeof(Single)));
                vectors.Add(Vector3.Transform(v, transform));
                offset += 3 * sizeof(Single);
            }
        }*/
    }
}
