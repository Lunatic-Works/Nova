using UnityEngine;

namespace Nova
{
    // TODO Current implementation of this class is inefficient, some modifications are needed.
    /// <summary>
    /// The class that loads assets at runtime
    /// Assets can be Sprite, BGM or Sound Effect
    /// </summary>
    /// <remarks>
    /// All assets should be stored in a Resources folder or a subfolder of a Resources folder
    /// </remarks>
    public class AssetsLoader
    {
        /// <summary>
        /// Get sprite by path
        /// </summary>
        /// <param name="path">
        /// The path of the sprite, the convention is the same as that of <see cref="Resources.Load(string)"/>
        /// </param>
        /// <returns>The specified sprite or null if not found</returns>
        public static Sprite GetSprite(string path)
        {
            return Resources.Load<Sprite>(path);
        }

        /// <summary>
        /// Get AudioClip by path
        /// </summary>
        /// <param name="path">
        /// The path of the AudioClip, the convention is the same as that of <see cref="Resources.Load(string)"/>
        /// </param>
        /// <returns>The specified AudioClip or null if not found</returns>
        public static AudioClip GetAudioClip(string path)
        {
            return Resources.Load<AudioClip>(path);
        }
    }
}