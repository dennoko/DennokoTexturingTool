using System;
using System.Collections.Generic;
using UnityEngine;

namespace Dennoko.TexturingTool.Runtime.Data
{
    [CreateAssetMenu(menuName = "Dennoko/Texturing Tool Config", fileName = "TextureToolConfig")]
    public sealed class TextureToolConfig : ScriptableObject
    {
        [Header("Input")]
        public GameObject fbxAsset;
        public List<int> selectedSubMeshIndices = new();

        [Header("Canvas")]
        public int width = 1024;
        public int height = 1024;
        public ColorSpace colorSpace = ColorSpace.sRGB;

        [Header("Export")]
        public string exportRelativePath = "Assets/Generated/output.png";

        [Header("Layers")]
        public List<LayerData> layers = new();

        [Header("Global Modifiers")]
        public List<ModifierData> globalModifiers = new();
    }

    [Serializable]
    public sealed class LayerData
    {
        public string name = "Layer";
        public bool enabled = true;
        public LayerType type = LayerType.Texture;
        public Texture2D texture;
        public Color color = Color.white;
        public BlendMode blendMode = BlendMode.Normal;
        public List<ModifierData> modifiers = new();
    }

    [Serializable]
    public sealed class ModifierData
    {
        public ModifierType type = ModifierType.ColorReplace;
        public Color sourceColor = Color.white;
        public Color targetColor = Color.white;
        [Range(0.001f, 1f)] public float threshold = 0.1f;
    }

    public enum LayerType
    {
        Texture = 0,
        Color = 1
    }

    public enum BlendMode
    {
        Normal = 0,
        Add = 1,
        Multiply = 2
    }

    public enum ModifierType
    {
        ColorReplace = 0
    }
}
