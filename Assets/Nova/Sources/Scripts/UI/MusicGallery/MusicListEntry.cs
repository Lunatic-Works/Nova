namespace Nova
{
    public class MusicListEntry
    {
        public int index;
        public readonly MusicEntry entry;

        public MusicListEntry(int index, MusicEntry entry)
        {
            this.index = index;
            this.entry = entry;
        }
    }
}
