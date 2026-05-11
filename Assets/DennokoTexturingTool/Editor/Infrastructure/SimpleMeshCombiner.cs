using System;
using System.Collections.Generic;
using Dennoko.TexturingTool.Runtime.Data;
using Dennoko.TexturingTool.Runtime.Domain;
using UnityEngine;

namespace Dennoko.TexturingTool.Editor.Infrastructure
{
    public sealed class SimpleMeshCombiner : IMeshCombiner
    {
        public CombineResult Combine(FbxLoadResult source, TextureToolConfig config)
        {
            if (source.Mesh == null)
            {
                throw new ArgumentException("Source mesh is missing.");
            }

            var selected = config.selectedSubMeshIndices.Count == 0
                ? BuildDefaultSubMeshList(source.Mesh.subMeshCount)
                : config.selectedSubMeshIndices;

            var combine = new List<CombineInstance>(selected.Count);
            foreach (var subMeshIndex in selected)
            {
                if (subMeshIndex < 0 || subMeshIndex >= source.Mesh.subMeshCount)
                {
                    continue;
                }

                combine.Add(new CombineInstance
                {
                    mesh = source.Mesh,
                    subMeshIndex = subMeshIndex,
                    transform = Matrix4x4.identity
                });
            }

            var outputMesh = new Mesh { name = $"{source.Mesh.name}_Combined" };
            outputMesh.CombineMeshes(combine.ToArray(), true, false, false);

            var baseTexture = FindBaseTexture(source.Materials, selected);
            return new CombineResult(outputMesh, baseTexture);
        }

        private static List<int> BuildDefaultSubMeshList(int subMeshCount)
        {
            var all = new List<int>(subMeshCount);
            for (var i = 0; i < subMeshCount; i++) all.Add(i);
            return all;
        }

        private static Texture2D FindBaseTexture(IReadOnlyList<Material> materials, IReadOnlyList<int> selected)
        {
            foreach (var index in selected)
            {
                if (index < 0 || index >= materials.Count || materials[index] == null)
                {
                    continue;
                }

                if (materials[index].HasProperty("_MainTex"))
                {
                    var texture = materials[index].GetTexture("_MainTex") as Texture2D;
                    if (texture != null)
                    {
                        return texture;
                    }
                }
            }

            return null;
        }
    }
}
