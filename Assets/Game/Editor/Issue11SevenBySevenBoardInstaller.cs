using GameJam.Gameplay;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GameJam.Editor
{
    public static class Issue11SevenBySevenBoardInstaller
    {
        private const string TilePrefabPath = "Assets/Game/Prefabs/Prototype/TileSlot.prefab";
        private const string BoardPrefabPath = "Assets/Game/Prefabs/Prototype/PuzzleBoard.prefab";
        private const string LevelPath = "Assets/Game/Data/Levels/Level_01_Prototype.asset";
        private const string ScenePath = "Assets/Game/Scenes/GameplayPrototype.unity";
        private const float GridSpacing = 1.1f;

        [MenuItem("Game Jam/Migrate Master Board To 7x7")]
        public static void Install()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new System.InvalidOperationException("Stop Play Mode before migrating the master board.");
            }

            var tilePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(TilePrefabPath);
            var level = AssetDatabase.LoadAssetAtPath<LevelDefinition>(LevelPath);
            if (tilePrefab == null || level == null)
            {
                throw new System.InvalidOperationException("TileSlot prefab or Level 1 asset is missing.");
            }

            var boardPrefab = RebuildBoardPrefab(tilePrefab);
            RebuildLevel(level);

            var scene = SceneManager.GetSceneByPath(ScenePath);
            var openedAdditively = !scene.IsValid() || !scene.isLoaded;
            if (openedAdditively)
            {
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            }

            var oldBoard = FindComponent<PuzzleBoard>(scene);
            var player = FindTransform(scene, "PrototypePlayer");
            var controller = FindComponent<PuzzleGameplayController>(scene);
            var bootstrap = FindComponent<GameplayPrototypeBootstrap>(scene);
            var eventHub = FindComponent<PuzzleGameplayEvents>(scene);
            var sceneCamera = FindComponent<Camera>(scene);
            if (oldBoard == null || player == null || controller == null || bootstrap == null || eventHub == null)
            {
                throw new System.InvalidOperationException("GameplayPrototype is missing a required gameplay reference.");
            }

            var cameraPosition = sceneCamera != null ? sceneCamera.transform.localPosition : Vector3.zero;
            var cameraRotation = sceneCamera != null ? sceneCamera.transform.localRotation : Quaternion.identity;
            var cameraFov = sceneCamera != null ? sceneCamera.fieldOfView : 0f;

            var oldTransform = oldBoard.transform;
            var parent = oldTransform.parent;
            var siblingIndex = oldTransform.GetSiblingIndex();
            var localPosition = oldTransform.localPosition;
            var localRotation = oldTransform.localRotation;
            var localScale = oldTransform.localScale;

            Object.DestroyImmediate(oldBoard.gameObject);
            var boardObject = (GameObject)PrefabUtility.InstantiatePrefab(boardPrefab, scene);
            boardObject.name = "PuzzleBoard";
            boardObject.transform.SetParent(parent, false);
            boardObject.transform.SetSiblingIndex(siblingIndex);
            boardObject.transform.localPosition = localPosition;
            boardObject.transform.localRotation = localRotation;
            boardObject.transform.localScale = localScale;

            var board = boardObject.GetComponent<PuzzleBoard>();
            controller.ConfigureReferences(level, board, player, eventHub);
            bootstrap.ConfigureReferences(level, board, player, controller);
            board.BuildBoard(level);
            player.position = board.GetPlayerStandPosition(level.PlayerStart);

            if (sceneCamera != null &&
                (sceneCamera.transform.localPosition != cameraPosition ||
                 Quaternion.Angle(sceneCamera.transform.localRotation, cameraRotation) > 0.0001f ||
                 !Mathf.Approximately(sceneCamera.fieldOfView, cameraFov)))
            {
                throw new System.InvalidOperationException("Camera changed during board migration; scene was not saved.");
            }

            EditorUtility.SetDirty(level);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();

            if (openedAdditively)
            {
                EditorSceneManager.CloseScene(scene, true);
            }

            Debug.Log("MASTER_BOARD_7X7_INSTALL_COMPLETE: 49 slots, centered 3x3 level, camera preserved.");
        }

        private static GameObject RebuildBoardPrefab(GameObject tilePrefab)
        {
            var root = new GameObject("PuzzleBoard");
            try
            {
                var board = root.AddComponent<PuzzleBoard>();
                var contentRoot = new GameObject("BoardContentRoot").transform;
                contentRoot.SetParent(root.transform, false);
                var slots = new TileSlot[LevelDefinition.CellCount];
                var boardCenter = (LevelDefinition.GridWidth - 1) * 0.5f;

                for (var row = 0; row < LevelDefinition.GridHeight; row++)
                {
                    var rowRoot = new GameObject($"Row_{row:00}").transform;
                    rowRoot.SetParent(contentRoot, false);
                    for (var column = 0; column < LevelDefinition.GridWidth; column++)
                    {
                        var tile = (GameObject)PrefabUtility.InstantiatePrefab(tilePrefab, rowRoot);
                        tile.name = $"Tile_{row:00}_{column:00}";
                        tile.transform.localPosition = new Vector3(
                            (column - boardCenter) * GridSpacing,
                            0f,
                            (boardCenter - row) * GridSpacing);

                        var slot = tile.GetComponent<TileSlot>();
                        slot.ConfigureReferences(
                            new Vector2Int(column, row),
                            tile.transform.Find("VisualRoot"),
                            tile.transform.Find("VisualRoot/EmissiveSurface").GetComponent<Renderer>(),
                            tile.GetComponent<BoxCollider>(),
                            tile.transform.Find("PlayerStandPoint"),
                            tile.transform.Find("VFXPoint"));
                        slots[LevelDefinition.ToIndex(new Vector2Int(column, row))] = slot;
                    }
                }

                board.ConfigureReferences(contentRoot, slots, GridSpacing);
                return PrefabUtility.SaveAsPrefabAsset(root, BoardPrefabPath);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static void RebuildLevel(LevelDefinition level)
        {
            var cells = new BoardCellDefinition[LevelDefinition.CellCount];
            for (var index = 0; index < cells.Length; index++)
            {
                cells[index] = new BoardCellDefinition(false, BeatColor.Magenta);
            }

            var layout = new[,]
            {
                { BeatColor.Yellow, BeatColor.Yellow, BeatColor.Blue },
                { BeatColor.Yellow, BeatColor.Magenta, BeatColor.Yellow },
                { BeatColor.Blue, BeatColor.Magenta, BeatColor.Magenta }
            };

            for (var row = 0; row < 3; row++)
            {
                for (var column = 0; column < 3; column++)
                {
                    var coordinate = new Vector2Int(column + 2, row + 2);
                    cells[LevelDefinition.ToIndex(coordinate)] = new BoardCellDefinition(true, layout[row, column]);
                }
            }

            level.SetData(
                cells,
                new Vector2Int(3, 3),
                new[]
                {
                    new ObjectiveRowDefinition(BeatColor.Magenta),
                    new ObjectiveRowDefinition(BeatColor.Yellow),
                    new ObjectiveRowDefinition(BeatColor.Magenta),
                    new ObjectiveRowDefinition(BeatColor.Blue),
                    new ObjectiveRowDefinition(BeatColor.Blue, BeatColor.Yellow, BeatColor.Magenta, BeatColor.Blue)
                });
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

        private static Transform FindTransform(Scene scene, string objectName)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var child in root.GetComponentsInChildren<Transform>(true))
                {
                    if (child.name == objectName)
                    {
                        return child;
                    }
                }
            }

            return null;
        }
    }
}
