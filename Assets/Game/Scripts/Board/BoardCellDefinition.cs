using System;
using UnityEngine;

namespace GameJam.Gameplay
{
    [Serializable]
    public sealed class BoardCellDefinition
    {
        [SerializeField] private bool isActive;
        [SerializeField] private BeatColor initialColor;

        public bool IsActive => isActive;
        public BeatColor InitialColor => initialColor;

        public BoardCellDefinition(bool isActive, BeatColor initialColor)
        {
            this.isActive = isActive;
            this.initialColor = initialColor;
        }
    }
}
