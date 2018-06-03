using System;
using SS14.Shared.Serialization;

namespace SS14.Shared.GameObjects.Components.Audio
{
    public class SharedAudioPlayerComponent : Component
    {
        public override string Name => "AudioPlayer";
        public override uint? NetID => NetIDs.AUDIO_PLAYER;
        public override Type StateType => typeof(AudioPlayerComponentState);

        [Serializable, NetSerializable]
        protected class AudioPlayerComponentState : ComponentState
        {

        }
    }
}
