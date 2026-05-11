using System;
using System.Collections.Generic;
using Dennoko.TexturingTool.Editor.Infrastructure;
using Dennoko.TexturingTool.Runtime.Application;
using Dennoko.TexturingTool.Runtime.Data;
using Dennoko.TexturingTool.Runtime.Domain;
using Dennoko.TexturingTool.Runtime.Infrastructure;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Dennoko.TexturingTool.Editor
{
    public sealed class TexturingToolWindow : EditorWindow
    {
        private TextureToolConfig _config;
        private Texture2D         _preview;
        private int               _selectedLayerIdx = -1;
        private Vector2           _scrollPos;
        private ReorderableList   _layerList;
        private bool              _showHistory;

        private static readonly string[] ResLabels = { "256", "512", "1024", "2048", "4096" };
        private static readonly int[]    ResValues = { 256,   512,   1024,   2048,   4096 };

        [MenuItem("dennokoworks/Texturing Tool")]
        private static void Open() => GetWindow<TexturingToolWindow>("Texturing Tool");

        private void OnEnable() => RebuildLayerList();

        private void OnGUI()
        {
            DrawConfigBar();
            DrawPreview();

            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);
            if (_config != null)
            {
                _layerList?.DoLayoutList();
                GUILayout.Space(4);
                if (_selectedLayerIdx >= 0 && _selectedLayerIdx < _config.layers.Count)
                    DrawLayerParams(_config.layers[_selectedLayerIdx]);
                GUILayout.Space(4);
                DrawGlobalModifiers();
                GUILayout.Space(4);
                DrawHistory();
            }
            EditorGUILayout.EndScrollView();

            DrawExportBar();
        }

        // ─── Always-visible sections ────────────────────────────────────────────

        private void DrawConfigBar()
        {
            EditorGUI.BeginChangeCheck();
            var cfg = (TextureToolConfig)EditorGUILayout.ObjectField("Config", _config, typeof(TextureToolConfig), false);
            if (EditorGUI.EndChangeCheck()) { _config = cfg; RebuildLayerList(); }
            if (_config == null) return;

            EditorGUI.BeginChangeCheck();
            int w = EditorGUILayout.IntPopup("Width",  _config.width,  ResLabels, ResValues);
            int h = EditorGUILayout.IntPopup("Height", _config.height, ResLabels, ResValues);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(_config, "Canvas Size");
                _config.width = w; _config.height = h;
                EditorUtility.SetDirty(_config);
            }
        }

        private void DrawPreview()
        {
            if (_preview == null) return;
            float side = Mathf.Min(position.width, 200f);
            var rect = GUILayoutUtility.GetRect(side, side, GUILayout.ExpandWidth(false));
            EditorGUI.DrawPreviewTexture(rect, _preview, null, ScaleMode.ScaleToFit);
        }

        private void DrawExportBar()
        {
            if (_config == null) return;
            EditorGUI.BeginChangeCheck();
            var path = EditorGUILayout.TextField("Export Path", _config.exportRelativePath);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(_config, "Export Path");
                _config.exportRelativePath = path;
                EditorUtility.SetDirty(_config);
            }
            using (new EditorGUI.DisabledScope(_config == null))
            {
                if (GUILayout.Button("Run Pipeline")) RunPipeline();
            }
        }

        // ─── Scrollable sections ─────────────────────────────────────────────────

        private void RebuildLayerList()
        {
            if (_config == null) { _layerList = null; return; }
            _layerList = new ReorderableList(_config.layers, typeof(LayerData), true, true, true, true)
            {
                drawHeaderCallback  = r => EditorGUI.LabelField(r, "Layers"),
                onReorderCallback   = _ => EditorUtility.SetDirty(_config),
                onSelectCallback    = l => _selectedLayerIdx = l.index,
                drawElementCallback = (rect, i, active, focused) =>
                {
                    if (i >= _config.layers.Count) return;
                    var layer = _config.layers[i];
                    float x = rect.x, y = rect.y + 2f, h = 18f;
                    EditorGUI.BeginChangeCheck();
                    bool  en   = EditorGUI.Toggle(new Rect(x, y, 16, h), layer.enabled);
                    string nm  = EditorGUI.TextField(new Rect(x+18, y, rect.width-130, h), layer.name);
                    var   bm   = (BlendMode)EditorGUI.EnumPopup(new Rect(rect.xMax-110, y, 108, h), layer.blendMode);
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(_config, "Layer");
                        layer.enabled = en; layer.name = nm; layer.blendMode = bm;
                        EditorUtility.SetDirty(_config);
                    }
                },
                onAddCallback = list =>
                {
                    Undo.RecordObject(_config, "Add Layer");
                    _config.layers.Add(new LayerData { name = $"Layer {_config.layers.Count + 1}" });
                    EditorUtility.SetDirty(_config);
                },
                onRemoveCallback = list =>
                {
                    Undo.RecordObject(_config, "Remove Layer");
                    _config.layers.RemoveAt(list.index);
                    _selectedLayerIdx = Mathf.Clamp(_selectedLayerIdx, -1, _config.layers.Count - 1);
                    EditorUtility.SetDirty(_config);
                }
            };
        }

        private void DrawLayerParams(LayerData layer)
        {
            EditorGUILayout.LabelField("Layer Parameters", EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            var type    = (LayerType) EditorGUILayout.EnumPopup("Type",    layer.type);
            var tex     = type == LayerType.Texture
                          ? (Texture2D)EditorGUILayout.ObjectField("Texture", layer.texture, typeof(Texture2D), false)
                          : layer.texture;
            var col     = type == LayerType.Color
                          ? EditorGUILayout.ColorField("Color", layer.color)
                          : layer.color;
            var opacity = EditorGUILayout.Slider("Opacity", layer.opacity, 0f, 1f);
            var tScale  = EditorGUILayout.Vector2Field("Tiling Scale",  layer.tilingScale);
            var tOffset = EditorGUILayout.Vector2Field("Tiling Offset", layer.tilingOffset);
            var useMask = EditorGUILayout.Toggle("Use ID Mask", layer.useIdMask);
            var maskIdx = useMask ? EditorGUILayout.IntField(" SubMesh Index", layer.idMaskSubMeshIndex)
                                  : layer.idMaskSubMeshIndex;
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(_config, "Layer Params");
                layer.type = type; layer.texture = tex; layer.color = col;
                layer.opacity = opacity; layer.tilingScale = tScale; layer.tilingOffset = tOffset;
                layer.useIdMask = useMask; layer.idMaskSubMeshIndex = maskIdx;
                EditorUtility.SetDirty(_config);
            }
            EditorGUILayout.LabelField("Per-Layer Modifiers", EditorStyles.miniBoldLabel);
            DrawModifiers(layer.modifiers);
        }

        private void DrawGlobalModifiers()
        {
            EditorGUILayout.LabelField("Global Modifiers", EditorStyles.boldLabel);
            DrawModifiers(_config.globalModifiers);
        }

        private void DrawModifiers(List<ModifierData> mods)
        {
            for (int i = 0; i < mods.Count; i++)
            {
                var m = mods[i];
                EditorGUILayout.BeginHorizontal();
                EditorGUI.BeginChangeCheck();
                var newType = (ModifierType)EditorGUILayout.EnumPopup(m.type, GUILayout.Width(110));
                if (EditorGUI.EndChangeCheck()) { Undo.RecordObject(_config, "Modifier Type"); m.type = newType; EditorUtility.SetDirty(_config); }
                if (GUILayout.Button("✕", GUILayout.Width(22))) { Undo.RecordObject(_config, "Remove Modifier"); mods.RemoveAt(i); i--; EditorUtility.SetDirty(_config); EditorGUILayout.EndHorizontal(); continue; }
                EditorGUILayout.EndHorizontal();
                EditorGUI.indentLevel++;
                DrawModifierParams(m);
                EditorGUI.indentLevel--;
            }
            if (GUILayout.Button("+ Add Modifier")) { Undo.RecordObject(_config, "Add Modifier"); mods.Add(new ModifierData()); EditorUtility.SetDirty(_config); }
        }

        private void DrawModifierParams(ModifierData m)
        {
            switch (m.type)
            {
                case ModifierType.ColorReplace:
                    EditorGUI.BeginChangeCheck();
                    var sc = EditorGUILayout.ColorField("From",      m.sourceColor);
                    var tc = EditorGUILayout.ColorField("To",        m.targetColor);
                    var th = EditorGUILayout.Slider   ("Threshold",  m.threshold, 0.001f, 1f);
                    if (EditorGUI.EndChangeCheck()) { Undo.RecordObject(_config, "ColorReplace"); m.sourceColor=sc; m.targetColor=tc; m.threshold=th; EditorUtility.SetDirty(_config); }
                    break;
                case ModifierType.HSV:
                    EditorGUI.BeginChangeCheck();
                    var mh = EditorGUILayout.Slider("Hue", m.hue, -0.5f, 0.5f);
                    var ms = EditorGUILayout.Slider("Sat", m.saturation, -1f, 1f);
                    var mv = EditorGUILayout.Slider("Val", m.value, -1f, 1f);
                    if (EditorGUI.EndChangeCheck()) { Undo.RecordObject(_config, "HSV"); m.hue=mh; m.saturation=ms; m.value=mv; EditorUtility.SetDirty(_config); }
                    break;
                case ModifierType.Levels:
                    EditorGUI.BeginChangeCheck();
                    var iMin = EditorGUILayout.Slider("In Min",  m.inMin, 0f, 1f);
                    var iMax = EditorGUILayout.Slider("In Max",  m.inMax, 0f, 1f);
                    var oMin = EditorGUILayout.Slider("Out Min", m.outMin, 0f, 1f);
                    var oMax = EditorGUILayout.Slider("Out Max", m.outMax, 0f, 1f);
                    if (EditorGUI.EndChangeCheck()) { Undo.RecordObject(_config, "Levels"); m.inMin=iMin; m.inMax=iMax; m.outMin=oMin; m.outMax=oMax; EditorUtility.SetDirty(_config); }
                    break;
                case ModifierType.EdgeDilation:
                    EditorGUI.BeginChangeCheck();
                    var rad = EditorGUILayout.IntSlider("Radius", m.dilationRadius, 1, 32);
                    if (EditorGUI.EndChangeCheck()) { Undo.RecordObject(_config, "EdgeDilation"); m.dilationRadius=rad; EditorUtility.SetDirty(_config); }
                    break;
            }
        }

        private void DrawHistory()
        {
            _showHistory = EditorGUILayout.Foldout(_showHistory, "Snapshot History");
            if (!_showHistory) return;
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Take Snapshot")) { _config.snapshots.Add(JsonUtility.ToJson(_config)); EditorUtility.SetDirty(_config); }
            if (_config.snapshots.Count > 0 && GUILayout.Button("Clear All")) { _config.snapshots.Clear(); EditorUtility.SetDirty(_config); }
            EditorGUILayout.EndHorizontal();
            for (int i = _config.snapshots.Count - 1; i >= 0; i--)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"Snapshot {i + 1}", GUILayout.ExpandWidth(true));
                if (GUILayout.Button("Restore", GUILayout.Width(60)))
                {
                    Undo.RecordObject(_config, "Restore Snapshot");
                    JsonUtility.FromJsonOverwrite(_config.snapshots[i], _config);
                    RebuildLayerList();
                    EditorUtility.SetDirty(_config);
                }
                EditorGUILayout.EndHorizontal();
            }
        }

        // ─── Pipeline ────────────────────────────────────────────────────────────

        private void RunPipeline()
        {
            ILayerProcessor processor;
            try   { processor = new GpuLayerProcessor(); }
            catch (Exception ex) { Debug.LogWarning($"GPU processor unavailable ({ex.Message}). Falling back to CPU."); processor = new SimpleLayerProcessor(); }

            using (processor)
            {
                var pipeline = new TextureProcessingPipeline(
                    new EditorFbxLoader(),
                    new SimpleMeshCombiner(),
                    processor,
                    new PngTextureExporter(),
                    new IdMaskGenerator());
                try
                {
                    var result = pipeline.Execute(new ProcessingRequest(_config));
                    SetPreview(result.OutputTexture);
                    ShowNotification(new GUIContent($"Exported: {result.ExportPath}"));
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                    ShowNotification(new GUIContent("Pipeline failed. Check Console."));
                }
            }
        }

        private void SetPreview(Texture2D tex)
        {
            if (_preview != null) DestroyImmediate(_preview);
            _preview = tex;
            if (_preview != null) _preview.hideFlags = HideFlags.DontSave;
        }
    }
}
