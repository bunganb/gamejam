using System;
using System.Linq;
using GameJam.Gameplay;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace GameJam.Editor
{
    public static class GameplayAtmosphereInstaller
    {
        private const string ScenePath = "Assets/Game/Scenes/GameplayPrototype.unity";
        private const string VolumeProfilePath = "Assets/Game/Data/GameplayClubVolumeProfile.asset";
        private const string LightingProfilePath = "Assets/Game/Data/NightclubLightingProfile_Prototype.asset";

        [MenuItem("Game Jam/Gameplay/Apply Main Menu Atmosphere")]
        public static void Install()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Exit Play Mode before applying gameplay atmosphere.");

            var scene = SceneManager.GetSceneByPath(ScenePath);
            var openedAdditively = !scene.IsValid() || !scene.isLoaded;
            if (openedAdditively)
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);

            try
            {
                var objects = scene.GetRootGameObjects()
                    .SelectMany(root => root.GetComponentsInChildren<Transform>(true)).ToArray();
                var camera = objects.Select(t => t.GetComponent<Camera>())
                    .Where(c => c != null && c.CompareTag("MainCamera"))
                    .OrderByDescending(c => c.gameObject.activeInHierarchy)
                    .ThenByDescending(c => c.depth)
                    .FirstOrDefault();
                var globalVolume = objects.Select(t => t.GetComponent<Volume>())
                    .FirstOrDefault(v => v != null && v.gameObject.name == "GlobalVolume");
                var reactionVolume = objects.Select(t => t.GetComponent<Volume>())
                    .FirstOrDefault(v => v != null && v.gameObject.name == "StageReactionVolume");
                var lightingProfile = AssetDatabase.LoadAssetAtPath<NightclubLightingProfile>(LightingProfilePath);
                var gameplayProfile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(VolumeProfilePath);

                if (camera == null || globalVolume == null || gameplayProfile == null || lightingProfile == null)
                    throw new InvalidOperationException("GameplayPrototype is missing its camera, GlobalVolume, or lighting profiles.");

                Undo.SetCurrentGroupName("Apply Main Menu atmosphere to gameplay");

                Undo.RecordObject(camera.GetUniversalAdditionalCameraData(), "Enable gameplay post processing");
                camera.GetUniversalAdditionalCameraData().renderPostProcessing = true;

                Undo.RecordObject(globalVolume, "Apply nightclub baseline volume");
                globalVolume.isGlobal = true;
                globalVolume.priority = 0f;
                globalVolume.weight = 1f;
                globalVolume.sharedProfile = gameplayProfile;

                if (reactionVolume != null)
                {
                    Undo.RecordObject(reactionVolume, "Preserve gameplay reaction volume");
                    reactionVolume.isGlobal = true;
                    reactionVolume.priority = 10f;
                    reactionVolume.weight = 1f;
                }

                ApplyProfileLook(gameplayProfile);
                ApplyReflectionProbes(objects);
                ApplyMoonlightFill(objects);
                ApplySpotlightTiming(lightingProfile);

                EditorUtility.SetDirty(gameplayProfile);
                EditorUtility.SetDirty(lightingProfile);
                EditorSceneManager.MarkSceneDirty(scene);
                AssetDatabase.SaveAssets();
                EditorSceneManager.SaveScene(scene, ScenePath);
                Debug.Log("GAMEPLAY_ATMOSPHERE_INSTALL_COMPLETE: main menu palette, post processing, probes, fill light, and gradual gameplay spotlight payoff applied.");
            }
            finally
            {
                if (openedAdditively)
                    EditorSceneManager.CloseScene(scene, true);
            }
        }

        private static void ApplyProfileLook(VolumeProfile profile)
        {
            var tonemapping = GetOrAdd<Tonemapping>(profile);
            tonemapping.mode.Override(TonemappingMode.ACES);

            var bloom = GetOrAdd<Bloom>(profile);
            bloom.threshold.Override(1.15f);
            bloom.intensity.Override(0.28f);
            bloom.scatter.Override(0.62f);

            var vignette = GetOrAdd<Vignette>(profile);
            vignette.intensity.Override(0.16f);
            vignette.smoothness.Override(0.6f);

            var color = GetOrAdd<ColorAdjustments>(profile);
            color.postExposure.Override(0f);
            color.contrast.Override(8f);
            color.saturation.Override(10f);
        }

        private static void ApplyReflectionProbes(Transform[] objects)
        {
            foreach (var probe in objects.Select(t => t.GetComponent<ReflectionProbe>()).Where(p => p != null))
            {
                Undo.RecordObject(probe, "Tune gameplay reflection probe");
                probe.mode = ReflectionProbeMode.Baked;
                probe.refreshMode = ReflectionProbeRefreshMode.OnAwake;
                probe.resolution = 128;
                probe.intensity = 1.48f;
                probe.boxProjection = false;
                probe.cullingMask = ~0;
            }
        }

        private static void ApplyMoonlightFill(Transform[] objects)
        {
            foreach (var light in objects.Select(t => t.GetComponent<Light>())
                         .Where(l => l != null && l.type == LightType.Directional))
            {
                Undo.RecordObject(light, "Apply nightclub moonlight fill");
                light.color = new Color(0.62f, 0.73f, 1f);
                light.intensity = 0.65f;
            }
        }

        private static void ApplySpotlightTiming(NightclubLightingProfile profile)
        {
            var serialized = new SerializedObject(profile);
            serialized.FindProperty("spotlightFadeDuration").floatValue = 2.5f;
            serialized.FindProperty("spotlightBeamOpacity").floatValue = 0.14f;
            serialized.FindProperty("spotlightBeamEmission").floatValue = 0.9f;
            serialized.FindProperty("fullGrooveSpotlightIntensity").floatValue = 14f;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static T GetOrAdd<T>(VolumeProfile profile) where T : VolumeComponent
        {
            if (profile.TryGet<T>(out var component))
                return component;
            component = profile.Add<T>(false);
            AssetDatabase.AddObjectToAsset(component, profile);
            return component;
        }
    }
}
