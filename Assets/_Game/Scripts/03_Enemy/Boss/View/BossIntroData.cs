using DG.Tweening;
using UnityEngine;

namespace TowerBreakers.Enemy.Boss.View
{
    /// <summary>
    /// [설명]: 보스 등장연출 설정 데이터입니다.
    /// </summary>
    [CreateAssetMenu(fileName = "BossIntroData", menuName = "TowerBreakers/Boss/BossIntroData")]
    public class BossIntroData : ScriptableObject
    {
        [Header("스케일 애니메이션")]
        [SerializeField, Tooltip("시작 스케일")]
        private Vector3 m_startScale = Vector3.zero;
        
        [SerializeField, Tooltip("종료 스케일")]
        private Vector3 m_endScale = Vector3.one;
        
        [SerializeField, Tooltip("스케일 애니메이션 시간")]
        private float m_scaleDuration = 1f;
        
        [SerializeField, Tooltip("스케일 이징")]
        private Ease m_scaleEase = Ease.OutBack;

        [Header("위치 애니메이션")]
        [SerializeField, Tooltip("시작 위치 오프셋 (로컬)")]
        private Vector3 m_startPositionOffset = new Vector3(0, 5f, 0);
        
        [SerializeField, Tooltip("위치 애니메이션 시간")]
        private float m_positionDuration = 0.8f;
        
        [SerializeField, Tooltip("위치 이징")]
        private Ease m_positionEase = Ease.OutQuart;

        [Header("페이드인")]
        [SerializeField, Tooltip("페이드인 사용 여부")]
        private bool m_useFadeIn = true;
        
        [SerializeField, Tooltip("페이드인 시간")]
        private float m_fadeInDuration = 0.5f;

        [Header("쉐이크 효과")]
        [SerializeField, Tooltip("등장 시 쉐이크 사용 여부")]
        private bool m_useShake = true;
        
        [SerializeField, Tooltip("쉐이크 강도")]
        private float m_shakeIntensity = 0.3f;
        
        [SerializeField, Tooltip("쉐이크 시간")]
        private float m_shakeDuration = 0.3f;

        [Header("사운드")]
        [SerializeField, Tooltip("등장 사운드 키 (SoundDatabase 참조)")]
        private string m_introSoundKey;

        [Header("BGM")]
        [SerializeField, Tooltip("보스 테마 BGM 키 (등장 시 재생)")]
        private string m_bgmKey;

        [SerializeField, Tooltip("BGM 페이드인 시간")]
        private float m_bgmFadeInDuration = 1f;

        [Header("타이머")]
        [SerializeField, Tooltip("총 등장연출 시간 (수동 설정, 0 = 자동 계산)")]
        private float m_totalDuration = 0f;

        #region 프로퍼티
        public Vector3 StartScale => m_startScale;
        public Vector3 EndScale => m_endScale;
        public float ScaleDuration => m_scaleDuration;
        public Ease ScaleEase => m_scaleEase;

        public Vector3 StartPositionOffset => m_startPositionOffset;
        public float PositionDuration => m_positionDuration;
        public Ease PositionEase => m_positionEase;

        public bool UseFadeIn => m_useFadeIn;
        public float FadeInDuration => m_fadeInDuration;

        public bool UseShake => m_useShake;
        public float ShakeIntensity => m_shakeIntensity;
        public float ShakeDuration => m_shakeDuration;

        public string IntroSoundKey => m_introSoundKey;

        public string BgmKey => m_bgmKey;
        public float BgmFadeInDuration => m_bgmFadeInDuration;

        public float TotalDuration
        {
            get
            {
                if (m_totalDuration > 0) return m_totalDuration;
                return Mathf.Max(m_scaleDuration, m_positionDuration) + m_fadeInDuration;
            }
        }
        #endregion
    }
}
