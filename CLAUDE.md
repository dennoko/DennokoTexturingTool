# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**Dennoko Texturing Tool** is a non-destructive, modular texture creation Unity Editor extension. Artists compose textures from a layer stack entirely inside Unity (no Substance Designer). The tool loads FBX meshes, selects SubMeshes, blends layers via a GPU pipeline, and exports as PNG.

The requirements spec at [Docs/Impl/requirements.md](Docs/Impl/requirements.md) (Japanese) defines the full roadmap.

## Development Workflow

No build scripts, no package.json, no automated test runner. Development is entirely through Unity Editor:

- Open via menu: **Dennoko > Texturing Tool**
- Create a `TextureToolConfig` ScriptableObject asset and assign it in the window
- Unity recompiles automatically on C# file save
- Testing is manual via the Editor window

## Architecture

3-layer clean architecture with interface-based DI:

```
Data            Runtime/Data/                TextureToolConfig (ScriptableObject)
Domain          Runtime/Domain/              ProcessingContracts (all interfaces + structs)
Application     Runtime/Application/         TextureProcessingPipeline (orchestrator)
Infrastructure  Runtime/Infrastructure/      GpuLayerProcessor, SimpleLayerProcessor, PngTextureExporter
                Editor/Infrastructure/       EditorFbxLoader, SimpleMeshCombiner, IdMaskGenerator
UI              Editor/                      TexturingToolWindow (EditorWindow)
```

**Pipeline flow**: `TextureToolConfig` → `ProcessingRequest` → `FbxLoad` → `MeshCombine` → `IdMaskGenerate` → `LayerProcess` → `Export` → `ProcessingResult`

All cross-layer dependencies go through interfaces in [Runtime/Domain/ProcessingContracts.cs](Runtime/Domain/ProcessingContracts.cs). The Editor layer never directly instantiates Runtime infrastructure and vice versa.

## Key Technical Decisions

**ScriptableObject config**: All state (canvas size, layer stack, SubMesh indices, export path, snapshots) lives in `TextureToolConfig`. Infrastructure classes receive config via `LayerProcessingContext` or method args — never read `TextureToolConfig` directly in `Runtime/Infrastructure/`.

**GPU pipeline (primary)**: `GpuLayerProcessor` uses ping-pong `RenderTexture` buffering with `Graphics.Blit`. Four shaders handle distinct operations: `Dennoko/LayerBlend` (blend modes + ID mask compositing), `Dennoko/ColorAdjust` (ColorReplace/HSV/Levels via `_Mode` float dispatch), `Dennoko/TilingOffset` (UV tiling), `Dennoko/EdgeDilation`. `TexturingToolWindow.RunPipeline()` tries GPU first, falls back to `SimpleLayerProcessor` on shader-load failure.

**CPU fallback (`SimpleLayerProcessor`)**: Pixel-loop fallback. Only implements `ColorReplace` modifier — HSV/Levels/EdgeDilation are GPU-only. Do not add modifier support here; CPU path is kept only as a safety fallback.

**IDMask generation**: `IdMaskGenerator` renders UV-space geometry into a `RenderTexture` using a `CommandBuffer` and the `Dennoko/SolidColor` shader. Each SubMesh gets a unique solid color baked into a separate `Texture2D`. Masks are indexed by SubMesh index and looked up per-layer in `GpuLayerProcessor.FindIdMask()`.

**Editor/Runtime split**: `EditorFbxLoader`, `SimpleMeshCombiner`, and `IdMaskGenerator` are in `Editor/Infrastructure/` because they use `AssetDatabase`/Editor-only APIs. `GpuLayerProcessor`, `SimpleLayerProcessor`, and `PngTextureExporter` are in `Runtime/Infrastructure/` (only `UnityEngine` APIs).

**Snapshot history**: `TextureToolConfig.snapshots` stores JSON-serialized copies of the config via `JsonUtility.ToJson`. Restore overwrites config with `JsonUtility.FromJsonOverwrite`. The `[HideInInspector]` attribute keeps snapshots out of the default Inspector view.

**Layer list UI**: Uses `UnityEditorInternal.ReorderableList`. `RebuildLayerList()` must be called whenever `_config` changes to rebind callbacks. The selected layer index drives the parameter panel rendered below the list.

## Shaders

All shaders live in [Runtime/Infrastructure/Shaders/](Runtime/Infrastructure/Shaders/) and are found at runtime via `Shader.Find("Dennoko/<Name>")`. If a shader is missing, `GpuLayerProcessor` constructor throws and the pipeline falls back to CPU. The `ColorAdjust` shader dispatches modifier type via a `_Mode` float (0=ColorReplace, 1=HSV, 2=Levels); `EdgeDilation` has its own dedicated shader because it requires a multi-pixel kernel.

## Code Style

- ≤200 lines per file (300 absolute max); SOLID principles; unidirectional data flow
- No comments except for non-obvious WHY (hidden constraints, workarounds, surprising invariants)
