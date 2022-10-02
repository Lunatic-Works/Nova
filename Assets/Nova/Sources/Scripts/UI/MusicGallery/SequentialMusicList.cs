using System.Collections.Generic;
using UnityEngine.Assertions;

namespace Nova
{
    public class SequentialMusicList : IMusicList
    {
        private readonly List<MusicListEntry> entries;
        private int currentIndex;

        public SequentialMusicList(List<MusicListEntry> entries, int currentIndex)
        {
            Assert.IsTrue(entries.Count > 0);
            this.entries = entries;
            this.currentIndex = currentIndex % entries.Count;
        }

        public virtual MusicListEntry Current()
        {
            return currentIndex < entries.Count ? entries[currentIndex] : null;
        }

        public virtual MusicListEntry Next()
        {
            currentIndex = (currentIndex + 1) % entries.Count;
            return Current();
        }

        public virtual MusicListEntry Previous()
        {
            currentIndex = (currentIndex + entries.Count - 1) % entries.Count;
            return Current();
        }

        public virtual MusicListEntry Step()
        {
            return Next();
        }
    }
}
