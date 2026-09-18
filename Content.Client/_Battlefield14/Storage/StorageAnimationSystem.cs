using System.Numerics;
using Content.Shared._Battlefield14.Storage;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Shared.Animations;

namespace Content.Client._Battlefield14.Storage;

/// <summary>
/// Plays a quick squash-and-stretch "rustle" on a storage's sprite as feedback for inserting into,
/// removing from, or opening it.
/// </summary>
public sealed class StorageAnimationSystem : EntitySystem
{
    [Dependency] private readonly AnimationPlayerSystem _animation = default!;

    private const string AnimationKey = "storage-rustle";

    /// <summary>
    /// Scale multiplier the sprite squashes to before springing back.
    /// </summary>
    private static readonly Vector2 SquashScale = new(1.07f, 0.9f);

    /// <summary>
    /// Time to squash. Kept short so the rustle keeps up with rapid inserts.
    /// </summary>
    private const float SquashTime = 0.1f;

    /// <summary>
    /// Time to spring back to the resting scale.
    /// </summary>
    private const float RestoreTime = 0.15f;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<StorageAnimationEvent>(OnStorageAnimation);
    }

    private void OnStorageAnimation(StorageAnimationEvent msg)
    {
        Play(GetEntity(msg.Storage));
    }

    public void Play(EntityUid uid)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite))
            return;

        var player = EnsureComp<AnimationPlayerComponent>(uid);

        // Let the current rustle finish so we always spring back to the true resting scale.
        if (_animation.HasRunningAnimation(player, AnimationKey))
            return;

        var restingScale = sprite.Scale;

        var animation = new Animation
        {
            Length = TimeSpan.FromSeconds(SquashTime + RestoreTime),
            AnimationTracks =
            {
                new AnimationTrackComponentProperty
                {
                    ComponentType = typeof(SpriteComponent),
                    Property = nameof(SpriteComponent.Scale),
                    InterpolationMode = AnimationInterpolationMode.Linear,
                    KeyFrames =
                    {
                        new AnimationTrackProperty.KeyFrame(restingScale, 0f),
                        new AnimationTrackProperty.KeyFrame(restingScale * SquashScale, SquashTime),
                        new AnimationTrackProperty.KeyFrame(restingScale, RestoreTime),
                    },
                },
            },
        };

        _animation.Play((uid, player), animation, AnimationKey);
    }
}
