using SS14.Client.Interfaces.GameObjects.Components;
using SS14.Client.Interfaces.ResourceManagement;
using SS14.Client.ResourceManagement;
using SS14.Shared.GameObjects;
using SS14.Shared.GameObjects.Components.Audio;
using SS14.Shared.IoC;

namespace SS14.Client.GameObjects.Components.Audio
{
    public class AudioPlayerComponent : SharedAudioPlayerComponent
    {
        private IGodotTransformComponent transform;
        private Godot.AudioStreamPlayer2D player;

        GodotGlue.GodotSignalSubscriber0 derp;

        public override void Initialize()
        {
            base.Initialize();

            var resourceCache = IoCManager.Resolve<IResourceCache>();

            transform = Owner.GetComponent<IGodotTransformComponent>();
            var stream = (Godot.AudioStreamOGGVorbis)(resourceCache.GetResource<AudioResource>("/Audio/generator_mid2.ogg").AudioStream.GodotAudioStream);
            stream.Loop = true;
            player = new Godot.AudioStreamPlayer2D
            {
                Name = "test",
                Stream = stream,
                Playing = true,
            };

            transform.SceneNode.AddChild(player);

            derp = new GodotGlue.GodotSignalSubscriber0();
            derp.Connect(player, "finished");
            //derp.Signal += Finished;
        }

        private void Finished()
        {
            System.Console.WriteLine("Yes");
            player.Play();
        }
    }
}
