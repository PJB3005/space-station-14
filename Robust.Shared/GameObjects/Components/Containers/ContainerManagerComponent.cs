using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using JetBrains.Annotations;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.GameObjects.Components;
using Robust.Shared.Interfaces.Reflection;
using Robust.Shared.Interfaces.Serialization;
using Robust.Shared.IoC;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Robust.Shared.GameObjects.Components.Containers
{
    public sealed class ContainerManagerComponent : Component, IContainerManager
    {
        public override string Name => "ContainerContainer";
        public override uint? NetID => NetIDs.CONTAINER_MANAGER;

        [Dependency] private readonly IReflectionManager _reflectionManager = default!;

        private readonly Dictionary<string, IContainer> _entityContainers = new Dictionary<string, IContainer>();
        private Dictionary<string, List<EntityUid>>? _entitiesWaitingResolve;

        [UsedImplicitly] [ViewVariables] private IEnumerable<IContainer> AllContainers => _entityContainers.Values;

        /// <summary>
        /// Shortcut method to make creation of containers easier.
        /// Creates a new container on the entity and gives it back to you.
        /// </summary>
        /// <param name="id">The ID of the new container.</param>
        /// <param name="entity">The entity to create the container for.</param>
        /// <returns>The new container.</returns>
        /// <exception cref="ArgumentException">Thrown if there already is a container with the specified ID.</exception>
        /// <seealso cref="IContainerManager.MakeContainer{T}(string)" />
        public static T Create<T>(string id, IEntity entity) where T : IContainer
        {
            if (!entity.TryGetComponent<IContainerManager>(out var containerManager))
            {
                containerManager = entity.AddComponent<ContainerManagerComponent>();
            }

            return containerManager.MakeContainer<T>(id);
        }

        public static T Ensure<T>(string id, IEntity entity) where T : IContainer
        {
            return Ensure<T>(id, entity, out _);
        }

        public static T Ensure<T>(string id, IEntity entity, out bool alreadyExisted) where T : IContainer
        {
            if (!entity.TryGetComponent<IContainerManager>(out var containerManager))
            {
                containerManager = entity.AddComponent<ContainerManagerComponent>();
            }

            if (!containerManager.TryGetContainer(id, out var existing))
            {
                alreadyExisted = false;
                return containerManager.MakeContainer<T>(id);
            }

            if (!(existing is T container))
            {
                throw new InvalidOperationException(
                    $"The container exists but is of a different type: {existing.GetType()}");
            }

            alreadyExisted = true;
            return container;
        }


        public T MakeContainer<T>(string id) where T : IContainer
        {
            return (T) MakeContainer(id, typeof(T));
        }

        private IContainer MakeContainer(string id, Type type)
        {
            if (HasContainer(id))
            {
                throw new ArgumentException($"Container with specified ID already exists: '{id}'");
            }

            var container = (IContainer) Activator.CreateInstance(type, id, this)!;
            _entityContainers[id] = container;
            Dirty();
            return container;
        }

        public bool Remove(IEntity entity)
        {
            foreach (var containers in _entityContainers.Values)
            {
                if (containers.Contains(entity))
                {
                    return containers.Remove(entity);
                }
            }

            return true; // If we don't contain the entity, it will always be removed
        }

        /// <inheritdoc />
        public IContainer GetContainer(string id)
        {
            return _entityContainers[id];
        }

        /// <inheritdoc />
        public bool HasContainer(string id)
        {
            return _entityContainers.ContainsKey(id);
        }


        /// <inheritdoc />
        public bool TryGetContainer(string id, [NotNullWhen(true)] out IContainer? container)
        {
            if (!HasContainer(id))
            {
                container = null;
                return false;
            }

            container = GetContainer(id);
            return true;
        }

        public bool TryGetContainer(IEntity entity, [NotNullWhen(true)] out IContainer? container)
        {
            foreach (var contain in _entityContainers.Values)
            {
                if (!contain.Deleted && contain.Contains(entity))
                {
                    container = contain;
                    return true;
                }
            }

            container = default;
            return false;
        }

        public bool ContainsEntity(IEntity entity)
        {
            foreach (var container in _entityContainers.Values)
            {
                if (!container.Deleted && container.Contains(entity))
                {
                    return true;
                }
            }

            return false;
        }


        public void ForceRemove(IEntity entity)
        {
            foreach (var container in _entityContainers.Values)
            {
                if (container.Contains(entity))
                {
                    container.ForceRemove(entity);
                }
            }
        }

        public void InternalContainerShutdown(IContainer container)
        {
            _entityContainers.Remove(container.ID);
        }

        public AllContainersEnumerable GetAllContainers()
        {
            return new AllContainersEnumerable(this);
        }

        public override void Initialize()
        {
            base.Initialize();

            if (_entitiesWaitingResolve == null)
            {
                return;
            }

            foreach (var (key, entities) in _entitiesWaitingResolve)
            {
                var container = GetContainer(key);
                foreach (var uid in entities)
                {
                    container.Insert(Owner.EntityManager.GetEntity(uid));
                }
            }

            _entitiesWaitingResolve = null;
        }

        public override ComponentState GetComponentState()
        {
            return new ContainerManagerComponentState(
                _entityContainers.Values.ToDictionary(
                    c => c.ID,
                    container => new ContainerManagerComponentState.ContainerData
                    {
                        ContainedEntities = container.ContainedEntities.Select(e => e.Uid).ToArray(),
                        ShowContents = container.ShowContents,
                        OccludesLight = container.OccludesLight,
                        ContainerType = container.GetType().FullName!
                    }));
        }

        public override void OnRemove()
        {
            base.OnRemove();

            // IContainer.Shutdown modifies the EntityContainers collection
            foreach (var container in _entityContainers.Values.ToArray())
            {
                container.Shutdown();
            }

            _entityContainers.Clear();
        }


        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            if (serializer.Reading)
            {
                if (serializer.TryReadDataField<Dictionary<string, ContainerPrototypeData>>("containers", out var data))
                {
                    _entitiesWaitingResolve = new Dictionary<string, List<EntityUid>>();
                    foreach (var (key, datum) in data)
                    {
                        if (datum.Type == null)
                        {
                            throw new InvalidOperationException("Container does not have type set.");
                        }

                        var type = _reflectionManager.LooseGetType(datum.Type);
                        MakeContainer(key, type);

                        if (datum.Entities.Count == 0)
                        {
                            continue;
                        }

                        var list = new List<EntityUid>(datum.Entities.Where(u => u.IsValid()));
                        _entitiesWaitingResolve.Add(key, list);
                    }
                }
            }
            else
            {
                var dict = new Dictionary<string, ContainerPrototypeData>();
                foreach (var (key, container) in _entityContainers)
                {
                    var list = new List<EntityUid>(container.ContainedEntities.Select(e => e.Uid));
                    var data = new ContainerPrototypeData(list, container.GetType().FullName!);
                    dict.Add(key, data);
                }

                // ReSharper disable once RedundantTypeArgumentsOfMethod
                serializer.DataWriteFunction<Dictionary<string, ContainerPrototypeData>?>("containers", null,
                    () => dict);
            }
        }

        public override void HandleComponentState(ComponentState? curState, ComponentState? nextState)
        {
            if (!(curState is ContainerManagerComponentState cast))
            {
                return;
            }

            // Delete now-gone containers.
            List<string>? toDelete = null;
            foreach (var (id, container) in _entityContainers)
            {
                if (!cast.Containers.TryGetValue(id, out var dat) || dat.ContainerType != container.GetType().FullName)
                {
                    container.Shutdown();
                    toDelete ??= new List<string>();
                    toDelete.Add(id);
                }
            }

            if (toDelete != null)
            {
                foreach (var dead in toDelete)
                {
                    _entityContainers.Remove(dead);
                }
            }

            // Add new containers and update existing contents.
            foreach (var (id, data) in cast.Containers)
            {
                if (!_entityContainers.TryGetValue(id, out var container))
                {
                    container = (IContainer) Activator.CreateInstance(_reflectionManager.GetType(data.ContainerType)!,
                        id, this)!;
                    _entityContainers.Add(id, container);
                }

                // sync show flag
                container.ShowContents = data.ShowContents;
                container.OccludesLight = data.OccludesLight;

                // Remove gone entities.
                List<IEntity>? toRemove = null;
                // Manual for loop to avoid allocation.
                // ReSharper disable once ForCanBeConvertedToForeach
                for (var i = 0; i < container.ContainedEntities.Count; i++)
                {
                    var entity = container.ContainedEntities[i];
                    if (!data.ContainedEntities.Contains(entity.Uid))
                    {
                        toRemove ??= new List<IEntity>();
                        toRemove.Add(entity);
                    }
                }

                if (toRemove != null)
                {
                    foreach (var goner in toRemove)
                    {
                        container.ForceRemove(goner);
                    }
                }

                // Add new entities.
                foreach (var uid in data.ContainedEntities)
                {
                    var entity = Owner.EntityManager.GetEntity(uid);

                    if (!container.Contains(entity))
                    {
                        container.Insert(entity);
                    }
                }
            }
        }

        protected override void Shutdown()
        {
            base.Shutdown();

            /*// On shutdown we won't get to process remove events in the containers so this has to be manually done.
            foreach (var container in _containers.Values)
            {
                foreach (var containerEntity in container.Entities)
                {
                    Owner.EntityManager.EventBus.RaiseEvent(EventSource.Local,
                        new UpdateContainerOcclusionMessage(containerEntity));
                }
            }*/
        }

        [Serializable, NetSerializable]
        private class ContainerManagerComponentState : ComponentState
        {
            public Dictionary<string, ContainerData> Containers { get; }

            public ContainerManagerComponentState(Dictionary<string, ContainerData> containers) : base(
                NetIDs.CONTAINER_MANAGER)
            {
                Containers = containers;
            }

            [Serializable, NetSerializable]
            public struct ContainerData
            {
                public bool ShowContents;
                public bool OccludesLight;
                public EntityUid[] ContainedEntities;
                public string ContainerType;
            }
        }

        private struct ContainerPrototypeData : IExposeData
        {
            public List<EntityUid> Entities;
            public string? Type;

            public ContainerPrototypeData(List<EntityUid> entities, string type)
            {
                Entities = entities;
                Type = type;
            }

            public void ExposeData(ObjectSerializer serializer)
            {
                serializer.DataField(ref Entities, "entities", new List<EntityUid>());
                serializer.DataField(ref Type, "type", null);
            }
        }

        public struct AllContainersEnumerable : IEnumerable<IContainer>
        {
            private readonly ContainerManagerComponent _manager;

            public AllContainersEnumerable(ContainerManagerComponent manager)
            {
                _manager = manager;
            }

            public AllContainersEnumerator GetEnumerator()
            {
                return new AllContainersEnumerator(_manager);
            }

            IEnumerator<IContainer> IEnumerable<IContainer>.GetEnumerator()
            {
                return GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        public struct AllContainersEnumerator : IEnumerator<IContainer>
        {
            private Dictionary<string, IContainer>.ValueCollection.Enumerator _enumerator;

            public AllContainersEnumerator(ContainerManagerComponent manager)
            {
                _enumerator = manager._entityContainers.Values.GetEnumerator();
                Current = default;
            }

            public bool MoveNext()
            {
                while (_enumerator.MoveNext())
                {
                    if (!_enumerator.Current.Deleted)
                    {
                        Current = _enumerator.Current;
                        return true;
                    }
                }

                return false;
            }

            void IEnumerator.Reset()
            {
                ((IEnumerator<IContainer>) _enumerator).Reset();
            }    

            [AllowNull] public IContainer Current { get; private set; }

            object IEnumerator.Current => Current;

            public void Dispose()
            {
            }
        }
    }
}
