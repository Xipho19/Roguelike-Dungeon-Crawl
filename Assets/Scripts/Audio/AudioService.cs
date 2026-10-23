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

        private static readonly Dictionary<string, AudioClip> Clips = new Dictionary<string, AudioClip>();
        private static readonly List<AudioSource> Sources = new List<AudioSource>();
        private static int _next;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            Clips.Clear();
            Sources.Clear();

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
        }

        /// <param name="pitchJitter">音高随机浮动；语音类音效传 0 保持原调。</param>
        public static void Play(string clipName, float volume = 1f, float pitchJitter = 0.05f)
        {
            if (string.IsNullOrEmpty(clipName)) return;
            if (!Clips.TryGetValue(clipName, out AudioClip clip) || clip == null) return;
            if (Sources.Count == 0) return;

            AudioSource source = Sources[_next];
            _next = (_next + 1) % Sources.Count;

            source.pitch = pitchJitter > 0f
                ? Random.Range(1f - pitchJitter, 1f + pitchJitter)
                : 1f;
            source.PlayOneShot(clip, Mathf.Clamp01(volume));
        }
    }
}
