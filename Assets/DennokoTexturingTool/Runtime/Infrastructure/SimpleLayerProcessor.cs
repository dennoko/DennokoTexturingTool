using Dennoko.TexturingTool.Runtime.Data;
using Dennoko.TexturingTool.Runtime.Domain;
using UnityEngine;

namespace Dennoko.TexturingTool.Runtime.Infrastructure
{
    public sealed class SimpleLayerProcessor : ILayerProcessor
    {
        public Texture2D Process(Texture2D baseTexture, TextureToolConfig config)
        {
            var output = CreateCanvasTexture(config, Color.clear);
            BlitToCanvas(baseTexture, output);

            var layers = config.layers ?? System.Array.Empty<LayerData>();
            foreach (var layer in layers)
            {
                if (!layer.enabled)
                {
                    continue;
                }

                var layerTexture = BuildLayerTexture(layer, config);
                ApplyBlend(output, layerTexture, layer.blendMode);
                ApplyModifiers(output, layer.modifiers);
            }

            ApplyModifiers(output, config.globalModifiers);
            output.Apply();
            return output;
        }

        private static Texture2D CreateCanvasTexture(TextureToolConfig config, Color fill)
        {
            var texture = new Texture2D(config.width, config.height, TextureFormat.RGBA32, false, config.colorSpace == ColorSpace.Linear);
            var pixels = new Color[config.width * config.height];
            for (var i = 0; i < pixels.Length; i++) pixels[i] = fill;
            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }

        private static Texture2D BuildLayerTexture(LayerData layer, TextureToolConfig config)
        {
            var texture = CreateCanvasTexture(config, layer.color);
            if (layer.type == LayerType.Texture && layer.texture != null)
            {
                BlitToCanvas(layer.texture, texture);
            }
            return texture;
        }

        private static void BlitToCanvas(Texture2D source, Texture2D destination)
        {
            if (source == null) return;
            for (var y = 0; y < destination.height; y++)
            {
                for (var x = 0; x < destination.width; x++)
                {
                    var u = destination.width == 1 ? 0f : x / (float)(destination.width - 1);
                    var v = destination.height == 1 ? 0f : y / (float)(destination.height - 1);
                    destination.SetPixel(x, y, source.GetPixelBilinear(u, v));
                }
            }
            destination.Apply();
        }

        private static void ApplyBlend(Texture2D baseTexture, Texture2D layerTexture, BlendMode mode)
        {
            for (var y = 0; y < baseTexture.height; y++)
            {
                for (var x = 0; x < baseTexture.width; x++)
                {
                    var baseColor = baseTexture.GetPixel(x, y);
                    var layerColor = layerTexture.GetPixel(x, y);
                    var mixed = mode switch
                    {
                        BlendMode.Add => baseColor + layerColor,
                        BlendMode.Multiply => baseColor * layerColor,
                        _ => Color.Lerp(baseColor, layerColor, layerColor.a)
                    };
                    baseTexture.SetPixel(x, y, ClampColor(mixed));
                }
            }
        }

        private static void ApplyModifiers(Texture2D texture, System.Collections.Generic.IReadOnlyList<ModifierData> modifiers)
        {
            if (modifiers == null)
            {
                return;
            }

            foreach (var modifier in modifiers)
            {
                if (modifier.type == ModifierType.ColorReplace)
                {
                    ApplyColorReplace(texture, modifier);
                }
            }
        }

        private static void ApplyColorReplace(Texture2D texture, ModifierData modifier)
        {
            for (var y = 0; y < texture.height; y++)
            {
                for (var x = 0; x < texture.width; x++)
                {
                    var current = texture.GetPixel(x, y);
                    var distance = Vector3.Distance(
                        new Vector3(current.r, current.g, current.b),
                        new Vector3(modifier.sourceColor.r, modifier.sourceColor.g, modifier.sourceColor.b));

                    if (distance <= modifier.threshold)
                    {
                        texture.SetPixel(x, y, modifier.targetColor);
                    }
                }
            }
        }

        private static Color ClampColor(Color color)
        {
            color.r = Mathf.Clamp01(color.r);
            color.g = Mathf.Clamp01(color.g);
            color.b = Mathf.Clamp01(color.b);
            color.a = Mathf.Clamp01(color.a);
            return color;
        }
    }
}
