using System;
using UnityEngine;
using TowerBreakers.Effects;

namespace TowerBreakers.Core.Events
{
    /// <summary>
    /// [설명]: 타격 발생 시 연출(쉐이크, 역경직 등)을 요청하는 이벤트입니다.
    /// </summary>
    public struct OnHitEffectRequested
    {
        public UnityEngine.Vector3 Position;
        public float ShakeIntensity;
        public float ShakeDuration;
        public float HitStopDuration;
        public EffectType HitEffectType;

        /// <summary>
        /// [설명]: 타격 연출 요청 이벤트를 생성합니다.
        /// intensity, duration, hitStop이 0 이하(기본값 -1)일 경우 매니저에 정의된 기본값을 사용합니다.
        /// </summary>
        public OnHitEffectRequested(UnityEngine.Vector3 position, float intensity = -1f, float duration = -1f, float hitStop = -1f, EffectType effectType = EffectType.Hit)
        {
            Position = position;
            ShakeIntensity = intensity;
            ShakeDuration = duration;
            HitStopDuration = hitStop;
            HitEffectType = effectType;
        }
    }

    /// <summary>
    /// [설명]: 사운드 재생을 요청하는 이벤트입니다.
    /// </summary>
    public struct OnSoundRequested
    {
        public string SoundKey;
        public float Volume;
        public float Pitch;

        public OnSoundRequested(string soundKey, float volume = 1f, float pitch = 1f)
        {
            SoundKey = soundKey;
            Volume = volume;
            Pitch = pitch;
        }
    }

    /// <summary>
    /// [설명]: BGM 재생을 요청하는 이벤트입니다.
    /// </summary>
    public struct OnBGMRequested
    {
        public string SoundKey;
        public float FadeInDuration;

        public OnBGMRequested(string soundKey, float fadeInDuration = 1f)
        {
            SoundKey = soundKey;
            FadeInDuration = fadeInDuration;
        }
    }

    /// <summary>
    /// [설명]: BGM 정지를 요청하는 이벤트입니다.
    /// </summary>
    public struct OnBGMStopRequested
    {
        public float FadeOutDuration;

        public OnBGMStopRequested(float fadeOutDuration = 1f)
        {
            FadeOutDuration = fadeOutDuration;
        }
    }
}
