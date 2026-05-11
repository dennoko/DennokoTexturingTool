using System.Collections.Generic;
using Dennoko.TexturingTool.Runtime.Data;
using Dennoko.TexturingTool.Runtime.Domain;
using UnityEngine;

namespace Dennoko.TexturingTool.Runtime.Infrastructure
{
    // CPU fallback — prototype only. Replace GPU shaders are the production path.
    public sealed class SimpleLayerProcessor : ILayerProcessor
    {
        public Texture2D Process(Texture2D baseTexture, LayerProcessingContext ctx)
        {
            var config = ctx.Config;
            var output = CreateCanvas(config, Color.clear);
            BlitToCanvas(baseTexture, output);

            foreach (var layer in config.layers ?? new List<LayerData>())
            {
                if (!layer.enabled) continue;
                var layerTex = BuildLayerTexture(layer, config);
                ApplyBlend(output, layerTex, layer.blendMode, layer.opacity);
                ApplyModifiers(output, layer.modifiers);
            }

            ApplyModifiers(output, config.globalModifiers);
            output.Apply();
            return output;
        }

        private static Texture2D CreateCanvas(TextureToolConfig config, Color fill)
        {
            var tex    = new Texture2D(config.width, config.height, TextureFormat.RGBA32, false,
                                        config.colorSpace == ColorSpace.Linear);
            var pixels = new Color[config.width * config.height];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = fill;
            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }

        private static Texture2D BuildLayerTexture(LayerData layer, TextureToolConfig config)
        {
            var tex = CreateCanvas(config, layer.color);
            if (layer.type == LayerType.Texture && layer.texture != null)
                BlitToCanvas(layer.texture, tex);
            return tex;
        }

        private static void BlitToCanvas(Texture2D src, Texture2D dst)
        {
            if (src == null) return;
            var pixels = new Color[dst.width * dst.height];
            for (int y = 0; y < dst.height; y++)
            for (int x = 0; x < dst.width;  x++)
            {
                float u = dst.width  == 1 ? 0f : x / (float)(dst.width  - 1);
                float v = dst.height == 1 ? 0f : y / (float)(dst.height - 1);
                pixels[y * dst.width + x] = src.GetPixelBilinear(u, v);
            }
            dst.SetPixels(pixels);
            dst.Apply();
        }

        private static void ApplyBlend(Texture2D baseTexture, Texture2D layerTexture, BlendMode mode, float opacity)
        {
            var basePixels  = baseTexture.GetPixels();
            var layerPixels = layerTexture.GetPixels();

            for (int i = 0; i < basePixels.Length; i++)
            {
                var b = basePixels[i];
                var l = layerPixels[i];
                var mixed = mode switch
                {
                    BlendMode.Add      => Clamp(b + l * opacity),
                    BlendMode.Multiply => new Color(Mathf.Lerp(b.r, b.r*l.r, opacity),
                                                    Mathf.Lerp(b.g, b.g*l.g, opacity),
                                                    Mathf.Lerp(b.b, b.b*l.b, opacity), b.a),
                    BlendMode.Overlay  => Overlay(b, l, opacity),
                    BlendMode.Screen   => Screen(b, l, opacity),
                    _                  => Color.Lerp(b, l, l.a * opacity)
                };
                basePixels[i] = mixed;
            }
            baseTexture.SetPixels(basePixels);
        }

        private static Color Overlay(Color b, Color l, float op)
        {
            float R = b.r < 0.5f ? 2*b.r*l.r : 1-2*(1-b.r)*(1-l.r);
            float G = b.g < 0.5f ? 2*b.g*l.g : 1-2*(1-b.g)*(1-l.g);
            float B = b.b < 0.5f ? 2*b.b*l.b : 1-2*(1-b.b)*(1-l.b);
            return Clamp(new Color(Mathf.Lerp(b.r,R,op), Mathf.Lerp(b.g,G,op), Mathf.Lerp(b.b,B,op), b.a));
        }

        private static Color Screen(Color b, Color l, float op)
        {
            float R = 1-(1-b.r)*(1-l.r);
            float G = 1-(1-b.g)*(1-l.g);
            float B = 1-(1-b.b)*(1-l.b);
            return Clamp(new Color(Mathf.Lerp(b.r,R,op), Mathf.Lerp(b.g,G,op), Mathf.Lerp(b.b,B,op), b.a));
        }

        private static void ApplyModifiers(Texture2D texture, IReadOnlyList<ModifierData> modifiers)
        {
            if (modifiers == null) return;
            foreach (var m in modifiers)
            {
                if (m.type == ModifierType.ColorReplace) ApplyColorReplace(texture, m);
            }
        }

        private static void ApplyColorReplace(Texture2D texture, ModifierData m)
        {
            var pixels = texture.GetPixels();
            float sq   = m.threshold * m.threshold;
            for (int i = 0; i < pixels.Length; i++)
            {
                var c  = pixels[i];
                float dr = c.r-m.sourceColor.r, dg = c.g-m.sourceColor.g, db = c.b-m.sourceColor.b;
                if (dr*dr + dg*dg + db*db <= sq)
                    pixels[i] = new Color(m.targetColor.r, m.targetColor.g, m.targetColor.b, c.a);
            }
            texture.SetPixels(pixels);
        }

        private static Color Clamp(Color c)
            => new Color(Mathf.Clamp01(c.r), Mathf.Clamp01(c.g), Mathf.Clamp01(c.b), Mathf.Clamp01(c.a));

        public void Dispose() { }
    }
}
