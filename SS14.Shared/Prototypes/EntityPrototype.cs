﻿using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.Interfaces.Map;
using SS14.Shared.Interfaces.Reflection;
using SS14.Shared.IoC;
using SS14.Shared.Prototypes;
using SS14.Shared.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using SS14.Shared.Log;
using SS14.Shared.Maths;
using YamlDotNet.RepresentationModel;
using SS14.Shared.Serialization;

namespace SS14.Shared.GameObjects
{
    /// <summary>
    /// Prototype that represents game entities.
    /// </summary>
    [Prototype("entity")]
    public class EntityPrototype : IPrototype, IIndexedPrototype, ISyncingPrototype
    {
        /// <summary>
        /// The type string of this prototype used in files.
        /// </summary>
        public string TypeString { get; private set; }

        /// <summary>
        /// The "in code name" of the object. Must be unique.
        /// </summary>
        public string ID { get; private set; }

        /// <summary>
        /// The "in game name" of the object. What is displayed to most players.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// The description of the object that shows upon using examine
        /// </summary>
        public string Description { get; private set; }

        /// <summary>
        /// The type of entity instantiated when a new entity is created from this template.
        /// </summary>
        public Type ClassType { get; private set; }

        /// <summary>
        /// The different mounting points on walls. (If any).
        /// </summary>
        public List<int> MountingPoints { get; private set; }

        /// <summary>
        /// The Placement mode used for client-initiated placement. This is used for admin and editor placement. The serverside version controls what type the server assigns in normal gameplay.
        /// </summary>
        public string PlacementMode { get; protected set; } = "PlaceNearby";

        /// <summary>
        /// The Range this entity can be placed from. This is only used serverside since the server handles normal gameplay. The client uses unlimited range since it handles things like admin spawning and editing.
        /// </summary>
        public int PlacementRange { get; protected set; } = DEFAULT_RANGE;
        private const int DEFAULT_RANGE = 200;

        /// <summary>
        /// Set to hold snapping categories that this object has applied to it such as pipe/wire/wallmount
        /// </summary>
        private readonly HashSet<string> _snapFlags = new HashSet<string>();
        private bool _snapOverriden = false;

        /// <summary>
        /// Offset that is added to the position when placing. (if any). Client only.
        /// </summary>
        public Vector2i PlacementOffset { get; protected set; }
        private bool _placementOverriden = false;

        /// <summary>
        /// True if this entity will be saved by the map loader.
        /// </summary>
        public bool MapSavable { get; protected set; } = true;

        /// <summary>
        /// The prototype we inherit from.
        /// </summary>
        public EntityPrototype Parent { get; private set; }

        /// <summary>
        /// A list of children inheriting from this prototype.
        /// </summary>
        public List<EntityPrototype> Children { get; private set; }
        public bool IsRoot => Parent == null;

        /// <summary>
        /// Used to store the parent id until we sync when all templates are done loading.
        /// </summary>
        private string parentTemp;

        /// <summary>
        /// A dictionary mapping the component type list to the YAML mapping containing their settings.
        /// </summary>
        public Dictionary<string, YamlMappingNode> Components { get; } = new Dictionary<string, YamlMappingNode>();

        /// <summary>
        /// The mapping node inside the <c>data</c> field of the prototype. Null if no data field exists.
        /// </summary>
        public YamlMappingNode DataNode { get; set; }

        private readonly HashSet<Type> ReferenceTypes = new HashSet<Type>();

        public void LoadFrom(YamlMappingNode mapping)
        {
            TypeString = mapping.GetNode("type").ToString();

            ID = mapping.GetNode("id").AsString();

            if (mapping.TryGetNode("name", out YamlNode node))
            {
                Name = node.AsString();
            }

            if (mapping.TryGetNode("class", out node))
            {
                var manager = IoCManager.Resolve<IReflectionManager>();
                ClassType = manager.GetType(node.AsString());

                if (ClassType == null)
                    Logger.Error($"[ENG] Prototype \"{ID}\" - Cannot find class type \"{node.AsString()}\"!");
            }

            if (mapping.TryGetNode("parent", out node))
            {
                parentTemp = node.AsString();
            }

            // DESCRIPTION
            if (mapping.TryGetNode("description", out node))
            {
                Description = node.AsString();
            }

            // COMPONENTS
            if (mapping.TryGetNode<YamlSequenceNode>("components", out var componentsequence))
            {
                var factory = IoCManager.Resolve<IComponentFactory>();
                foreach (var componentMapping in componentsequence.Cast<YamlMappingNode>())
                {
                    ReadComponent(componentMapping, factory);
                }

                // Assert that there are no conflicting component references.
                foreach (var componentName in Components.Keys)
                {
                    var registration = factory.GetRegistration(componentName);
                    foreach (var type in registration.References)
                    {
                        if (ReferenceTypes.Contains(type))
                        {
                            throw new InvalidOperationException($"Duplicate component reference in prototype: '{type}'");
                        }
                        ReferenceTypes.Add(type);
                    }
                }
            }

            // DATA FIELD
            if (mapping.TryGetNode<YamlMappingNode>("data", out var dataMapping))
            {
                DataNode = dataMapping;
            }

            // PLACEMENT
            // TODO: move to a component or something. Shouldn't be a root part of prototypes IMO.
            if (mapping.TryGetNode<YamlMappingNode>("placement", out var placementMapping))
            {
                ReadPlacementProperties(placementMapping);
            }

            // SAVING
            if (mapping.TryGetNode("save", out node))
            {
                MapSavable = node.AsBool();
            }
        }

        private void ReadPlacementProperties(YamlMappingNode mapping)
        {
            if (mapping.TryGetNode("mode", out YamlNode node))
            {
                PlacementMode = node.AsString();
            }

            if (mapping.TryGetNode("offset", out node))
            {
                PlacementOffset = node.AsVector2i();
                _placementOverriden = true;
            }

            if (mapping.TryGetNode<YamlSequenceNode>("nodes", out var sequence))
            {
                MountingPoints = sequence.Select(p => p.AsInt()).ToList();
            }

            if (mapping.TryGetNode("range", out node))
            {
                PlacementRange = node.AsInt();
            }

            // Reads snapping flags that this object holds that describe its properties to such as wire/pipe/wallmount, used to prevent certain stacked placement
            if (mapping.TryGetNode<YamlSequenceNode>("snap", out var snapSequence))
            {
                var flagsList = snapSequence.Select(p => p.AsString());
                foreach (var flag in flagsList)
                {
                    _snapFlags.Add(flag);
                }
                _snapOverriden = true;
            }
        }

        // Resolve inheritance.
        public bool Sync(IPrototypeManager manager, int stage)
        {
            switch (stage)
            {
                case 0:
                    if (parentTemp == null)
                    {
                        return true;
                    }

                    Parent = manager.Index<EntityPrototype>(parentTemp);
                    if (Parent.Children == null)
                    {
                        Parent.Children = new List<EntityPrototype>();
                    }
                    Parent.Children.Add(this);
                    return false;

                case 1:
                    // We are a root-level prototype.
                    // As such we're getting the duty of pushing inheritance into everybody's face.
                    // Can't do a "puller" system where each queries the parent because it requires n stages
                    //  (n being the depth of each inheritance tree)

                    if (Children == null)
                    {
                        break;
                    }
                    foreach (EntityPrototype child in Children)
                    {
                        PushInheritance(this, child);
                    }

                    break;
            }
            return false;
        }

        private static void PushInheritance(EntityPrototype source, EntityPrototype target)
        {
            // Copy component data over.
            foreach (KeyValuePair<string, YamlMappingNode> component in source.Components)
            {
                if (target.Components.TryGetValue(component.Key, out YamlMappingNode targetComponent))
                {
                    // Copy over values the target component does not have.
                    foreach (YamlNode key in component.Value.Children.Keys)
                    {
                        if (!targetComponent.Children.ContainsKey(key))
                        {
                            targetComponent.Children[key] = component.Value[key];
                        }
                    }
                }
                else
                {
                    // Copy component into the target, since it doesn't have it yet.
                    // Unless it'd cause a conflict.
                    var factory = IoCManager.Resolve<IComponentFactory>();
                    foreach (var refType in factory.GetRegistration(component.Key).References)
                    {
                        if (target.ReferenceTypes.Contains(refType))
                        {
                            // yeah nope the child's got it!! NEXT!!
                            goto next;
                        }
                    }
                    target.Components[component.Key] = new YamlMappingNode(component.Value.AsEnumerable());
                }
            next:;
            }

            // Copy all simple data over.
            if (!target._placementOverriden)
            {
                target.PlacementMode = source.PlacementMode;
            }

            if (!target._placementOverriden)
            {
                target.PlacementOffset = source.PlacementOffset;
            }

            if (target.MountingPoints == null && source.MountingPoints != null)
            {
                target.MountingPoints = new List<int>(source.MountingPoints);
            }

            if (target.PlacementRange == DEFAULT_RANGE)
            {
                target.PlacementRange = source.PlacementRange;
            }

            if (!target._snapOverriden)
            {
                foreach (var flag in source._snapFlags)
                {
                    target._snapFlags.Add(flag);
                }
            }

            if (target.Name == null)
            {
                target.Name = source.Name;
            }

            if (target.ClassType == null)
            {
                target.ClassType = source.ClassType;
            }

            if (target.Children == null)
            {
                return;
            }

            // TODO: remove recursion somehow.
            foreach (EntityPrototype child in target.Children)
            {
                PushInheritance(target, child);
            }
        }

        internal Entity AllocEntity(EntityUid uid, IEntityManager manager, IEntityNetworkManager networkManager)
        {
            var entity = (Entity)Activator.CreateInstance(ClassType ?? typeof(Entity));

            entity.SetManagers(manager, networkManager);
            entity.SetUid(uid);
            entity.Prototype = this;
            entity.Name = Name;

            return entity;
        }

        internal void FinishEntity(Entity entity, IComponentFactory factory, IEntityFinishContext context)
        {
            foreach (var componentData in Components)
            {
                var component = (Component)factory.GetComponent(componentData.Key);

                component.Owner = entity;
                ObjectSerializer ser;
                if (context != null)
                {
                    ser = context.GetComponentSerializer(componentData.Key, componentData.Value);
                }
                else
                {
                    ser = new YamlObjectSerializer(componentData.Value, reading: true);
                }
                component.ExposeData(ser);

                entity.AddComponent(component);
            }
        }

        /// <summary>
        /// Tests whether the prototype is going to conflict with anything previously placed in this location on the snap grid
        /// </summary>
        public bool CanSpawnAt(IMapGrid grid, Vector2 position)
        {
            if (!grid.OnSnapCenter(position) && !grid.OnSnapBorder(position)) //We only check snap position logic at this time, extensible for other behavior
            {
                return true;
            }
            var entitymanager = IoCManager.Resolve<IEntityManager>();
            var entities = entitymanager.GetEntitiesAt(position);
            return !entities.SelectMany(e => e.Prototype._snapFlags).Any(f => _snapFlags.Contains(f));
        }

        private void ReadComponent(YamlMappingNode mapping, IComponentFactory factory)
        {
            string type = mapping.GetNode("type").AsString();
            // See if type exists to detect errors.
            switch (factory.GetComponentAvailability(type))
            {
                case ComponentAvailability.Available:
                    break;

                case ComponentAvailability.Ignore:
                    return;

                case ComponentAvailability.Unknown:
                    Log.Logger.Error($"Unknown component '{type}' in prototype {ID}!");
                    return;
            }

            var copy = new YamlMappingNode(mapping.AsEnumerable());
            // TODO: figure out a better way to exclude the type node.
            // Also maybe deep copy this? Right now it's pretty error prone.
            copy.Children.Remove(new YamlScalarNode("type"));

            Components[type] = copy;
        }

        public override string ToString()
        {
            return $"EntityPrototype({ID})";
        }
    }
}
