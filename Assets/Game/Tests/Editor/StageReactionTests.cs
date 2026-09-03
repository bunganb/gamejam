using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace GameJam.Gameplay.Tests
{
    public sealed class StageReactionTests
    {
        [TestCase(0f, StageReactionState.Hening)]
        [TestCase(0.01f, StageReactionState.Groove)]
        [TestCase(0.99f, StageReactionState.Groove)]
        [TestCase(1f, StageReactionState.FullGroove)]
        public void Progress_ResolvesExpectedState(float progress, StageReactionState expected)
        {
            Assert.That(StageReactionMath.ResolveState(progress, false), Is.EqualTo(expected));
        }

        [Test]
        public void RingFill_AdvancesClockwiseAcrossFourSegments()
        {
            Assert.That(StageReactionMath.SegmentFill(0f, 0), Is.Zero);
            Assert.That(StageReactionMath.SegmentFill(0.125f, 0), Is.EqualTo(0.5f).Within(0.001f));
            Assert.That(StageReactionMath.SegmentFill(0.25f, 0), Is.EqualTo(1f));
            Assert.That(StageReactionMath.SegmentFill(0.25f, 1), Is.Zero);
            Assert.That(StageReactionMath.SegmentFill(0.625f, 2), Is.EqualTo(0.5f).Within(0.001f));
            Assert.That(StageReactionMath.SegmentFill(1f, 3), Is.EqualTo(1f));
        }

        [Test]
        public void GameplayEvents_DriveDirectorAndResetImmediately()
        {
            var eventObject = new GameObject("Events");
            var directorObject = new GameObject("Director");
            var profile = ScriptableObject.CreateInstance<StageReactionProfile>();
            try
            {
                profile.ApplyPrototypeDefaults();
                var eventHub = eventObject.AddComponent<PuzzleGameplayEvents>();
                var director = directorObject.AddComponent<StageReactionDirector>();
                director.ConfigureReferences(eventHub, profile, null, null, null, null);

                var snapshot = new GameplayProgressSnapshot(0, 0, 1, 4, BeatColor.Blue, BeatColor.Blue, Vector2Int.zero);
                eventHub.PublishChainAdvanced(snapshot);
                Assert.That(director.State, Is.EqualTo(StageReactionState.Groove));
                Assert.That(director.TargetProgress, Is.EqualTo(0.25f).Within(0.001f));

                eventHub.PublishChainCompleted(new GameplayProgressSnapshot(0, 3, 4, 4, BeatColor.Magenta, BeatColor.Magenta, Vector2Int.one));
                Assert.That(director.State, Is.EqualTo(StageReactionState.FullGroove));
                Assert.That(director.TargetProgress, Is.EqualTo(1f));

                eventHub.PublishChainReset();
                Assert.That(director.State, Is.EqualTo(StageReactionState.Hening));
                Assert.That(director.TargetProgress, Is.Zero);
                Assert.That(director.VisualProgress, Is.Zero);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(eventObject);
                UnityEngine.Object.DestroyImmediate(directorObject);
                UnityEngine.Object.DestroyImmediate(profile);
            }
        }

        [Test]
        public void SpotlightProfile_UsesFourLightFriendlyDefaults()
        {
            var profile = ScriptableObject.CreateInstance<StageReactionProfile>();
            try
            {
                profile.ApplyPrototypeDefaults();
                Assert.That(profile.SpotlightRange, Is.EqualTo(14f));
                Assert.That(profile.SpotlightInnerAngle, Is.LessThan(profile.SpotlightOuterAngle));
                Assert.That(profile.SpotlightFadeDuration, Is.GreaterThan(0f));
                Assert.That(profile.SpotlightColors, Has.Length.EqualTo(3));
                Assert.That(profile.RgbFilterIntensity, Is.InRange(0f, 0.5f));
                Assert.That(profile.RgbFilterFrequency, Is.InRange(0.5f, 6f));
                Assert.That(profile.RgbFilterColors, Has.Length.EqualTo(3));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(profile);
            }
        }

        [Test]
        public void ReactionVolumeProfiles_ContainRequiredOverrides()
        {
            var reactionProfile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(
                "Assets/Game/Data/StageReactionVolumeProfile_Prototype.asset");
            var gameplayProfile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(
                "Assets/Game/Data/GameplayClubVolumeProfile.asset");

            Assert.That(reactionProfile, Is.Not.Null);
            Assert.That(reactionProfile.TryGet(out Bloom reactionBloom), Is.True);
            Assert.That(reactionProfile.TryGet(out ColorAdjustments reactionColor), Is.True);
            Assert.That(reactionBloom.intensity.overrideState, Is.True);
            Assert.That(reactionColor.colorFilter.overrideState, Is.True);

            Assert.That(gameplayProfile, Is.Not.Null);
            Assert.That(gameplayProfile.TryGet(out Tonemapping tonemapping), Is.True);
            Assert.That(gameplayProfile.TryGet(out Bloom gameplayBloom), Is.True);
            Assert.That(gameplayProfile.TryGet(out ColorAdjustments gameplayColor), Is.True);
            Assert.That(gameplayProfile.TryGet(out Vignette vignette), Is.True);
            Assert.That(tonemapping.mode.value, Is.EqualTo(TonemappingMode.ACES));
            Assert.That(gameplayBloom.intensity.value, Is.EqualTo(0.15f).Within(0.001f));
            Assert.That(gameplayColor.contrast.value, Is.EqualTo(5f).Within(0.001f));
            Assert.That(vignette.intensity.value, Is.EqualTo(0.12f).Within(0.001f));
        }

        [Test]
        public void AudiencePresenter_FallbackColorReactsAndRestoresWithoutReplacingMaterial()
        {
            var root = new GameObject("AudiencePresenterTest");
            var member = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var profile = ScriptableObject.CreateInstance<StageReactionProfile>();
            var shader = Shader.Find("Universal Render Pipeline/Unlit");
            var material = new Material(shader);
            try
            {
                member.transform.SetParent(root.transform);
                var renderer = member.GetComponent<Renderer>();
                var colorId = Shader.PropertyToID("_BaseColor");
                var baseline = new Color(0.2f, 0.4f, 0.3f, 1f);
                material.SetColor(colorId, baseline);
                renderer.sharedMaterial = material;
                profile.ApplyPrototypeDefaults();

                var presenter = root.AddComponent<AudienceReactionPresenter>();
                presenter.ConfigureReferences(new[] { member.transform }, profile);
                presenter.Apply(StageReactionState.Groove, 1f, 1f, 1f, 0f);

                var block = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(block);
                Assert.That(presenter.ReactiveRendererCount, Is.EqualTo(1));
                Assert.That(block.GetColor(colorId), Is.Not.EqualTo(baseline));
                Assert.That(
                    block.GetColor(colorId).grayscale,
                    Is.LessThanOrEqualTo(baseline.grayscale * 1.101f));
                Assert.That(renderer.sharedMaterial, Is.SameAs(material));

                presenter.Apply(StageReactionState.Hening, 0f, 0f, 0f, 0f);
                renderer.GetPropertyBlock(block);
                var restored = block.GetColor(colorId);
                Assert.That(restored.r, Is.EqualTo(baseline.r).Within(0.0001f));
                Assert.That(restored.g, Is.EqualTo(baseline.g).Within(0.0001f));
                Assert.That(restored.b, Is.EqualTo(baseline.b).Within(0.0001f));
                Assert.That(restored.a, Is.EqualTo(baseline.a).Within(0.0001f));
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(material);
                Object.DestroyImmediate(profile);
            }
        }

        [Test]
        public void TileSurface_UsesEmissionAndKeepsDormantPadDim()
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(
                "Assets/Game/Art/Prototype/Materials/M_Prototype_Emissive.mat");
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Game/Prefabs/Prototype/TileSlot.prefab");

            Assert.That(material, Is.Not.Null);
            Assert.That(material.IsKeywordEnabled("_EMISSION"), Is.True);
            Assert.That(prefab, Is.Not.Null);

            var instance = Object.Instantiate(prefab);
            try
            {
                var tile = instance.GetComponent<TileSlot>();
                var surface = instance.transform.Find("VisualRoot/EmissiveSurface")?.GetComponent<Renderer>()
                    ?? instance.GetComponentInChildren<Renderer>();
                var block = new MaterialPropertyBlock();
                var emissionId = Shader.PropertyToID("_EmissionColor");

                tile.SetActiveState(true);
                tile.SetColor(BeatColor.Magenta);
                surface.GetPropertyBlock(block);
                Assert.That(block.GetColor(emissionId).maxColorComponent, Is.InRange(1.2f, 1.6f));

                tile.SetActiveState(false);
                tile.SetColor(BeatColor.Magenta);
                surface.GetPropertyBlock(block);
                Assert.That(block.GetColor(emissionId).maxColorComponent, Is.LessThan(0.01f));
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }

        [Test]
        public void EnvironmentMaterialOverrides_AreExternalAssets()
        {
            var categories = new[] { "StageFloor", "Speaker", "Grave", "Wall", "Ground" };
            foreach (var category in categories)
            {
                var material = AssetDatabase.LoadAssetAtPath<Material>(
                    $"Assets/Game/Art/Prototype/Materials/M_Environment_{category}.mat");
                Assert.That(material, Is.Not.Null, $"Missing external material for {category}.");
                Assert.That(AssetDatabase.IsSubAsset(material), Is.False);
            }
        }

        [Test]
        public void BaseLighting_FollowsReactionStateAndKeepsShadowsDisabled()
        {
            var root = new GameObject("BaseLightingTest");
            var fill = new GameObject("Fill").AddComponent<Light>();
            var rim = new GameObject("Rim").AddComponent<Light>();
            var profile = ScriptableObject.CreateInstance<StageReactionProfile>();
            try
            {
                fill.transform.SetParent(root.transform);
                rim.transform.SetParent(root.transform);
                profile.ApplyPrototypeDefaults();
                var presenter = root.AddComponent<StageBaseLightingPresenter>();
                presenter.ConfigureReferences(fill, rim, profile);

                presenter.Apply(StageReactionState.Hening, 0f, 0f, 0f, 0f);
                var heningIntensity = fill.intensity;
                presenter.Apply(StageReactionState.Groove, 0.5f, 1f, 0f, 0f);
                var grooveIntensity = fill.intensity;
                presenter.Apply(StageReactionState.FullGroove, 1f, 0f, 1f, 0f);

                Assert.That(heningIntensity, Is.EqualTo(profile.HeningLightIntensity).Within(0.001f));
                Assert.That(grooveIntensity, Is.GreaterThan(heningIntensity));
                Assert.That(fill.intensity, Is.GreaterThan(grooveIntensity));
                Assert.That(fill.shadows, Is.EqualTo(LightShadows.None));
                Assert.That(rim.shadows, Is.EqualTo(LightShadows.None));
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(profile);
            }
        }
    }
}
