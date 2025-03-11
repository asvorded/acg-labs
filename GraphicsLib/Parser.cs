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
                            for(int i = 0; i < vertices.Length - 2; i++)
                            {
                                int[] triangleVertices = [vertices[0], vertices[i + 1], vertices[i + 2]];
                                int[]? triangleTextures = hasTextureIndices ? [textures![0], textures[i + 1], textures[i + 2]] : null;
                                int[]? triangleNormals = hasNormalIndices ? [normals![0], normals[i + 1], normals[i + 2]] : null;
                                Face newFace = new(triangleVertices, triangleTextures, triangleNormals);
                                obj.faces.Add(newFace);
                            }

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
                    if(face.nIndices!= null)
                    {
                        int n = face.nIndices[i];
                        if (n < 0)
                            n = obj.normals.Count + n + 1;
                        face.nIndices[i] = n;
                    }
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
                    Matrix4x4 normalTransform = node.GlobalNormalTransform;
                    if (node.Mesh.HasValue)
                    {
                        var mesh = gltfRoot.Meshes![node.Mesh.Value];
                        foreach (var primitive in mesh.Primitives)
                        {
                            var mode = primitive.Mode;
                            var attributes = primitive.Attributes;
                            int vIndexOffset = obj.vertices.Count;
                            int vNormalIndex = obj.normals.Count;
                            Vector3[]? vertices = primitive.Position;
                            Vector3[]? normals = primitive.Normal;
                            if (vertices != null)
                            {
                                foreach (var v in vertices)
                                {
                                    Vector3 transformed = Vector3.Transform(v, transform);
                                    obj.vertices.Add(transformed);
                                }
                            }
                            if(normals != null)
                            {
                                
                                foreach (var n in normals)
                                {
                                    Vector3 transformed = Vector3.Normalize(Vector3.TransformNormal(n, normalTransform));
                                    obj.normals.Add(transformed);
                                }
                            }
                            int[]? indicies = primitive.PointIndices;
                            if (indicies != null)
                            {
                                if (mode == GltfMeshMode.TRIANGLES)
                                {
                                    for (int i = 0; i < indicies.Length; i += 3)
                                    {
                                        int[] triangleVertices = [indicies[i] + vIndexOffset, indicies[i + 1] + vIndexOffset, indicies[i + 2] + vIndexOffset];
                                        int[]? triangleNormals = null;
                                        if (obj.normals.Count > vNormalIndex)
                                            triangleNormals = [indicies[i] + vNormalIndex, indicies[i + 1] + vNormalIndex, indicies[i + 2] + vNormalIndex];
                                        Face newFace = new(triangleVertices, null, triangleNormals);
                                        obj.faces.Add(newFace);
                                    }
                                }
                                else if (mode == GltfMeshMode.TRIANGLE_STRIP)
                                {
                                    for (int i = 0; i < indicies.Length - 2; i++)
                                    {
                                        int[] triangleVertices = [indicies[i] + vIndexOffset, indicies[i + 1] + vIndexOffset, indicies[i + 2] + vIndexOffset];
                                        int[]? triangleNormals = null;
                                        if(obj.normals.Count > vNormalIndex)
                                            triangleNormals = [indicies[i] + vNormalIndex, indicies[i + 1] + vNormalIndex, indicies[i + 2] + vNormalIndex];
                                        Face newFace = new(triangleVertices, null, triangleNormals);
                                        obj.faces.Add(newFace);
                                    }
                                }
                                else if (mode == GltfMeshMode.TRIANGLE_FAN)
                                {
                                    for (int i = 0; i < indicies.Length - 2; i++)
                                    {
                                        int[] triangleVertices = [indicies[0] + vIndexOffset, indicies[i + 1] + vIndexOffset, indicies[i + 2] + vIndexOffset];
                                        int[]? triangleNormals = null;
                                        if (obj.normals.Count > vNormalIndex)
                                            triangleNormals = [indicies[0] + vNormalIndex, indicies[i + 1] + vNormalIndex, indicies[i + 2] + vNormalIndex];
                                        Face newFace = new(triangleVertices, null, triangleNormals);
                                        obj.faces.Add(newFace);
                                    }
                                }
                            }
                        }
                    }
                }

            return obj;
        }
    }
}


