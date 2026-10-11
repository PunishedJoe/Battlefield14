/*
 * This file is sublicensed under MIT License
 * https://github.com/space-wizards/space-station-14/blob/master/LICENSE.TXT
 */

using Content.Shared._CE.ZLevels.Core.Components;
using Content.Shared._CE.ZLevels.Core.EntitySystems;
using Robust.Client.Player;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Physics.Systems;

namespace Content.Client._CE.ZLevels.Audio;

/// <summary>
/// Mirrors positional audio that arrives from another z-level onto the local player's level, so that sounds are
/// synchronized between z-levels and keep a spatial position.
/// The sound is attenuated by the vertical distance and muffled when an opaque floor/ceiling blocks the path.
/// </summary>
public sealed partial class CEClientZLevelsAudioSystem : EntitySystem
{
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedTransformSystem _xform = default!;
    [Dependency] private IPlayerManager _player = default!;
    [Dependency] private CESharedZLevelsSystem _zLevels = default!;

    private EntityQuery<TransformComponent> _xformQuery = default!;
    private EntityQuery<CEZMapComponent> _zMapQuery = default!;

    private readonly Dictionary<EntityUid, List<EntityUid>> _echoes = new();
    private readonly HashSet<EntityUid> _pending = new();
    private readonly List<EntityUid> _processing = new();

    private bool _spawningEcho;

    public override void Initialize()
    {
        base.Initialize();

        _xformQuery = GetEntityQuery<TransformComponent>();
        _zMapQuery = GetEntityQuery<CEZMapComponent>();

        // AudioComponent already subscribes to ComponentStartup/Shutdown in the engine, and the event bus only allows
        // a single subscription per (component, event) pair. ComponentAdd is free, so we hook that and process on the
        // next frame once the networked state (FileName, Global, Params) has been applied.
        SubscribeLocalEvent<AudioComponent, ComponentAdd>(OnAudioAdd);
    }

    private void OnAudioAdd(Entity<AudioComponent> ent, ref ComponentAdd args)
    {
        if (_spawningEcho)
            return;

        _pending.Add(ent.Owner);
    }

    private void ProcessPendingAudio()
    {
        if (_pending.Count == 0)
            return;

        _processing.Clear();
        _processing.AddRange(_pending);
        _pending.Clear();

        foreach (var uid in _processing)
        {
            if (TerminatingOrDeleted(uid) || !TryComp<AudioComponent>(uid, out var audio))
                continue;

            TryStartEcho(uid, audio);
        }
    }

    private void TryStartEcho(EntityUid ent, AudioComponent audio)
    {
        // Our own echoes must never be echoed again.
        if (HasComp<CEZLevelAudioEchoComponent>(ent))
            return;

        // Global audio has no position and already plays on every map.
        if (audio.Global || string.IsNullOrEmpty(audio.FileName))
            return;

        if (!TryGetListener(out var listenerMap, out var listenerZ))
            return;

        if (!_xformQuery.TryComp(ent, out var xform) || xform.MapUid is not { } sourceMap)
            return;

        if (sourceMap == listenerMap)
            return;

        if (!_zMapQuery.TryComp(sourceMap, out var sourceZ) ||
            sourceZ.NetworkUid != listenerZ.NetworkUid)
            return;

        var depthDelta = listenerZ.Depth - sourceZ.Depth;
        if (Math.Abs(depthDelta) > CESharedZLevelsSystem.MaxZLevelsAudioSync)
            return;

        SpawnEcho(ent, audio, sourceMap, listenerMap, depthDelta);
    }

    private bool TryGetListener(out EntityUid listenerMap, out CEZMapComponent listenerZ)
    {
        listenerMap = EntityUid.Invalid;
        listenerZ = default!;

        if (_player.LocalEntity is not { } local ||
            !_xformQuery.TryComp(local, out var xform) ||
            xform.MapUid is not { } mapUid ||
            !_zMapQuery.TryComp(mapUid, out var zMap))
        {
            return false;
        }

        listenerMap = mapUid;
        listenerZ = zMap;
        return true;
    }

    private void SpawnEcho(EntityUid source, AudioComponent audio, EntityUid sourceMap, EntityUid listenerMap, int depthDelta)
    {
        var worldPos = _xform.GetWorldPosition(source);

        var blocked = _zLevels.IsZLevelPathBlocked(sourceMap, depthDelta, worldPos);
        var sourceParams = audio.Params;
        var attenuationDb = SharedAudioSystem.GainToVolume(GetAttenuation(depthDelta, blocked));
        var echoParams = sourceParams.WithVolume(sourceParams.Volume + attenuationDb);

        var coords = new EntityCoordinates(listenerMap, worldPos);
        var specifier = new ResolvedPathSpecifier(audio.FileName);

        _spawningEcho = true;
        var echo = _audio.PlayStatic(specifier, Filter.Local(), coords, false, echoParams);
        _spawningEcho = false;

        if (echo is null)
            return;

        var echoComp = AddComp<CEZLevelAudioEchoComponent>(echo.Value.Entity);
        echoComp.Source = source;

        if (!_echoes.TryGetValue(source, out var list))
        {
            list = new List<EntityUid>();
            _echoes[source] = list;
        }

        list.Add(echo.Value.Entity);
    }

    private static float GetAttenuation(int depthDelta, bool blocked)
    {
        if (blocked)
            return CESharedZLevelsSystem.ZLevelAudioBlockedAttenuation;

        var distance = Math.Abs(depthDelta) * CESharedZLevelsSystem.ZLevelAudioHeight;
        return 1f / (1f + distance * distance * CESharedZLevelsSystem.ZLevelAudioOpenFalloff);
    }

    public override void FrameUpdate(float frameTime)
    {
        ProcessPendingAudio();

        if (_echoes.Count == 0)
            return;

        List<EntityUid>? finished = null;

        foreach (var (source, echoes) in _echoes)
        {
            if (TerminatingOrDeleted(source) || !TryComp<AudioComponent>(source, out _))
            {
                (finished ??= new List<EntityUid>()).Add(source);
                continue;
            }

            var worldPos = _xform.GetWorldPosition(source);

            for (var i = echoes.Count - 1; i >= 0; i--)
            {
                var echo = echoes[i];
                if (TerminatingOrDeleted(echo))
                {
                    echoes.RemoveAt(i);
                    continue;
                }

                _xform.SetWorldPosition(echo, worldPos);
            }
        }

        if (finished is null)
            return;

        foreach (var source in finished)
        {
            if (!_echoes.Remove(source, out var echoes))
                continue;

            foreach (var echo in echoes)
            {
                if (!TerminatingOrDeleted(echo))
                    QueueDel(echo);
            }
        }
    }
}
