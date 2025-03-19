using GraphicsLib.Primitives;
using GraphicsLib.Types;
using GraphicsLib.Types.GltfTypes;
using GraphicsLib.Types.JsonConverters;
using Newtonsoft.Json;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.PixelFormats;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Numerics;
using System.Windows.Media.Imaging;
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
            List<Vector3> verticesList = [];
            List<Vector3> normalsList = [];
            List<Vector2> uvsList = [];
            List<Face> facesList = [];
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
                            verticesList.Add(newVertex);
                        }
                        break;
                    case "vt":
                        {
                            Vector2 newTextureCoord;
                            newTextureCoord.X = Single.Parse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture);
                            newTextureCoord.Y = Single.Parse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture);
                            uvsList.Add(newTextureCoord);
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
                            normalsList.Add(newNormal);
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
                                Face newFace = new(triangleVertices, triangleTextures, triangleNormals, 0);
                                facesList.Add(newFace);
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
                        p = verticesList.Count + p + 1;
                    face.vIndices[i] = p;
                    if(face.nIndices!= null)
                    {
                        int n = face.nIndices[i];
                        if (n < 0)
                            n = normalsList.Count + n + 1;
                        face.nIndices[i] = n;
                    }
                    if (face.tIndices != null)
                    {
                        int t = face.tIndices[i];
                        if (t < 0)
                            t = uvsList.Count + t + 1;
                        face.tIndices[i] = t;
                    }
                }
            }
            StaticTriangle[] staticTriangles = new StaticTriangle[facesList.Count];
            for (int i = 0; i < facesList.Count; i++)
            {
                Face face = facesList[i];
                staticTriangles[i] = new StaticTriangle()
                {
                    position0 = verticesList[face.vIndices[0]],
                    position1 = verticesList[face.vIndices[1]],
                    position2 = verticesList[face.vIndices[2]],
                };
                if (face.nIndices != null)
                {
                    staticTriangles[i].normal0 = normalsList[face.nIndices[0]];
                    staticTriangles[i].normal1 = normalsList[face.nIndices[1]];
                    staticTriangles[i].normal2 = normalsList[face.nIndices[2]];
                }
                if (face.tIndices != null)
                {
                    staticTriangles[i].uvCoordinate0 = uvsList[face.tIndices[0]];
                    staticTriangles[i].uvCoordinate1 = uvsList[face.tIndices[1]];
                    staticTriangles[i].uvCoordinate2 = uvsList[face.tIndices[2]];
                }
            }
            obj.triangles = [.. staticTriangles];
            obj.faces = [.. facesList];
            obj.vertices = [.. verticesList];
            obj.normals = [.. normalsList];
            obj.uvs = [.. uvsList];
            return obj;
        }
        public static Obj ParseGltfFile(string filePath)
        {
            Obj obj = new();
            using FileStream fileStream = new(filePath, FileMode.Open);
            using StreamReader sr = new(fileStream);
            string data = sr.ReadToEnd();
            string sourceDirectory = Path.GetDirectoryName(filePath)!;
            using GltfRoot gltfRoot = JsonConvert.DeserializeObject<GltfRoot>(data, GltfSerializerSettings.GetSettings(sourceDirectory))
                    ?? throw new FormatException("invalid json gltf");
            List<Vector3> verticesList = [];
            List<Vector3> normalsList = [];
            List<Vector2> uvsList = [];
            List<Face> facesList = [];
            List<Material> materialsList = [];
            if (gltfRoot.Materials != null)
            {
                foreach (var material in gltfRoot.Materials)
                {
                    Material newMaterial = new();
                    if (material.Name != null)
                        newMaterial.name = material.Name;
                    if (material.PbrMetallicRoughness != null)
                    {
                        var pbr = material.PbrMetallicRoughness;
                        newMaterial.metallic = pbr.MetallicFactor;
                        newMaterial.roughness = pbr.RoughnessFactor;
                        newMaterial.baseColor = pbr.BaseColorFactor;
                        if (pbr.BaseColorTexture != null)
                        {

                            var textureInfo = pbr.BaseColorTexture;
                            var texture = gltfRoot.Textures![textureInfo.Index];
                            var samplerSettings = gltfRoot.Samplers![texture.Sampler];
                            var image = gltfRoot.Images![texture.Source];
                            var textureImage = image.Texture;
                            var sampler = samplerSettings.GetSampler();
                            Rgba32[] pixels = new Rgba32[textureImage.Height*textureImage.Width];
                            textureImage.CopyPixelDataTo(pixels);
                            sampler.BindTexture(pixels, textureImage.Width, textureImage.Height);
                            newMaterial.baseColorTextureSampler = sampler;
                        }
                        if (pbr.MetallicRoughnessTexture != null)
                        {
                            //var texture = pbr.MetallicRoughnessTexture;
                            //var image = gltfRoot.Images![texture.Index];
                            //newMaterial.metallicRoughnessTexture = Path.Combine(sourceDirectory, image.UriString);
                        }
                    }
                    materialsList.Add(newMaterial);
                }
            }
            else
            {
                materialsList.Add(Material.defaultMaterial);
            }
            if (gltfRoot.Nodes != null)
            {
                foreach (var node in gltfRoot.Nodes)
                {
                    Matrix4x4 transform = node.GlobalTransform;
                    Matrix4x4 normalTransform = node.GlobalNormalTransform;
                    //Matrix4x4 normalTransform = node.GlobalTransform;
                    if (node.Mesh.HasValue)
                    {
                        var mesh = gltfRoot.Meshes![node.Mesh.Value];
                        foreach (var primitive in mesh.Primitives)
                        {
                            var mode = primitive.Mode;
                            var attributes = primitive.Attributes;
                            int vIndexOffset = verticesList.Count;
                            int nIndexOffset = normalsList.Count;
                            int tIndexOffset = uvsList.Count;
                            Vector3[]? vertices = primitive.Position;
                            Vector3[]? normals = primitive.Normal;
                            Vector2[]? uvs = primitive.GetTextureCoords(0);
                            int[]? indicies = primitive.PointIndices;
                            if (vertices != null)
                            {
                                foreach (var v in vertices)
                                {
                                    Vector3 transformed = Vector3.Transform(v, transform);
                                    verticesList.Add(transformed);
                                }
                            }
                            if (normals != null)
                            {

                                foreach (var n in normals)
                                {
                                    Vector3 transformed = Vector3.Normalize(Vector3.TransformNormal(n, normalTransform));
                                    normalsList.Add(transformed);
                                }
                            }
                            if (uvs != null)
                            {
                                foreach (var uv in uvs)
                                {
                                    uvsList.Add(uv);
                                }
                            }
                            if (indicies != null)
                            {
                                Face AssembleTriangle(int index0, int index1, int index2)
                                {
                                    short materialIndex = (primitive.Material.HasValue) ? (short)primitive.Material.Value : (short)0;
                                    int[] triangleVertices = [indicies[index0] + vIndexOffset,
                                            indicies[index1] + vIndexOffset,
                                            indicies[index2] + vIndexOffset];
                                    int[]? triangleNormals = null;
                                    if (normalsList.Count > nIndexOffset)
                                        triangleNormals = [indicies[index0] + nIndexOffset,
                                            indicies[index1] + nIndexOffset,
                                            indicies[index2] + nIndexOffset];
                                    int[]? triangleUvs = null;
                                    if (uvsList.Count > tIndexOffset)
                                        triangleUvs = [indicies[index0] + tIndexOffset,
                                            indicies[index1] + tIndexOffset,
                                            indicies[index2] + tIndexOffset];
                                    return new Face(triangleVertices, triangleUvs, triangleNormals, materialIndex);
                                }

                                if (mode == GltfMeshMode.TRIANGLES)
                                {
                                    for (int i = 0; i < indicies.Length; i += 3)
                                    {
                                        int index0 = i;
                                        int index1 = i + 1;
                                        int index2 = i + 2;
                                        Face newFace = AssembleTriangle(index0, index1, index2);
                                        facesList.Add(newFace);
                                    }
                                }
                                else if (mode == GltfMeshMode.TRIANGLE_STRIP)
                                {
                                    for (int i = 0; i < indicies.Length - 2; i++)
                                    {
                                        int index0 = i;
                                        int index1 = i + 1;
                                        int index2 = i + 2;
                                        Face newFace = AssembleTriangle(index0, index1, index2);
                                        facesList.Add(newFace);
                                    }
                                }
                                else if (mode == GltfMeshMode.TRIANGLE_FAN)
                                {
                                    for (int i = 0; i < indicies.Length - 2; i++)
                                    {
                                        int index0 = 0;
                                        int index1 = i + 1;
                                        int index2 = i + 2;
                                        Face newFace = AssembleTriangle(index0, index1, index2);
                                        facesList.Add(newFace);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            StaticTriangle[] staticTriangles = new StaticTriangle[facesList.Count];
            for (int i = 0; i < facesList.Count; i++)
            {
                Face face = facesList[i];
                staticTriangles[i] = new StaticTriangle()
                {
                    position0 = verticesList[face.vIndices[0]],
                    position1 = verticesList[face.vIndices[1]],
                    position2 = verticesList[face.vIndices[2]],
                };
                if(face.nIndices != null)
                {
                    staticTriangles[i].normal0 = normalsList[face.nIndices[0]];
                    staticTriangles[i].normal1 = normalsList[face.nIndices[1]];
                    staticTriangles[i].normal2 = normalsList[face.nIndices[2]];
                }
                if(face.tIndices != null)
                {
                    staticTriangles[i].uvCoordinate0 = uvsList[face.tIndices[0]];
                    staticTriangles[i].uvCoordinate1 = uvsList[face.tIndices[1]];
                    staticTriangles[i].uvCoordinate2 = uvsList[face.tIndices[2]];
                }
                staticTriangles[i].material = materialsList[face.MaterialIndex];
            }
            obj.triangles = [.. staticTriangles];
            obj.materials = [.. materialsList];
            obj.faces = [.. facesList];
            obj.vertices = [.. verticesList];
            obj.normals = [.. normalsList];
            obj.uvs = [.. uvsList];
            return obj;
        }
    
    }
}


