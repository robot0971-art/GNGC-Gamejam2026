using UnityEngine;

namespace Gamejam2026.Presentation
{
    [DisallowMultipleComponent]
    public sealed class BgmPlayer : MonoBehaviour
    {
        [SerializeField] private AudioClip bgmClip;
        [SerializeField, Range(0f, 1f)] private float volume = 0.6f;

        private AudioSource audioSource;
        private bool temporaryMusicPlaying;

        private void Awake()
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }

            ConfigureAudioSource(volume, bgmClip, true);
        }

        private void Start()
        {
            PlayMainLoop();
        }

        public void PlayTemporaryLoop(AudioClip clip)
        {
            PlayTemporaryLoop(clip, volume);
        }

        public void PlayTemporaryLoop(AudioClip clip, float temporaryVolume)
        {
            if (clip == null)
            {
                return;
            }

            EnsureAudioSource();
            temporaryMusicPlaying = true;
            audioSource.Stop();
            ConfigureAudioSource(Mathf.Clamp01(temporaryVolume), clip, true);
            audioSource.Play();
        }

        public void RestoreMainLoop()
        {
            if (!temporaryMusicPlaying)
            {
                return;
            }

            temporaryMusicPlaying = false;
            PlayMainLoop();
        }

        private void PlayMainLoop()
        {
            EnsureAudioSource();

            if (bgmClip != null)
            {
                audioSource.Stop();
                ConfigureAudioSource(volume, bgmClip, true);
                audioSource.Play();
            }
        }

        private void EnsureAudioSource()
        {
            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
            }

            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
        }

        private void ConfigureAudioSource(float sourceVolume, AudioClip clip, bool loop)
        {
            audioSource.playOnAwake = false;
            audioSource.loop = loop;
            audioSource.volume = sourceVolume;
            audioSource.clip = clip;
            audioSource.spatialBlend = 0f;
        }

        private void OnValidate()
        {
            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
            }

            if (audioSource != null)
            {
                ConfigureAudioSource(volume, bgmClip, true);
            }
        }
    }
}
