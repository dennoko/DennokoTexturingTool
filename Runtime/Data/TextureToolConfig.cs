using System;
using System.Collections.Generic;
using UnityEngine;

namespace Dennoko.TexturingTool.Runtime.Data
{
    [CreateAssetMenu(menuName = "dennokoworks/Texturing Tool Config", fileName = "TextureToolConfig")]
    public sealed class TextureToolConfig : ScriptableObject
    {
        [Header("Input")]
        public GameObject fbxAsset;
        public List<int> selectedSubMeshIndices = new();

        [Header("Canvas")]
        public int width = 1024;
        public int height = 1024;
        public ColorSpace colorSpace = ColorSpace.Gamma;

        [Header("Export")]
        public string exportRelativePath = "Assets/Generated/output.png";

        [Header("Layers")]
        public List<LayerData> layers = new();

        [Header("Global Modifiers")]
        public List<ModifierData> globalModifiers = new();

        [HideInInspector]
        public List<string> snapshots = new();
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
        [Range(0f, 1f)] public float opacity = 1f;
        public Vector2 tilingScale = Vector2.one;
        public Vector2 tilingOffset = Vector2.zero;
        public bool useIdMask;
        public int idMaskSubMeshIndex;
        public List<ModifierData> modifiers = new();
    }

    [Serializable]
    public sealed class ModifierData
    {
        public ModifierType type = ModifierType.ColorReplace;
        // ColorReplace
        public Color sourceColor = Color.white;
        public Color targetColor = Color.white;
        [Range(0.001f, 1f)] public float threshold = 0.1f;
        // HSV
        [Range(-0.5f, 0.5f)] public float hue;
        [Range(-1f, 1f)] public float saturation;
        [Range(-1f, 1f)] public float value;
        // Levels
        [Range(0f, 1f)] public float inMin;
        [Range(0f, 1f)] public float inMax = 1f;
        [Range(0f, 1f)] public float outMin;
        [Range(0f, 1f)] public float outMax = 1f;
        // EdgeDilation
        [Range(1, 32)] public int dilationRadius = 4;
    }

    public enum LayerType { Texture = 0, Color = 1 }

    public enum BlendMode { Normal = 0, Add = 1, Multiply = 2, Overlay = 3, Screen = 4 }

    public enum ModifierType { ColorReplace = 0, HSV = 1, Levels = 2, EdgeDilation = 3 }
}
