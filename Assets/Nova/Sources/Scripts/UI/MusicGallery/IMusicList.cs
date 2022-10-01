namespace Nova
{
    public interface IMusicList
    {
        /// <summary>
        /// return current playing music entry
        /// </summary>
        MusicListEntry Current();

        /// <summary>
        /// the next music when user wants to switch song (eg. on Next button clicked)
        /// </summary>
        MusicListEntry Next();

        /// <summary>
        /// the previous music when user wants to switch song (eg. on Previous button clicked)
        /// </summary>
        MusicListEntry Previous();

        /// <summary>
        /// the next music to automatic play when current music finishes.
        /// </summary>
        MusicListEntry Step();
    }
}
