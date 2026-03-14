using UnityEngine;
using TowerBreakers.Player.Data.Models;
using TowerBreakers.Core.Events;
using VContainer;
using System;

namespace TowerBreakers.Player.View
{
    /// <summary>
    /// [설명]: 플레이어 캐릭터와 관련된 시각 효과(VFX)를 통합 관리하는 뷰 클래스입니다.
    /// 생명력 증가, 무기 타격, 스킬 효과 등의 시각적 연출을 담당합니다.
    /// </summary>
    public class PlayerEffectView : MonoBehaviour
    {
        #region 에디터 설정
        [Header("하트(생명력) 효과")]
        [SerializeField, Tooltip("하트가 추가(회복/최대치 증가)될 때 재생할 파티클")]
        private ParticleSystem m_heartGainParticle;

        [Header("타격 효과 설정")]
        [SerializeField, Tooltip("기본 타격 시 플레이어 위치에서 발생할 파티클 (향후 확장용)")]
        private ParticleSystem m_defaultHitParticle;
        #endregion

        #region 내부 필드
        private PlayerModel m_model;
        private IEventBus m_eventBus;
        
        private int m_lastLifeCount;
        private int m_lastMaxLifeCount;
        #endregion

        #region 초기화 및 바인딩 로직
        /// <summary>
        /// [설명]: 의존성을 주입받고 이벤트를 구독하여 초기화합니다.
        /// </summary>
        [Inject]
        public void Initialize(PlayerModel model, IEventBus eventBus)
        {
            m_model = model;
            m_eventBus = eventBus;

            if (m_model != null)
            {
                // 초기 값 캐싱
                m_lastLifeCount = m_model.CurrentLifeCount;
                m_lastMaxLifeCount = m_model.MaxLifeCount;

                // 생명력 변경 이벤트 구독
                m_model.OnLifeCountChanged += HandleLifeCountChanged;
            }

            if (m_eventBus != null)
            {
                // [참고]: 향후 무기 타격 이펙트 수신 시 활용
                // m_eventBus.Subscribe<OnPlayerAttackHit>(HandleAttackHit);
            }
        }

        private void OnDestroy()
        {
            if (m_model != null)
            {
                m_model.OnLifeCountChanged -= HandleLifeCountChanged;
            }
        }
        #endregion

        #region 공개 메서드 (API)
        /// <summary>
        /// [설명]: 특정 타입의 이펙트를 지정된 위치에서 재생합니다.
        /// 외부 시스템(Skill, Combat 등)에서 호출하여 확장할 수 있습니다.
        /// </summary>
        public void PlayEffect(EffectType type, Vector3? position = null)
        {
            Vector3 targetPos = position ?? transform.position;

            switch (type)
            {
                case EffectType.HeartGain:
                    if (m_heartGainParticle != null)
                    {
                        m_heartGainParticle.transform.position = targetPos;
                        m_heartGainParticle.Play();
                    }
                    break;
                case EffectType.BasicHit:
                    if (m_defaultHitParticle != null)
                    {
                        m_defaultHitParticle.transform.position = targetPos;
                        m_defaultHitParticle.Play();
                    }
                    break;
            }
        }
        #endregion

        #region 내부 로직
        /// <summary>
        /// [설명]: 생명력 수치 변화를 감지하여 증가한 경우 이펙트를 재생합니다.
        /// </summary>
        private void HandleLifeCountChanged(int current, int max)
        {
            // 현재 생명력이 늘어났거나, 최대 생명력이 늘어난 경우 (하트 추가 상황)
            if (current > m_lastLifeCount || max > m_lastMaxLifeCount)
            {
                PlayEffect(EffectType.HeartGain);
            }

            m_lastLifeCount = current;
            m_lastMaxLifeCount = max;
        }
        #endregion

        #region 내부 클래스 및 구조체
        public enum EffectType
        {
            HeartGain,
            BasicHit,
            SkillActivate,
            LevelUp
        }
        #endregion
    }
}
