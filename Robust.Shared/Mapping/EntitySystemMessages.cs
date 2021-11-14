using System;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;

namespace Robust.Shared.Mapping;

[Serializable, NetSerializable]
public sealed class MappingPlaceEntities : EntityEventArgs
{
    public string Prototype = default!;
    public (EntityCoordinates, Direction)[] Coordinates = default!;
}

[Serializable, NetSerializable]
public sealed class MappingEraseEntities : EntityEventArgs
{
    public EntityUid[] Entities = default!;
}
