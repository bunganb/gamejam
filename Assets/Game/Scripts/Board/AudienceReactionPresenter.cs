using System;
using UnityEngine;

namespace GameJam.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class AudienceReactionPresenter : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
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
        [SerializeField] private Color baseColor = new Color(0.72f, 0.82f, 1f);

        private MemberCache[] cache;
        private MaterialPropertyBlock propertyBlock;
        private float energy;
        private float beatPulse;
        private float rowPulse;
        private float failurePulse;
        private StageReactionState state;

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
                    Renderers = member != null ? member.GetComponentsInChildren<Renderer>(true) : Array.Empty<Renderer>(),
                    Animator = member != null ? member.GetComponentInChildren<Animator>(true) : null
                };
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
                foreach (var targetRenderer in member.Renderers)
                {
                    if (targetRenderer == null)
                    {
                        continue;
                    }

                    var material = targetRenderer.sharedMaterial;
                    if (material == null || !material.HasProperty(ReactionEnergyId))
                    {
                        continue;
                    }

                    targetRenderer.GetPropertyBlock(propertyBlock);
                    if (material.HasProperty(BaseColorId))
                    {
                        propertyBlock.SetColor(BaseColorId, baseColor);
                    }
                    propertyBlock.SetFloat(ReactionEnergyId, energy);
                    propertyBlock.SetFloat(BeatPulseId, beatPulse);
                    propertyBlock.SetFloat(RowCompletePulseId, rowPulse);
                    propertyBlock.SetFloat(FailurePulseId, failurePulse);
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
            }
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
            public Renderer[] Renderers;
            public Animator Animator;
        }
    }
}
