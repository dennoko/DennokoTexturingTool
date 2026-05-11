using System;
using System.Linq;
using Dennoko.TexturingTool.Runtime.Domain;
using UnityEngine;

namespace Dennoko.TexturingTool.Runtime.Application
{
    public sealed class TextureProcessingPipeline
    {
        private readonly IFbxLoader       _fbxLoader;
        private readonly IMeshCombiner    _meshCombiner;
        private readonly ILayerProcessor  _layerProcessor;
        private readonly ITextureExporter _textureExporter;
        private readonly IIdMaskGenerator _idMaskGenerator;

        public TextureProcessingPipeline(
            IFbxLoader       fbxLoader,
            IMeshCombiner    meshCombiner,
            ILayerProcessor  layerProcessor,
            ITextureExporter textureExporter,
            IIdMaskGenerator idMaskGenerator = null)
        {
            _fbxLoader       = fbxLoader;
            _meshCombiner    = meshCombiner;
            _layerProcessor  = layerProcessor;
            _textureExporter = textureExporter;
            _idMaskGenerator = idMaskGenerator;
        }

        public ProcessingResult Execute(ProcessingRequest request)
        {
            if (request.Config == null)
                throw new ArgumentNullException(nameof(request.Config), "Configuration is required.");

            var loaded   = _fbxLoader.Load(request.Config.fbxAsset);
            var combined = _meshCombiner.Combine(loaded, request.Config);

            var (idMasks, idMaskIndices) = GenerateIdMasks(loaded.Mesh, request.Config);

            var ctx    = new LayerProcessingContext(request.Config, idMasks, idMaskIndices);
            var output = _layerProcessor.Process(combined.BaseTexture, ctx);
            _textureExporter.Export(output, request.Config.exportRelativePath);

            return new ProcessingResult(combined.Mesh, output, request.Config.exportRelativePath);
        }

        private (Texture2D[] masks, int[] indices) GenerateIdMasks(Mesh mesh, Runtime.Data.TextureToolConfig config)
        {
            if (_idMaskGenerator == null || mesh == null) return (null, null);

            var selected = config.selectedSubMeshIndices?.Count > 0
                ? config.selectedSubMeshIndices.ToArray()
                : Enumerable.Range(0, mesh.subMeshCount).ToArray();

            var masks = _idMaskGenerator.Generate(mesh, selected, config.width, config.height);
            return (masks, selected);
        }
    }
}
