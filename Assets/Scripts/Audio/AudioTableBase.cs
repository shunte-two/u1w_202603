using System;
using System.Collections.Generic;
using UnityEngine;

namespace U1W.Audio
{
    public abstract class AudioTableBase : ScriptableObject
    {
        [SerializeField] private List<Entry> entries = new();

        private Dictionary<string, AudioClip> clipByKey;

        public bool TryGetClip(string key, out AudioClip clip)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                clip = null;
                return false;
            }

            EnsureCache();
            return clipByKey.TryGetValue(key, out clip);
        }

        private void EnsureCache()
        {
            if (clipByKey != null)
            {
                return;
            }

            clipByKey = new Dictionary<string, AudioClip>(StringComparer.Ordinal);

            foreach (Entry entry in entries)
            {
                if (string.IsNullOrWhiteSpace(entry.Key) || entry.Clip == null)
                {
                    continue;
                }

                clipByKey[entry.Key] = entry.Clip;
            }
        }

        [Serializable]
        private struct Entry
        {
            public string Key;
            public AudioClip Clip;
        }
    }
}
