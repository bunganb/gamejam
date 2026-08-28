using UnityEngine;

namespace GameJam.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class StageRingPresenter : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
        private static readonly int ReactionEnergyId = Shader.PropertyToID("_ReactionEnergy");
        private static readonly int StageProgressId = Shader.PropertyToID("_StageProgress");
        private static readonly int SegmentFillId = Shader.PropertyToID("_SegmentFill");
        private static readonly int BeatPulseId = Shader.PropertyToID("_BeatPulse");
        private static readonly int RowCompletePulseId = Shader.PropertyToID("_RowCompletePulse");
        private static readonly int FailurePulseId = Shader.PropertyToID("_FailurePulse");

        [SerializeField] private Renderer[] orderedSegments;
        [SerializeField] private Color dimColor = new Color(0.06f, 0.01f, 0.10f);
        [SerializeField] private Color[] segmentColors =
        {
            new Color(0.05f, 0.25f, 1f),
            new Color(1f, 0.65f, 0.05f),
            new Color(1f, 0.05f, 0.35f),
            new Color(0.75f, 0.05f, 1f)
        };

        private MaterialPropertyBlock propertyBlock;

        public void ConfigureReferences(Renderer[] segments)
        {
            orderedSegments = segments;
            Apply(0f, 0f, 0f, 0f, 0f);
        }

        public void Apply(float progress, float energy, float beatPulse, float rowPulse, float failurePulse)
        {
            if (orderedSegments == null)
            {
                return;
            }

            propertyBlock ??= new MaterialPropertyBlock();
            for (var index = 0; index < orderedSegments.Length; index++)
            {
                var segment = orderedSegments[index];
                if (segment == null)
                {
                    continue;
                }

                var fill = StageReactionMath.SegmentFill(progress, index, orderedSegments.Length);
                var activeColor = segmentColors != null && segmentColors.Length > 0
                    ? segmentColors[index % segmentColors.Length]
                    : Color.magenta;
                var emission = Color.Lerp(dimColor, activeColor * (1.2f + energy), fill);

                segment.GetPropertyBlock(propertyBlock);
                propertyBlock.SetColor(BaseColorId, Color.Lerp(dimColor, activeColor, fill));
                propertyBlock.SetColor(EmissionColorId, emission);
                propertyBlock.SetFloat(ReactionEnergyId, energy);
                propertyBlock.SetFloat(StageProgressId, progress);
                propertyBlock.SetFloat(SegmentFillId, fill);
                propertyBlock.SetFloat(BeatPulseId, beatPulse);
                propertyBlock.SetFloat(RowCompletePulseId, rowPulse);
                propertyBlock.SetFloat(FailurePulseId, failurePulse);
                segment.SetPropertyBlock(propertyBlock);
            }
        }
    }
}
