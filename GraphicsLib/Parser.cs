using GraphicsLib.Types;
using System.Globalization;
using System.IO;
using System.Numerics;
using System.Text.Json.Serialization;
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Data;
using System.Buffers.Text;
using System.Text;
namespace GraphicsLib
{
    public class Parser
    {
        /// <summary>
        /// Загружает модель из текста формата Wavefront Obj
        /// </summary>
        /// <param name="source">Входной текст</param>
        /// <returns>Модель</returns>
        public static Obj ParseObjFile(Stream source)
        {
            Obj obj = new();
            using StreamReader sr = new(source);
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
                            newVertex.X = Single.Parse(parts[1], NumberStyles.Any, CultureInfo.InvariantCulture);
                            newVertex.Y = Single.Parse(parts[2], NumberStyles.Any, CultureInfo.InvariantCulture);
                            newVertex.Z = Single.Parse(parts[3], NumberStyles.Any, CultureInfo.InvariantCulture);
                            if (parts.Length == 5)
                            {
                                //Нормализуем
                                Single w = Single.Parse(parts[4], NumberStyles.Any, CultureInfo.InvariantCulture);
                                newVertex /= w;
                            }
                            obj.vertices.Add(newVertex);
                        }
                        break;
                    case "vn":
                        {
                            Vector3 newNormal;
                            newNormal.X = Single.Parse(parts[1], NumberStyles.Any, CultureInfo.InvariantCulture);
                            newNormal.Y = Single.Parse(parts[2], NumberStyles.Any, CultureInfo.InvariantCulture);
                            newNormal.Z = Single.Parse(parts[3], NumberStyles.Any, CultureInfo.InvariantCulture);
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
                                vertices[i - 1] = int.Parse(faceParts[0], NumberStyles.Any, CultureInfo.InvariantCulture) - 1;
                                if (textures!=null)
                                {
                                    textures[i - 1] = int.Parse(faceParts[1], NumberStyles.Any, CultureInfo.InvariantCulture) - 1;
                                }
                                if (normals != null)
                                {
                                    normals[i - 1] = int.Parse(faceParts[2], NumberStyles.Any, CultureInfo.InvariantCulture) - 1;
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
            return obj;
        }
        public static byte[] GetBufferData(String uri, String sourceFileDirectory)
        {
            const String octetStreamMime = "data:application/octet-stream;";
            const String base64EncodingText = "base64,";
            if (Path.Exists(String.Join(Path.DirectorySeparatorChar, sourceFileDirectory,uri)))
            {
                return File.ReadAllBytes(String.Join(Path.DirectorySeparatorChar, sourceFileDirectory, uri));
            }
            if(uri.StartsWith(octetStreamMime, StringComparison.InvariantCulture))
            {
                int dataIndex = octetStreamMime.Length;
                if (uri.IndexOf(base64EncodingText, dataIndex, base64EncodingText.Length) != -1)
                {
                    byte[] bytes = new byte[uri.Length * sizeof(char)];
                    int bytesConsumed = 0;
                    int bytesWritten = 0;
                    Base64.DecodeFromUtf8(UTF8Encoding.UTF8.GetBytes(uri.Substring(dataIndex + base64EncodingText.Length)),
                                            bytes,out bytesConsumed, out bytesWritten); 
                    byte[] result = new byte[bytesWritten];
                    for (int i = 0; i < bytesWritten; i++)
                    {
                        result[i] = bytes[i];
                    }
                    return result;
                }
            }
            throw new Exception("failed to retreive data");
        }
        public static Obj ParseGltfFile(Stream source, String sourceFileDirectory)
        {
            try
            {
                Obj obj = new();
                using StreamReader sr = new(source);
                String data = sr.ReadToEnd();
                dynamic gltfData = JsonConvert.DeserializeObject<dynamic>(data);
                List<byte[]> buffers = [];
                if (gltfData == null)
                    throw new FormatException("invalid json file");
                foreach(var buffer in gltfData.buffers)
                {
                    String uri = buffer.uri;
                    buffers.Add(GetBufferData(uri, sourceFileDirectory));
                }    
                return obj;
            }
            catch (Exception ex)
            {
                throw;
            }

        }

    }
}
