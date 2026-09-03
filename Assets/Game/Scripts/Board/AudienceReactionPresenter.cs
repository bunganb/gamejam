using System;
using UnityEngine;

namespace GameJam.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class AudienceReactionPresenter : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int ReactionEnergyId = Shader.PropertyToID("_ReactionEnergy");
        private static readonly int BeatPulseId = Shader.PropertyToID("_BeatPulse");
        private static readonly int RowCompletePulseId = Shader.PropertyToID("_RowCompletePulse");
        private static readonly int FailurePulseId = Shader.PropertyToID("_FailurePulse");
        private static readonly int ReactionEnergyAnimatorId = Animator.StringToHash("ReactionEnergy");
        private static readonly int ReactionStateAnimatorId = Animator.StringToHash("ReactionState");
        private static readonly int BeatAnimatorId = Animator.StringToHash("Beat");
        private static readonly int RowCompleteAnimatorId = Animator.StringToHash("RowComplete");
        private static readonly int BooAnimatorId = Animator.StringToHash("Boo");
        private static readonly int FullGrooveAnimatorId = Animator.StringToHash("FullGroove");

        [SerializeField] private Transform[] members;
        [SerializeField] private StageReactionProfile profile;
        private MemberCache[] cache;
        private MaterialPropertyBlock propertyBlock;
        private float energy;
        private float beatPulse;
        private float rowPulse;
        private float failurePulse;
        private StageReactionState state;

        public int ReactiveRendererCount { get; private set; }

        public void ConfigureReferences(Transform[] audienceMembers, StageReactionProfile reactionProfile)
        {
            RestoreBaselines();
            members = audienceMembers;
            profile = reactionProfile;
            RebuildCache();
        }

        public void Apply(StageReactionState reactionState, float reactionEnergy, float beat, float row, float failure)
        {
            var triggerBeat = beat >= 0.99f && beatPulse < 0.99f;
            var triggerRow = row >= 0.99f && rowPulse < 0.99f;
            var triggerFailure = failure >= 0.99f && failurePulse < 0.99f;
            var triggerFullGroove = reactionState == StageReactionState.FullGroove && state != StageReactionState.FullGroove;
            state = reactionState;
            energy = Mathf.Clamp01(reactionEnergy);
            beatPulse = Mathf.Clamp01(beat);
            rowPulse = Mathf.Clamp01(row);
            failurePulse = Mathf.Clamp01(failure);
            ApplyMaterialAndAnimatorState(triggerBeat, triggerRow, triggerFailure, triggerFullGroove);
        }

        private void OnEnable()
        {
            if (cache == null || cache.Length != (members?.Length ?? 0))
            {
                RebuildCache();
            }
        }

        private void OnDisable() => RestoreBaselines();

        private void LateUpdate()
        {
            if (cache == null || profile == null)
            {
                return;
            }

            var time = Time.unscaledTime * profile.AudienceDanceSpeed;
            for (var index = 0; index < cache.Length; index++)
            {
                ref var member = ref cache[index];
                if (member.Transform == null)
                {
                    continue;
                }

                var phase = index * 0.73f;
                var fullBoost = state == StageReactionState.FullGroove ? 1.35f : 1f;
                var strength = energy * fullBoost;
                var wave = Mathf.Sin(time + phase);
                var secondary = Mathf.Sin(time * 0.5f + phase * 1.7f);
                var bob = Mathf.Abs(wave) * profile.AudienceBobHeight * strength;
                var booDrop = failurePulse * 0.10f;
                member.Transform.localPosition = member.BasePosition + Vector3.up * (bob - booDrop);
                member.Transform.localRotation = member.BaseRotation
                    * Quaternion.Euler(0f, secondary * 4f * strength, wave * profile.AudienceSwayAngle * strength);
                var squash = wave * 0.035f * strength - failurePulse * 0.08f;
                member.Transform.localScale = Vector3.Scale(member.BaseScale, new Vector3(1f - squash, 1f + squash, 1f - squash));
            }
        }

        private void RebuildCache()
        {
            if (members == null)
            {
                cache = Array.Empty<MemberCache>();
                return;
            }

            cache = new MemberCache[members.Length];
            for (var index = 0; index < members.Length; index++)
            {
                var member = members[index];
                cache[index] = new MemberCache
                {
                    Transform = member,
                    BasePosition = member != null ? member.localPosition : Vector3.zero,
                    BaseRotation = member != null ? member.localRotation : Quaternion.identity,
                    BaseScale = member != null ? member.localScale : Vector3.one,
                    Renderers = BuildRendererCache(member),
                    Animator = member != null ? member.GetComponentInChildren<Animator>(true) : null
                };
            }

            ReactiveRendererCount = 0;
            foreach (var member in cache)
            {
                ReactiveRendererCount += member.Renderers.Length;
            }
        }

        private void ApplyMaterialAndAnimatorState(bool triggerBeat, bool triggerRow, bool triggerFailure, bool triggerFullGroove)
        {
            if (cache == null)
            {
                return;
            }

            propertyBlock ??= new MaterialPropertyBlock();
            foreach (var member in cache)
            {
                foreach (var rendererCache in member.Renderers)
                {
                    var targetRenderer = rendererCache.Renderer;
                    if (targetRenderer == null)
                    {
                        continue;
                    }

                    targetRenderer.GetPropertyBlock(propertyBlock);
                    if (rendererCache.SupportsReactionShader)
                    {
                        propertyBlock.SetColor(BaseColorId, rendererCache.BaselineColor);
                        propertyBlock.SetFloat(ReactionEnergyId, energy);
                        propertyBlock.SetFloat(BeatPulseId, beatPulse);
                        propertyBlock.SetFloat(RowCompletePulseId, rowPulse);
                        propertyBlock.SetFloat(FailurePulseId, failurePulse);
                    }
                    else if (rendererCache.ColorPropertyId != 0)
                    {
                        propertyBlock.SetColor(rendererCache.ColorPropertyId, EvaluateFallbackColor(rendererCache.BaselineColor));
                    }
                    targetRenderer.SetPropertyBlock(propertyBlock);
                }

                if (member.Animator != null)
                {
                    SetAnimatorFloatIfPresent(member.Animator, ReactionEnergyAnimatorId, energy);
                    SetAnimatorIntegerIfPresent(member.Animator, ReactionStateAnimatorId, (int)state);
                    SetAnimatorTriggerIfPresent(member.Animator, BeatAnimatorId, triggerBeat);
                    SetAnimatorTriggerIfPresent(member.Animator, RowCompleteAnimatorId, triggerRow);
                    SetAnimatorTriggerIfPresent(member.Animator, BooAnimatorId, triggerFailure);
                    SetAnimatorTriggerIfPresent(member.Animator, FullGrooveAnimatorId, triggerFullGroove);
                }
            }
        }

        private void RestoreBaselines()
        {
            if (cache == null)
            {
                return;
            }

            foreach (var member in cache)
            {
                if (member.Transform == null)
                {
                    continue;
                }

                member.Transform.localPosition = member.BasePosition;
                member.Transform.localRotation = member.BaseRotation;
                member.Transform.localScale = member.BaseScale;

                foreach (var rendererCache in member.Renderers)
                {
                    if (rendererCache.Renderer == null || rendererCache.ColorPropertyId == 0)
                    {
                        continue;
                    }

                    propertyBlock ??= new MaterialPropertyBlock();
                    rendererCache.Renderer.GetPropertyBlock(propertyBlock);
                    propertyBlock.SetColor(rendererCache.ColorPropertyId, rendererCache.BaselineColor);
                    if (rendererCache.SupportsReactionShader)
                    {
                        propertyBlock.SetFloat(ReactionEnergyId, 0f);
                        propertyBlock.SetFloat(BeatPulseId, 0f);
                        propertyBlock.SetFloat(RowCompletePulseId, 0f);
                        propertyBlock.SetFloat(FailurePulseId, 0f);
                    }
                    rendererCache.Renderer.SetPropertyBlock(propertyBlock);
                }
            }
        }

        private Color EvaluateFallbackColor(Color original)
        {
            const float minimumLuminance = 0.001f;
            var grooveTint = Color.Lerp(
                original,
                new Color(0.72f, 0.48f, 1f, original.a),
                Mathf.Clamp01(energy) * 0.12f);
            var luminanceScale = original.grayscale / Mathf.Max(minimumLuminance, grooveTint.grayscale);
            grooveTint = new Color(
                grooveTint.r * luminanceScale,
                grooveTint.g * luminanceScale,
                grooveTint.b * luminanceScale,
                original.a);
            var brightness = 1f
                + Mathf.Clamp01(energy) * 0.02f
                + Mathf.Clamp01(beatPulse) * 0.03f
                + Mathf.Clamp01(rowPulse) * 0.05f;
            var energized = new Color(
                grooveTint.r * brightness,
                grooveTint.g * brightness,
                grooveTint.b * brightness,
                original.a);
            return Color.Lerp(energized, new Color(0.18f, 0.04f, 0.24f, original.a), failurePulse * 0.75f);
        }

        private static RendererCache[] BuildRendererCache(Transform member)
        {
            if (member == null)
            {
                return Array.Empty<RendererCache>();
            }

            var renderers = member.GetComponentsInChildren<Renderer>(true);
            var result = new RendererCache[renderers.Length];
            for (var index = 0; index < renderers.Length; index++)
            {
                var renderer = renderers[index];
                var material = renderer != null ? renderer.sharedMaterial : null;
                var supportsReaction = material != null && material.HasProperty(ReactionEnergyId);
                var colorPropertyId = material != null && material.HasProperty(BaseColorId)
                    ? BaseColorId
                    : material != null && material.HasProperty(ColorId) ? ColorId : 0;
                var baselineColor = colorPropertyId != 0 ? material.GetColor(colorPropertyId) : Color.white;
                result[index] = new RendererCache
                {
                    Renderer = renderer,
                    SupportsReactionShader = supportsReaction,
                    ColorPropertyId = colorPropertyId,
                    BaselineColor = baselineColor
                };
            }

            return result;
        }

        private static void SetAnimatorFloatIfPresent(Animator animator, int id, float value)
        {
            foreach (var parameter in animator.parameters)
            {
                if (parameter.nameHash == id && parameter.type == AnimatorControllerParameterType.Float)
                {
                    animator.SetFloat(id, value);
                    return;
                }
            }
        }

        private static void SetAnimatorIntegerIfPresent(Animator animator, int id, int value)
        {
            foreach (var parameter in animator.parameters)
            {
                if (parameter.nameHash == id && parameter.type == AnimatorControllerParameterType.Int)
                {
                    animator.SetInteger(id, value);
                    return;
                }
            }
        }

        private static void SetAnimatorTriggerIfPresent(Animator animator, int id, bool shouldTrigger)
        {
            if (!shouldTrigger)
            {
                return;
            }

            foreach (var parameter in animator.parameters)
            {
                if (parameter.nameHash == id && parameter.type == AnimatorControllerParameterType.Trigger)
                {
                    animator.SetTrigger(id);
                    return;
                }
            }
        }

        private struct MemberCache
        {
            public Transform Transform;
            public Vector3 BasePosition;
            public Quaternion BaseRotation;
            public Vector3 BaseScale;
            public RendererCache[] Renderers;
            public Animator Animator;
        }

        private struct RendererCache
        {
            public Renderer Renderer;
            public bool SupportsReactionShader;
            public int ColorPropertyId;
            public Color BaselineColor;
        }
    }
}
