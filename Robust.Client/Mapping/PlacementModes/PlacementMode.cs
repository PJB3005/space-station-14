using System.Collections.Generic;
using JetBrains.Annotations;
using Robust.Shared.Map;

namespace Robust.Client.Mapping.PlacementModes;

[MeansImplicitUse(ImplicitUseTargetFlags.WithInheritors)]
public abstract class PlacementMode
{
    public virtual bool AllowGroupedPlace => false;
    public virtual EntityCoordinates AlignMousePos(EntityCoordinates mousePosWorld) => mousePosWorld;

    public virtual IEnumerable<EntityCoordinates> UpdateGroupedPlace(
        EntityCoordinates startPos,
        EntityCoordinates endPos,
        PlacementGroupType groupType)
    {
        yield break;
    }
}
