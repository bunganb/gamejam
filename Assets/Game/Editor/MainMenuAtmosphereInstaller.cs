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
    public static class MainMenuAtmosphereInstaller
    {
        private const string ScenePath = "Assets/Game/Scenes/MainMenu.unity";
        private const string ProfilePath = "Assets/Game/Data/MainMenuAtmosphere.asset";

        [MenuItem("Game Jam/Main Menu/Apply Graveyard Lighting")]
        public static void Install()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Exit Play Mode before applying menu lighting.");
            var scene = SceneManager.GetSceneByPath(ScenePath);
            if (!scene.IsValid() || !scene.isLoaded)
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            var objects = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Transform>(true)).ToArray();
            var altar = objects.FirstOrDefault(t => t.name == "altar new");
            var camera = objects.Select(t => t.GetComponent<Camera>())
                .Where(c => c != null && c.CompareTag("MainCamera"))
                .OrderByDescending(c => c.gameObject.activeInHierarchy)
                .ThenByDescending(c => c.depth)
                .FirstOrDefault();
            if (altar == null || camera == null)
                throw new InvalidOperationException("Main Menu needs altar new and Main Camera.");

            Undo.SetCurrentGroupName("Main Menu graveyard lighting");
            // Decorative chains have very large bounds; use the actual dance floor.
            var renderers = altar.GetComponentsInChildren<Renderer>()
                .Where(r => r.name.StartsWith("Tilemash", StringComparison.OrdinalIgnoreCase)).ToArray();
            if (renderers.Length == 0)
                throw new InvalidOperationException("Cannot aim spotlights: altar has no Tilemash renderers.");
            var bounds = new Bounds(altar.position, Vector3.one);
            if (renderers.Length > 0)
            {
                bounds = renderers[0].bounds;
                foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
            }
            float span = Mathf.Clamp(Mathf.Max(bounds.size.x, bounds.size.z), 2f, 12f);
            Vector3 focus = bounds.center + Vector3.up * 0.7f;
            var root = objects.FirstOrDefault(t => t.name == "MenuAtmosphere");
            if (root == null)
            {
                var created = new GameObject("MenuAtmosphere");
                SceneManager.MoveGameObjectToScene(created, scene);
                Undo.RegisterCreatedObjectUndo(created, "Create menu lighting controls");
                root = created.transform;
            }
            CreateSpot(root, "Altar Cyan Key", altar, focus, new Vector3(-0.6f, 1f, -0.4f) * span,
                new Color(0.45f, 0.85f, 1f), 450f, span, 0f);
            CreateSpot(root, "Altar Magenta Rim", altar, focus, new Vector3(0.65f, 0.85f, 0.35f) * span,
                new Color(0.85f, 0.38f, 0.68f), 320f, span, 2f);

            foreach (var light in objects.Select(t => t.GetComponent<Light>()).Where(l => l != null && l.type == LightType.Directional))
            {
                Undo.RecordObject(light, "Moonlight fill");
                light.color = new Color(0.62f, 0.73f, 1f);
                light.intensity = 0.65f;
            }
            foreach (var particles in objects.Where(t => t.name.StartsWith("fog", StringComparison.OrdinalIgnoreCase))
                         .Select(t => t.GetComponent<ParticleSystem>()).Where(p => p != null))
            {
                Undo.RecordObject(particles, "Cool graveyard fog");
                var main = particles.main;
                // Preserve authored density/alpha and motion; change only the purple tint.
                var color = main.startColor;
                var low = color.colorMin;
                var high = color.colorMax;
                main.startColor = new ParticleSystem.MinMaxGradient(
                    new Color(0.48f, 0.62f, 0.72f, low.a), new Color(0.72f, 0.8f, 0.86f, high.a));
            }

            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(ProfilePath);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<VolumeProfile>();
                AssetDatabase.CreateAsset(profile, ProfilePath);
            }
            var bloom = Override<Bloom>(profile);
            bloom.threshold.Override(1.15f);
            bloom.intensity.Override(0.2f);
            var vignette = Override<Vignette>(profile);
            vignette.intensity.Override(0.16f);
            vignette.smoothness.Override(0.6f);
            var depth = Override<DepthOfField>(profile);
            depth.mode.Override(DepthOfFieldMode.Bokeh);
            float distance = Mathf.Max(0.1f, Vector3.Distance(camera.transform.position, focus));
            depth.focusDistance.Override(distance);
            depth.aperture.Override(3.2f);
            depth.focalLength.Override(45f);
            depth.bladeCount.Override(7);
            depth.active = true;
            var volume = root.GetComponent<Volume>();
            if (volume == null) volume = Undo.AddComponent<Volume>(root.gameObject);
            Undo.RecordObject(volume, "Menu-only post processing");
            volume.isGlobal = true;
            volume.priority = 10f;
            volume.sharedProfile = profile;
            var data = camera.GetUniversalAdditionalCameraData();
            Undo.RecordObject(data, "Enable menu post processing");
            data.renderPostProcessing = true;
            var director = camera.GetComponentInParent<PrototypeCameraDirector>();
            if (director != null)
            {
                const string motionPath = "Assets/Game/Data/MainMenuCameraMotion.asset";
                var serializedDirector = new SerializedObject(director);
                var original = serializedDirector.FindProperty("profile").objectReferenceValue as CameraMotionProfile;
                var motion = AssetDatabase.LoadAssetAtPath<CameraMotionProfile>(motionPath);
                if (motion == null && original != null)
                {
                    motion = UnityEngine.Object.Instantiate(original);
                    AssetDatabase.CreateAsset(motion, motionPath);
                }
                if (motion != null)
                {
                    var settings = new SerializedObject(motion);
                    settings.FindProperty("idlePositionAmplitude").vector3Value = new Vector3(0.09f, 0f, 0.045f);
                    settings.FindProperty("idleRotationAmplitude").vector3Value = new Vector3(0.1f, 0.18f, 0.06f);
                    settings.FindProperty("idleFrequency").floatValue = 0.09f;
                    settings.FindProperty("transformDamping").floatValue = 0.45f;
                    settings.ApplyModifiedProperties();
                    serializedDirector.FindProperty("profile").objectReferenceValue = motion;
                    serializedDirector.ApplyModifiedProperties();
                }
            }
            EditorUtility.SetDirty(profile);
            EditorSceneManager.MarkSceneDirty(scene);
            AssetDatabase.SaveAssets();
            // Keep the scene dirty for visual review, preserving other unsaved scene work.
            Debug.Log("Menu atmosphere applied. Review in Play Mode; save MainMenu when satisfied. Optional DoF is disabled by default.");
        }

        private static T Override<T>(VolumeProfile profile) where T : VolumeComponent
        {
            if (profile.TryGet<T>(out var value)) return value;
            value = profile.Add<T>(false);
            AssetDatabase.AddObjectToAsset(value, profile);
            return value;
        }

        private static void CreateSpot(Transform root, string name, Transform focusTransform, Vector3 focus,
            Vector3 offset, Color color, float intensity, float span, float phase)
        {
            var child = root.Find(name);
            if (child == null)
            {
                var created = new GameObject(name);
                Undo.RegisterCreatedObjectUndo(created, "Create spotlight");
                created.transform.SetParent(root, false);
                child = created.transform;
            }
            Undo.RecordObject(child, "Aim spotlight");
            child.position = focus + offset;
            child.LookAt(focus);
            var light = child.GetComponent<Light>();
            if (light == null) light = Undo.AddComponent<Light>(child.gameObject);
            Undo.RecordObject(light, "Configure spotlight");
            light.type = LightType.Spot;
            light.enabled = true;
            light.lightmapBakeType = LightmapBakeType.Realtime;
            light.color = color;
            light.intensity = intensity;
            light.range = span * 3f;
            light.spotAngle = 65f;
            light.innerSpotAngle = 30f;
            light.shadows = LightShadows.None;
            var motion = child.GetComponent<MenuSpotlightMotion>();
            if (motion == null) motion = Undo.AddComponent<MenuSpotlightMotion>(child.gameObject);
            Undo.RecordObject(motion, "Configure gentle light sweep");
            motion.focus = focusTransform;
            motion.focusOffset = focus - focusTransform.position;
            motion.sweepRadius = span * 0.06f;
            motion.intensity = intensity;
            motion.phase = phase;
        }
    }
}
