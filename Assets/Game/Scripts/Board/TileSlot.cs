using UnityEngine;

namespace GameJam.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class TileSlot : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        [SerializeField] private Vector2Int coordinate;
        [SerializeField] private Transform visualRoot;
        [SerializeField] private Renderer surfaceRenderer;
        [SerializeField] private BoxCollider tileCollider;
        [SerializeField] private Transform playerStandPoint;
        [SerializeField] private Transform vfxPoint;
        [SerializeField] private Color magentaColor = new(1f, 0.05f, 0.55f, 1f);
        [SerializeField] private Color blueColor = new(0.05f, 0.45f, 1f, 1f);
        [SerializeField] private Color yellowColor = new(1f, 0.75f, 0.05f, 1f);
        [SerializeField] private Color inactiveColor = new(0.1f, 0.055f, 0.18f, 1f);

        private MaterialPropertyBlock propertyBlock;
        private BeatColor initialColor;
        private BeatColor currentColor;
        private bool isActiveCell;

        public Vector2Int Coordinate => coordinate;
        public Transform PlayerStandPoint => playerStandPoint;
        public Transform VfxPoint => vfxPoint;
        public BeatColor CurrentColor => currentColor;
        public bool IsActiveCell => isActiveCell;

        public void ConfigureReferences(
            Vector2Int newCoordinate,
            Transform newVisualRoot,
            Renderer newSurfaceRenderer,
            BoxCollider newCollider,
            Transform newPlayerStandPoint,
            Transform newVfxPoint)
        {
            coordinate = newCoordinate;
            visualRoot = newVisualRoot;
            surfaceRenderer = newSurfaceRenderer;
            tileCollider = newCollider;
            playerStandPoint = newPlayerStandPoint;
            vfxPoint = newVfxPoint;
        }

        public void ApplyCellDefinition(BoardCellDefinition definition)
        {
            if (definition == null)
            {
                throw new System.ArgumentNullException(nameof(definition));
            }

            initialColor = definition.InitialColor;
            currentColor = initialColor;
            SetActiveState(definition.IsActive);
            ApplyColor(currentColor);
        }

        public void SetActiveState(bool isActive)
        {
            isActiveCell = isActive;
            if (visualRoot != null)
            {
                // Dormant pads remain visible so the complete launchpad layout is readable.
                visualRoot.gameObject.SetActive(true);
            }

            if (tileCollider != null)
            {
                tileCollider.enabled = isActive;
            }
        }

        public void SetColor(BeatColor color)
        {
            currentColor = color;
            ApplyColor(color);
        }

        public BeatColor AdvanceColor()
        {
            if (!isActiveCell)
            {
                throw new System.InvalidOperationException($"Dormant tile {coordinate} cannot be activated.");
            }

            currentColor = currentColor.Next();
            ApplyColor(currentColor);
            return currentColor;
        }

        public void ResetVisual()
        {
            currentColor = initialColor;
            ApplyColor(currentColor);
        }

        private void ApplyColor(BeatColor color)
        {
            if (surfaceRenderer == null)
            {
                return;
            }

            propertyBlock ??= new MaterialPropertyBlock();
            surfaceRenderer.GetPropertyBlock(propertyBlock);
            var displayColor = isActiveCell ? GetDisplayColor(color) : inactiveColor;
            propertyBlock.SetColor(BaseColorId, displayColor);
            propertyBlock.SetColor(EmissionColorId, isActiveCell ? displayColor * 1.5f : displayColor * 0.08f);
            surfaceRenderer.SetPropertyBlock(propertyBlock);
        }

        private Color GetDisplayColor(BeatColor color)
        {
            return color switch
            {
                BeatColor.Blue => blueColor,
                BeatColor.Yellow => yellowColor,
                _ => magentaColor
            };
        }
    }
}
