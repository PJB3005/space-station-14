using System.Collections.Generic;
using System.Linq;

namespace SS14.Client.Audio
{
    /// <summary>
    ///     Represents an audio loop with a start, middle sections and end.
    /// </summary>
    public class AudioLoop
    {
        public AudioStream Start { get; }
        public AudioStream End { get; }
        public IReadOnlyList<AudioStream> Middle { get; }

        public AudioLoop(AudioStream start, AudioStream end, IEnumerable<AudioStream> middle)
        {
            Start = start;
            End = end;
            Middle = middle.ToList();
        }
    }
}
