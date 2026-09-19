using System.Numerics;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Shared.Animations;
using Robust.Shared.Map;
using Robust.Shared.Random;
using Robust.Shared.Spawners;
using static Robust.Client.Animations.AnimationTrackProperty;

namespace Content.Client.Animations;

public sealed partial class EntityPickupAnimationSystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;

    private const string AnimationKey = "fancy_pickup_anim";

    /// <summary>
    /// Scale the clone starts at.
    /// </summary>
    private const float StartScale = 0.75f;

    /// <summary>
    /// Scale the clone shrinks to by the end of travel, relative to the start scale.
    /// </summary>
    private const float TravelEndScale = StartScale * 0.65f;

    /// <summary>
    /// Scale the clone settles at during the fade-out.
    /// </summary>
    private const float FadeEndScale = 0.7f;

    /// <summary>
    /// Opacity the clone dims to by the end of travel.
    /// </summary>
    private const float TravelEndAlpha = 175f / 255f;

    /// <summary>
    /// Random tilt applied during travel, in either direction.
    /// </summary>
    private const double TiltDegrees = 30;

    /// <summary>
    /// Time the clone spends travelling toward the picker-upper.
    /// </summary>
    private static readonly TimeSpan TravelTime = TimeSpan.FromSeconds(0.3);

    /// <summary>
    /// Time the clone spends fading out after travelling.
    /// </summary>
    private static readonly TimeSpan FadeTime = TimeSpan.FromSeconds(0.1);

    /// <summary>
    /// Keeps the clone alive past the last keyframe so the final scale/alpha is applied before despawn.
    /// </summary>
    private static readonly TimeSpan DespawnGrace = TimeSpan.FromSeconds(0.05);

    /// <summary>
    /// Number of linear keyframes an eased track is baked into.
    /// Robust's Cubic interpolation is a Catmull-Rom spline through keyframes, not an easing curve,
    /// so ease-in-out is emulated by sampling an eased curve into several linear segments.
    /// </summary>
    private const int EasingSteps = 8;

    /// <summary>
    /// Vertical hop, in tiles, when the item is picked up from the same tile it lands on.
    /// </summary>
    private const float SameTileLiftY = 10f / 32f;

    /// <summary>
    /// Random sideways nudge, in tiles, for same-tile pickups.
    /// </summary>
    private const float SameTileJitterX = 6f / 32f;

    /// <summary>
    /// Distance, in tiles, under which a pickup counts as same-tile.
    /// </summary>
    private const float SameTileDistanceTiles = 0.5f;

    /// <summary>
    /// Spawns a clone of <paramref name="uid"/> that shrinks, tilts and eases toward <paramref name="final"/>,
    /// then fades out. Same-tile pickups hop upward instead of being skipped by the caller.
    /// </summary>
    private void AnimateFancyPickup(EntityUid uid, MetaDataComponent metadata, EntityCoordinates initial, Vector2 final, Angle initialAngle)
    {
        if (!TryComp(uid, out SpriteComponent? sprite0))
        {
            Log.Error($"{ToPrettyString(uid)} couldn't be animated for pickup since it doesn't have a {nameof(SpriteComponent)}!");
            return;
        }

        var start = initial.Position;
        var end = final;

        if ((end - start).Length() < SameTileDistanceTiles)
        {
            start.X += _random.Prob(0.5f) ? SameTileJitterX : -SameTileJitterX;
            end.Y += SameTileLiftY;
        }

        var clone = Spawn("clientsideclone", initial.WithPosition(start));
        EnsureComp<EntityPickupAnimationComponent>(clone);
        _metaData.SetEntityName(clone, metadata.EntityName);

        var sprite = Comp<SpriteComponent>(clone);
        _sprite.CopySprite((uid, sprite0), (clone, sprite));
        _sprite.SetVisible((clone, sprite), true);

        var baseScale = sprite0.Scale;
        var baseRotation = sprite0.Rotation;
        var baseColor = sprite0.Color;
        var tilt = baseRotation + Angle.FromDegrees(_random.Prob(0.5f) ? TiltDegrees : -TiltDegrees);

        _sprite.SetScale((clone, sprite), baseScale * StartScale);

        var despawn = EnsureComp<TimedDespawnComponent>(clone);
        despawn.Lifetime = (float) (TravelTime + FadeTime + DespawnGrace).TotalSeconds;
        _transform.SetLocalRotationNoLerp(clone, initialAngle);

        var fadeSeconds = (float) FadeTime.TotalSeconds;

        var positionTrack = NewTrack<TransformComponent>(nameof(TransformComponent.LocalPosition));
        BakeEased(positionTrack, TravelTime, t => Vector2.Lerp(start, end, t));

        var scaleTrack = NewTrack<SpriteComponent>(nameof(SpriteComponent.Scale));
        BakeEased(scaleTrack, TravelTime, t => baseScale * MathHelper.Lerp(StartScale, TravelEndScale, t));
        scaleTrack.KeyFrames.Add(new KeyFrame(baseScale * FadeEndScale, fadeSeconds));

        var rotationTrack = NewTrack<SpriteComponent>(nameof(SpriteComponent.Rotation));
        BakeEased(rotationTrack, TravelTime, t => Angle.Lerp(baseRotation, tilt, t));
        rotationTrack.KeyFrames.Add(new KeyFrame(baseRotation, fadeSeconds));

        var colorTrack = NewTrack<SpriteComponent>(nameof(SpriteComponent.Color));
        BakeEased(colorTrack, TravelTime, t => baseColor.WithAlpha(baseColor.A * MathHelper.Lerp(1f, TravelEndAlpha, t)));
        colorTrack.KeyFrames.Add(new KeyFrame(baseColor.WithAlpha(0f), fadeSeconds));

        var player = Comp<AnimationPlayerComponent>(clone);
        _animations.Play((clone, player), new Animation
        {
            Length = TravelTime + FadeTime,
            AnimationTracks =
            {
                positionTrack,
                scaleTrack,
                rotationTrack,
                colorTrack,
            },
        }, AnimationKey);
    }

    private static AnimationTrackComponentProperty NewTrack<TComp>(string property) where TComp : IComponent
    {
        return new AnimationTrackComponentProperty
        {
            ComponentType = typeof(TComp),
            Property = property,
            InterpolationMode = AnimationInterpolationMode.Linear,
        };
    }

    /// <summary>
    /// Fills <paramref name="track"/> with keyframes sampled along <paramref name="sample"/>,
    /// with the sample parameter run through an ease-in-out cubic curve.
    /// The first keyframe is placed at time 0 so the track applies immediately.
    /// </summary>
    private static void BakeEased(AnimationTrackComponentProperty track, TimeSpan duration, Func<float, object> sample)
    {
        var step = (float) duration.TotalSeconds / EasingSteps;

        track.KeyFrames.Add(new KeyFrame(sample(0f), 0f));

        for (var i = 1; i <= EasingSteps; i++)
        {
            var t = (float) i / EasingSteps;
            track.KeyFrames.Add(new KeyFrame(sample(EaseInOutCubic(t)), step));
        }
    }

    private static float EaseInOutCubic(float t)
    {
        return t < 0.5f
            ? 4f * t * t * t
            : 1f - MathF.Pow(-2f * t + 2f, 3f) / 2f;
    }
}
