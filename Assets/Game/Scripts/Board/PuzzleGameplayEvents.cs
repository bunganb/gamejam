using System;
using UnityEngine;

namespace GameJam.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class PuzzleGameplayEvents : MonoBehaviour
    {
        public event Action<Vector2Int, BeatColor> TileActivated;
        public event Action<GameplayProgressSnapshot> ChainAdvanced;
        public event Action<GameplayProgressSnapshot> ObjectiveRowCompleted;
        public event Action<GameplayProgressSnapshot> ChainFailed;
        public event Action ChainReset;
        public event Action<GameplayProgressSnapshot> ChainCompleted;
        public event Action WinPresentationReady;

        public void PublishTileActivated(Vector2Int coordinate, BeatColor color) => TileActivated?.Invoke(coordinate, color);
        public void PublishChainAdvanced(GameplayProgressSnapshot snapshot) => ChainAdvanced?.Invoke(snapshot);
        public void PublishObjectiveRowCompleted(GameplayProgressSnapshot snapshot) => ObjectiveRowCompleted?.Invoke(snapshot);
        public void PublishChainFailed(GameplayProgressSnapshot snapshot) => ChainFailed?.Invoke(snapshot);
        public void PublishChainReset() => ChainReset?.Invoke();
        public void PublishChainCompleted(GameplayProgressSnapshot snapshot) => ChainCompleted?.Invoke(snapshot);
        public void PublishWinPresentationReady() => WinPresentationReady?.Invoke();
    }
}
