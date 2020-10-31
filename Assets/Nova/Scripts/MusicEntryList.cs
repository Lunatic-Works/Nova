using System.Collections.Generic;
using UnityEngine;

namespace Nova
{
    public class MusicEntryList : ScriptableObject
    {
        public List<MusicEntry> entries;

        public MusicEntry FindEntryByID(string id)
        {
            return entries.Find(e => e.id == id);
        }
    }
}