using System;
using System.Threading.Tasks;
using UnityEngine;

namespace UnityEssentials
{
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

        public void Awake()
        {
            if (_logMessages)
            {
                Manager.OnSceneLoaded += (sceneName, sceneState) => Debug.Log("Loaded: " + sceneName + $" [{sceneState}]");
                Manager.OnSceneUnloaded += (sceneName) => Debug.Log("Unloaded: " + sceneName);
                Manager.OnSceneGroupLoaded += () => Debug.Log("Scene group loaded");
            }
        }

        public async void Start() =>
            await LoadSceneGroup();

        public void Update()
        {
            if (!IsLoading)
                return;

            float currentFillAmount = SmoothProgress;
            float progressDifference = Mathf.Abs(currentFillAmount - TargetProgress);

            float dynamicFillSpeed = progressDifference * SmoothProgressSpeed;

            _smoothProgress = Mathf.Lerp(currentFillAmount, TargetProgress, Time.deltaTime * dynamicFillSpeed);
        }

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

    public class LoadingProgress : IProgress<float>
    {
        public event Action<float> Progressed;

        private const float c_ratio = 1f;

        public void Report(float value) =>
            Progressed?.Invoke(value / c_ratio);
    }
}