using System;
using System.Collections.Generic;
using GameJam.Gameplay;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace GameJam.Editor
{
    public static class Issue8PrototypeBuilder
    {
        private const string RootFolder = "Assets/Game";
        private const string MaterialFolder = RootFolder + "/Art/Prototype/Materials";
        private const string PrefabFolder = RootFolder + "/Prefabs/Prototype";
        private const string LevelFolder = RootFolder + "/Data/Levels";
        private const string SceneFolder = RootFolder + "/Scenes";

        private const string TilePrefabPath = PrefabFolder + "/TileSlot.prefab";
        private const string BoardPrefabPath = PrefabFolder + "/PuzzleBoard.prefab";
        private const string PlayerPrefabPath = PrefabFolder + "/PrototypePlayer.prefab";
        private const string LevelPath = LevelFolder + "/Level_01_Prototype.asset";
        private const string CameraMotionProfilePath = RootFolder + "/Data/CameraMotionProfile_Prototype.asset";
        private const string ScenePath = SceneFolder + "/GameplayPrototype.unity";
        private const float GridSpacing = 1.1f;

        [MenuItem("Game Jam/Build Issue 8 Prototype")]
        public static void BuildFromMenu()
        {
            BuildPrototypeAssets();
        }

        private static void BuildPrototypeAssets()
        {
            EnsureFolders();

            var darkMaterial = CreateOrUpdateMaterial(
                MaterialFolder + "/M_Prototype_Dark.mat",
                new Color(0.035f, 0.025f, 0.09f, 1f),
                Color.black);
            var tileMaterial = CreateOrUpdateMaterial(
                MaterialFolder + "/M_Prototype_Tile.mat",
                new Color(0.12f, 0.08f, 0.23f, 1f),
                new Color(0.08f, 0.03f, 0.18f, 1f));
            var emissiveMaterial = CreateOrUpdateMaterial(
                MaterialFolder + "/M_Prototype_Emissive.mat",
                Color.white,
                new Color(0.35f, 0.1f, 0.5f, 1f));
            var playerMaterial = CreateOrUpdateMaterial(
                MaterialFolder + "/M_Prototype_Player.mat",
                new Color(0.72f, 0.95f, 1f, 1f),
                new Color(0.12f, 0.55f, 0.65f, 1f));
            var stageLineMaterial = CreateOrUpdateMaterial(
                MaterialFolder + "/M_Prototype_StageLine.mat",
                new Color(1f, 0.08f, 0.55f, 1f),
                new Color(2f, 0.04f, 0.85f, 1f));

            var tilePrefab = CreateTilePrefab(tileMaterial, emissiveMaterial);
            var boardPrefab = CreateBoardPrefab(tilePrefab);
            var playerPrefab = CreatePlayerPrefab(playerMaterial);
            var level = CreateLevelDefinition();
            var cameraMotionProfile = CreateCameraMotionProfile();
            CreatePrototypeScene(boardPrefab, playerPrefab, level, cameraMotionProfile, darkMaterial, stageLineMaterial);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("ISSUE8_PROTOTYPE_BUILD_COMPLETE: GameplayPrototype scene and Level 1 assets created.");
        }

        private static void EnsureFolders()
        {
            EnsureFolder("Assets", "Game");
            EnsureFolder(RootFolder, "Art");
            EnsureFolder(RootFolder + "/Art", "Prototype");
            EnsureFolder(RootFolder + "/Art/Prototype", "Materials");
            EnsureFolder(RootFolder, "Prefabs");
            EnsureFolder(RootFolder + "/Prefabs", "Prototype");
            EnsureFolder(RootFolder, "Data");
            EnsureFolder(RootFolder + "/Data", "Levels");
            EnsureFolder(RootFolder, "Scenes");
        }

        private static void EnsureFolder(string parent, string child)
        {
            var path = parent + "/" + child;
            if (!AssetDatabase.IsValidFolder(path))
            {
                AssetDatabase.CreateFolder(parent, child);
            }
        }

        private static Material CreateOrUpdateMaterial(string path, Color baseColor, Color emissionColor)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                if (shader == null)
                {
                    throw new InvalidOperationException("No compatible Lit shader was found.");
                }

                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }

            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", baseColor);
            }
            else if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", baseColor);
            }

            if (material.HasProperty("_EmissionColor"))
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", emissionColor);
            }

            EditorUtility.SetDirty(material);
            return material;
        }

        private static GameObject CreateTilePrefab(Material tileMaterial, Material surfaceMaterial)
        {
            var root = new GameObject("TileSlot");
            try
            {
                var tileSlot = root.AddComponent<TileSlot>();
                var tileCollider = root.AddComponent<BoxCollider>();
                tileCollider.center = new Vector3(0f, 0.1f, 0f);
                tileCollider.size = new Vector3(1f, 0.2f, 1f);

                var visualRoot = CreateChild(root.transform, "VisualRoot");
                var tileBase = CreatePrimitiveChild(visualRoot, "TileBase", PrimitiveType.Cube, tileMaterial);
                tileBase.transform.localPosition = new Vector3(0f, 0.1f, 0f);
                tileBase.transform.localScale = new Vector3(1f, 0.2f, 1f);

                var surface = CreatePrimitiveChild(visualRoot, "EmissiveSurface", PrimitiveType.Cube, surfaceMaterial);
                surface.transform.localPosition = new Vector3(0f, 0.215f, 0f);
                surface.transform.localScale = new Vector3(0.86f, 0.03f, 0.86f);

                var standPoint = CreateChild(root.transform, "PlayerStandPoint");
                standPoint.localPosition = new Vector3(0f, 0.25f, 0f);
                var vfxPoint = CreateChild(root.transform, "VFXPoint");
                vfxPoint.localPosition = new Vector3(0f, 0.35f, 0f);

                tileSlot.ConfigureReferences(
                    Vector2Int.zero,
                    visualRoot,
                    surface.GetComponent<Renderer>(),
                    tileCollider,
                    standPoint,
                    vfxPoint);

                return PrefabUtility.SaveAsPrefabAsset(root, TilePrefabPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static GameObject CreateBoardPrefab(GameObject tilePrefab)
        {
            var root = new GameObject("PuzzleBoard");
            try
            {
                var board = root.AddComponent<PuzzleBoard>();
                var contentRoot = CreateChild(root.transform, "BoardContentRoot");
                var slots = new TileSlot[LevelDefinition.CellCount];

                for (var row = 0; row < LevelDefinition.GridHeight; row++)
                {
                    var rowRoot = CreateChild(contentRoot, $"Row_{row:00}");
                    for (var column = 0; column < LevelDefinition.GridWidth; column++)
                    {
                        var tile = (GameObject)PrefabUtility.InstantiatePrefab(tilePrefab, rowRoot);
                        tile.name = $"Tile_{row:00}_{column:00}";
                        var boardCenter = (LevelDefinition.GridWidth - 1) * 0.5f;
                        tile.transform.localPosition = new Vector3(
                            (column - boardCenter) * GridSpacing,
                            0f,
                            (boardCenter - row) * GridSpacing);

                        var tileSlot = tile.GetComponent<TileSlot>();
                        tileSlot.ConfigureReferences(
                            new Vector2Int(column, row),
                            tile.transform.Find("VisualRoot"),
                            tile.transform.Find("VisualRoot/EmissiveSurface").GetComponent<Renderer>(),
                            tile.GetComponent<BoxCollider>(),
                            tile.transform.Find("PlayerStandPoint"),
                            tile.transform.Find("VFXPoint"));
                        slots[LevelDefinition.ToIndex(new Vector2Int(column, row))] = tileSlot;
                    }
                }

                board.ConfigureReferences(contentRoot, slots, GridSpacing);
                return PrefabUtility.SaveAsPrefabAsset(root, BoardPrefabPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static GameObject CreatePlayerPrefab(Material playerMaterial)
        {
            var root = new GameObject("PrototypePlayer");
            try
            {
                var body = CreatePrimitiveChild(root.transform, "GhostBody", PrimitiveType.Sphere, playerMaterial);
                body.transform.localPosition = new Vector3(0f, 0.48f, 0f);
                body.transform.localScale = new Vector3(0.48f, 0.62f, 0.48f);

                var head = CreatePrimitiveChild(root.transform, "GhostHead", PrimitiveType.Sphere, playerMaterial);
                head.transform.localPosition = new Vector3(0f, 0.9f, 0f);
                head.transform.localScale = Vector3.one * 0.43f;

                var shadow = CreatePrimitiveChild(root.transform, "Shadow", PrimitiveType.Cylinder, playerMaterial);
                shadow.transform.localPosition = new Vector3(0f, 0.02f, 0f);
                shadow.transform.localScale = new Vector3(0.32f, 0.015f, 0.32f);

                return PrefabUtility.SaveAsPrefabAsset(root, PlayerPrefabPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static LevelDefinition CreateLevelDefinition()
        {
            var level = AssetDatabase.LoadAssetAtPath<LevelDefinition>(LevelPath);
            if (level == null)
            {
                level = ScriptableObject.CreateInstance<LevelDefinition>();
                AssetDatabase.CreateAsset(level, LevelPath);
            }

            var cells = new BoardCellDefinition[LevelDefinition.CellCount];
            for (var index = 0; index < cells.Length; index++)
            {
                cells[index] = new BoardCellDefinition(false, BeatColor.Magenta);
            }

            var layout = new[,]
            {
                { BeatColor.Yellow, BeatColor.Yellow, BeatColor.Blue },
                { BeatColor.Yellow, BeatColor.Magenta, BeatColor.Blue },
                { BeatColor.Yellow, BeatColor.Blue, BeatColor.Blue }
            };

            for (var localRow = 0; localRow < 3; localRow++)
            {
                for (var localColumn = 0; localColumn < 3; localColumn++)
                {
                    var coordinate = new Vector2Int(localColumn + 2, localRow + 2);
                    cells[LevelDefinition.ToIndex(coordinate)] = new BoardCellDefinition(true, layout[localRow, localColumn]);
                }
            }

            var objectives = new[]
            {
                new ObjectiveRowDefinition(BeatColor.Magenta, BeatColor.Magenta, BeatColor.Magenta, BeatColor.Magenta),
                new ObjectiveRowDefinition(BeatColor.Yellow, BeatColor.Yellow, BeatColor.Yellow, BeatColor.Yellow),
                new ObjectiveRowDefinition(BeatColor.Blue, BeatColor.Blue, BeatColor.Blue)
            };
            level.SetData(
                "Level_01",
                cells,
                new Vector2Int(3, 3),
                objectives,
                new[]
                {
                    MoveDirection.Down, MoveDirection.Left, MoveDirection.Up, MoveDirection.Up,
                    MoveDirection.Right, MoveDirection.Right, MoveDirection.Down, MoveDirection.Down,
                    MoveDirection.Left, MoveDirection.Left, MoveDirection.Up
                });
            EditorUtility.SetDirty(level);
            return level;
        }

        private static CameraMotionProfile CreateCameraMotionProfile()
        {
            var profile = AssetDatabase.LoadAssetAtPath<CameraMotionProfile>(CameraMotionProfilePath);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<CameraMotionProfile>();
                AssetDatabase.CreateAsset(profile, CameraMotionProfilePath);
            }

            profile.ApplyPrototypeDefaults();
            EditorUtility.SetDirty(profile);
            return profile;
        }

        private static void CreatePrototypeScene(
            GameObject boardPrefab,
            GameObject playerPrefab,
            LevelDefinition level,
            CameraMotionProfile cameraMotionProfile,
            Material darkMaterial,
            Material stageLineMaterial)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var systems = new GameObject("Systems");
            var gameplay = new GameObject("Gameplay");
            var environment = new GameObject("Environment");
            var lighting = new GameObject("Lighting");
            var cameraRig = new GameObject("CameraRig");

            var boardObject = (GameObject)PrefabUtility.InstantiatePrefab(boardPrefab, scene);
            boardObject.transform.SetParent(gameplay.transform);
            boardObject.transform.localPosition = Vector3.zero;
            var board = boardObject.GetComponent<PuzzleBoard>();

            var player = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab, scene);
            player.transform.SetParent(gameplay.transform);

            var eventHub = systems.AddComponent<PuzzleGameplayEvents>();
            var gameplayController = systems.AddComponent<PuzzleGameplayController>();
            gameplayController.ConfigureReferences(level, board, player.transform, eventHub);
            var objectiveHud = systems.AddComponent<PrototypeObjectiveHud>();
            objectiveHud.ConfigureReferences(gameplayController, eventHub);
            CreatePrototypeMusic(systems.transform, eventHub);

            var bootstrap = systems.AddComponent<GameplayPrototypeBootstrap>();
            bootstrap.ConfigureReferences(level, board, player.transform, gameplayController);
            board.BuildBoard(level);
            player.transform.position = board.GetPlayerStandPosition(level.PlayerStart);

            CreateStage(environment.transform, darkMaterial, stageLineMaterial);
            CreateAudienceAnchors(environment.transform);
            CreateReactionAnchors(environment.transform);
            CreateLighting(lighting.transform);
            CreateCamera(cameraRig.transform, eventHub, cameraMotionProfile);
            Issue11StageReactionInstaller.InstallIntoLoadedScene(scene);

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
            {
                throw new InvalidOperationException("GameplayPrototype scene could not be saved.");
            }

            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        }

        private static void CreatePrototypeMusic(Transform parent, PuzzleGameplayEvents eventHub)
        {
            var musicRoot = CreateChild(parent, "PrototypeMusic");
            var harmony = CreateAudioLayer(musicRoot, "Harmony_01", "Assets/Game/Art/Beat/Level_1/1_Harmony.wav");
            var drumKick = CreateAudioLayer(musicRoot, "DrumKick_02", "Assets/Game/Art/Beat/Level_1/2_DrumKick.wav");
            var ketipungOne = CreateAudioLayer(musicRoot, "Ketipung1_03", "Assets/Game/Art/Beat/Level_1/3_Ketipung1.wav");
            var ketipungTwo = CreateAudioLayer(musicRoot, "Ketipung2_04", "Assets/Game/Art/Beat/Level_1/4_Ketipung2.wav");
            var bass = CreateAudioLayer(musicRoot, "BassGuitar_05", "Assets/Game/Art/Beat/Level_1/5_BassGuitar.wav");
            var fullSong = CreateAudioLayer(musicRoot, "FullSong_06", "Assets/Game/Art/Beat/Level_1/6_FullSong.wav");
            var director = musicRoot.gameObject.AddComponent<PrototypeMusicDirector>();
            director.ConfigureReferences(eventHub, harmony, drumKick, ketipungOne, ketipungTwo, bass, fullSong);
            director.ConfigureSampleSequencerScope(null, 0);
            director.ConfigureFullSongTransition(1, 0.75f, 1.15f);
            var sequencer = musicRoot.gameObject.AddComponent<TileBeatSequencer>();
            sequencer.ConfigureReferences(
                eventHub,
                director,
                null,
                RequireAudioClip("Assets/Game/Art/Beat/OneShots/Magenta_Kick.wav"),
                RequireAudioClip("Assets/Game/Art/Beat/OneShots/Yellow_Ketipung1.wav"),
                RequireAudioClip("Assets/Game/Art/Beat/OneShots/Yellow_Ketipung1_Accent.wav"),
                RequireAudioClip("Assets/Game/Art/Beat/OneShots/Blue_Ketipung2.wav"),
                RequireAudioClip("Assets/Game/Art/Beat/OneShots/Blue_Ketipung2_Accent.wav"),
                0);
            sequencer.ConfigurePattern(
                8,
                2,
                new[] { 0, 3, 5, 6, 1, 3, 5, 7, 2, 4, 6 },
                new[] { false, false, false, false, false, false, false, true, false, true, true });
            sequencer.ConfigureTileBeatTransition(0.06f);
        }

        private static AudioSource CreateAudioLayer(Transform parent, string name, string clipPath)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(clipPath);
            if (clip == null)
            {
                throw new InvalidOperationException($"Required audio clip was not found at {clipPath}.");
            }

            var layer = CreateChild(parent, name);
            var source = layer.gameObject.AddComponent<AudioSource>();
            source.clip = clip;
            source.playOnAwake = false;
            source.loop = true;
            source.spatialBlend = 0f;
            return source;
        }

        private static AudioClip RequireAudioClip(string clipPath)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(clipPath);
            if (clip == null)
            {
                throw new InvalidOperationException($"Required audio clip was not found at {clipPath}.");
            }

            return clip;
        }

        private static void CreateStage(Transform parent, Material darkMaterial, Material lineMaterial)
        {
            var stage = CreateChild(parent, "Stage");
            var floor = CreatePrimitiveChild(stage, "StageFloor", PrimitiveType.Cube, darkMaterial);
            floor.transform.localPosition = new Vector3(0f, -0.15f, 0f);
            floor.transform.localScale = new Vector3(8.5f, 0.25f, 8.5f);

            var lineRoot = CreateChild(stage, "StageLightLine");
            CreateLineSegment(lineRoot, "North", new Vector3(0f, 0.05f, 3.8f), new Vector3(7.6f, 0.08f, 0.08f), lineMaterial);
            CreateLineSegment(lineRoot, "South", new Vector3(0f, 0.05f, -3.8f), new Vector3(7.6f, 0.08f, 0.08f), lineMaterial);
            CreateLineSegment(lineRoot, "East", new Vector3(3.8f, 0.05f, 0f), new Vector3(0.08f, 0.08f, 7.6f), lineMaterial);
            CreateLineSegment(lineRoot, "West", new Vector3(-3.8f, 0.05f, 0f), new Vector3(0.08f, 0.08f, 7.6f), lineMaterial);
        }

        private static void CreateLineSegment(Transform parent, string name, Vector3 position, Vector3 scale, Material material)
        {
            var segment = CreatePrimitiveChild(parent, name, PrimitiveType.Cube, material);
            segment.transform.localPosition = position;
            segment.transform.localScale = scale;
        }

        private static void CreateAudienceAnchors(Transform parent)
        {
            var root = CreateChild(parent, "AudienceAnchors");
            var positions = new[]
            {
                new Vector3(-4.5f, 0f, 2.5f), new Vector3(-4.5f, 0f, 0f), new Vector3(-4.5f, 0f, -2.5f),
                new Vector3(4.5f, 0f, 2.5f), new Vector3(4.5f, 0f, 0f), new Vector3(4.5f, 0f, -2.5f),
                new Vector3(-2.5f, 0f, 4.5f), new Vector3(0f, 0f, 4.5f), new Vector3(2.5f, 0f, 4.5f)
            };

            for (var index = 0; index < positions.Length; index++)
            {
                var anchor = CreateChild(root, $"AudienceAnchor_{index + 1:00}");
                anchor.localPosition = positions[index];
            }
        }

        private static void CreateReactionAnchors(Transform parent)
        {
            var root = CreateChild(parent, "StageReactionAnchors");
            var names = new[] { "North", "East", "South", "West" };
            var positions = new[]
            {
                new Vector3(0f, 0.4f, 3.8f), new Vector3(3.8f, 0.4f, 0f),
                new Vector3(0f, 0.4f, -3.8f), new Vector3(-3.8f, 0.4f, 0f)
            };

            for (var index = 0; index < names.Length; index++)
            {
                var anchor = CreateChild(root, $"ReactionAnchor_{names[index]}");
                anchor.localPosition = positions[index];
            }
        }

        private static void CreateLighting(Transform parent)
        {
            var keyLightObject = new GameObject("KeyLight");
            keyLightObject.transform.SetParent(parent, false);
            keyLightObject.transform.localRotation = Quaternion.Euler(48f, -30f, 0f);
            var keyLight = keyLightObject.AddComponent<Light>();
            keyLight.type = LightType.Directional;
            keyLight.color = new Color(0.72f, 0.78f, 1f);
            keyLight.intensity = 1.15f;
            keyLight.shadows = LightShadows.Soft;

            var volumeObject = new GameObject("GlobalVolume");
            volumeObject.transform.SetParent(parent, false);
            var volume = volumeObject.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 0f;
            volume.sharedProfile = AssetDatabase.LoadAssetAtPath<VolumeProfile>("Assets/Settings/SampleSceneProfile.asset");
        }

        private static void CreateCamera(
            Transform parent,
            PuzzleGameplayEvents eventHub,
            CameraMotionProfile cameraMotionProfile)
        {
            var cameraObject = new GameObject("Main Camera");
            cameraObject.transform.SetParent(parent, false);
            cameraObject.transform.localPosition = new Vector3(0f, 7.5f, -11.5f);
            cameraObject.transform.LookAt(new Vector3(0f, 0f, 0.75f));
            cameraObject.tag = "MainCamera";

            var camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = false;
            camera.fieldOfView = 48f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.018f, 0.012f, 0.055f, 1f);
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 100f;
            cameraObject.AddComponent<AudioListener>();

            var cameraDirector = parent.gameObject.AddComponent<PrototypeCameraDirector>();
            cameraDirector.ConfigureReferences(eventHub, camera, cameraMotionProfile);
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

            child.GetComponent<Renderer>().sharedMaterial = material;
            return child;
        }
    }
}
