using System;
using System.Collections.Generic;
using Dennoko.TexturingTool.Runtime.Data;
using Dennoko.TexturingTool.Runtime.Domain;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Dennoko.TexturingTool.Runtime.Infrastructure
{
    public sealed class GpuLayerProcessor : ILayerProcessor
    {
        private readonly Material _blendMat;
        private readonly Material _adjustMat;
        private readonly Material _tilingMat;
        private readonly Material _dilationMat;
        private bool _disposed;

        public GpuLayerProcessor()
        {
            _blendMat   = LoadMaterial("Dennoko/LayerBlend");
            _adjustMat  = LoadMaterial("Dennoko/ColorAdjust");
            _tilingMat  = LoadMaterial("Dennoko/TilingOffset");
            _dilationMat = LoadMaterial("Dennoko/EdgeDilation");
        }

        private static Material LoadMaterial(string shaderName)
        {
            var shader = Shader.Find(shaderName)
                ?? throw new InvalidOperationException($"Shader '{shaderName}' not found.");
            return new Material(shader) { hideFlags = HideFlags.DontSave };
        }

        public Texture2D Process(Texture2D baseTexture, LayerProcessingContext ctx)
        {
            var config = ctx.Config;
            var desc   = new RenderTextureDescriptor(config.width, config.height, RenderTextureFormat.ARGB32, 0);
            var ping   = RenderTexture.GetTemporary(desc);
            var pong   = RenderTexture.GetTemporary(desc);
            try
            {
                BlitOrClear(baseTexture, ping);
                var src = ping;
                var dst = pong;

                foreach (var layer in config.layers ?? new List<LayerData>())
                {
                    if (!layer.enabled) continue;
                    ProcessLayer(layer, ctx, ref src, ref dst, desc);
                }

                foreach (var mod in config.globalModifiers ?? new List<ModifierData>())
                    ApplyModifier(mod, ref src, ref dst, desc);

                return ReadBack(src, config.width, config.height, config.colorSpace == ColorSpace.Linear);
            }
            finally
            {
                RenderTexture.ReleaseTemporary(ping);
                RenderTexture.ReleaseTemporary(pong);
            }
        }

        private void ProcessLayer(LayerData layer, LayerProcessingContext ctx,
            ref RenderTexture src, ref RenderTexture dst, RenderTextureDescriptor desc)
        {
            var layerRt = BuildLayerRt(layer, desc);
            var maskTex = FindIdMask(layer, ctx);

            _blendMat.SetTexture("_Base",    src);
            _blendMat.SetFloat  ("_Mode",    (float)layer.blendMode);
            _blendMat.SetFloat  ("_Opacity", layer.opacity);
            _blendMat.SetTexture("_Mask",    maskTex != null ? (Texture)maskTex : Texture2D.whiteTexture);
            _blendMat.SetFloat  ("_UseMask", maskTex != null ? 1f : 0f);
            Graphics.Blit(layerRt, dst, _blendMat);
            Swap(ref src, ref dst);
            RenderTexture.ReleaseTemporary(layerRt);

            foreach (var mod in layer.modifiers ?? new List<ModifierData>())
                ApplyModifier(mod, ref src, ref dst, desc);
        }

        private RenderTexture BuildLayerRt(LayerData layer, RenderTextureDescriptor desc)
        {
            var rt = RenderTexture.GetTemporary(desc);
            if (layer.type == LayerType.Color)
            {
                var tmp = new Texture2D(1, 1); tmp.SetPixel(0, 0, layer.color); tmp.Apply();
                Graphics.Blit(tmp, rt);
                Object.DestroyImmediate(tmp);
            }
            else if (layer.type == LayerType.Texture && layer.texture != null)
            {
                bool hasTiling = layer.tilingScale != Vector2.one || layer.tilingOffset != Vector2.zero;
                if (hasTiling)
                {
                    _tilingMat.SetVector("_TilingScale",  new Vector4(layer.tilingScale.x,  layer.tilingScale.y,  0, 0));
                    _tilingMat.SetVector("_TilingOffset", new Vector4(layer.tilingOffset.x, layer.tilingOffset.y, 0, 0));
                    Graphics.Blit(layer.texture, rt, _tilingMat);
                }
                else
                {
                    Graphics.Blit(layer.texture, rt);
                }
            }
            return rt;
        }

        private void ApplyModifier(ModifierData mod, ref RenderTexture src, ref RenderTexture dst, RenderTextureDescriptor desc)
        {
            if (mod.type == ModifierType.EdgeDilation)
            {
                _dilationMat.SetInt  ("_Radius",         mod.dilationRadius);
                _dilationMat.SetFloat("_AlphaThreshold", 0.01f);
                Graphics.Blit(src, dst, _dilationMat);
            }
            else
            {
                _adjustMat.SetFloat ("_Mode",        (float)mod.type);
                _adjustMat.SetColor ("_SrcColor",    mod.sourceColor);
                _adjustMat.SetColor ("_DstColor",    mod.targetColor);
                _adjustMat.SetFloat ("_Threshold",   mod.threshold);
                _adjustMat.SetFloat ("_Hue",         mod.hue);
                _adjustMat.SetFloat ("_Saturation",  mod.saturation);
                _adjustMat.SetFloat ("_Value",       mod.value);
                _adjustMat.SetFloat ("_InMin",       mod.inMin);
                _adjustMat.SetFloat ("_InMax",       mod.inMax);
                _adjustMat.SetFloat ("_OutMin",      mod.outMin);
                _adjustMat.SetFloat ("_OutMax",      mod.outMax);
                Graphics.Blit(src, dst, _adjustMat);
            }
            Swap(ref src, ref dst);
        }

        private static Texture2D FindIdMask(LayerData layer, LayerProcessingContext ctx)
        {
            if (!layer.useIdMask || ctx.IdMasks == null || ctx.IdMaskSubMeshIndices == null)
                return null;
            for (int i = 0; i < ctx.IdMaskSubMeshIndices.Length; i++)
                if (ctx.IdMaskSubMeshIndices[i] == layer.idMaskSubMeshIndex)
                    return ctx.IdMasks[i];
            return null;
        }

        private static void BlitOrClear(Texture2D src, RenderTexture dst)
        {
            if (src != null)
            {
                Graphics.Blit(src, dst);
                return;
            }
            var prev = RenderTexture.active;
            RenderTexture.active = dst;
            GL.Clear(true, true, Color.clear);
            RenderTexture.active = prev;
        }

        private static Texture2D ReadBack(RenderTexture rt, int w, int h, bool linear)
        {
            var prev = RenderTexture.active;
            RenderTexture.active = rt;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false, linear);
            tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            tex.Apply();
            RenderTexture.active = prev;
            return tex;
        }

        private static void Swap(ref RenderTexture a, ref RenderTexture b) => (a, b) = (b, a);

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            Object.DestroyImmediate(_blendMat);
            Object.DestroyImmediate(_adjustMat);
            Object.DestroyImmediate(_tilingMat);
            Object.DestroyImmediate(_dilationMat);
        }
    }
}
