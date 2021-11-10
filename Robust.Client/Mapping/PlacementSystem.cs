using System;
using System.Linq;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.Mapping.PlacementModes;
using Robust.Client.Utility;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.Input;
using Robust.Shared.Input.Binding;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Players;
using Robust.Shared.Timing;

namespace Robust.Client.Mapping;

/// <summary>
/// Manages active placement interactions.
/// i.e. this is the system you use to get the placement UI, input handling, etc...
/// </summary>
/// <remarks>
/// This type does not handle actual entity placement. Behavior of placement clicks is up to consumers.
/// </remarks>
/// <seealso cref="MappingSystem"/>
public sealed class PlacementSystem : EntitySystem
{
    [Dependency] private readonly IOverlayManager _overlayManager = default!;
    [Dependency] private readonly IInputManager _inputManager = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly IEyeManager _eyeManager = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;

    public bool PlacementActive => _currentType != PlacementType.None;

    private PlacementType _currentType;
    private MappingPlacementOptions _placement;
    private MappingErasementOptions _erasement;
    private Direction _currentDirection;
    private PlacementGroupType? _placementGroup;
    private EntityCoordinates _groupPlaceOrigin;

    private ISawmill _sawmill = default!;

    private Direction EffectiveDirection => _placement.AllowRotation ? _currentDirection : Direction.South;

    public override void Initialize()
    {
        base.Initialize();

        _sawmill = Logger.GetSawmill("mapping.placement");

        CommandBinds.Builder
            .Bind(EngineKeyFunctions.EditorPlaceObject, new PointerInputCmdHandler(InputPlaceObject))
            .Bind(EngineKeyFunctions.EditorLinePlace, new PointerInputCmdHandler(InputLinePlace))
            .Bind(EngineKeyFunctions.EditorGridPlace, new PointerInputCmdHandler(InputGridPlace))
            .Bind(EngineKeyFunctions.EditorCancelPlace, InputCmdHandler.FromDelegate(InputCancelPlace))
            .Bind(EngineKeyFunctions.EditorRotateObject, InputCmdHandler.FromDelegate(InputRotateObject))
            .Register<PlacementSystem>();
    }

    public void StartPlacement(in MappingPlacementOptions options)
    {
        CancelCurrentPlacement();

        _placement = options;
        _placement.Mode ??= PlaceFree.Instance;
        _overlayManager.AddOverlay(new PlacementOverlay(this));
        StartPlacementShared(PlacementType.Placement);
    }

    public void StartErasement(in MappingErasementOptions options)
    {
        CancelCurrentPlacement();

        _erasement = options;
        StartPlacementShared(PlacementType.Erasement);
    }

    public void CancelCurrentPlacement()
    {
        if (!PlacementActive)
            return;

        _erasement.EventCancelled?.Invoke();
        _placement.EventCancelled?.Invoke();

        _placementGroup = null;
        _groupPlaceOrigin = default;
        _placement = default;
        _erasement = default;
        _overlayManager.RemoveOverlay<PlacementOverlay>();
        _currentType = PlacementType.None;
        Get<InputSystem>().SetEntityContextActive();
    }

    private void StartPlacementShared(PlacementType type)
    {
        _currentType = type;

        _inputManager.Contexts.SetActiveContext("editor");
    }

    private bool InputPlaceObject(in PointerInputCmdHandler.PointerInputCmdArgs args)
    {
        if (!_gameTiming.IsFirstTimePredicted)
            return false;

        switch (_currentType)
        {
            case PlacementType.Placement:
                var coords = NormalizeCoords(args.Coordinates);
                var tiles = CoordsListFromGroup(coords);

                var events = new MappingPlacedEventArgs[tiles.Length];

                for (var i = 0; i < tiles.Length; i++)
                {
                    events[i] = new MappingPlacedEventArgs
                    {
                        Coordinates = tiles[i],
                        Direction = _currentDirection
                    };
                }

                _placement.EventPlaced?.Invoke(events);
                return true;

            case PlacementType.Erasement:
                if (args.EntityUid.IsValid())
                    return false;

                var erasedEv = new MappingErasedEventArgs
                {
                    Entity = args.EntityUid
                };
                _erasement.EventErased?.Invoke(erasedEv);
                return true;

            default:
                _sawmill.Warning("Got EditorPlaceObject but placement is not currently active!");
                return false;
        }
    }

    private bool InputGridPlace(in PointerInputCmdHandler.PointerInputCmdArgs args)
    {
        if (!_gameTiming.IsFirstTimePredicted)
            return false;

        if (_currentType == PlacementType.None || _placement.Mode?.AllowGroupedPlace != true)
            return false;

        _groupPlaceOrigin = NormalizeCoords(args.Coordinates);
        _placementGroup = PlacementGroupType.Grid;
        return true;
    }

    private bool InputLinePlace(in PointerInputCmdHandler.PointerInputCmdArgs args)
    {
        if (!_gameTiming.IsFirstTimePredicted)
            return false;

        if (_currentType == PlacementType.None || _placement.Mode?.AllowGroupedPlace != true)
            return false;

        _groupPlaceOrigin = NormalizeCoords(args.Coordinates);
        _placementGroup = PlacementGroupType.Line;
        return true;
    }

    private void InputCancelPlace(ICommonSession? session)
    {
        if (!_gameTiming.IsFirstTimePredicted)
            return;

        CancelCurrentPlacement();
    }

    private void InputRotateObject(ICommonSession? session)
    {
        if (!_gameTiming.IsFirstTimePredicted)
            return;

        if (_currentType != PlacementType.Placement || !_placement.AllowRotation)
            return;

        _currentDirection = _currentDirection.TurnCw();
    }

    /// <summary>
    /// Normalize EntityCoordinates to be either to the relevant grid or the map.
    /// </summary>
    private EntityCoordinates NormalizeCoords(EntityCoordinates coords)
    {
        var map = coords.ToMap(EntityManager);
        return ToGridMaybe(map);
    }

    private EntityCoordinates CalcMouseEntityCoordinates()
    {
        var map = _eyeManager.ScreenToMap(_inputManager.MouseScreenPosition);
        return ToGridMaybe(map);
    }

    private EntityCoordinates ToGridMaybe(MapCoordinates coords)
    {
        return _mapManager.TryFindGridAt(coords, out var grid)
            ? grid.MapToGrid(coords)
            : EntityCoordinates.FromMap(_mapManager, coords);
    }

    private EntityCoordinates[] CoordsListFromGroup(EntityCoordinates endWorldPos)
    {
        if (_placementGroup is { } group)
            return _placement.Mode!.UpdateGroupedPlace(_groupPlaceOrigin, endWorldPos, group).ToArray();
        return new[] { _placement.Mode!.AlignMousePos(endWorldPos) };
    }

    private sealed class PlacementOverlay : Overlay
    {
        private readonly PlacementSystem _system;

        public PlacementOverlay(PlacementSystem system)
        {
            _system = system;
        }

        public override OverlaySpace Space => OverlaySpace.WorldSpace;

        protected internal override void Draw(in OverlayDrawArgs args)
        {
            ref var placement = ref _system._placement;
            var worldHandle = args.WorldHandle;

            if (placement.TextureLayers is not { } textures)
                return;

            var worldPos = _system.CalcMouseEntityCoordinates();
            var tiles = _system.CoordsListFromGroup(worldPos).ToArray();
            foreach (var texture in textures)
            {

                var directedTex = texture.TextureFor(_system.EffectiveDirection);
                var size = directedTex.Size / ((float)EyeManager.PixelsPerMeter * 2);

                foreach (var tile in tiles)
                {
                    worldHandle.DrawTexture(
                        directedTex,
                        tile.ToMapPos(_system.EntityManager) - size,
                        Color.Green.WithAlpha(0.75f));
                }
            }
        }
    }

    private enum PlacementType : byte
    {
        None = 0,
        Placement,
        Erasement
    }
}

public enum PlacementGroupType : byte
{
    Line,
    Grid
}

public struct MappingPlacementOptions
{
    public IDirectionalTextureProvider[]? TextureLayers;
    public PlacementMode? Mode;
    public bool AllowRotation;

    // Events
    public Action<MappingPlacedEventArgs[]>? EventPlaced;
    public Action? EventCancelled;
}

public struct MappingErasementOptions
{
    // Events
    public Action<MappingErasedEventArgs>? EventErased;
    public Action? EventCancelled;
}

public struct MappingPlacedEventArgs
{
    public EntityCoordinates Coordinates;
    public Direction Direction;
}

public struct MappingErasedEventArgs
{
    public EntityUid Entity;
}
