using Dennoko.TexturingTool.Editor.Infrastructure;
using Dennoko.TexturingTool.Runtime.Application;
using Dennoko.TexturingTool.Runtime.Data;
using Dennoko.TexturingTool.Runtime.Domain;
using Dennoko.TexturingTool.Runtime.Infrastructure;
using UnityEditor;
using UnityEngine;

namespace Dennoko.TexturingTool.Editor
{
    public sealed class TexturingToolWindow : EditorWindow
    {
        private TextureToolConfig _config;
        private Texture2D _preview;
        private Mesh _combinedMesh;

        [MenuItem("Dennoko/Texturing Tool")]
        private static void Open() => GetWindow<TexturingToolWindow>("Texturing Tool");

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Dennoko Texturing Tool (Prototype)", EditorStyles.boldLabel);
            _config = (TextureToolConfig)EditorGUILayout.ObjectField("Config", _config, typeof(TextureToolConfig), false);

            using (new EditorGUI.DisabledScope(_config == null))
            {
                if (GUILayout.Button("Run Pipeline"))
                {
                    RunPipeline();
                }
            }

            if (_preview != null)
            {
                GUILayout.Space(8);
                EditorGUILayout.LabelField("Preview", EditorStyles.boldLabel);
                var rect = GUILayoutUtility.GetRect(256, 256, GUILayout.ExpandWidth(false));
                EditorGUI.DrawPreviewTexture(rect, _preview, null, ScaleMode.ScaleToFit);
            }

            if (_combinedMesh != null)
            {
                EditorGUILayout.HelpBox($"Combined Mesh: {_combinedMesh.name}", MessageType.Info);
            }
        }

        private void RunPipeline()
        {
            if (_config == null)
            {
                ShowNotification(new GUIContent("Config is not set."));
                return;
            }

            var pipeline = new TextureProcessingPipeline(
                new EditorFbxLoader(),
                new SimpleMeshCombiner(),
                new SimpleLayerProcessor(),
                new PngTextureExporter());

            try
            {
                var result = pipeline.Execute(new ProcessingRequest(_config));
                _combinedMesh = result.CombinedMesh;
                _preview = result.OutputTexture;
                ShowNotification(new GUIContent($"Exported: {result.ExportPath}"));
            }
            catch (System.Exception ex)
            {
                Debug.LogException(ex);
                ShowNotification(new GUIContent("Pipeline failed. Check Console."));
            }
        }
    }
}
