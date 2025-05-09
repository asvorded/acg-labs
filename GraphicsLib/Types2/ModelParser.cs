using GraphicsLib.Types.GltfTypes;
using GraphicsLib.Types.JsonConverters;
using GraphicsLib.Types;
using Newtonsoft.Json;
using System.IO;
using System.Windows;
using System.Xml.Linq;
using System.Linq;
using System;
using System.Numerics;

namespace GraphicsLib.Types2
{
    public static class ModelParser
    {
        public static ModelScene ParseGltfFile(string filePath)
        {
            using FileStream fileStream = new(filePath, FileMode.Open);
            using StreamReader sr = new(fileStream);
            string data = sr.ReadToEnd();
            string sourceDirectory = Path.GetDirectoryName(filePath)!;
            GltfRoot gltfRoot = JsonConvert.DeserializeObject<GltfRoot>(data, GltfSerializerSettings.GetSettings(sourceDirectory))
                    ?? throw new FormatException("invalid json gltf");
            if (gltfRoot.ExtensionsRequired != null && gltfRoot.ExtensionsRequired.Count != 0)
            {
                MessageBox.Show($"Model requires extensions : {gltfRoot.ExtensionsRequired.Aggregate((a, b) => a + ", " + b)}.");
            }
            Material[] materialsList;
            if (gltfRoot.Materials != null)
            {
                materialsList = new Material[gltfRoot.Materials.Count];
                Parallel.For(0, gltfRoot.Materials.Count, i =>
                    materialsList[i] = Material.FromGltfMaterial(gltfRoot.Materials[i])
                );
            }
            else
            {
                materialsList = [Material.defaultMaterial];
            }
            if (gltfRoot.Scenes != null)
            {
                ModelScene scene = new();
                GltfScene gltfScene = gltfRoot.Scenes[0];
                if (gltfScene.Nodes != null)
                {
                    scene.RootModelNodes = new ModelNode[gltfScene.Nodes.Length];
                    for(int i = 0; i < gltfScene.Nodes.Length; i++)
                    {
                        scene.RootModelNodes[i] = ParseNode(gltfRoot.Nodes![gltfScene.Nodes[i]], materialsList).Result;
                    }

                }
                return scene;
            }
            return new ModelScene();
        }

        private static async Task<ModelMesh> ParseMesh(GltfMesh gltfMesh, Material[] materialsList)
        {
            var primitives = new ModelPrimitive[gltfMesh.Primitives.Length];
            Parallel.For(0, gltfMesh.Primitives.Length, i =>
            {
                var p = gltfMesh.Primitives[i];
                var attributes = p.Attributes.Select<KeyValuePair<string, int>, KeyValuePair<string, GltfAccessor>>(a => new(a.Key, p.Root!.Accessors![a.Value])).ToDictionary();
                Dictionary<string, short> attributesOffsets = [];
                List<float[]> floatData = [];
                foreach (var attribute in attributes)
                {
                    floatData.Add(GltfUtils.GetAccessorData<float>(attribute.Value));
                    attributesOffsets.Add(attribute.Key, (short)(floatData.Count - 1));
                }
                primitives[i] = new ModelPrimitive()
                {

                    Indices = p.PointIndices,
                    Material = p.Material.HasValue ? materialsList[p.Material.Value] : Material.defaultMaterial,
                    Mode = p.Mode,
                    AttributesOffsets = attributesOffsets,
                    FloatData = [.. floatData],
                    BoundingBox = GltfUtils.GetBoundingBox(attributes, floatData[attributesOffsets["POSITION"]])
                };
            });
            var mesh = new ModelMesh()
            {
                Primitives = primitives,
                BoundingBox = primitives.Select(p => p.BoundingBox).Aggregate((a, b) =>
                    new(Vector3.Min(a!.Value.Min, b!.Value.Min), Vector3.Max(a.Value.Max, b.Value.Max)))
            };
            return mesh;
        }
        private static async Task<ModelNode> ParseNode(GltfNode gltfNode, Material[] materialsList)
        {
            var childrenTasks = gltfNode.ChildrenNodes?
            .Select(child => ParseNode(child, materialsList))
                .ToArray() ?? [];
            //bullshit
#pragma warning disable CS8619 // Допустимость значения NULL для ссылочных типов в значении не соответствует целевому типу.
            Task<ModelMesh?> meshTask = gltfNode.Mesh != null ?
                ParseMesh(gltfNode.Mesh, materialsList)
                : Task.FromResult<ModelMesh?>(null);
#pragma warning restore CS8619 // Допустимость значения NULL для ссылочных типов в значении не соответствует целевому типу.

            ModelMesh? mesh = await meshTask;
            ModelNode[]? childrenNodes = await Task.WhenAll(childrenTasks);
            if(childrenNodes.Length == 0)
            {
                childrenNodes = null;
            }
            ModelNode modelNode = new()
            {
                ChildNodes = childrenNodes,
                Mesh = mesh,
            };
            if (gltfNode.Matrix.HasValue)
            {
                modelNode.TransformationMatrix = gltfNode.Matrix.Value;
            }
            else
            {
                if (gltfNode.Translation.HasValue)
                {
                    modelNode.Translation = gltfNode.Translation.Value;
                }
                if (gltfNode.Rotation.HasValue)
                {
                    modelNode.Rotation = gltfNode.Rotation.Value;
                }
                if (gltfNode.Scale.HasValue)
                {
                    modelNode.Scale = gltfNode.Scale.Value;
                }
            }
            return modelNode;
        }
    }
}
