using Robust.Server.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.Log;
using Robust.Shared.Mapping;
using Robust.Shared.Maths;
using Robust.Shared.Players;

namespace Robust.Server.Mapping;

public sealed class MappingSystem : EntitySystem
{
    private readonly ISawmill _sawmill = Logger.GetSawmill("mapping");

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<MappingPlaceEntities>(OnMappingPlaceEntities);
        SubscribeNetworkEvent<MappingEraseEntities>(OnMappingEraseEntities);
    }

    private void OnMappingPlaceEntities(MappingPlaceEntities msg, EntitySessionEventArgs args)
    {
        if (!CheckActionAccess(args.SenderSession))
        {
            _sawmill.Warning($"User {args.SenderSession} tried to do entity placement but was blocked.");
            return;
        }

        foreach (var (coordinate, direction) in msg.Coordinates)
        {
            var entity = EntityManager.SpawnEntity(msg.Prototype, coordinate);
            var transform = entity.Transform;
            transform.LocalRotation = direction.ToAngle();
        }
    }

    private void OnMappingEraseEntities(MappingEraseEntities msg, EntitySessionEventArgs args)
    {
        if (!CheckActionAccess(args.SenderSession))
        {
            _sawmill.Warning($"User {args.SenderSession} tried to do entity erasement but was blocked.");
            return;
        }

        foreach (var entity in msg.Entities)
        {
            EntityManager.DeleteEntity(entity);
        }
    }

    private bool CheckActionAccess(ICommonSession session)
    {
        if (session is not IPlayerSession playerSession)
            return false;

        var msg = new MappingTryAction { Session = playerSession };

        RaiseLocalEvent(msg);

        return !msg.Blocked;
    }
}

/// <summary>
/// Fired by <see cref="MappingSystem"/> to poll if a mapping action should be blocked for a client.
/// Use this to do access control on placement based on admin status or similar.
/// </summary>
public sealed class MappingTryAction : EntityEventArgs
{
    /// <summary>
    /// The player session trying to do the mapping action.
    /// </summary>
    public IPlayerSession Session = default!;

    /// <summary>
    /// Set to true to disallow the mapping action.
    /// </summary>
    public bool Blocked;
}
