using UnityEngine;

namespace U1W.Audio
{
    public static class AudioSettingsStore
    {
        private const string MasterVolumeKey = "u1w.audio.masterVolume";

        public static float LoadMasterVolume()
        {
            return Mathf.Clamp01(PlayerPrefs.GetFloat(MasterVolumeKey, 1f));
        }

        public static void SaveMasterVolume(float volume)
        {
            PlayerPrefs.SetFloat(MasterVolumeKey, Mathf.Clamp01(volume));
            PlayerPrefs.Save();
        }
    }
}
