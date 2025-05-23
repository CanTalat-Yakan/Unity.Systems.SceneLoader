using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;

namespace UnityEssentials
{
    /// <summary>
    /// Represents a group of asynchronous operations that can be tracked collectively.
    /// </summary>
    /// <remarks>This struct provides functionality to monitor the progress and completion status of a
    /// collection of asynchronous operations. It is particularly useful for scenarios where multiple asynchronous tasks
    /// need to be managed as a single unit.</remarks>
    public readonly struct AsyncOperationGroup
    {
        public readonly List<AsyncOperation> Operations;

        public float Progress => Operations.Count == 0 ? 0 : Operations.Average(operation => operation.progress);
        public bool IsDone => Operations.All(operation => operation.isDone);

        public AsyncOperationGroup(int initialCapacity) =>
            Operations = new(initialCapacity);
    }

    /// <summary>
    /// Represents a group of asynchronous operation handles, providing aggregated progress and completion status.
    /// </summary>
    /// <remarks>This struct is designed to manage and monitor a collection of asynchronous operations, such
    /// as scene loading tasks. It provides properties to calculate the overall progress and determine whether all
    /// operations in the group are complete.</remarks>
    public readonly struct AsyncOperationHandleGroup
    {
        public readonly List<AsyncOperationHandle<SceneInstance>> Handles;

        public float Progress => Handles.Count == 0 ? 0 : Handles.Average(handle => handle.PercentComplete);
        public bool IsDone => Handles.Count == 0 || Handles.All(operation => operation.IsDone);

        public AsyncOperationHandleGroup(int initialCapacity) =>
            Handles = new(initialCapacity);
    }

    /// <summary>
    /// Manages the loading and unloading of scene groups, including individual scenes within a group.
    /// </summary>
    /// <remarks>This class provides functionality to load and unload scenes in groups, track the active scene
    /// group,  and raise events when scenes or scene groups are loaded or unloaded. It supports both regular and 
    /// addressable scenes, and allows progress tracking during asynchronous operations.</remarks>
    public class SceneGroupManager
    {
        public string BootSceneName = "Boot";
        public bool UnloadResources = false;

        public event Action<string, string> OnSceneLoaded = delegate { };
        public event Action<string> OnSceneUnloaded = delegate { };
        public event Action OnSceneGroupLoaded = delegate { };

        public SceneGroup ActiveSceneGroup;

        private readonly AsyncOperationHandleGroup _handleGroup = new(10);

        /// <summary>
        /// Asynchronously loads the scenes specified in the given <see cref="SceneGroup"/>.
        /// </summary>
        /// <remarks>This method unloads any currently loaded scenes before loading the scenes specified
        /// in the <paramref name="group"/>. Scenes are loaded additively, and the active scene is set based on the
        /// scene type defined in the <paramref name="group"/>.  If the <paramref name="progress"/> parameter is
        /// provided, it will report the combined progress of all scene loading operations.  The method invokes the
        /// <c>OnSceneLoaded</c> event for each scene loaded and the <c>OnSceneGroupLoaded</c> event once all scenes in
        /// the group are loaded.</remarks>
        /// <param name="group">The <see cref="SceneGroup"/> containing the scenes to load.</param>
        /// <param name="progress">An optional progress reporter that reports the loading progress as a value between 0.0 and 1.0.</param>
        /// <param name="reloadDuplicateScenes">A boolean value indicating whether to reload scenes that are already loaded.  If <see langword="false"/>,
        /// duplicate scenes will be skipped.</param>
        /// <returns>A task that represents the asynchronous operation of loading the scenes.</returns>
        public async Task LoadScenes(SceneGroup group, IProgress<float> progress, bool reloadDuplicateScenes = false)
        {
            ActiveSceneGroup = group;
            var loadedScenes = new List<string>();

            await UnloadScenes();

            var sceneCount = SceneManager.sceneCount;

            for (var i = 0; i < sceneCount; i++)
                loadedScenes.Add(SceneManager.GetSceneAt(i).name);

            var operationGroup = new AsyncOperationGroup(1);

            foreach (var entry in ActiveSceneGroup.Scenes)
            {
                var sceneReference = entry.Key;
                var sceneType = entry.Value;

                if (reloadDuplicateScenes == false && loadedScenes.Contains(sceneReference.Name))
                    continue;

                if (sceneReference.State == SceneReferenceState.Regular)
                    operationGroup.Operations.Add(sceneReference.LoadAsync(LoadSceneMode.Additive));

                if (sceneReference.State == SceneReferenceState.Addressable)
                    _handleGroup.Handles.Add(sceneReference.LoadAsyncAddressable(LoadSceneMode.Additive));

                OnSceneLoaded.Invoke(sceneReference.Name, sceneReference.State.ToString());
            }

            // Wait until all AsyncOperations in the group are done
            while (!operationGroup.IsDone || !_handleGroup.IsDone)
            {
                progress?.Report((operationGroup.Progress + _handleGroup.Progress) / 2);
                await Task.Delay(100);
            }

            Scene activeScene = SceneManager.GetSceneByName(ActiveSceneGroup.FindSceneNameByType(SceneType.ActiveScene));
            if (activeScene.IsValid())
                SceneManager.SetActiveScene(activeScene);

            OnSceneGroupLoaded.Invoke();
        }

        /// <summary>
        /// Unloads all non-essential scenes currently loaded in the application, including regular and addressable
        /// scenes.
        /// </summary>
        /// <remarks>This method unloads all scenes except the active scene and a designated boot scene. 
        /// It also handles the unloading of addressable scenes separately and clears their associated handles. If the
        /// <see cref="UnloadResources"/> flag is set, unused assets are also unloaded from memory  to free up
        /// resources.</remarks>
        /// <returns></returns>
        public async Task UnloadScenes()
        {
            var scenes = new List<string>();
            var activeScene = SceneManager.GetActiveScene().name;

            var sceneCount = SceneManager.sceneCount;
            for (var i = sceneCount - 1; i > 0; i--)
            {
                var sceneAt = SceneManager.GetSceneAt(i);
                if (!sceneAt.isLoaded)
                    continue;

                var sceneName = sceneAt.name;

                // Optional: For a persistent scene
                if (sceneName.Equals(activeScene) || sceneName == BootSceneName)
                    continue;

                // Skip addressable, those will be unloaded separately
                if (_handleGroup.Handles.Any(handle => handle.IsValid() && handle.Result.Scene.name == sceneName))
                    continue;

                scenes.Add(sceneName);
            }

            // Create an AsyncOperationGroup
            var operationGroup = new AsyncOperationGroup(scenes.Count);

            // Unload regular scenes
            foreach (var scene in scenes)
            {
                var operation = SceneManager.UnloadSceneAsync(scene);
                if (operation == null)
                    continue;

                operationGroup.Operations.Add(operation);

                OnSceneUnloaded.Invoke(scene);
            }

            // Unload addressable scenes
            foreach (var handle in _handleGroup.Handles)
                if (handle.IsValid())
                    Addressables.UnloadSceneAsync(handle);

            _handleGroup.Handles.Clear();

            // Wait until all AsyncOperations in the group are done
            while (!operationGroup.IsDone)
                await Task.Delay(100); // delay to avoid tight loop

            // Optional: UnloadUnusedAssets - unloads all unused assets from memory
            if (UnloadResources)
                await Resources.UnloadUnusedAssets();
        }
    }
}