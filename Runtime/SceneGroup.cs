using System;
using System.Linq;
using UnityEngine;

namespace UnityEssentials
{
    [Serializable]

    [CreateAssetMenu(fileName = "SceneGroup_", menuName = "SceneLoader/Group", order = 1)]
    public class SceneGroup : ScriptableObject
    {
        public SerializedDictionary<SceneReference, SceneType> Scenes;

        public string FindSceneNameByType(SceneType sceneType) =>
            Scenes.FirstOrDefault(scene => scene.Value == sceneType).Key.Name;
    }

    public enum SceneType { ActiveScene, MainMenu, UserInterface, HUD, Cinematic, Environment, Tooling }
}