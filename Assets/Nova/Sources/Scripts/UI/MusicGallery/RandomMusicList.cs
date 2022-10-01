using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace Nova
{
    public class RandomMusicList : IMusicList
    {
        private readonly List<MusicListEntry> entries;
        private int currentIndex;

        public RandomMusicList(List<MusicListEntry> entries, int currentIndex)
        {
            Assert.IsTrue(entries.Count > 0);
            this.entries = entries;
            this.currentIndex = currentIndex % entries.Count;
        }

        public MusicListEntry Current()
        {
            return currentIndex < entries.Count ? entries[currentIndex] : null;
        }

        private int RandomNotSelf()
        {
            if (entries.Count <= 1) return currentIndex;
            var index = Random.Range(0, entries.Count - 1);
            if (index < currentIndex) return index;
            return index + 1;
        }

        public MusicListEntry Next()
        {
            currentIndex = RandomNotSelf();
            return Current();
        }

        public MusicListEntry Previous()
        {
            currentIndex = RandomNotSelf();
            return Current();
        }

        public MusicListEntry Step()
        {
            return Next();
        }
    }
}
