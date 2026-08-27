using System.Text;
using UnityEngine;

namespace GameJam.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class PrototypeObjectiveHud : MonoBehaviour
    {
        [SerializeField] private PuzzleGameplayController gameplayController;
        [SerializeField] private PuzzleGameplayEvents gameplayEvents;
        [SerializeField] private Vector2 panelPosition = new(20f, 20f);
        [SerializeField] private Vector2 panelSize = new(320f, 250f);

        private GUIStyle panelStyle;
        private GUIStyle textStyle;
        private GUIStyle winTitleStyle;
        private GUIStyle winSubtitleStyle;
        private bool showWinPanel;
        private float smoothedFrameDuration = 1f / 60f;

        public void ConfigureReferences(PuzzleGameplayController controller, PuzzleGameplayEvents eventHub = null)
        {
            Unsubscribe();
            gameplayController = controller;
            gameplayEvents = eventHub;
            Subscribe();
        }

        private void OnEnable()
        {
            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void Update()
        {
            var frameDuration = Mathf.Max(0.0001f, Time.unscaledDeltaTime);
            smoothedFrameDuration = Mathf.Lerp(smoothedFrameDuration, frameDuration, 0.08f);
        }

        private void OnGUI()
        {
            if (gameplayController == null || gameplayController.ProgressTracker == null || gameplayController.Level == null)
            {
                return;
            }

            EnsureStyles();
            var panelRect = new Rect(panelPosition.x, panelPosition.y, panelSize.x, panelSize.y);
            GUI.Box(panelRect, GUIContent.none, panelStyle);
            GUI.Label(
                new Rect(panelRect.x + 18f, panelRect.y + 14f, panelRect.width - 36f, panelRect.height - 28f),
                BuildObjectiveText(),
                textStyle);
            var fps = Mathf.RoundToInt(1f / Mathf.Max(0.0001f, smoothedFrameDuration));
            GUI.Label(
                new Rect(panelRect.x + 18f, panelRect.yMax - 34f, panelRect.width - 36f, 24f),
                $"FPS: {fps}  |  State: {gameplayController.State}",
                textStyle);

            if (showWinPanel)
            {
                DrawWinPanel();
            }
        }

        private string BuildObjectiveText()
        {
            var tracker = gameplayController.ProgressTracker;
            var builder = new StringBuilder();
            builder.AppendLine("GOAL CHAIN");
            builder.AppendLine();
            var globalNoteIndex = 0;

            for (var rowIndex = 0; rowIndex < gameplayController.Level.ObjectiveRows.Count; rowIndex++)
            {
                var row = gameplayController.Level.ObjectiveRows[rowIndex];
                builder.Append(rowIndex == tracker.CurrentRowIndex && !tracker.IsComplete ? "> " : "  ");

                for (var noteIndex = 0; noteIndex < row.NoteCount; noteIndex++)
                {
                    var label = GetColorLabel(row.Notes[noteIndex]);
                    if (globalNoteIndex < tracker.MatchedTotal)
                    {
                        builder.Append('(').Append(label).Append(')');
                    }
                    else if (rowIndex == tracker.CurrentRowIndex && noteIndex == tracker.CurrentNoteIndex && !tracker.IsComplete)
                    {
                        builder.Append('[').Append(label).Append(']');
                    }
                    else
                    {
                        builder.Append(label);
                    }

                    if (noteIndex < row.NoteCount - 1)
                    {
                        builder.Append(" -> ");
                    }

                    globalNoteIndex++;
                }

                builder.AppendLine();
            }

            builder.AppendLine();
            builder.Append("Progress: ").Append(tracker.MatchedTotal).Append('/').Append(tracker.TotalNotes);
            return builder.ToString();
        }

        private void EnsureStyles()
        {
            panelStyle ??= new GUIStyle(GUI.skin.box)
            {
                normal = { background = Texture2D.grayTexture }
            };
            textStyle ??= new GUIStyle(GUI.skin.label)
            {
                fontSize = 18,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };
            winTitleStyle ??= new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 42,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(1f, 0.82f, 0.16f) }
            };
            winSubtitleStyle ??= new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 20,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };
        }

        private void DrawWinPanel()
        {
            var width = Mathf.Min(520f, Screen.width - 40f);
            var height = 210f;
            var rect = new Rect((Screen.width - width) * 0.5f, (Screen.height - height) * 0.5f, width, height);
            GUI.depth = -100;
            GUI.Box(rect, GUIContent.none, panelStyle);
            GUI.Label(new Rect(rect.x + 20f, rect.y + 35f, rect.width - 40f, 70f), "LEVEL COMPLETE!", winTitleStyle);
            GUI.Label(new Rect(rect.x + 20f, rect.y + 112f, rect.width - 40f, 45f), "FULL GROOVE", winSubtitleStyle);
        }

        private void Subscribe()
        {
            if (gameplayEvents == null)
            {
                return;
            }

            Unsubscribe();
            gameplayEvents.WinPresentationReady += HandleWinPresentationReady;
            gameplayEvents.ChainReset += HandleChainReset;
        }

        private void Unsubscribe()
        {
            if (gameplayEvents == null)
            {
                return;
            }

            gameplayEvents.WinPresentationReady -= HandleWinPresentationReady;
            gameplayEvents.ChainReset -= HandleChainReset;
        }

        private void HandleWinPresentationReady()
        {
            showWinPanel = true;
        }

        private void HandleChainReset()
        {
            showWinPanel = false;
        }

        private static string GetColorLabel(BeatColor color)
        {
            return color switch
            {
                BeatColor.Magenta => "M",
                BeatColor.Blue => "B",
                _ => "Y"
            };
        }
    }
}
