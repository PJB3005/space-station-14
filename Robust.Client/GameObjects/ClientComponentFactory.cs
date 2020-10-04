﻿using Robust.Client.GameObjects.Components;
using Robust.Client.GameObjects.Components.Animations;
using Robust.Client.GameObjects.Components.UserInterface;
using Robust.Client.Interfaces.GameObjects.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.GameObjects.Components.Containers;
using Robust.Shared.GameObjects.Components.Map;
using Robust.Shared.GameObjects.Components.Transform;
using Robust.Shared.GameObjects.Components.UserInterface;
using Robust.Shared.Interfaces.GameObjects.Components;
using Robust.Shared.Physics;

namespace Robust.Client.GameObjects
{
    public class ClientComponentFactory : ComponentFactory
    {
        public ClientComponentFactory()
        {
            // Required for the engine to work
            Register<MetaDataComponent>();
            RegisterReference<MetaDataComponent, IMetaDataComponent>();

            // Required for the engine to work
            Register<TransformComponent>();
            RegisterReference<TransformComponent, ITransformComponent>();

            Register<MapComponent>();
            RegisterReference<MapComponent, IMapComponent>();

            Register<MapGridComponent>();
            RegisterReference<MapGridComponent, IMapGridComponent>();

            Register<CollidableComponent>();
            RegisterReference<CollidableComponent, IPhysBody>();
            RegisterReference<CollidableComponent, ICollidableComponent>();
            RegisterIgnore("KeyBindingInput");
            Register<PointLightComponent>();

            Register<InputComponent>();

            Register<SpriteComponent>();
            RegisterReference<SpriteComponent, ISpriteComponent>();

            Register<ClientOccluderComponent>();
            RegisterReference<ClientOccluderComponent, OccluderComponent>();

            Register<EyeComponent>();

            Register<AppearanceComponent>();
            Register<AppearanceTestComponent>();
            Register<SnapGridComponent>();

            Register<ClientUserInterfaceComponent>();
            RegisterReference<ClientUserInterfaceComponent, SharedUserInterfaceComponent>();

            RegisterIgnore("IgnorePause");

            Register<AnimationPlayerComponent>();

            Register<ContainerManagerComponent>();
            RegisterReference<ContainerManagerComponent, IContainerManager>();

#if DEBUG
            Register<DebugExceptionOnAddComponent>();
            Register<DebugExceptionExposeDataComponent>();
            Register<DebugExceptionInitializeComponent>();
            Register<DebugExceptionStartupComponent>();
#endif

        }
    }
}
