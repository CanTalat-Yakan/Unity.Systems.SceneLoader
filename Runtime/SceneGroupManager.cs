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
    public class SceneGroupManager
    {
        public event Action<string, string> OnSceneLoaded = delegate { };
        public event Action<string> OnSceneUnloaded = delegate { };
        public event Action OnSceneGroupLoaded = delegate { };

        public SceneGroup ActiveSceneGroup;

        private readonly AsyncOperationHandleGroup _handleGroup = new(10);

        private string _bootSceneName = "Boot";
        private bool _unloadResources = false;

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
                if (sceneName.Equals(activeScene) || sceneName == _bootSceneName)
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
            if (_unloadResources)
                await Resources.UnloadUnusedAssets();
        }
    }

    public readonly struct AsyncOperationGroup
    {
        public readonly List<AsyncOperation> Operations;

        public float Progress => Operations.Count == 0 ? 0 : Operations.Average(operation => operation.progress);
        public bool IsDone => Operations.All(operation => operation.isDone);

        public AsyncOperationGroup(int initialCapacity) =>
            Operations = new(initialCapacity);
    }

    public readonly struct AsyncOperationHandleGroup
    {
        public readonly List<AsyncOperationHandle<SceneInstance>> Handles;

        public float Progress => Handles.Count == 0 ? 0 : Handles.Average(handle => handle.PercentComplete);
        public bool IsDone => Handles.Count == 0 || Handles.All(operation => operation.IsDone);

        public AsyncOperationHandleGroup(int initialCapacity) =>
            Handles = new(initialCapacity);
    }
}