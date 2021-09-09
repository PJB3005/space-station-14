using System;
using System.Collections.Generic;
using System.Linq;
using Robust.Client.Mapping.PlacementModes;
using Robust.Shared.IoC;
using Robust.Shared.Reflection;

namespace Robust.Client.Mapping
{
    /// <summary>
    /// Manages available placement modes for the placement system.
    /// </summary>
    public interface IPlacementManager
    {
        /// <summary>
        /// Create an instance of the specified placement mode, by name.
        /// </summary>
        PlacementMode MakePlacementMode(string type);

        IEnumerable<string> PrimaryPlacementModes { get; }
    }

    internal interface IPlacementManagerInternal : IPlacementManager
    {
        void Initialize();
    }

    internal sealed class PlacementManager : IPlacementManagerInternal
    {
        [Dependency] private readonly IReflectionManager _reflectionManager = default!;

        private readonly Dictionary<string, (Type type, bool isPrimary)> _placementModes = new();

        public void Initialize()
        {
            foreach (var modeType in _reflectionManager.GetAllChildren<PlacementMode>())
            {
                _placementModes.Add(modeType.Name, (modeType, true));

                foreach (var attr in modeType.GetCustomAttributes(typeof(PlacementModeNameAttribute), inherit: false))
                {
                    var placeAttr = (PlacementModeNameAttribute)attr;
                    _placementModes.Add(placeAttr.Name, (modeType, false));
                }
            }
        }

        public PlacementMode MakePlacementMode(string type)
        {
            return (PlacementMode)Activator.CreateInstance(_placementModes[type].type)!;
        }

        public IEnumerable<string> PrimaryPlacementModes =>
            _placementModes.Where(kv => kv.Value.isPrimary).Select(kv => kv.Key);
    }
}
