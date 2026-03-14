using System.Collections;
using UnityEngine;
using DG.Tweening;

namespace TowerBreakers.Sound.View
{
    /// <summary>
    /// [기능]: AudioSource 풀 기반 사운드 재생기
    /// [작성자]: Claude
    /// </summary>
    public class SoundPlayer : MonoBehaviour
    {
        [SerializeField, Tooltip("SFX용 AudioSource 풀 크기")]
        private int m_poolSize = 8;

        [SerializeField, Tooltip("BGM용 AudioSource (별도)")]
        private AudioSource m_bgmSource;

        private AudioSource[] m_sfxPool;
        private int m_nextPoolIndex;

        private float m_masterVolume = 1f;
        private float m_sfxVolume = 1f;
        private float m_bgmVolume = 1f;

        public float MasterVolume
        {
            get => m_masterVolume;
            set => m_masterVolume = Mathf.Clamp01(value);
        }

        public float SfxVolume
        {
            get => m_sfxVolume;
            set => m_sfxVolume = Mathf.Clamp01(value);
        }

        public float BgmVolume
        {
            get => m_bgmVolume;
            set => m_bgmVolume = Mathf.Clamp01(value);
        }

        private void Awake()
        {
            InitializeSfxPool();
            InitializeBgmSource();
        }

        private void InitializeSfxPool()
        {
            m_sfxPool = new AudioSource[m_poolSize];

            for (int i = 0; i < m_poolSize; i++)
            {
                var source = gameObject.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.loop = false;
                source.spatialBlend = 0f;
                m_sfxPool[i] = source;
            }

            m_nextPoolIndex = 0;
        }

        private void InitializeBgmSource()
        {
            if (m_bgmSource == null)
            {
                m_bgmSource = gameObject.AddComponent<AudioSource>();
                m_bgmSource.playOnAwake = false;
                m_bgmSource.loop = true;
                m_bgmSource.spatialBlend = 0f;
            }
        }

        public void PlaySFX(AudioClip clip, float volume = 1f, float pitch = 1f)
        {
            if (clip == null)
                return;

            var source = GetNextSfxSource();
            source.clip = clip;
            source.volume = volume * m_sfxVolume * m_masterVolume;
            source.pitch = pitch;
            source.Play();
        }

        private AudioSource GetNextSfxSource()
        {
            var source = m_sfxPool[m_nextPoolIndex];

            while (source.isPlaying)
            {
                m_nextPoolIndex = (m_nextPoolIndex + 1) % m_poolSize;
                source = m_sfxPool[m_nextPoolIndex];

                if (!source.isPlaying)
                    break;
            }

            m_nextPoolIndex = (m_nextPoolIndex + 1) % m_poolSize;
            return source;
        }

        public void PlayBGM(AudioClip clip, float volume = 1f, float fadeInDuration = 1f)
        {
            if (clip == null)
                return;

            if (m_bgmSource.isPlaying && m_bgmSource.clip == clip)
                return;

            StopAllCoroutines();

            if (m_bgmSource.isPlaying)
            {
                StartCoroutine(FadeOutBgm(fadeInDuration, () =>
                {
                    m_bgmSource.clip = clip;
                    m_bgmSource.volume = 0f;
                    m_bgmSource.Play();
                    StartCoroutine(FadeInBgm(volume, fadeInDuration));
                }));
            }
            else
            {
                m_bgmSource.clip = clip;
                m_bgmSource.volume = 0f;
                m_bgmSource.Play();
                StartCoroutine(FadeInBgm(volume, fadeInDuration));
            }
        }

        public void StopBGM(float fadeOutDuration = 1f)
        {
            StopAllCoroutines();
            StartCoroutine(FadeOutBgm(fadeOutDuration, () =>
            {
                m_bgmSource.Stop();
            }));
        }

        private IEnumerator FadeInBgm(float targetVolume, float duration)
        {
            float elapsed = 0f;
            float startVolume = m_bgmSource.volume;
            float finalVolume = targetVolume * m_bgmVolume * m_masterVolume;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                m_bgmSource.volume = Mathf.Lerp(startVolume, finalVolume, t);
                yield return null;
            }

            m_bgmSource.volume = finalVolume;
        }

        private IEnumerator FadeOutBgm(float duration, System.Action onComplete)
        {
            float elapsed = 0f;
            float startVolume = m_bgmSource.volume;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                m_bgmSource.volume = Mathf.Lerp(startVolume, 0f, t);
                yield return null;
            }

            m_bgmSource.volume = 0f;
            onComplete?.Invoke();
        }
    }
}
