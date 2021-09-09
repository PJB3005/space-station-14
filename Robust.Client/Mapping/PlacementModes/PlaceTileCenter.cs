using System;
using System.Collections.Generic;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;

namespace Robust.Client.Mapping.PlacementModes
{
    [PlacementModeName("SnapgridCenter")]
    [PlacementModeName("AlignTileAny")]
    public sealed class PlaceTileCenter : PlacementMode
    {
        [Dependency] private readonly IComponentManager _componentManager = default!;

        public PlaceTileCenter()
        {
            IoCManager.InjectDependencies(this);
        }

        public override bool AllowGroupedPlace => true;

        public override EntityCoordinates AlignMousePos(EntityCoordinates mousePosWorld)
        {
            if (!_componentManager.TryGetComponent<MapGridComponent>(mousePosWorld.EntityId, out var mapGridComp))
                return mousePosWorld;

            var grid = mapGridComp.Grid;
            var tile = grid.TileIndicesFor(mousePosWorld);
            return grid.GridTileToLocal(tile);
        }

        public override IEnumerable<EntityCoordinates> UpdateGroupedPlace(
            EntityCoordinates startPos,
            EntityCoordinates endPos,
            PlacementGroupType groupType)
        {
            if (startPos.EntityId != endPos.EntityId)
                yield break;

            if (!_componentManager.TryGetComponent<MapGridComponent>(startPos.EntityId, out var mapGridComp))
                yield break;

            var grid = mapGridComp.Grid;
            var startTile = grid.TileIndicesFor(startPos);
            var endTile = grid.TileIndicesFor(endPos);

            var startX = startTile.X;
            var endX = endTile.X;
            var startY = startTile.Y;
            var endY = endTile.Y;
            if (startX > endX)
                (startX, endX) = (endX, startX);
            if (startY > endY)
                (startY, endY) = (endY, startY);

            switch (groupType)
            {
                case PlacementGroupType.Line:
                    var diff = endTile - startTile;
                    if (Math.Abs(diff.X) > Math.Abs(diff.Y))
                    {
                        for (var x = startX; x <= endX; x++)
                        {
                            var tilePos = grid.GridTileToLocal((x, startTile.Y));
                            yield return tilePos;
                        }
                    }
                    else
                    {
                        for (var y = startY; y <= endY; y++)
                        {
                            var tilePos = grid.GridTileToLocal((startTile.X, y));
                            yield return tilePos;
                        }
                    }
                    break;

                case PlacementGroupType.Grid:

                    for (var y = startY; y <= endY; y++)
                    {
                        for (var x = startX; x <= endX; x++)
                        {
                            var tilePos = grid.GridTileToLocal((x, y));
                            yield return tilePos;
                        }
                    }
                    break;
            }
        }
    }
}
