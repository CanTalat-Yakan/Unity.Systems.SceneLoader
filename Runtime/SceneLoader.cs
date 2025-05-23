using System;
using System.Threading.Tasks;
using UnityEngine;

namespace UnityEssentials
{
    /// <summary>
    /// Represents a mechanism for reporting and handling progress updates as a percentage.
    /// </summary>
    /// <remarks>This class implements <see cref="IProgress{T}"/> to provide progress updates as a
    /// floating-point value. The progress value is normalized by a predefined ratio before being reported.</remarks>
    public class LoadingProgress : IProgress<float>
    {
        public event Action<float> Progressed;

        private const float Ratio = 1f;

        public void Report(float value) =>
            Progressed?.Invoke(value / Ratio);
    }

    /// <summary>
    /// Provides functionality for managing and loading scene groups asynchronously in a Unity application.
    /// </summary>
    /// <remarks>The <see cref="SceneLoader"/> class is responsible for handling the loading and unloading of
    /// scene groups, tracking progress, and optionally logging scene-related events. It provides properties to monitor
    /// the loading state and progress, as well as methods to initiate and manage scene group loading
    /// operations.</remarks>
    public class SceneLoader : MonoBehaviour
    {
        [SerializeField] private bool _logMessages;
        [SerializeField] private SceneGroup _sceneGroup;

        public float TargetProgress => _targetProgress;
        private float _targetProgress;

        public bool IsLoading => _isLoading;
        private bool _isLoading;

        public float SmoothProgress => _smoothProgress;
        private float _smoothProgress;

        public float SmoothProgressSpeed { get; set; } = 0.5f;

        public readonly SceneGroupManager Manager = new();

        /// <summary>
        /// Initializes the component and subscribes to scene-related events for logging purposes.
        /// </summary>
        /// <remarks>This method sets up event handlers for scene load and unload events if logging is
        /// enabled. The event handlers log messages to the Unity console when scenes are loaded, unloaded, or when a
        /// scene group is loaded.</remarks>
        public void Awake()
        {
            if (_logMessages)
            {
                Manager.OnSceneLoaded += (sceneName, sceneState) => Debug.Log("Loaded: " + sceneName + $" [{sceneState}]");
                Manager.OnSceneUnloaded += (sceneName) => Debug.Log("Unloaded: " + sceneName);
                Manager.OnSceneGroupLoaded += () => Debug.Log("Scene group loaded");
            }
        }

        /// <summary>
        /// Initiates the asynchronous loading of a scene group.
        /// </summary>
        /// <remarks>This method starts the process of loading a group of scenes asynchronously.  Callers
        /// should be aware that this method does not block the calling thread  and any exceptions during the loading
        /// process will not be propagated to the caller.</remarks>
        public async void Start() =>
            await LoadSceneGroup();

        /// <summary>
        /// Updates the smooth progress value towards the target progress.
        /// </summary>
        /// <remarks>This method should be called periodically to animate the progress value smoothly.  It
        /// adjusts the smooth progress based on the difference between the current and target progress,  using a
        /// dynamically calculated speed factor.</remarks>
        public void Update()
        {
            if (!IsLoading)
                return;

            float currentFillAmount = SmoothProgress;
            float progressDifference = Mathf.Abs(currentFillAmount - TargetProgress);

            float dynamicFillSpeed = progressDifference * SmoothProgressSpeed;

            _smoothProgress = Mathf.Lerp(currentFillAmount, TargetProgress, Time.deltaTime * dynamicFillSpeed);
        }

        /// <summary>
        /// Asynchronously loads a group of scenes specified by the given <see cref="SceneGroup"/>.
        /// </summary>
        /// <remarks>If no <see cref="SceneGroup"/> is provided and no previously assigned scene group
        /// exists, the method will return without performing any operation. The method updates the loading progress
        /// dynamically and ensures that the loading process is completed before returning.</remarks>
        /// <param name="sceneGroup">The <see cref="SceneGroup"/> to load. If null, the previously assigned scene group will be used.</param>
        /// <returns>A <see cref="Task"/> that represents the asynchronous operation. The task completes when the scene group has
        /// been fully loaded.</returns>
        public async Task LoadSceneGroup(SceneGroup sceneGroup = null)
        {
            if (sceneGroup != null)
                _sceneGroup = sceneGroup;

            if (_sceneGroup == null)
                return;

            _targetProgress = 1f;

            LoadingProgress progress = new();
            progress.Progressed += (target) => _targetProgress = Mathf.Max(target, TargetProgress);

            _isLoading = true;
            await Manager.LoadScenes(_sceneGroup, progress);
            _isLoading = false;
        }
    }
}