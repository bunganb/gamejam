using System;
using System.Collections.Generic;
using GameJam.Gameplay;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GameJam.Editor
{
    public static class AudienceModelInstaller
    {
        private const string ScenePath = "Assets/Game/Scenes/GameplayPrototype.unity";
        private const string CharFolder = "Assets/Game/Art/Model/char/";
        private const string RootName = "AudienceAnchors";

        private static readonly string[] ModelPaths =
        {
            CharFolder + "full animasi penonton 1 new.glb",
            CharFolder + "full animasi penonton 2 new.glb",
            CharFolder + "full animasi penonton 3 new.glb",
            CharFolder + "full animasi penonton 4 new.glb"
        };

        // Names are intentionally kept as authored in the GLB files.
        private static readonly string[,] Clips =
        {
            { "idle", "dance_santuyy", "dance_santai" },
            { "IDLE", "dance_biasa", "dance_brutalllll" },
            { "diem", "dance_biasa", "dance_brutal" },
            { "diam", "dance_1", "dance_2" }
        };

        [MenuItem("Game Jam/Install Audience Character Models")]
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
            Debug.Log("AUDIENCE_MODEL_INSTALL_COMPLETE: 9 anchors use four Legacy animation character types.");
        }

        [MenuItem("Game Jam/Log Audience Animation Clips")]
        public static void LogAnimationClips()
        {
            foreach (var path in ModelPaths)
            {
                var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                var animation = asset != null ? asset.GetComponent<Animation>() : null;
                if (animation == null)
                {
                    Debug.LogWarning("AUDIENCE_CLIPS_MISSING_ANIMATION: " + path);
                    continue;
                }

                var names = new List<string>();
                foreach (AnimationState state in animation) names.Add(state.name);
                Debug.Log("AUDIENCE_CLIPS " + path + ": " + string.Join(" | ", names));
            }
        }

        private static void InstallIntoScene(Scene scene)
        {
            var root = FindTransform(scene, RootName);
            var events = FindComponent<PuzzleGameplayEvents>(scene);
            if (root == null || events == null) throw new InvalidOperationException("AudienceAnchors or PuzzleGameplayEvents missing.");

            var models = new GameObject[ModelPaths.Length];
            for (var i = 0; i < ModelPaths.Length; i++)
            {
                models[i] = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPaths[i]);
                if (models[i] == null) throw new InvalidOperationException("Missing audience model: " + ModelPaths[i]);
            }

            RemoveLooseImportedCharacters(scene);
            var members = new List<Transform>();
            for (var i = 0; i < root.childCount; i++)
            {
                var anchor = root.GetChild(i);
                if (!anchor.name.StartsWith("AudienceAnchor_", StringComparison.Ordinal)) continue;
                RemoveChildren(anchor);
                var type = i % models.Length;
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(models[type], scene);
                instance.name = "AudienceModel_" + (type + 1).ToString("00") + "_" + anchor.name;
                var modelTransform = instance.transform;
                modelTransform.SetParent(anchor, false);
                modelTransform.localPosition = Vector3.zero;
                modelTransform.localRotation = Quaternion.identity;
                modelTransform.localScale = Vector3.one;
                FaceBoard(modelTransform);

                var animation = instance.GetComponent<Animation>() ?? instance.GetComponentInChildren<Animation>(true);
                if (animation == null) throw new InvalidOperationException(instance.name + " has no Legacy Animation component.");
                for (var clipIndex = 0; clipIndex < 3; clipIndex++)
                    if (animation[Clips[type, clipIndex]] == null)
                        throw new InvalidOperationException(instance.name + " is missing Legacy clip '" + Clips[type, clipIndex] + "'.");
                var presenter = instance.GetComponent<AudienceLegacyAnimationPresenter>() ?? instance.AddComponent<AudienceLegacyAnimationPresenter>();
                presenter.Configure(animation, events, Clips[type, 0], Clips[type, 1], Clips[type, 2]);
                members.Add(modelTransform);
            }

            var audience = root.GetComponent<AudienceReactionPresenter>();
            if (audience != null) audience.ConfigureReferences(members.ToArray(), FindAsset<StageReactionProfile>("Assets/Game/Data/StageReactionProfile_Prototype.asset"));
        }

        private static void RemoveChildren(Transform parent)
        {
            for (var i = parent.childCount - 1; i >= 0; i--) UnityEngine.Object.DestroyImmediate(parent.GetChild(i).gameObject);
        }

        private static void RemoveLooseImportedCharacters(Scene scene)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                if (root.name.StartsWith("full animasi penonton ", StringComparison.Ordinal))
                    UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static void FaceBoard(Transform model)
        {
            var target = new Vector3(0f, model.position.y, 0f);
            if ((target - model.position).sqrMagnitude > 0.001f) model.LookAt(target);
        }

        private static T FindAsset<T>(string path) where T : UnityEngine.Object => AssetDatabase.LoadAssetAtPath<T>(path);

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
