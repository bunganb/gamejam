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
    public static class NightclubEnvironmentInstaller
    {
        private const string ScenePath = "Assets/Game/Scenes/GameplayPrototype.unity";
        private const string PrefabFolder = "Assets/Game/Prefabs/Environment";
        private const string PrefabPath = PrefabFolder + "/NightclubSet.prefab";
        private const string MaterialFolder = "Assets/Game/Art/Nightclub/Materials";
        private const string MeshFolder = "Assets/Game/Art/Nightclub/Meshes";
        private const string SpotlightConePath = MeshFolder + "/SpotlightCone.asset";
        private const string ProfilePath = "Assets/Game/Data/NightclubLightingProfile_Prototype.asset";
        private const string ReactionVolumePath = "Assets/Game/Data/StageReactionVolumeProfile_Prototype.asset";
        private const string GameplayVolumePath = "Assets/Game/Data/GameplayClubVolumeProfile.asset";

        [MenuItem("Game Jam/Install Cyber Nightclub Environment")]
        public static void Install()
        {
            var scene = SceneManager.GetSceneByPath(ScenePath);
            var openedAdditively = !scene.IsValid() || !scene.isLoaded;
            if (openedAdditively) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);

            InstallIntoLoadedScene(scene);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            if (openedAdditively) EditorSceneManager.CloseScene(scene, true);
            Debug.Log("NIGHTCLUB_ENVIRONMENT_INSTALL_COMPLETE");
        }

        public static void ApplyFromCommandLine()
        {
            Install();
        }

        public static void InstallIntoLoadedScene(Scene scene)
        {
            EnsureFolder("Assets/Game/Art/Nightclub");
            EnsureFolder(MaterialFolder);
            EnsureFolder(MeshFolder);
            EnsureFolder(PrefabFolder);

            var profile = LoadOrCreateProfile();
            var materials = CreateMaterials();
            var environment = FindInScene(scene, "Environment")
                ?? throw new InvalidOperationException("GameplayPrototype requires /Environment.");
            var existing = environment.transform.Find("NightclubSet");
            if (existing != null) Undo.DestroyObjectImmediate(existing.gameObject);

            var root = BuildSet(environment.transform, materials, profile, out var manager,
                out var movingLights, out var strobe, out var neonGroups, out var danceFloor);
            var prefab = PrefabUtility.SaveAsPrefabAssetAndConnect(root, PrefabPath, InteractionMode.AutomatedAction);
            if (prefab == null) throw new InvalidOperationException("Failed to create NightclubSet prefab.");

            var music = FindComponent<PrototypeMusicDirector>(scene);
            var reactionVolume = FindNamedComponent<Volume>(scene, "StageReactionVolume");
            manager = root.GetComponent<NightclubLightingManager>();
            manager.ConfigureReferences(profile, music, movingLights, strobe, neonGroups, danceFloor, reactionVolume);

            var director = FindComponent<StageReactionDirector>(scene);
            if (director != null)
            {
                director.ConfigureNightclubLighting(manager);
                SetObjectReference(director, "nightclubLighting", manager);
            }

            var legacyRig = FindComponent<DiscoSpotlightRig>(scene);
            if (legacyRig != null)
            {
                legacyRig.ResetImmediately();
                legacyRig.enabled = false;
                EditorUtility.SetDirty(legacyRig);
            }

            ConfigurePostProcessing();
            ConfigureAmbientLighting();
            EditorUtility.SetDirty(manager);
            EditorUtility.SetDirty(director);
            EditorSceneManager.MarkSceneDirty(scene);
        }

        private static GameObject BuildSet(Transform parent, NightclubMaterials materials,
            NightclubLightingProfile profile, out NightclubLightingManager manager,
            out MovingSpotlightController movingLights, out StrobeLightController strobe,
            out NeonLightController[] neonGroups, out DanceFloorController danceFloor)
        {
            var root = new GameObject("NightclubSet");
            Undo.RegisterCreatedObjectUndo(root, "Create nightclub environment");
            root.transform.SetParent(parent, false);

            var architecture = CreateEmpty(root.transform, "Architecture");
            CreatePart(architecture, "GlossyOuterFloor", PrimitiveType.Cube, new Vector3(0f, -0.42f, 0.4f),
                new Vector3(18f, 0.22f, 18f), materials.Environment, true);
            CreatePart(architecture, "BackWall", PrimitiveType.Cube, new Vector3(0f, 3.2f, 8.8f),
                new Vector3(18f, 7f, 0.35f), materials.Environment, true);
            CreatePart(architecture, "LeftWall", PrimitiveType.Cube, new Vector3(-9f, 3.2f, 0.4f),
                new Vector3(0.35f, 7f, 17f), materials.Environment, true);
            CreatePart(architecture, "RightWall", PrimitiveType.Cube, new Vector3(9f, 3.2f, 0.4f),
                new Vector3(0.35f, 7f, 17f), materials.Environment, true);

            var pillars = CreateEmpty(architecture, "Pillars");
            foreach (var x in new[] { -8f, 8f })
            foreach (var z in new[] { -4.5f, 0.5f, 5.5f })
                CreatePart(pillars, $"Pillar_{x}_{z}", PrimitiveType.Cube, new Vector3(x, 2.6f, z),
                    new Vector3(0.55f, 5.5f, 0.55f), materials.Metal, true);

            var ceiling = CreateEmpty(architecture, "CircularCeiling");
            CreateSegmentedRing(ceiling, "OuterRing", 5.7f, 3.5f, 20, 0.3f, materials.Metal);
            CreateSegmentedRing(ceiling, "InnerRing", 4.45f, 3.5f, 16, 0.1f, materials.NeonBlue);

            var dj = CreateEmpty(architecture, "DJStage");
            CreatePart(dj, "Booth", PrimitiveType.Cube, new Vector3(0f, 0.7f, 6.9f),
                new Vector3(5.2f, 1.4f, 1.25f), materials.Metal, true);
            CreatePart(dj, "BoothFace", PrimitiveType.Cube, new Vector3(0f, 0.85f, 6.23f),
                new Vector3(4.4f, 0.75f, 0.08f), materials.NeonMagenta, true);
            CreatePart(dj, "Deck", PrimitiveType.Cube, new Vector3(0f, 1.48f, 6.55f),
                new Vector3(4.8f, 0.12f, 1.15f), materials.Environment, true);

            var cyan = new List<Renderer>();
            var magenta = new List<Renderer>();
            var blue = new List<Renderer>();
            var green = new List<Renderer>();
            var neon = CreateEmpty(root.transform, "NeonArchitecture");
            AddStrip(neon, "BackCyan", new Vector3(-4.6f, 3.3f, 8.58f), new Vector3(6f, 0.12f, 0.08f), materials.NeonCyan, cyan);
            AddStrip(neon, "BackMagenta", new Vector3(4.6f, 4.4f, 8.58f), new Vector3(6f, 0.12f, 0.08f), materials.NeonMagenta, magenta);
            AddStrip(neon, "LeftBlue", new Vector3(-8.78f, 3.8f, 1.5f), new Vector3(0.08f, 0.12f, 9f), materials.NeonBlue, blue);
            AddStrip(neon, "RightGreen", new Vector3(8.78f, 2.8f, 1.5f), new Vector3(0.08f, 0.12f, 9f), materials.NeonGreen, green);
            for (var i = 0; i < 6; i++)
            {
                var x = -7.5f + i * 3f;
                var list = i % 2 == 0 ? cyan : magenta;
                var mat = i % 2 == 0 ? materials.NeonCyan : materials.NeonMagenta;
                AddStrip(neon, $"BackVertical_{i}", new Vector3(x, 2.6f, 8.55f), new Vector3(0.1f, 3.4f, 0.08f), mat, list);
            }

            var panels = CreateEmpty(root.transform, "EmissivePanels");
            for (var side = -1; side <= 1; side += 2)
            for (var i = 0; i < 3; i++)
            {
                var target = i == 0 ? cyan : i == 1 ? magenta : green;
                var mat = i == 0 ? materials.NeonCyan : i == 1 ? materials.NeonMagenta : materials.NeonGreen;
                AddStrip(panels, $"Panel_{side}_{i}", new Vector3(side * 8.72f, 1.6f + i * 1.35f, -2.8f + i * 3.2f),
                    new Vector3(0.08f, 0.85f, 1.5f), mat, target);
            }

            var floorTiles = new List<Renderer>();
            var floorAccent = CreateEmpty(root.transform, "FloorAccentTiles");
            for (var i = 0; i < 7; i++)
            {
                var x = -6f + i * 2f;
                floorTiles.Add(CreatePart(floorAccent, $"Front_{i}", PrimitiveType.Cube,
                    new Vector3(x, -0.27f, -5.5f), new Vector3(1.55f, 0.08f, 0.55f), materials.DanceFloor, true));
                floorTiles.Add(CreatePart(floorAccent, $"Back_{i}", PrimitiveType.Cube,
                    new Vector3(x, -0.27f, 5.25f), new Vector3(1.55f, 0.08f, 0.55f), materials.DanceFloor, true));
            }

            neonGroups = new[]
            {
                CreateNeonController(neon, "CyanController", cyan, profile.Cyan, NeonAnimationMode.SmoothPulse, 11),
                CreateNeonController(neon, "MagentaController", magenta, profile.Magenta, NeonAnimationMode.SmoothPulse, 17),
                CreateNeonController(neon, "BlueController", blue, profile.Blue, NeonAnimationMode.Flicker, 23),
                CreateNeonController(neon, "GreenController", green, profile.Green, NeonAnimationMode.SmoothPulse, 29)
            };

            danceFloor = floorAccent.gameObject.AddComponent<DanceFloorController>();
            danceFloor.Configure(floorTiles.ToArray(), profile.Palette);

            var lighting = CreateEmpty(root.transform, "LightingRig");
            CreateHouseLight(lighting);
            CreateRoomFill(lighting, "CyanRoomFill", new Vector3(-5.5f, 2.8f, 3.5f), profile.Cyan);
            CreateRoomFill(lighting, "MagentaRoomFill", new Vector3(5.5f, 2.8f, 3.5f), profile.Magenta);
            movingLights = CreateSpotlights(lighting, profile, materials.SpotlightBeam, LoadOrCreateSpotlightCone());
            var strobeLight = CreateEmpty(lighting, "FullGrooveStrobe").gameObject.AddComponent<Light>();
            strobeLight.type = LightType.Point;
            strobeLight.transform.localPosition = new Vector3(0f, 4.8f, 0.5f);
            strobeLight.range = 13f;
            strobeLight.shadows = LightShadows.None;
            strobe = strobeLight.gameObject.AddComponent<StrobeLightController>();
            strobe.Configure(strobeLight, profile.StrobeInterval, profile.StrobeDuration,
                profile.StrobeIntensity, profile.Palette, profile.RandomStrobeColor);

            var probe = root.AddComponent<ReflectionProbe>();
            probe.mode = ReflectionProbeMode.Baked;
            probe.size = new Vector3(17f, 7f, 17f);
            probe.center = new Vector3(0f, 2.6f, 0.5f);
            probe.intensity = 0.65f;
            probe.boxProjection = true;

            manager = root.AddComponent<NightclubLightingManager>();
            manager.ConfigureReferences(profile, null, movingLights, strobe, neonGroups, danceFloor, null);
            SetStaticRecursively(architecture.gameObject);
            return root;
        }

        private static MovingSpotlightController CreateSpotlights(Transform parent, NightclubLightingProfile profile,
            Material beamMaterial, Mesh beamMesh)
        {
            var positions = new[]
            {
                new Vector3(-6.3f, 5.2f, -1.8f), new Vector3(6.3f, 5.2f, -1.8f),
                new Vector3(-5.4f, 5.4f, 5.7f), new Vector3(5.4f, 5.4f, 5.7f)
            };
            var pans = new Transform[4];
            var tilts = new Transform[4];
            var lights = new Light[4];
            var beams = new Renderer[4];
            for (var i = 0; i < 4; i++)
            {
                pans[i] = CreateEmpty(parent, $"Spotlight_{i + 1}_Pan");
                pans[i].localPosition = positions[i];
                pans[i].rotation = Quaternion.LookRotation(new Vector3(0f, 0f, 0.5f) - positions[i]);
                tilts[i] = CreateEmpty(pans[i], "Tilt");
                lights[i] = CreateEmpty(tilts[i], "Light").gameObject.AddComponent<Light>();
                var beam = CreateEmpty(tilts[i], "VisibleBeam").gameObject;
                beam.AddComponent<MeshFilter>().sharedMesh = beamMesh;
                beams[i] = beam.AddComponent<MeshRenderer>();
                beams[i].sharedMaterial = beamMaterial;
                beams[i].shadowCastingMode = ShadowCastingMode.Off;
                beams[i].receiveShadows = false;
            }
            var controller = parent.gameObject.AddComponent<MovingSpotlightController>();
            controller.Configure(pans, tilts, lights, beams, profile);
            return controller;
        }

        private static void CreateRoomFill(Transform parent, string name, Vector3 position, Color color)
        {
            var light = CreateEmpty(parent, name).gameObject.AddComponent<Light>();
            light.transform.localPosition = position;
            light.type = LightType.Point;
            light.color = color;
            light.intensity = 5.5f;
            light.range = 11.5f;
            light.shadows = LightShadows.None;
        }

        private static void CreateHouseLight(Transform parent)
        {
            var light = CreateEmpty(parent, "HeningHouseLight").gameObject.AddComponent<Light>();
            light.transform.localEulerAngles = new Vector3(48f, -28f, 0f);
            light.type = LightType.Directional;
            light.color = new Color(0.58f, 0.68f, 1f);
            light.intensity = 0.48f;
            light.shadows = LightShadows.None;
        }

        private static Mesh LoadOrCreateSpotlightCone()
        {
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(SpotlightConePath);
            if (mesh == null)
            {
                mesh = new Mesh { name = "SpotlightCone" };
                AssetDatabase.CreateAsset(mesh, SpotlightConePath);
            }

            const int sides = 20;
            const float length = 11f;
            const float radius = 3.2f;
            var vertices = new Vector3[sides + 1];
            var triangles = new int[sides * 3];
            vertices[0] = Vector3.zero;
            for (var index = 0; index < sides; index++)
            {
                var angle = index * Mathf.PI * 2f / sides;
                vertices[index + 1] = new Vector3(
                    Mathf.Cos(angle) * radius,
                    Mathf.Sin(angle) * radius,
                    length);
                var triangle = index * 3;
                triangles[triangle] = 0;
                triangles[triangle + 1] = index + 1;
                triangles[triangle + 2] = (index + 1) % sides + 1;
            }

            mesh.Clear();
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            EditorUtility.SetDirty(mesh);
            return mesh;
        }

        private static NeonLightController CreateNeonController(Transform parent, string name,
            List<Renderer> targets, Color color, NeonAnimationMode mode, int seed)
        {
            var host = CreateEmpty(parent, name).gameObject;
            var controller = host.AddComponent<NeonLightController>();
            controller.Configure(targets.ToArray(), color, mode, seed);
            return controller;
        }

        private static void CreateSegmentedRing(Transform parent, string name, float radius,
            float centerHeight, int segments, float thickness, Material material)
        {
            var root = CreateEmpty(parent, name);
            var length = 2f * Mathf.PI * radius / segments * 0.96f;
            for (var i = 0; i < segments; i++)
            {
                var angle = i * 360f / segments;
                var radians = angle * Mathf.Deg2Rad;
                CreatePart(root, $"Segment_{i:00}", PrimitiveType.Cube,
                    new Vector3(Mathf.Cos(radians) * radius, centerHeight + Mathf.Sin(radians) * radius, 8.48f),
                    new Vector3(length, thickness, 0.12f), material, true,
                    new Vector3(0f, 0f, angle + 90f));
            }
        }

        private static void AddStrip(Transform parent, string name, Vector3 position, Vector3 scale,
            Material material, List<Renderer> collection)
        {
            collection.Add(CreatePart(parent, name, PrimitiveType.Cube, position, scale, material, true));
        }

        private static MeshRenderer CreatePart(Transform parent, string name, PrimitiveType primitive,
            Vector3 position, Vector3 scale, Material material, bool removeCollider, Vector3? rotation = null)
        {
            var go = GameObject.CreatePrimitive(primitive);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localEulerAngles = rotation ?? Vector3.zero;
            go.transform.localScale = scale;
            if (removeCollider && go.TryGetComponent<Collider>(out var collider)) UnityEngine.Object.DestroyImmediate(collider);
            var renderer = go.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = material.IsKeywordEnabled("_EMISSION") ? ShadowCastingMode.Off : ShadowCastingMode.On;
            renderer.receiveShadows = !material.IsKeywordEnabled("_EMISSION");
            return renderer;
        }

        private static NightclubMaterials CreateMaterials()
        {
            return new NightclubMaterials
            {
                Environment = CreateLitMaterial("M_Nightclub_Environment", new Color(0.055f, 0.06f, 0.11f), 0.5f, 0.8f),
                Metal = CreateLitMaterial("M_Nightclub_Metal", new Color(0.06f, 0.065f, 0.12f), 0.9f, 0.9f),
                DanceFloor = CreateLitMaterial("M_Nightclub_DanceFloor", new Color(0.02f, 0.025f, 0.055f), 0.35f, 0.85f, Color.white * 0.05f),
                NeonCyan = CreateLitMaterial("M_Neon_Cyan", new Color(0.01f, 0.15f, 0.18f), 0f, 0.5f, new Color(0.02f, 0.9f, 1f) * 1.2f),
                NeonMagenta = CreateLitMaterial("M_Neon_Magenta", new Color(0.18f, 0.01f, 0.12f), 0f, 0.5f, new Color(1f, 0.03f, 0.75f) * 1.2f),
                NeonBlue = CreateLitMaterial("M_Neon_Blue", new Color(0.01f, 0.03f, 0.18f), 0f, 0.5f, new Color(0.05f, 0.2f, 1f) * 1.2f),
                NeonGreen = CreateLitMaterial("M_Neon_Green", new Color(0.01f, 0.16f, 0.07f), 0f, 0.5f, new Color(0.05f, 1f, 0.45f) * 1.2f),
                SpotlightBeam = CreateBeamMaterial()
            };
        }

        private static Material CreateBeamMaterial()
        {
            var path = $"{MaterialFolder}/M_Nightclub_SpotlightBeam.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit")
                    ?? throw new InvalidOperationException("URP Lit shader is unavailable.");
                material = new Material(shader) { name = "M_Nightclub_SpotlightBeam" };
                AssetDatabase.CreateAsset(material, path);
            }

            material.SetFloat("_Surface", 1f);
            material.SetFloat("_Blend", 2f);
            material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)BlendMode.One);
            material.SetFloat("_ZWrite", 0f);
            material.SetFloat("_Cull", 0f);
            material.SetColor("_BaseColor", new Color(0.1f, 0.8f, 1f, 0.055f));
            material.SetColor("_EmissionColor", new Color(0.1f, 0.8f, 1f) * 0.45f);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.EnableKeyword("_EMISSION");
            material.renderQueue = (int)RenderQueue.Transparent;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Material CreateLitMaterial(string name, Color baseColor, float metallic,
            float smoothness, Color? emission = null)
        {
            var path = $"{MaterialFolder}/{name}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit")
                    ?? throw new InvalidOperationException("URP Lit shader is unavailable.");
                material = new Material(shader) { name = name };
                AssetDatabase.CreateAsset(material, path);
            }
            material.SetColor("_BaseColor", baseColor);
            material.SetFloat("_Metallic", metallic);
            material.SetFloat("_Smoothness", smoothness);
            if (emission.HasValue)
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", emission.Value);
                material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.BakedEmissive;
            }
            else
            {
                material.DisableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", Color.black);
            }
            EditorUtility.SetDirty(material);
            return material;
        }

        private static NightclubLightingProfile LoadOrCreateProfile()
        {
            var profile = AssetDatabase.LoadAssetAtPath<NightclubLightingProfile>(ProfilePath);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<NightclubLightingProfile>();
                AssetDatabase.CreateAsset(profile, ProfilePath);
            }
            profile.ApplyPrototypeDefaults();
            EditorUtility.SetDirty(profile);
            return profile;
        }

        private static void ConfigurePostProcessing()
        {
            var reaction = AssetDatabase.LoadAssetAtPath<VolumeProfile>(ReactionVolumePath);
            if (reaction != null)
            {
                if (reaction.TryGet(out Bloom bloom))
                {
                    bloom.threshold.Override(1f);
                    bloom.intensity.Override(0.5f);
                    bloom.scatter.Override(0.58f);
                }
                EditorUtility.SetDirty(reaction);
            }

            var gameplay = AssetDatabase.LoadAssetAtPath<VolumeProfile>(GameplayVolumePath);
            if (gameplay == null) return;
            if (gameplay.TryGet(out Tonemapping tone)) tone.mode.Override(TonemappingMode.ACES);
            if (gameplay.TryGet(out Bloom baseBloom)) { baseBloom.threshold.Override(1f); baseBloom.intensity.Override(0.18f); }
            if (gameplay.TryGet(out ColorAdjustments color)) { color.postExposure.Override(0.05f); color.contrast.Override(7f); }
            if (gameplay.TryGet(out Vignette vignette)) vignette.intensity.Override(0.17f);
            EditorUtility.SetDirty(gameplay);
        }

        private static void ConfigureAmbientLighting()
        {
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.18f, 0.13f, 0.28f);
            RenderSettings.ambientEquatorColor = new Color(0.10f, 0.10f, 0.18f);
            RenderSettings.ambientGroundColor = new Color(0.05f, 0.04f, 0.09f);
            RenderSettings.ambientIntensity = 1.6f;
        }

        private static void SetStaticRecursively(GameObject root)
        {
            foreach (var transform in root.GetComponentsInChildren<Transform>(true))
                GameObjectUtility.SetStaticEditorFlags(transform.gameObject,
                    StaticEditorFlags.BatchingStatic | StaticEditorFlags.ContributeGI | StaticEditorFlags.ReflectionProbeStatic);
        }

        private static Transform CreateEmpty(Transform parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go.transform;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parent = System.IO.Path.GetDirectoryName(path)?.Replace('\\', '/');
            var name = System.IO.Path.GetFileName(path);
            if (!string.IsNullOrEmpty(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }

        private static GameObject FindInScene(Scene scene, string name)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                if (root.name == name) return root;
                var nested = Array.Find(root.GetComponentsInChildren<Transform>(true), value => value.name == name);
                if (nested != null) return nested.gameObject;
            }
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

        private static T FindNamedComponent<T>(Scene scene, string name) where T : Component
        {
            return FindInScene(scene, name)?.GetComponent<T>();
        }

        private static void SetObjectReference(UnityEngine.Object target, string propertyName, UnityEngine.Object value)
        {
            var serializedObject = new SerializedObject(target);
            var property = serializedObject.FindProperty(propertyName);
            if (property == null)
                throw new InvalidOperationException($"Missing serialized field {propertyName} on {target.name}.");
            property.objectReferenceValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }

        private sealed class NightclubMaterials
        {
            public Material Environment;
            public Material Metal;
            public Material DanceFloor;
            public Material NeonCyan;
            public Material NeonMagenta;
            public Material NeonBlue;
            public Material NeonGreen;
            public Material SpotlightBeam;
        }
    }
}
