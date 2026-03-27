using System;
using UnityEngine;

namespace U1W.Game
{
    [CreateAssetMenu(
        fileName = "StoryNameTable",
        menuName = "U1W/Game/Story Name Table")]
    public sealed class StoryNameTable : ScriptableObject
    {
        [SerializeField] private StoryNameEntry[] entries = Array.Empty<StoryNameEntry>();

        public StoryNameEntry[] Entries => entries;

        public bool TryGetEntry(string key, out StoryNameEntry entry)
        {
            if (string.IsNullOrEmpty(key) || entries == null)
            {
                entry = null;
                return false;
            }

            for (int i = 0; i < entries.Length; i++)
            {
                StoryNameEntry candidate = entries[i];
                if (candidate == null || string.IsNullOrEmpty(candidate.Key))
                {
                    continue;
                }

                if (string.Equals(candidate.Key, key, StringComparison.Ordinal))
                {
                    entry = candidate;
                    return true;
                }
            }

            entry = null;
            return false;
        }
    }

    [Serializable]
    public sealed class StoryNameEntry
    {
        [SerializeField] private string key = "name_0";
        [SerializeField] private string displayName = "A";
        [SerializeField] private Color nameColor = Color.white;

        public string Key => key;
        public string DisplayName => displayName;
        public Color NameColor => nameColor;
    }
}
