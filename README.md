# Unity Essentials

**Unity Essentials** is a lightweight, modular utility namespace designed to streamline development in Unity. 
It provides a collection of foundational tools, extensions, and helpers to enhance productivity and maintain clean code architecture.

## 📦 This Package

This package is part of the **Unity Essentials** ecosystem.  
It integrates seamlessly with other Unity Essentials modules and follows the same lightweight, dependency-free philosophy.

## 🌐 Namespace

All utilities are under the `UnityEssentials` namespace. This keeps your project clean, consistent, and conflict-free.

```csharp
using UnityEssentials;
```

# SceneGroupManager  
Handles loading and unloading of multiple Unity scenes (both regular and Addressable) in a group, with progress reporting and event notifications.

## Usage Examples
- Initialize a `SceneGroupManager` and call `LoadScenes` with a `SceneGroup` asset to load a collection of scenes additively.  
- Subscribe to `OnSceneLoaded`, `OnSceneUnloaded`, and `OnSceneGroupLoaded` for scene event tracking.  
- Use `UnloadScenes` to remove all scenes except the active and boot scenes, with automatic addressable cleanup.  
- Progress is reported using `IProgress<float>`, ideal for loading screens or feedback UI.  
- Automatically sets the active scene based on the group-defined `SceneType.ActiveScene`.  

# AsyncOperationGroup  
Encapsulates a list of `AsyncOperation` instances and tracks their aggregate completion and progress.

## Usage Examples
- Use to group and monitor the progress of additive scene loads.  
- Call `IsDone` in an async loop to wait for group completion.  
- Use `Progress` to compute average progress of all operations.  

# AsyncOperationHandleGroup  
Tracks multiple Addressables `AsyncOperationHandle<SceneInstance>` objects with progress and completion checks.

## Usage Examples
- Store handles from addressable scene loads.  
- Use `IsDone` and `Progress` for progress monitoring during bulk loading/unloading.  
- Clear handles post-unload to avoid memory retention.  

# SceneGroup  
A ScriptableObject storing a dictionary of `SceneReference` to `SceneType`, defining a group of scenes.

## Usage Examples
- Create scene collections via Unity's asset menu (`Create > SceneLoader > Group`).  
- Use `FindSceneNameByType` to retrieve the name of a scene marked with a specific `SceneType`.  

# SceneType  
Defines roles or purposes for scenes in a `SceneGroup`.

## Usage Examples
- Label scenes as `ActiveScene`, `MainMenu`, `UserInterface`, etc., to guide loading logic.  
- Used for determining which scene should be made active post-load.  

# SceneLoader  
Monobehaviour that orchestrates scene group loading with optional logging and smooth progress interpolation.

## Usage Examples
- Attach to a boot scene object to load a `SceneGroup` on startup.  
- Assign `SceneGroup` in the Inspector or at runtime before calling `LoadSceneGroup`.  
- Monitor `SmoothProgress` for UI-based loading bars.  
- Set `SmoothProgressSpeed` to control how quickly the UI responds to progress changes.  

# LoadingProgress  
Implements `IProgress<float>` to report normalized progress updates.

## Usage Examples
- Instantiate and pass to `SceneGroupManager.LoadScenes` to receive progress callbacks.  
- Listen to `Progressed` event to update visuals like progress bars or status text.

