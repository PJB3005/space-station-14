using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Mapping;
using Robust.Shared.Maths;

namespace Robust.Client.Mapping;

public sealed class MappingSystem : EntitySystem
{
    public void DoEntityPlace(string prototype, (EntityCoordinates, Direction)[] coordinates)
    {
        var msg = new MappingPlaceEntities
        {
            Prototype = prototype,
            Coordinates = coordinates
        };

        RaiseNetworkEvent(msg);
    }

    public void DoEntityErase(EntityUid entity)
    {
        var msg = new MappingEraseEntities
        {
            Entities = new[] { entity }
        };

        RaiseNetworkEvent(msg);
    }
}
