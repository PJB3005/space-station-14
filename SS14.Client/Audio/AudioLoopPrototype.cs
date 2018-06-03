using System.Collections.Generic;
using System.Linq;
using SS14.Client.Interfaces.ResourceManagement;
using SS14.Client.ResourceManagement;
using SS14.Shared.IoC;
using SS14.Shared.Prototypes;
using SS14.Shared.Utility;
using YamlDotNet.RepresentationModel;

namespace SS14.Client.Audio
{
    [Prototype("audioloop")]
    public class AudioLoopPrototype : IPrototype, IIndexedPrototype
    {
        public string ID { get; private set; }

        public AudioResource Start { get; private set; }
        public AudioResource End { get; private set; }
        public IReadOnlyList<AudioResource> Middle => _middle;
        private readonly List<AudioResource> _middle = new List<AudioResource>();

        public AudioLoop Instance()
        {
            return new AudioLoop(Start, End, Middle.Select(a => a.AudioStream));
        }

        void IPrototype.LoadFrom(YamlMappingNode mapping)
        {
            ID = mapping.GetNode("id").ToString();
            var resourceCache = IoCManager.Resolve<IResourceCache>();

            if (mapping.TryGetNode("start", out var node))
            {
                Start = resourceCache.GetResource<AudioResource>(node.AsResourcePath());
            }

            if (mapping.TryGetNode("end", out node))
            {
                End = resourceCache.GetResource<AudioResource>(node.AsResourcePath());
            }

            foreach (var item in mapping.GetNode<YamlSequenceNode>("middle"))
            {
                _middle.Add(resourceCache.GetResource<AudioResource>(item.AsResourcePath()));
            }
        }
    }
}
