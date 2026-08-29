using System;
using System.Collections.Generic;
using GameJam.Gameplay;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace GameJam.Editor
{
    public static class Issue11StageReactionInstaller
    {
        private const string ScenePath = "Assets/Game/Scenes/GameplayPrototype.unity";
        private const string ProfilePath = "Assets/Game/Data/StageReactionProfile_Prototype.asset";
        private const string VolumeProfilePath = "Assets/Game/Data/StageReactionVolumeProfile_Prototype.asset";
        private const string MaterialFolder = "Assets/Game/Art/Prototype/Materials";
        private const string RingMaterialPath = MaterialFolder + "/M_Prototype_StageRingReaction.mat";
        private const string AudienceMaterialPath = MaterialFolder + "/M_Prototype_AudienceReaction.mat";
        private const string DarkMaterialPath = MaterialFolder + "/M_Prototype_Dark.mat";

        [MenuItem("Game Jam/Install Issue 11 Stage Reaction")]
        public static void Install()
        {
            EnsureAssets(out var reactionProfile, out var volumeProfile, out var ringMaterial, out var audienceMaterial);
            var scene = SceneManager.GetSceneByPath(ScenePath);
            var openedAdditively = !scene.IsValid() || !scene.isLoaded;
            if (openedAdditively)
            {
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            }

            InstallIntoLoadedScene(scene, reactionProfile, volumeProfile, ringMaterial, audienceMaterial);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();

            if (openedAdditively)
            {
                EditorSceneManager.CloseScene(scene, true);
            }

            Debug.Log("ISSUE_11_STAGE_REACTION_INSTALL_COMPLETE: ring, audience placeholders, Bloom, and four Full Groove spotlights installed.");
        }

        public static void InstallIntoLoadedScene(Scene scene)
        {
            EnsureAssets(out var reactionProfile, out var volumeProfile, out var ringMaterial, out var audienceMaterial);
            InstallIntoLoadedScene(scene, reactionProfile, volumeProfile, ringMaterial, audienceMaterial);
        }

        private static void InstallIntoLoadedScene(
            Scene scene,
            StageReactionProfile reactionProfile,
            VolumeProfile volumeProfile,
            Material ringMaterial,
            Material audienceMaterial)
        {
            var systems = FindTransform(scene, "Systems");
            var lighting = FindTransform(scene, "Lighting");
            var ringRoot = FindTransform(scene, "StageLightLine");
            var audienceRoot = FindTransform(scene, "AudienceAnchors");
            var eventHub = FindComponent<PuzzleGameplayEvents>(scene);
            var mainCamera = FindComponent<Camera>(scene);
            if (systems == null || lighting == null || ringRoot == null || audienceRoot == null || eventHub == null || mainCamera == null)
            {
                throw new InvalidOperationException("Issue 11 requires Systems, Lighting, StageLightLine, AudienceAnchors, PuzzleGameplayEvents, and Main Camera.");
            }

            var stageRing = GetOrAdd<StageRingPresenter>(ringRoot.gameObject);
            var orderedSegments = new[]
            {
                FindDirectRenderer(ringRoot, "North"),
                FindDirectRenderer(ringRoot, "East"),
                FindDirectRenderer(ringRoot, "South"),
                FindDirectRenderer(ringRoot, "West")
            };
            foreach (var segment in orderedSegments)
            {
                if (segment != null)
                {
                    segment.sharedMaterial = ringMaterial;
                    EditorUtility.SetDirty(segment);
                }
            }
            stageRing.ConfigureReferences(orderedSegments);

            var audienceMembers = CreateOrCollectAudience(audienceRoot, audienceMaterial);
            var audience = GetOrAdd<AudienceReactionPresenter>(audienceRoot.gameObject);
            audience.ConfigureReferences(audienceMembers, reactionProfile);

            var spotlightRig = CreateOrUpdateSpotlights(lighting, reactionProfile);
            var reactionVolume = CreateOrUpdateReactionVolume(lighting, volumeProfile);
            mainCamera.GetUniversalAdditionalCameraData().renderPostProcessing = true;

            var director = GetOrAdd<StageReactionDirector>(systems.gameObject);
            director.ConfigureReferences(eventHub, reactionProfile, stageRing, audience, spotlightRig, reactionVolume);

            EditorUtility.SetDirty(stageRing);
            EditorUtility.SetDirty(audience);
            EditorUtility.SetDirty(spotlightRig);
            EditorUtility.SetDirty(director);
            EditorUtility.SetDirty(mainCamera);
        }

        private static void EnsureAssets(
            out StageReactionProfile reactionProfile,
            out VolumeProfile volumeProfile,
            out Material ringMaterial,
            out Material audienceMaterial)
        {
            reactionProfile = AssetDatabase.LoadAssetAtPath<StageReactionProfile>(ProfilePath);
            if (reactionProfile == null)
            {
                reactionProfile = ScriptableObject.CreateInstance<StageReactionProfile>();
                AssetDatabase.CreateAsset(reactionProfile, ProfilePath);
            }
            reactionProfile.ApplyPrototypeDefaults();

            volumeProfile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(VolumeProfilePath);
            if (volumeProfile == null)
            {
                volumeProfile = ScriptableObject.CreateInstance<VolumeProfile>();
                AssetDatabase.CreateAsset(volumeProfile, VolumeProfilePath);
            }

            if (!volumeProfile.TryGet<Bloom>(out var bloom))
            {
                bloom = volumeProfile.Add<Bloom>(true);
            }
            bloom.active = true;
            bloom.threshold.Override(1f);
            bloom.intensity.Override(reactionProfile.BaselineBloom);
            bloom.scatter.Override(0.55f);
            bloom.highQualityFiltering.Override(true);

            if (!volumeProfile.TryGet<ColorAdjustments>(out var colorAdjustments))
            {
                colorAdjustments = volumeProfile.Add<ColorAdjustments>(true);
            }
            colorAdjustments.active = true;
            colorAdjustments.colorFilter.Override(Color.white);

            ringMaterial = EnsureMaterial(RingMaterialPath, "GameJam/Stage Ring Reaction");
            audienceMaterial = EnsureMaterial(AudienceMaterialPath, "GameJam/Audience Reaction");
            EditorUtility.SetDirty(reactionProfile);
            EditorUtility.SetDirty(volumeProfile);
        }

        private static Material EnsureMaterial(string path, string shaderName)
        {
            var shader = Shader.Find(shaderName);
            if (shader == null)
            {
                throw new InvalidOperationException($"Shader '{shaderName}' was not imported.");
            }

            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(shader) { name = System.IO.Path.GetFileNameWithoutExtension(path) };
                AssetDatabase.CreateAsset(material, path);
            }
            else if (material.shader != shader)
            {
                material.shader = shader;
            }

            EditorUtility.SetDirty(material);
            return material;
        }

        private static Transform[] CreateOrCollectAudience(Transform root, Material material)
        {
            var members = new List<Transform>();
            for (var index = 0; index < root.childCount; index++)
            {
                var anchor = root.GetChild(index);
                Transform member;
                if (anchor.childCount == 0)
                {
                    member = CreateChild(anchor, "AudienceGhostPlaceholder");
                    CreateAudiencePlaceholder(member, material);
                }
                else
                {
                    member = anchor.GetChild(0);
                }

                var worldTarget = new Vector3(0f, member.position.y, 0f);
                if ((worldTarget - member.position).sqrMagnitude > 0.001f)
                {
                    member.LookAt(worldTarget);
                }
                members.Add(member);
            }

            return members.ToArray();
        }

        private static void CreateAudiencePlaceholder(Transform member, Material material)
        {
            var body = CreatePrimitiveChild(member, "Body", PrimitiveType.Capsule, material);
            body.transform.localPosition = new Vector3(0f, 0.58f, 0f);
            body.transform.localScale = new Vector3(0.52f, 0.52f, 0.52f);

            var head = CreatePrimitiveChild(member, "Head", PrimitiveType.Sphere, material);
            head.transform.localPosition = new Vector3(0f, 1.25f, 0f);
            head.transform.localScale = new Vector3(0.72f, 0.62f, 0.68f);

            var darkMaterial = AssetDatabase.LoadAssetAtPath<Material>(DarkMaterialPath);
            var leftEye = CreatePrimitiveChild(head.transform, "Eye_L", PrimitiveType.Sphere, darkMaterial);
            leftEye.transform.localPosition = new Vector3(-0.22f, 0.08f, 0.43f);
            leftEye.transform.localScale = Vector3.one * 0.12f;
            var rightEye = CreatePrimitiveChild(head.transform, "Eye_R", PrimitiveType.Sphere, darkMaterial);
            rightEye.transform.localPosition = new Vector3(0.22f, 0.08f, 0.43f);
            rightEye.transform.localScale = Vector3.one * 0.12f;
        }

        private static DiscoSpotlightRig CreateOrUpdateSpotlights(Transform lighting, StageReactionProfile profile)
        {
            var root = lighting.Find("DiscoSpotlights") ?? CreateChild(lighting, "DiscoSpotlights");
            var positions = new[]
            {
                new Vector3(-4.7f, 5.8f, -4.2f),
                new Vector3(4.7f, 5.8f, -4.2f),
                new Vector3(4.7f, 5.8f, 4.2f),
                new Vector3(-4.7f, 5.8f, 4.2f)
            };
            var darkMaterial = AssetDatabase.LoadAssetAtPath<Material>(DarkMaterialPath);
            var pans = new Transform[positions.Length];
            var tilts = new Transform[positions.Length];
            var lights = new Light[positions.Length];

            for (var index = 0; index < positions.Length; index++)
            {
                var rig = root.Find($"SpotRig_{index + 1:00}") ?? CreateChild(root, $"SpotRig_{index + 1:00}");
                rig.localPosition = positions[index];
                var pan = rig.Find("PanPivot") ?? CreateChild(rig, "PanPivot");
                var tilt = pan.Find("TiltPivot") ?? CreateChild(pan, "TiltPivot");
                AimPivotsAtStage(pan, tilt, positions[index]);

                var lightTransform = tilt.Find("SpotLight") ?? CreateChild(tilt, "SpotLight");
                var spotlight = GetOrAdd<Light>(lightTransform.gameObject);
                spotlight.type = LightType.Spot;
                spotlight.range = profile.SpotlightRange;
                spotlight.spotAngle = profile.SpotlightOuterAngle;
                spotlight.innerSpotAngle = profile.SpotlightInnerAngle;
                spotlight.intensity = 0f;
                spotlight.shadows = LightShadows.None;
                spotlight.enabled = false;

                if (tilt.Find("SpotlightHousing") == null)
                {
                    var housing = CreatePrimitiveChild(tilt, "SpotlightHousing", PrimitiveType.Cylinder, darkMaterial);
                    housing.transform.localPosition = new Vector3(0f, 0f, -0.10f);
                    housing.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                    housing.transform.localScale = new Vector3(0.18f, 0.28f, 0.18f);
                }
                if (tilt.Find("BeamVisual") == null)
                {
                    CreateChild(tilt, "BeamVisual");
                }

                pans[index] = pan;
                tilts[index] = tilt;
                lights[index] = spotlight;
            }

            var controller = GetOrAdd<DiscoSpotlightRig>(root.gameObject);
            controller.ConfigureReferences(pans, tilts, lights, profile);
            return controller;
        }

        private static void AimPivotsAtStage(Transform pan, Transform tilt, Vector3 position)
        {
            var direction = -position;
            var yaw = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
            var horizontal = new Vector2(direction.x, direction.z).magnitude;
            var pitch = Mathf.Atan2(-direction.y, horizontal) * Mathf.Rad2Deg;
            pan.localRotation = Quaternion.Euler(0f, yaw, 0f);
            tilt.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        }

        private static Volume CreateOrUpdateReactionVolume(Transform lighting, VolumeProfile profile)
        {
            var volumeTransform = lighting.Find("StageReactionVolume") ?? CreateChild(lighting, "StageReactionVolume");
            var volume = GetOrAdd<Volume>(volumeTransform.gameObject);
            volume.isGlobal = true;
            volume.priority = 10f;
            volume.weight = 1f;
            volume.sharedProfile = profile;
            return volume;
        }

        private static Renderer FindDirectRenderer(Transform parent, string childName)
        {
            return parent.Find(childName)?.GetComponent<Renderer>();
        }

        private static Transform FindTransform(Scene scene, string name)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var child in root.GetComponentsInChildren<Transform>(true))
                {
                    if (child.name == name)
                    {
                        return child;
                    }
                }
            }
            return null;
        }

        private static T FindComponent<T>(Scene scene) where T : Component
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                var component = root.GetComponentInChildren<T>(true);
                if (component != null)
                {
                    return component;
                }
            }
            return null;
        }

        private static T GetOrAdd<T>(GameObject target) where T : Component
        {
            var component = target.GetComponent<T>();
            return component != null ? component : target.AddComponent<T>();
        }

        private static Transform CreateChild(Transform parent, string name)
        {
            var child = new GameObject(name).transform;
            child.SetParent(parent, false);
            return child;
        }

        private static GameObject CreatePrimitiveChild(Transform parent, string name, PrimitiveType type, Material material)
        {
            var child = GameObject.CreatePrimitive(type);
            child.name = name;
            child.transform.SetParent(parent, false);
            var collider = child.GetComponent<Collider>();
            if (collider != null)
            {
                UnityEngine.Object.DestroyImmediate(collider);
            }
            var targetRenderer = child.GetComponent<Renderer>();
            if (targetRenderer != null)
            {
                targetRenderer.sharedMaterial = material;
            }
            return child;
        }
    }
}
