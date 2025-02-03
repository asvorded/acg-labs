using GraphicsLib.Types;
using System.Globalization;
using System.IO;
using System.Numerics;
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
            Obj obj = new Obj();
            using (StreamReader sr = new StreamReader(source))
            {
                sr.ReadToEnd().Split(Environment.NewLine).ToList().ForEach((line) =>
                    {
                        if (line.Length == 0)
                        {
                            return;
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
                                    for (int i = 1; i < parts.Length; i++)
                                    {
                                        faceParts = parts[i].Split('/');
                                        //vertices
                                        indices[i - 1, 0] = int.Parse(faceParts[0], NumberStyles.Any, CultureInfo.InvariantCulture);
                                        if (hasTextureIndices)
                                        {
                                            //textures
                                            indices[i - 1, 1] = int.Parse(faceParts[1], NumberStyles.Any, CultureInfo.InvariantCulture);

                                        }
                                        if (hasNormalIndices)
                                        {
                                            //normals
                                            indices[i - 1, 2] = int.Parse(faceParts[2], NumberStyles.Any, CultureInfo.InvariantCulture);
                                        }
                                    }
                                    //триангулируем
                                    for (int i = 1; i < (parts.Length - 1) - 1; i++)
                                    {
                                        int[] vertices = [indices[0, 0],
                                                          indices[i, 0],
                                                          indices[i + 1, 0]];
                                        int[]? textures = hasTextureIndices
                                                                ?  [indices[0, 1],
                                                                    indices[i, 1],
                                                                    indices[i + 1, 1]]
                                                                : null;
                                        int[]? normals = hasNormalIndices
                                                                ?  [indices[0, 1],
                                                                    indices[i, 1],
                                                                    indices[i + 1, 2]]
                                                                : null;
                                        Face newFace = new(vertices, textures, normals);
                                        obj.faces.Add(newFace);
                                    }
                                }
                                break;
                            case "#":
                            default:
                                //skip
                                break;
                        }
                    });
            }
            return obj;
        }
    }
}
