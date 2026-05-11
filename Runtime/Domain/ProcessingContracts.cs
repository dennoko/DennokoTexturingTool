using System;
using UnityEngine;
using Dennoko.TexturingTool.Runtime.Data;

namespace Dennoko.TexturingTool.Runtime.Domain
{
    public readonly struct FbxLoadResult
    {
        public FbxLoadResult(Mesh mesh, Material[] materials) { Mesh = mesh; Materials = materials; }
        public Mesh Mesh { get; }
        public Material[] Materials { get; }
    }

    public readonly struct CombineResult
    {
        public CombineResult(Mesh mesh, Texture2D baseTexture) { Mesh = mesh; BaseTexture = baseTexture; }
        public Mesh Mesh { get; }
        public Texture2D BaseTexture { get; }
    }

    public readonly struct LayerProcessingContext
    {
        public LayerProcessingContext(TextureToolConfig config, Texture2D[] idMasks = null, int[] idMaskSubMeshIndices = null)
        {
            Config = config;
            IdMasks = idMasks;
            IdMaskSubMeshIndices = idMaskSubMeshIndices;
        }

        public TextureToolConfig Config { get; }
        public Texture2D[] IdMasks { get; }
        public int[] IdMaskSubMeshIndices { get; }
    }

    public readonly struct ProcessingRequest
    {
        public ProcessingRequest(TextureToolConfig config) => Config = config;
        public TextureToolConfig Config { get; }
    }

    public readonly struct ProcessingResult
    {
        public ProcessingResult(Mesh combinedMesh, Texture2D outputTexture, string exportPath)
        {
            CombinedMesh = combinedMesh;
            OutputTexture = outputTexture;
            ExportPath = exportPath;
        }

        public Mesh CombinedMesh { get; }
        public Texture2D OutputTexture { get; }
        public string ExportPath { get; }
    }

    public interface IFbxLoader
    {
        FbxLoadResult Load(GameObject fbxAsset);
    }

    public interface IMeshCombiner
    {
        CombineResult Combine(FbxLoadResult source, TextureToolConfig config);
    }

    public interface ILayerProcessor : IDisposable
    {
        Texture2D Process(Texture2D baseTexture, LayerProcessingContext context);
    }

    public interface ITextureExporter
    {
        void Export(Texture2D texture, string assetPath);
    }

    public interface IIdMaskGenerator
    {
        Texture2D[] Generate(Mesh mesh, int[] subMeshIndices, int width, int height);
    }
}
