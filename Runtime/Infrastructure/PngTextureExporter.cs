using System.IO;
using Dennoko.TexturingTool.Runtime.Domain;
using UnityEngine;

namespace Dennoko.TexturingTool.Runtime.Infrastructure
{
    public sealed class PngTextureExporter : ITextureExporter
    {
        public void Export(Texture2D texture, string assetPath)
        {
            if (texture == null || string.IsNullOrWhiteSpace(assetPath))
            {
                return;
            }

            var fullPath = ToFullPath(assetPath);
            var directory = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllBytes(fullPath, texture.EncodeToPNG());
#if UNITY_EDITOR
            UnityEditor.AssetDatabase.Refresh();
#endif
        }

        private static string ToFullPath(string path)
        {
            if (Path.IsPathRooted(path))
            {
                return path;
            }

            if (path.StartsWith("Assets"))
            {
                var projectRoot = Directory.GetParent(Application.dataPath)?.FullName ?? ".";
                var relativePath = path.Substring("Assets".Length).TrimStart('/', '\\');
                return Path.Combine(projectRoot, "Assets", relativePath);
            }

            return Path.Combine(Application.dataPath, path);
        }
    }
}
