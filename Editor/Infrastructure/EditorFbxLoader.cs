using System;
using Dennoko.TexturingTool.Runtime.Domain;
using UnityEngine;

namespace Dennoko.TexturingTool.Editor.Infrastructure
{
    public sealed class EditorFbxLoader : IFbxLoader
    {
        public FbxLoadResult Load(GameObject fbxAsset)
        {
            if (fbxAsset == null)
            {
                throw new ArgumentNullException(nameof(fbxAsset), "FBX asset is required.");
            }

            var renderer = fbxAsset.GetComponentInChildren<SkinnedMeshRenderer>();
            if (renderer != null)
            {
                return new FbxLoadResult(renderer.sharedMesh, renderer.sharedMaterials);
            }

            var meshRenderer = fbxAsset.GetComponentInChildren<MeshRenderer>();
            var meshFilter = fbxAsset.GetComponentInChildren<MeshFilter>();
            if (meshRenderer != null && meshFilter != null)
            {
                return new FbxLoadResult(meshFilter.sharedMesh, meshRenderer.sharedMaterials);
            }

            throw new InvalidOperationException("No mesh renderer found in FBX asset.");
        }
    }
}
