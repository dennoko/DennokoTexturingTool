using System;
using Dennoko.TexturingTool.Runtime.Domain;
using UnityEngine;

namespace Dennoko.TexturingTool.Runtime.Application
{
    public sealed class TextureProcessingPipeline
    {
        private readonly IFbxLoader _fbxLoader;
        private readonly IMeshCombiner _meshCombiner;
        private readonly ILayerProcessor _layerProcessor;
        private readonly ITextureExporter _textureExporter;

        public TextureProcessingPipeline(
            IFbxLoader fbxLoader,
            IMeshCombiner meshCombiner,
            ILayerProcessor layerProcessor,
            ITextureExporter textureExporter)
        {
            _fbxLoader = fbxLoader;
            _meshCombiner = meshCombiner;
            _layerProcessor = layerProcessor;
            _textureExporter = textureExporter;
        }

        public ProcessingResult Execute(ProcessingRequest request)
        {
            if (request.Config == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            var loaded = _fbxLoader.Load(request.Config.fbxAsset);
            var combined = _meshCombiner.Combine(loaded, request.Config);
            var output = _layerProcessor.Process(combined.BaseTexture, request.Config);
            _textureExporter.Export(output, request.Config.exportRelativePath);
            return new ProcessingResult(combined.Mesh, output, request.Config.exportRelativePath);
        }
    }
}
