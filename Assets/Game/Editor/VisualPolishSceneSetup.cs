using System;
using GameJam.Gameplay;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace GameJam.Editor
{
    public static class VisualPolishSceneSetup
    {
        private const string GameplayScenePath = "Assets/Game/Scenes/GameplayPrototype.unity";

        public static void ApplyFromCommandLine()
        {
            var scene = EditorSceneManager.OpenScene(GameplayScenePath, OpenSceneMode.Single);
            Apply();
            EditorSceneManager.SaveScene(scene);
        }

        [MenuItem("Game Jam/Setup Gameplay Visual Polish")]
        public static string Apply()
        {
            var scene = EditorSceneManager.GetActiveScene();
            if (scene.path != GameplayScenePath)
            {
                throw new InvalidOperationException("Open GameplayPrototype before applying visual polish.");
            }

            var reactionProfile = AssetDatabase.LoadAssetAtPath<StageReactionProfile>(
                "Assets/Game/Data/StageReactionProfile_Prototype.asset");
            if (reactionProfile == null)
            {
                throw new InvalidOperationException("Missing StageReactionProfile_Prototype asset.");
            }
            reactionProfile.ApplyPrototypeDefaults();
            EditorUtility.SetDirty(reactionProfile);

            var reactionVolumeProfile = LoadOrCreateProfile(
                "Assets/Game/Data/StageReactionVolumeProfile_Prototype.asset");
            RemoveMissingComponents(reactionVolumeProfile);
            var reactionBloom = AddVolumeComponent<Bloom>(reactionVolumeProfile);
            reactionBloom.threshold.Override(0.85f);
            reactionBloom.intensity.Override(reactionProfile.BaselineBloom);
            reactionBloom.scatter.Override(0.6f);
            reactionBloom.highQualityFiltering.Override(true);
            var reactionColor = AddVolumeComponent<ColorAdjustments>(reactionVolumeProfile);
            reactionColor.colorFilter.Override(Color.white);
            EditorUtility.SetDirty(reactionVolumeProfile);

            var gameplayProfile = LoadOrCreateProfile("Assets/Game/Data/GameplayClubVolumeProfile.asset");
            RemoveMissingComponents(gameplayProfile);
            var tonemapping = AddVolumeComponent<Tonemapping>(gameplayProfile);
            tonemapping.mode.Override(TonemappingMode.ACES);
            var bloom = AddVolumeComponent<Bloom>(gameplayProfile);
            bloom.threshold.Override(1f);
            bloom.intensity.Override(0.15f);
            bloom.scatter.Override(0.5f);
            bloom.highQualityFiltering.Override(true);
            var adjustments = AddVolumeComponent<ColorAdjustments>(gameplayProfile);
            adjustments.postExposure.Override(0.15f);
            adjustments.contrast.Override(5f);
            adjustments.saturation.Override(8f);
            adjustments.colorFilter.Override(Color.white);
            var vignette = AddVolumeComponent<Vignette>(gameplayProfile);
            vignette.intensity.Override(0.12f);
            vignette.smoothness.Override(0.25f);
            EditorUtility.SetDirty(gameplayProfile);

            var systems = RequireObject("Systems");
            var audience = systems.GetComponent<AudienceReactionPresenter>();
            if (audience == null)
            {
                audience = Undo.AddComponent<AudienceReactionPresenter>(systems);
            }
            var audienceMembers = new[]
            {
                RequireObject("audience_1").transform,
                RequireObject("full animasi penonton 1 new (1)").transform,
                RequireObject("full animasi penonton 4 new").transform,
                RequireObject("full animasi penonton 3 new").transform
            };
            SetAudienceMembers(audience, audienceMembers);
            SetObjectReference(audience, "profile", reactionProfile);

            var lightingRoot = RequireObject("Lighting").transform;
            var baseLightsObject = GetOrCreateChild(lightingRoot, "StageBaseLights");
            var magentaObject = GetOrCreateChild(baseLightsObject.transform, "MagentaFill");
            var cyanObject = GetOrCreateChild(baseLightsObject.transform, "CyanRim");
            magentaObject.transform.localPosition = new Vector3(-4f, 3.2f, -1.5f);
            cyanObject.transform.localPosition = new Vector3(4f, 3f, 2.2f);
            var magentaLight = magentaObject.GetComponent<Light>() ?? Undo.AddComponent<Light>(magentaObject);
            var cyanLight = cyanObject.GetComponent<Light>() ?? Undo.AddComponent<Light>(cyanObject);
            var baseLighting = baseLightsObject.GetComponent<StageBaseLightingPresenter>()
                ?? Undo.AddComponent<StageBaseLightingPresenter>(baseLightsObject);
            baseLighting.ConfigureReferences(magentaLight, cyanLight, reactionProfile);
            SetFloat(baseLighting, "rimIntensityRatio", 0.65f);
            EditorUtility.SetDirty(baseLighting);

            var director = systems.GetComponent<StageReactionDirector>()
                ?? throw new InvalidOperationException("Systems has no StageReactionDirector.");
            SetObjectReference(director, "audience", audience);
            SetObjectReference(director, "baseLighting", baseLighting);

            var reactionVolume = RequireComponent<Volume>("Lighting/StageReactionVolume");
            reactionVolume.sharedProfile = reactionVolumeProfile;
            EditorUtility.SetDirty(reactionVolume);
            var globalVolume = RequireComponent<Volume>("Lighting/GlobalVolume");
            globalVolume.sharedProfile = gameplayProfile;
            EditorUtility.SetDirty(globalVolume);

            var keyLight = RequireComponent<Light>("Lighting/KeyLight");
            keyLight.color = new Color(0.52f, 0.58f, 1f, 1f);
            keyLight.intensity = 1.35f;
            keyLight.shadows = LightShadows.Soft;
            keyLight.shadowStrength = 0.45f;
            EditorUtility.SetDirty(keyLight);

            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.18f, 0.10f, 0.25f, 1f);
            RenderSettings.ambientEquatorColor = new Color(0.10f, 0.06f, 0.16f, 1f);
            RenderSettings.ambientGroundColor = new Color(0.05f, 0.03f, 0.08f, 1f);
            RenderSettings.ambientIntensity = 1.6f;

            ConfigureTileEmission();
            ConfigureAudienceBaseMaterial();
            ConfigureEnvironmentMaterials();

            var cameraData = RequireComponent<UniversalAdditionalCameraData>("CameraRig/Main Camera");
            cameraData.renderPostProcessing = true;
            cameraData.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            cameraData.antialiasingQuality = AntialiasingQuality.High;
            EditorUtility.SetDirty(cameraData);

            EditorSceneManager.MarkSceneDirty(scene);
            AssetDatabase.SaveAssets();
            return $"Visual polish configured for {audienceMembers.Length} audience members.";
        }

        private static void ConfigureTileEmission()
        {
            var tileSurfaceMaterial = AssetDatabase.LoadAssetAtPath<Material>(
                "Assets/Game/Art/Prototype/Materials/M_Prototype_Emissive.mat");
            if (tileSurfaceMaterial == null)
            {
                throw new InvalidOperationException("Missing M_Prototype_Emissive material.");
            }

            tileSurfaceMaterial.EnableKeyword("_EMISSION");
            tileSurfaceMaterial.globalIlluminationFlags = MaterialGlobalIlluminationFlags.BakedEmissive;
            EditorUtility.SetDirty(tileSurfaceMaterial);
        }

        private static void ConfigureAudienceBaseMaterial()
        {
            var audienceMaterial = AssetDatabase.LoadAssetAtPath<Material>(
                "Assets/Game/3d/Shader/ShaderBaru.mat");
            if (audienceMaterial == null)
            {
                throw new InvalidOperationException("Missing audience ShaderBaru material.");
            }

            if (audienceMaterial.HasProperty("_Color"))
            {
                audienceMaterial.SetColor("_Color", new Color(0.212f, 0.311f, 0.244f, 1f));
            }
            EditorUtility.SetDirty(audienceMaterial);
        }

        private static void ConfigureEnvironmentMaterials()
        {
            ApplyMaterialToRenderers(
                RequireObject("Environment/Stage/StageFloor").GetComponentsInChildren<Renderer>(true),
                "StageFloor",
                new Color(0.18f, 0.12f, 0.25f, 1f),
                0.18f);

            var environmentRoot = RequireObject("env").transform;
            var renderers = environmentRoot.GetComponentsInChildren<Renderer>(true);
            foreach (var renderer in renderers)
            {
                var hierarchyName = BuildRelativeHierarchyName(renderer.transform, environmentRoot);
                if (hierarchyName.Contains("speakergj"))
                {
                    ApplyMaterialToRenderers(
                        new[] { renderer },
                        "Speaker",
                        new Color(0.28f, 0.22f, 0.38f, 1f),
                        0.32f);
                }
                else if (hierarchyName.Contains("grave"))
                {
                    ApplyMaterialToRenderers(
                        new[] { renderer },
                        "Grave",
                        new Color(0.35f, 0.18f, 0.30f, 1f),
                        0.16f);
                }
                else if (hierarchyName.Contains("tembok"))
                {
                    ApplyMaterialToRenderers(
                        new[] { renderer },
                        "Wall",
                        new Color(0.32f, 0.18f, 0.42f, 1f),
                        0.12f);
                }
                else if (hierarchyName.Contains("ground"))
                {
                    ApplyMaterialToRenderers(
                        new[] { renderer },
                        "Ground",
                        new Color(0.28f, 0.18f, 0.38f, 1f),
                        0.08f);
                }
            }
        }

        private static void ApplyMaterialToRenderers(
            Renderer[] renderers,
            string category,
            Color color,
            float smoothness)
        {
            foreach (var renderer in renderers)
            {
                if (renderer == null)
                {
                    continue;
                }

                var materials = renderer.sharedMaterials;
                for (var index = 0; index < materials.Length; index++)
                {
                    var source = materials[index];
                    if (source == null)
                    {
                        continue;
                    }

                    var suffix = materials.Length > 1 ? $"_{index + 1}" : string.Empty;
                    var path = $"Assets/Game/Art/Prototype/Materials/M_Environment_{category}{suffix}.mat";
                    materials[index] = LoadOrCreateMaterialOverride(path, source, color, smoothness);
                }

                Undo.RecordObject(renderer, $"Assign {category} environment material");
                renderer.sharedMaterials = materials;
                EditorUtility.SetDirty(renderer);
            }
        }

        private static Material LoadOrCreateMaterialOverride(
            string path,
            Material source,
            Color color,
            float smoothness)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(source)
                {
                    name = System.IO.Path.GetFileNameWithoutExtension(path)
                };
                AssetDatabase.CreateAsset(material, path);
            }

            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }
            if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", color);
            }
            if (material.HasProperty("_Smoothness"))
            {
                material.SetFloat("_Smoothness", smoothness);
            }
            if (material.HasProperty("_EmissionColor"))
            {
                material.SetColor("_EmissionColor", Color.black);
                material.DisableKeyword("_EMISSION");
            }

            EditorUtility.SetDirty(material);
            return material;
        }

        private static string BuildRelativeHierarchyName(Transform target, Transform root)
        {
            var result = string.Empty;
            for (var current = target; current != null && current != root; current = current.parent)
            {
                result = $"{current.name}/{result}";
            }
            return result.ToLowerInvariant();
        }

        private static T RequireComponent<T>(string path) where T : Component
        {
            var gameObject = RequireObject(path);
            var component = gameObject.GetComponent<T>();
            return component != null ? component : Undo.AddComponent<T>(gameObject);
        }

        private static GameObject RequireObject(string path)
        {
            var gameObject = GameObject.Find(path);
            return gameObject != null
                ? gameObject
                : throw new InvalidOperationException($"Missing scene object: {path}");
        }

        private static GameObject GetOrCreateChild(Transform parent, string name)
        {
            var child = parent.Find(name);
            if (child != null)
            {
                return child.gameObject;
            }

            var gameObject = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(gameObject, $"Create {name}");
            gameObject.transform.SetParent(parent, false);
            return gameObject;
        }

        private static void SetObjectReference(UnityEngine.Object target, string propertyName, UnityEngine.Object value)
        {
            var serializedObject = new SerializedObject(target);
            var property = serializedObject.FindProperty(propertyName)
                ?? throw new InvalidOperationException($"Missing property {propertyName} on {target.name}");
            property.objectReferenceValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetFloat(UnityEngine.Object target, string propertyName, float value)
        {
            var serializedObject = new SerializedObject(target);
            var property = serializedObject.FindProperty(propertyName)
                ?? throw new InvalidOperationException($"Missing property {propertyName} on {target.name}");
            property.floatValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetAudienceMembers(AudienceReactionPresenter presenter, Transform[] members)
        {
            var serializedObject = new SerializedObject(presenter);
            var property = serializedObject.FindProperty("members")
                ?? throw new InvalidOperationException("Audience presenter has no members property.");
            property.arraySize = members.Length;
            for (var index = 0; index < members.Length; index++)
            {
                property.GetArrayElementAtIndex(index).objectReferenceValue = members[index];
            }
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void RemoveMissingComponents(VolumeProfile profile)
        {
            for (var index = profile.components.Count - 1; index >= 0; index--)
            {
                var component = profile.components[index];
                if (component == null)
                {
                    profile.components.RemoveAt(index);
                }
            }
        }

        private static T AddVolumeComponent<T>(VolumeProfile profile) where T : VolumeComponent
        {
            if (profile.TryGet<T>(out var existing) && existing != null && AssetDatabase.Contains(existing))
            {
                return existing;
            }

            var component = profile.Add<T>(false);
            component.hideFlags = HideFlags.HideInHierarchy | HideFlags.HideInInspector;
            AssetDatabase.AddObjectToAsset(component, profile);
            return component;
        }

        private static VolumeProfile LoadOrCreateProfile(string path)
        {
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
            if (profile != null)
            {
                return profile;
            }

            profile = ScriptableObject.CreateInstance<VolumeProfile>();
            profile.name = System.IO.Path.GetFileNameWithoutExtension(path);
            AssetDatabase.CreateAsset(profile, path);
            return profile;
        }
    }
}
