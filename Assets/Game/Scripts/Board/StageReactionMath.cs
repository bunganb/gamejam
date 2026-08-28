using UnityEngine;

namespace GameJam.Gameplay
{
    public static class StageReactionMath
    {
        public static StageReactionState ResolveState(float progress, bool completed)
        {
            if (completed || progress >= 1f)
            {
                return StageReactionState.FullGroove;
            }

            return progress > 0f ? StageReactionState.Groove : StageReactionState.Hening;
        }

        public static float SegmentFill(float progress, int segmentIndex, int segmentCount = 4)
        {
            if (segmentCount <= 0 || segmentIndex < 0 || segmentIndex >= segmentCount)
            {
                return 0f;
            }

            return Mathf.Clamp01(Mathf.Clamp01(progress) * segmentCount - segmentIndex);
        }
    }
}
