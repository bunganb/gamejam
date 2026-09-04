using System;
using System.Collections.Generic;
using GameJam.Gameplay;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GameJam.Editor
{
    public static class McModelInstaller
    {
        private const string ScenePath = "Assets/Game/Scenes/GameplayPrototype.unity";
        private const string ModelPath = "Assets/Game/Art/Model/char/full animasi mc.glb";

        [MenuItem("Game Jam/Log MC Animation Clips")]
        public static void LogClips()
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
            var animation = asset != null ? asset.GetComponent<Animation>() : null;
            if (animation == null)
            {
                Debug.LogWarning("MC_CLIPS_MISSING_ANIMATION: " + ModelPath);
                return;
            }

            var names = new List<string>();
            foreach (AnimationState state in animation) names.Add(state.name);
            Debug.Log("MC_CLIPS: " + string.Join(" | ", names));
        }

        [MenuItem("Game Jam/Install MC Character Model")]
        public static void Install()
        {
            var scene = SceneManager.GetSceneByPath(ScenePath);
            var openedAdditively = !scene.IsValid() || !scene.isLoaded;
            if (openedAdditively) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            InstallIntoScene(scene);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            if (openedAdditively) EditorSceneManager.CloseScene(scene, true);
            Debug.Log("MC_MODEL_INSTALL_COMPLETE: full animasi mc installed with Legacy movement and win animation.");
        }

        private static void InstallIntoScene(Scene scene)
        {
            var player = FindTransform(scene, "PrototypePlayer");
            var events = FindComponent<PuzzleGameplayEvents>(scene);
            var controller = FindComponent<PuzzleGameplayController>(scene);
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
            if (player == null || events == null || controller == null || asset == null)
                throw new InvalidOperationException("MC install requires PrototypePlayer, gameplay events/controller, and full animasi mc.glb.");

            RemoveExistingVisuals(player);
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(asset, scene);
            instance.name = "MC_CharacterModel";
            instance.transform.SetParent(player, false);
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one;

            var animation = instance.GetComponent<Animation>() ?? instance.GetComponentInChildren<Animation>(true);
            if (animation == null) throw new InvalidOperationException("full animasi mc.glb has no Legacy Animation component.");
            var clips = ResolveClipNames(animation);
            var presenter = instance.GetComponent<McLegacyAnimationPresenter>() ?? instance.AddComponent<McLegacyAnimationPresenter>();
            presenter.ConfigureReferences(animation, events);
            presenter.ConfigureClipNames(clips[0], clips[1], clips[2]);
            EditorUtility.SetDirty(presenter);
        }

        private static string[] ResolveClipNames(Animation animation)
        {
            var names = new List<string>();
            foreach (AnimationState state in animation) names.Add(state.name);
            if (names.Count < 3) throw new InvalidOperationException("MC model needs at least idle, movement, and win clips.");

            // Prefer the authored names from the recovered implementation.
            var idle = FirstExisting(animation, "IDLE", "idle");
            var jump = FirstExisting(animation, "LOMPAT", "lompat", names[1]);
            var win = FirstExisting(animation, "dance", "DANCE", names[names.Count - 1]);
            return new[] { idle, jump, win };
        }

        private static string FirstExisting(Animation animation, params string[] candidates)
        {
            foreach (var candidate in candidates)
                if (!string.IsNullOrWhiteSpace(candidate) && animation[candidate] != null) return candidate;
            throw new InvalidOperationException("Missing MC animation clip. Tried: " + string.Join(", ", candidates));
        }

        private static void RemoveExistingVisuals(Transform player)
        {
            for (var i = player.childCount - 1; i >= 0; i--)
            {
                var child = player.GetChild(i);
                if (!string.Equals(child.name, "Shadow", StringComparison.Ordinal))
                    UnityEngine.Object.DestroyImmediate(child.gameObject);
            }
        }

        private static Transform FindTransform(Scene scene, string name)
        {
            foreach (var root in scene.GetRootGameObjects())
                foreach (var child in root.GetComponentsInChildren<Transform>(true))
                    if (child.name == name) return child;
            return null;
        }

        private static T FindComponent<T>(Scene scene) where T : Component
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                var component = root.GetComponentInChildren<T>(true);
                if (component != null) return component;
            }
            return null;
        }
    }
}
