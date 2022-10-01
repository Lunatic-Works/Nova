using System.Collections.Generic;

namespace Nova
{
    public class SingleLoopMusicList : SequentialMusicList
    {
        public SingleLoopMusicList(List<MusicListEntry> entries, int currentIndex) : base(entries, currentIndex) { }

        public override MusicListEntry Step()
        {
            return Current();
        }
    }
}
