using System;
using System.Linq;
using UnityEngine;

namespace UnityEssentials
{
    /// <summary>
    /// Represents a group of scenes, categorized by their types, that can be used for scene management.
    /// </summary>
    /// <remarks>This class is a ScriptableObject designed to store and manage a collection of scenes,  each
    /// associated with a specific <see cref="SceneType"/>. It provides functionality to  retrieve scene names based on
    /// their type.</remarks>
    [Serializable]
    [CreateAssetMenu(fileName = "SceneGroup_", menuName = "SceneLoader/Group", order = 1)]
    public class SceneGroup : ScriptableObject
    {
        public SerializedDictionary<SceneReference, SceneType> Scenes;

        public string FindSceneNameByType(SceneType sceneType) =>
            Scenes.FirstOrDefault(scene => scene.Value == sceneType).Key.Name;
    }

    /// <summary>
    /// Represents the type of a scene in an application or game.
    /// </summary>
    /// <remarks>This enumeration is used to categorize scenes based on their purpose or functionality. Common
    /// scene types include active gameplay, menus, user interfaces, and cinematic sequences.</remarks>
    public enum SceneType { ActiveScene, MainMenu, UserInterface, HUD, Cinematic, Environment, Tooling }
}