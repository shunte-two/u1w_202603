using System;
using System.Collections.Generic;
using UnityEngine;

namespace U1W.Audio
{
    [DefaultExecutionOrder(-900)]
    public sealed class AudioManager : MonoBehaviour
    {
        private static AudioManager instance;

        [SerializeField] private BgmTable bgmTable;
        [SerializeField] private SeTable seTable;
        [SerializeField] [Range(0f, 1f)] private float bgmMasterVolume = 1f;
        [SerializeField] [Range(0f, 1f)] private float seMasterVolume = 1f;

        private AudioSource bgmSource;
        private AudioSource seSource;
        private float currentBgmVolume = 1f;

        public static string CurrentBgmKey { get; private set; }

        public static AudioManager EnsureInstance()
        {
            if (instance != null)
            {
                return instance;
            }

            instance = FindFirstObjectByType<AudioManager>();
            if (instance != null)
            {
                instance.Initialize();
                return instance;
            }

            Debug.LogError("AudioManager is not present in the scene. Place an AudioManager in the scene before using audio APIs.");
            return null;
        }

        public static void PlayBgm(string key, float volume = 1f, bool loop = true)
        {
            AudioManager manager = EnsureInstance();
            if (manager == null)
            {
                return;
            }

            if (!manager.TryGetBgmClip(key, out AudioClip clip))
            {
                Debug.LogWarning($"AudioManager.PlayBgm failed: key '{key}' was not found in BGMTable.");
                return;
            }

            manager.PlayBgmInternal(key, clip, volume, loop);
        }

        public static void StopBgm()
        {
            if (instance == null)
            {
                return;
            }

            instance.bgmSource.Stop();
            instance.bgmSource.clip = null;
            CurrentBgmKey = null;
        }

        public static void PauseBgm()
        {
            if (instance == null || !instance.bgmSource.isPlaying)
            {
                return;
            }

            instance.bgmSource.Pause();
        }

        public static void ResumeBgm()
        {
            if (instance == null || instance.bgmSource.clip == null)
            {
                return;
            }

            instance.bgmSource.UnPause();
        }

        public static void SetBgmVolume(float volume)
        {
            AudioManager manager = EnsureInstance();
            if (manager == null)
            {
                return;
            }

            manager.bgmMasterVolume = Mathf.Clamp01(volume);
            manager.ApplyBgmVolume();
        }

        public static void SetSeMasterVolume(float volume)
        {
            AudioManager manager = EnsureInstance();
            if (manager == null)
            {
                return;
            }

            manager.seMasterVolume = Mathf.Clamp01(volume);
        }

        public static void PlaySe(string key, float volume = 1f)
        {
            AudioManager manager = EnsureInstance();
            if (manager == null)
            {
                return;
            }

            if (!manager.TryGetSeClip(key, out AudioClip clip))
            {
                Debug.LogWarning($"AudioManager.PlaySe failed: key '{key}' was not found in SETable.");
                return;
            }

            float finalVolume = Mathf.Clamp01(volume) * manager.seMasterVolume;
            manager.seSource.PlayOneShot(clip, finalVolume);
        }

        public static void StopAllSe()
        {
            if (instance == null)
            {
                return;
            }

            instance.seSource.Stop();
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            Initialize();
        }

        private void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
            }
        }

        private void Initialize()
        {
            DontDestroyOnLoad(gameObject);

            if (bgmSource == null)
            {
                bgmSource = GetOrAddAudioSource("BGM Source");
                bgmSource.loop = true;
            }

            if (seSource == null)
            {
                seSource = GetOrAddAudioSource("SE Source");
                seSource.loop = false;
            }
            ApplyBgmVolume();
        }

        private bool TryGetBgmClip(string key, out AudioClip clip)
        {
            if (bgmTable != null)
            {
                return bgmTable.TryGetClip(key, out clip);
            }

            clip = null;
            return false;
        }

        private bool TryGetSeClip(string key, out AudioClip clip)
        {
            if (seTable != null)
            {
                return seTable.TryGetClip(key, out clip);
            }

            clip = null;
            return false;
        }

        private void PlayBgmInternal(string key, AudioClip clip, float volume, bool loop)
        {
            currentBgmVolume = Mathf.Clamp01(volume);
            CurrentBgmKey = key;
            bgmSource.clip = clip;
            bgmSource.loop = loop;
            ApplyBgmVolume();
            bgmSource.Play();
        }

        private void ApplyBgmVolume()
        {
            bgmSource.volume = currentBgmVolume * bgmMasterVolume;
        }

        private AudioSource GetOrAddAudioSource(string childName)
        {
            Transform child = transform.Find(childName);
            GameObject target = child != null ? child.gameObject : new GameObject(childName);
            target.transform.SetParent(transform, false);

            AudioSource source = target.GetComponent<AudioSource>();
            if (source == null)
            {
                source = target.AddComponent<AudioSource>();
            }

            source.playOnAwake = false;
            source.spatialBlend = 0f;
            return source;
        }
    }

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

    [CreateAssetMenu(fileName = "BGMTable", menuName = "Audio/BGM Table")]
    public sealed class BgmTable : AudioTableBase
    {
    }

    [CreateAssetMenu(fileName = "SETable", menuName = "Audio/SE Table")]
    public sealed class SeTable : AudioTableBase
    {
    }
}
