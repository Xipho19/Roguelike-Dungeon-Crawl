using System.Collections.Generic;
using UnityEngine;

namespace Roguelike.Audio
{
    /// <summary>
    /// 极简音效服务：从 Resources/Audio/KenneyRPG 与 Resources/Audio/Custom
    /// 加载全部音效，用一组 AudioSource 轮播 PlayOneShot，带轻微随机音高。
    ///
    /// 用法：AudioService.Play("tear_shot", 0.5f);
    /// </summary>
    public static class AudioService
    {
        private static readonly string[] ResourceFolders =
        {
            "Audio/KenneyRPG",
            "Audio/Custom",
        };

        private const string MusicVolumeKey = "audio.musicVolume";
        private const string SfxVolumeKey = "audio.sfxVolume";

        private static readonly Dictionary<string, AudioClip> Clips = new Dictionary<string, AudioClip>();
        private static readonly List<AudioSource> Sources = new List<AudioSource>();
        private static readonly HashSet<string> MissingWarnings = new HashSet<string>();
        private static AudioSource _musicSource;
        private static int _next;
        private static float _musicVolume = 1f;
        private static float _sfxVolume = 1f;
        private static float _musicBaseVolume;

        public static float MusicVolume => _musicVolume;
        public static float SfxVolume => _sfxVolume;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            Clips.Clear();
            Sources.Clear();
            MissingWarnings.Clear();

            _musicVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(MusicVolumeKey, 1f));
            _sfxVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(SfxVolumeKey, 1f));

            for (int folder = 0; folder < ResourceFolders.Length; folder++)
            {
                AudioClip[] clips = Resources.LoadAll<AudioClip>(ResourceFolders[folder]);
                for (int i = 0; i < clips.Length; i++)
                    Clips[clips[i].name] = clips[i];
            }

            var root = new GameObject("AudioService");
            Object.DontDestroyOnLoad(root);

            for (int i = 0; i < 12; i++)
            {
                var source = root.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.spatialBlend = 0f;
                Sources.Add(source);
            }

            // 单独的循环音乐源，不与一次性音效抢占通道
            _musicSource = root.AddComponent<AudioSource>();
            _musicSource.playOnAwake = false;
            _musicSource.spatialBlend = 0f;
            _musicSource.loop = true;
        }

        /// <summary>循环播放楼层 BGM；传入 null 或换层时自动切换。</summary>
        public static void PlayMusic(AudioClip clip, float volume = 0.5f)
        {
            _musicBaseVolume = Mathf.Clamp01(volume);

            if (clip == null)
            {
                StopMusic();
                return;
            }

            if (_musicSource == null) return;

            if (_musicSource.clip == clip)
            {
                _musicSource.loop = true;
                _musicSource.volume = _musicBaseVolume * _musicVolume;
                if (!_musicSource.isPlaying) _musicSource.Play();
                return;
            }

            _musicSource.clip = clip;
            _musicSource.loop = true;
            _musicSource.volume = _musicBaseVolume * _musicVolume;
            _musicSource.Play();
        }

        public static void StopMusic()
        {
            _musicBaseVolume = 0f;

            if (_musicSource == null) return;

            _musicSource.Stop();
            _musicSource.clip = null;
        }

        public static void SetMusicVolume(float value)
        {
            _musicVolume = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(MusicVolumeKey, _musicVolume);

            if (_musicSource != null && _musicSource.clip != null)
                _musicSource.volume = _musicBaseVolume * _musicVolume;
        }

        public static void SetSfxVolume(float value)
        {
            _sfxVolume = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(SfxVolumeKey, _sfxVolume);
        }

        public static void SaveAudioSettings()
        {
            PlayerPrefs.Save();
        }

        public static AudioClip CurrentMusic => _musicSource != null ? _musicSource.clip : null;
        public static bool IsMusicPlaying => _musicSource != null && _musicSource.isPlaying;

        /// <param name="pitchJitter">音高随机浮动；语音类音效传 0 保持原调。</param>
        public static void Play(string clipName, float volume = 1f, float pitchJitter = 0.05f)
        {
            if (string.IsNullOrEmpty(clipName)) return;
            if (!Clips.TryGetValue(clipName, out AudioClip clip) || clip == null)
            {
                WarnMissingClip(clipName);
                return;
            }

            if (_sfxVolume <= 0.0001f) return;
            if (Sources.Count == 0) return;

            AudioSource source = Sources[_next];
            _next = (_next + 1) % Sources.Count;

            source.pitch = pitchJitter > 0f
                ? Random.Range(1f - pitchJitter, 1f + pitchJitter)
                : 1f;
            source.PlayOneShot(clip, Mathf.Clamp01(volume * _sfxVolume));
        }

        private static void WarnMissingClip(string clipName)
        {
            if (!MissingWarnings.Add(clipName)) return;

            Debug.LogWarning($"[Audio] 找不到音效 '{clipName}'，请检查 Resources/Audio。");
        }
    }
}
