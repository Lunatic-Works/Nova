using System;
using UnityEngine;

namespace Nova
{
    public class MusicEntryTimes
    {
        public readonly int loopBeginSample;
        public readonly int loopEndSample;

        public static implicit operator MusicEntryTimes(MusicEntry musicEntry) =>
            musicEntry != null ? new MusicEntryTimes(musicEntry) : null;

        public static implicit operator MusicEntryTimes(AudioClip clip) =>
            clip != null ? new MusicEntryTimes(clip) : null;

        public MusicEntryTimes(MusicEntry musicEntry)
        {
            this.loopBeginSample = musicEntry.loopBeginSample;
            this.loopEndSample = musicEntry.loopEndSample;
        }

        public MusicEntryTimes(AudioClip audioClip)
        {
            this.loopEndSample = audioClip.samples;
        }
    }

    public class MusicEntry : ScriptableObject
    {
        public string id;
        public SerializableDictionary<SystemLanguage, string> displayNames;
        public string resourcePath;
        public int loopBeginSample;
        public int loopEndSample;

        public string GetDisplayName()
        {
            if (displayNames.TryGetValue(I18n.CurrentLocale, out var name))
            {
                return name;
            }
            else
            {
                return "(No title)";
            }
        }
    }
}
