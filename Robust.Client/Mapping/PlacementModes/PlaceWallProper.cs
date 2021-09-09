using System.Linq;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;

namespace Robust.Client.Mapping.PlacementModes
{
    [PlacementModeName("AlignWallProper")]
    public sealed class PlaceWallProper : PlacementMode
    {
        [Dependency] private readonly IComponentManager _componentManager = default!;
        [Dependency] private readonly IEntityManager _entityManager = default!;

        public PlaceWallProper()
        {
            IoCManager.InjectDependencies(this);
        }

        public override EntityCoordinates AlignMousePos(EntityCoordinates mousePosWorld)
        {
            if (!_componentManager.TryGetComponent<MapGridComponent>(mousePosWorld.EntityId, out var mapGridComp))
                return mousePosWorld;

            var grid = mapGridComp.Grid;
            var tile = grid.TileIndicesFor(mousePosWorld);
            var tileCenter = grid.GridTileToLocal(tile);

            var offsets = new Vector2[]
            {
                (0f, 0.5f),
                (0.5f, 0f),
                (0, -0.5f),
                (-0.5f, 0f)
            };

            var closestNode = offsets
                .Select(o => tileCenter.Offset(o))
                .OrderBy(node => node.TryDistance(_entityManager, mousePosWorld, out var distance) ? distance : (float?) null)
                .First();

            return closestNode;
        }
    }
}
