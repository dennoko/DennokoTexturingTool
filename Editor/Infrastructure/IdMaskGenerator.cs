using System;
using Dennoko.TexturingTool.Runtime.Domain;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace Dennoko.TexturingTool.Editor.Infrastructure
{
    public sealed class IdMaskGenerator : IIdMaskGenerator
    {
        private static readonly Color[] SubMeshColors =
        {
            Color.red,
            Color.green,
            Color.blue,
            Color.yellow,
            Color.cyan,
            Color.magenta,
            new Color(1f, 0.5f, 0f),
            new Color(0.5f, 0f, 1f),
        };

        public Texture2D[] Generate(Mesh mesh, int[] subMeshIndices, int width, int height)
        {
            if (mesh == null || subMeshIndices == null || subMeshIndices.Length == 0)
                return Array.Empty<Texture2D>();

            var shader = Shader.Find("Dennoko/SolidColor");
            if (shader == null)
            {
                Debug.LogWarning("[IdMaskGenerator] Shader 'Dennoko/SolidColor' not found. Skipping ID mask generation.");
                return Array.Empty<Texture2D>();
            }

            var mat  = new Material(shader) { hideFlags = HideFlags.DontSave };
            var rt   = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32);
            var proj = GL.GetGPUProjectionMatrix(Matrix4x4.Ortho(0f, 1f, 0f, 1f, -1f, 1f), true);
            var cmd  = new CommandBuffer { name = "IDMaskGenerate" };

            var results = new Texture2D[subMeshIndices.Length];

            for (int i = 0; i < subMeshIndices.Length; i++)
            {
                int subIdx = subMeshIndices[i];
                if (subIdx < 0 || subIdx >= mesh.subMeshCount)
                {
                    results[i] = CreateBlankTexture(width, height);
                    continue;
                }

                mat.color = SubMeshColors[i % SubMeshColors.Length];

                cmd.Clear();
                cmd.SetRenderTarget(rt);
                cmd.ClearRenderTarget(true, true, Color.clear);
                cmd.SetViewProjectionMatrices(Matrix4x4.identity, proj);

                var uvMesh = BuildUvMesh(mesh, subIdx);
                cmd.DrawMesh(uvMesh, Matrix4x4.identity, mat, 0, 0);
                Graphics.ExecuteCommandBuffer(cmd);
                Object.DestroyImmediate(uvMesh);

                var prev = RenderTexture.active;
                RenderTexture.active = rt;
                var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
                tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                tex.Apply();
                RenderTexture.active = prev;

                results[i] = tex;
            }

            cmd.Release();
            RenderTexture.ReleaseTemporary(rt);
            Object.DestroyImmediate(mat);
            return results;
        }

        private static Mesh BuildUvMesh(Mesh srcMesh, int subMeshIndex)
        {
            var uvs  = srcMesh.uv;
            var tris = srcMesh.GetTriangles(subMeshIndex);

            var positions = new Vector3[uvs.Length];
            for (int i = 0; i < uvs.Length; i++)
                positions[i] = new Vector3(uvs[i].x, uvs[i].y, 0f);

            var m = new Mesh { vertices = positions, triangles = tris };
            return m;
        }

        private static Texture2D CreateBlankTexture(int width, int height)
        {
            var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            tex.SetPixels(new Color[width * height]);
            tex.Apply();
            return tex;
        }
    }
}
