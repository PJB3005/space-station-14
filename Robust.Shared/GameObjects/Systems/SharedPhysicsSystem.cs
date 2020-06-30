using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.Interfaces.Physics;
using Robust.Shared.Interfaces.Random;
using Robust.Shared.Interfaces.Timing;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using DependencyAttribute = Robust.Shared.IoC.DependencyAttribute;

namespace Robust.Shared.GameObjects.Systems
{
    public abstract class SharedPhysicsSystem : EntitySystem
    {
        [Dependency] private readonly ITileDefinitionManager _tileDefinitionManager = default!;
        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly IPhysicsManager _physicsManager = default!;
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly IGameTiming _timing = default!;

        private const float Epsilon = 1.0e-6f;

        private readonly List<Manifold> _collisionCache = new List<Manifold>();


        public SharedPhysicsSystem()
        {
            EntityQuery = new TypeEntityQuery(typeof(SharedPhysicsComponent));
        }


        [MethodImpl(MethodImplOptions.NoInlining)]
        protected void SimulateWorld(float frameTime, List<IEntity> entities)
        {
            foreach (var entity in entities)
            {
                var physics = entity.GetComponent<SharedPhysicsComponent>();

                physics.Controller?.UpdateBeforeProcessing();
            }

            // Calculate collisions and store them in the cache
            ProcessCollisions();

            // Remove all entities that were deleted during collision handling
            entities.RemoveAll(p => p.Deleted);

            // Process frictional forces
            foreach (var entity in entities)
            {
                ProcessFriction(entity, frameTime);
            }

            foreach (var entity in entities)
            {
                var physics = entity.GetComponent<SharedPhysicsComponent>();

                physics.Controller?.UpdateAfterProcessing();
            }

            // Remove all entities that were deleted due to the controller
            entities.RemoveAll(p => p.Deleted);

            const int solveIterationsAt60 = 4;

            var multiplier = frameTime / (1f / 60);

            var divisions = Math.Clamp(
                MathF.Round(solveIterationsAt60 * multiplier, MidpointRounding.AwayFromZero),
                1,
                20
            );

            if (_timing.InSimulation) divisions = 1;

            for (var i = 0; i < divisions; i++)
            {
                foreach (var entity in entities)
                {
                    UpdatePosition(entity, frameTime / divisions);
                }

                for (var j = 0; j < divisions; ++j)
                {
                    if (FixClipping(_collisionCache, divisions))
                    {
                        break;
                    }
                }
            }
        }

        // Runs collision behavior and updates cache
        private void ProcessCollisions()
        {
            var collisionsWith = new Dictionary<ICollideBehavior, int>();

            FindCollisions();

            var counter = 0;

            while (GetNextCollision(_collisionCache, counter, out var collision))
            {
                counter++;
                var impulse = _physicsManager.SolveCollisionImpulse(collision);
                if (collision.APhysics != null)
                {
                    collision.APhysics.Momentum -= impulse;
                }

                if (collision.BPhysics != null)
                {
                    collision.BPhysics.Momentum += impulse;
                }
            }

            foreach (var collision in _collisionCache)
            {
                // Apply onCollide behavior
                var aBehaviors = collision.A.Owner.GetAllComponents<ICollideBehavior>();
                foreach (var behavior in aBehaviors)
                {
                    var entity = collision.B.Owner;
                    if (entity.Deleted) continue;
                    behavior.CollideWith(entity);
                    if (collisionsWith.ContainsKey(behavior))
                    {
                        collisionsWith[behavior] += 1;
                    }
                    else
                    {
                        collisionsWith[behavior] = 1;
                    }
                }

                var bBehaviors = collision.B.Owner.GetAllComponents<ICollideBehavior>();
                foreach (var behavior in bBehaviors)
                {
                    var entity = collision.A.Owner;
                    if (entity.Deleted) continue;
                    behavior.CollideWith(entity);
                    if (collisionsWith.ContainsKey(behavior))
                    {
                        collisionsWith[behavior] += 1;
                    }
                    else
                    {
                        collisionsWith[behavior] = 1;
                    }
                }
            }

            foreach (var (behavior, amount) in collisionsWith)
            {
                behavior.PostCollide(amount);
            }
        }

        private void FindCollisions()
        {
            _collisionCache.Clear();
            var combinations = new HashSet<(EntityUid, EntityUid)>();
            var collisions = _physicsManager.GetAllCollisions();
            foreach (var (bodyA, bodyB) in collisions)
            {
                var entityA = bodyA.Owner;
                var entityB = bodyB.Owner;
                var uidA = entityA.Uid;
                var uidB = entityB.Uid;

                if (uidA.CompareTo(uidB) > 0)
                {
                    // Swap so that we always have one ordering of UIDs to check to make removing dupes easier.
                    var tmpUid = uidA;
                    uidA = uidB;
                    uidB = tmpUid;
                }

                if (!combinations.Add((uidA, uidB)))
                {
                    continue;
                }

                if (!PhysicsManager.CollidesOnMask(bodyA, bodyB))
                {
                    continue;
                }

                var aMods = entityA.GetAllComponents<ICollideSpecial>();
                foreach (var modifier in aMods)
                {
                    if (modifier.PreventCollide(bodyB))
                    {
                        goto Next;
                    }
                }

                var bMods = entityB.GetAllComponents<ICollideSpecial>();
                foreach (var modifier in bMods)
                {
                    if (modifier.PreventCollide(bodyA))
                    {
                        goto Next;
                    }
                }

                var physicsA = entityA.GetComponentOrNull<SharedPhysicsComponent>();
                var physicsB = entityB.GetComponentOrNull<SharedPhysicsComponent>();

                if (physicsA == null && physicsB == null)
                {
                    continue;
                }

                var collision = new Manifold(bodyA, bodyB, physicsA, physicsB, _physicsManager);

                _collisionCache.Add(collision);

                Next: ;
            }
        }

        private bool GetNextCollision(List<Manifold> collisions, int counter, out Manifold collision)
        {
            // The *4 is completely arbitrary
            if (counter > collisions.Count * 4)
            {
                collision = default;
                return false;
            }

            var indexes = new List<int>();
            for (int i = 0; i < collisions.Count; i++)
            {
                indexes.Add(i);
            }

            _random.Shuffle(indexes);
            foreach (var index in indexes)
            {
                if (collisions[index].Unresolved)
                {
                    collision = collisions[index];
                    return true;
                }
            }

            collision = default;
            return false;
        }

        private void ProcessFriction(IEntity entity, float frameTime)
        {
            var physics = entity.GetComponent<SharedPhysicsComponent>();

            if (physics.LinearVelocity == Vector2.Zero) return;

            // Calculate frictional force
            var friction = GetFriction(entity);

            // Clamp friction because friction can't make you accelerate backwards
            friction = Math.Min(friction, physics.LinearVelocity.Length);

            // No multiplication/division by mass here since that would be redundant.
            var frictionVelocityChange = physics.LinearVelocity.Normalized * -friction;

            physics.LinearVelocity += frictionVelocityChange;
        }

        private void UpdatePosition(IEntity entity, float frameTime)
        {
            var physics = entity.GetComponent<SharedPhysicsComponent>();
            physics.LinearVelocity =
                new Vector2(Math.Abs(physics.LinearVelocity.X) < Epsilon ? 0.0f : physics.LinearVelocity.X,
                    Math.Abs(physics.LinearVelocity.Y) < Epsilon ? 0.0f : physics.LinearVelocity.Y);
            if (physics.Anchored ||
                physics.LinearVelocity == Vector2.Zero && Math.Abs(physics.AngularVelocity) < Epsilon) return;

            if (ContainerHelpers.IsInContainer(entity) && physics.LinearVelocity != Vector2.Zero)
            {
                entity.Transform.Parent!.Owner.SendMessage(entity.Transform, new RelayMovementEntityMessage(entity));
                // This prevents redundant messages from being sent if solveIterations > 1 and also simulates the entity "colliding" against the locker door when it opens.
                physics.LinearVelocity = Vector2.Zero;
            }

            physics.Owner.Transform.WorldRotation += physics.AngularVelocity * frameTime;
            physics.Owner.Transform.WorldPosition += physics.LinearVelocity * frameTime;
        }

        // Based off of Randy Gaul's ImpulseEngine code
        private bool FixClipping(List<Manifold> collisions, float divisions)
        {
            const float allowance = 0.05f;
            var percent = Math.Clamp(1f / divisions, 0.01f, 1f);
            var done = true;
            foreach (var collision in collisions)
            {
                var penetration = _physicsManager.CalculatePenetration(collision.A, collision.B);
                if (penetration > allowance)
                {
                    done = false;
                    var correction = collision.Normal * Math.Abs(penetration) * percent;
                    if (collision.APhysics != null && !collision.APhysics.Anchored && !collision.APhysics.Deleted)
                        collision.APhysics.Owner.Transform.WorldPosition -= correction;
                    if (collision.BPhysics != null && !collision.BPhysics.Anchored && !collision.BPhysics.Deleted)
                        collision.BPhysics.Owner.Transform.WorldPosition += correction;
                }
            }

            return done;
        }

        private float GetFriction(IEntity entity)
        {
            if (entity.HasComponent<ICollidableComponent>() &&
                entity.TryGetComponent(out SharedPhysicsComponent physics) && physics.OnGround)
            {
                var location = entity.Transform;
                var grid = _mapManager.GetGrid(location.GridPosition.GridID);
                var tile = grid.GetTileRef(location.GridPosition);
                var tileDef = _tileDefinitionManager[tile.Tile.TypeId];
                return tileDef.Friction;
            }

            return 0.0f;
        }
    }
}
