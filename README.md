# Unity Essentials

This module is part of the Unity Essentials ecosystem and follows the same lightweight, editor-first approach.
Unity Essentials is a lightweight, modular set of editor utilities and helpers that streamline Unity development. It focuses on clean, dependency-free tools that work well together.

All utilities are under the `UnityEssentials` namespace.

```csharp
using UnityEssentials;
```

## Installation

Install the Unity Essentials entry package via Unity's Package Manager, then install modules from the Tools menu.

- Add the entry package (via Git URL)
    - Window → Package Manager
    - "+" → "Add package from git URL…"
    - Paste: `https://github.com/CanTalat-Yakan/UnityEssentials.git`

- Install or update Unity Essentials packages
    - Tools → Install & Update UnityEssentials
    - Install all or select individual modules; run again anytime to update

---

# Scene Loader

> Quick overview: Scene‑group based additive loading with optional Addressables support, aggregated progress reporting, smooth UI‑friendly progress, and lifecycle events for loaded/unloaded scenes.

Scene groups are loaded asynchronously, combining regular build scenes and Addressables entries in one operation. The active scene is set based on a designated type in the group, progress is reported and can be smoothed for UI display, and previously loaded scenes are unloaded (preserving a boot/persistent scene). Loading, unloading, and completion events are exposed for instrumentation or UI hooks.

![screenshot](Documentation/Screenshot.png)

## Features
- Scene groups as assets
  - `SceneGroup` ScriptableObject maps `SceneReference` → `SceneType` (e.g., ActiveScene, Menu, UI, HUD, Cinematic)
  - Utility `FindSceneNameByType(SceneType)` to resolve the active scene
- Additive loading (regular + Addressables)
  - Regular scenes loaded via `SceneManager.LoadSceneAsync(..., Additive)`
  - Addressable scenes loaded via `Addressables.LoadSceneAsync(..., Additive)`
  - Duplicate scenes can be skipped by default; optional reload of duplicates
- Aggregated progress and smooth display
  - Overall progress aggregated across regular AsyncOperation and Addressables handles
  - `SceneLoader` exposes `TargetProgress`, `SmoothProgress`, and `SmoothProgressSpeed` for UI bars
- Lifecycle events
  - Per‑scene: `OnSceneLoaded(sceneName, state)` and `OnSceneUnloaded(sceneName)`
  - Group done: `OnSceneGroupLoaded()`
- Active scene selection
  - After load, the scene tagged `SceneType.ActiveScene` becomes the active scene
- Boot/persistent handling and cleanup
  - Unloads all non‑essential scenes (skips the current active and a configurable `BootSceneName`)
  - Optional `UnloadResources` to call `Resources.UnloadUnusedAssets()`
- Optional debug logging
  - `SceneLoaderSettings.DebugLogMessages` prints load/unload events to the Console

## Requirements
- Unity 6000.0+
- For regular scenes: add them to Build Settings
- For Addressables: Unity Addressables package configured; your scene references must be Addressable
- Create a `SceneGroup` asset (Assets → Create → SceneLoader → Group)
- Optional: a boot/persistent scene named to match `BootSceneName` (default "Boot")

## Usage
1) Create a Scene Group
   - Right‑click in Project → Create → SceneLoader → Group
   - Populate the `Scenes` dictionary with `SceneReference` keys and `SceneType` values
   - Mark exactly one entry as `ActiveScene` to define which scene becomes active

2) Add the Scene Loader
   - Add `SceneLoader` to a bootstrap GameObject in your boot scene
   - Assign your `SceneGroup` asset
   - Optionally enable `DebugLogMessages` and tune `SmoothProgressSpeed`

3) Subscribe to events (optional)
```csharp
// In a MonoBehaviour with a reference to the SceneLoader component
void Awake()
{
    var mgr = sceneLoader.Manager;
    mgr.OnSceneLoaded += (name, state) => Debug.Log($"Loaded {name} [{state}]");
    mgr.OnSceneUnloaded += name => Debug.Log($"Unloaded {name}");
    mgr.OnSceneGroupLoaded += () => Debug.Log("Group loaded");
}
```

4) Start loading
- Automatic: `SceneLoader.Start()` calls `LoadSceneGroup()` using the assigned group
- Manual: call `await sceneLoader.LoadSceneGroup(myGroup);`

5) Drive a progress bar (optional)
- Bind UI fill to `sceneLoader.SmoothProgress` for a stable visual, or to `TargetProgress` for raw progress

## How It Works
- Data model
  - `SceneGroup` stores a `SerializedDictionary<SceneReference, SceneType>`; entries may be regular or Addressable
- Load sequence
  - `SceneGroupManager.LoadScenes` first calls `UnloadScenes()` (preserves current active + `BootSceneName`)
  - For each entry: load via regular AsyncOperation or Addressables handle; raise `OnSceneLoaded`
  - While not finished, report combined progress every ~100 ms
  - Set active scene to the entry marked `ActiveScene`; raise `OnSceneGroupLoaded`
- Unload sequence
  - Enumerate loaded scenes (except preserved) and unload via `SceneManager.UnloadSceneAsync`
  - Unload any Addressables scene handles and clear the handle list
  - Optionally run `Resources.UnloadUnusedAssets()` if `UnloadResources = true`
- Smoothing
  - `SceneLoader` lerps `SmoothProgress` toward `TargetProgress` with a dynamic speed based on the gap

## Notes and Limitations
- One active scene per group is expected; if none is marked `ActiveScene`, the active scene remains unchanged
- Duplicate handling: when `reloadDuplicateScenes` is false, scenes already loaded are skipped
- Progress cadence uses `Task.Delay(100)`; UI can poll `SmoothProgress` each frame
- Addressables are required only for entries marked as Addressable; others must be present in Build Settings
- Boot/persistent scenes are skipped during unload if their name equals the current active scene or `BootSceneName`
- Threading: API uses async/await but relies on Unity’s main thread for scene operations

## Files in This Package
- `Runtime/SceneLoader.cs` – Component wrapper with smoothed progress and optional logging
- `Runtime/SceneGroupManager.cs` – Core loader/unloader, events, progress aggregation
- `Runtime/SceneGroup.cs` – ScriptableObject scene group and `SceneType` enum
- `Runtime/UnityEssentials.SceneLoader.asmdef` – Runtime assembly definition

## Tags
unity, scenes, scene-management, additive, addressables, async, progress, events, boot, unload, runtime
