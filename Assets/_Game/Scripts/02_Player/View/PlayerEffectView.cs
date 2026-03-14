using TowerBreakers.Player.Data.Models;
using TowerBreakers.Core.Events;
using TowerBreakers.Effects;
using VContainer;
using UnityEngine;
using System;

namespace TowerBreakers.Player.View
{
    /// <summary>
    /// [설명]: 플레이어 캐릭터와 관련된 시각 효과(VFX)를 통합 관리하는 뷰 클래스입니다.
    /// 생명력 증가, 무기 타격, 스킬 효과 등의 시각적 연출을 담당합니다.
    /// </summary>
    public class PlayerEffectView : MonoBehaviour
    {
        #region 내부 필드
        private PlayerModel m_model;
        private IEventBus m_eventBus;
        private Effects.EffectManager m_effectManager;
        
        private int m_lastLifeCount;
        private int m_lastMaxLifeCount;
        #endregion

        #region 초기화 및 바인딩 로직
        /// <summary>
        /// [설명]: 의존성을 주입받고 이벤트를 구독하여 초기화합니다.
        /// </summary>
        [Inject]
        public void Initialize(PlayerModel model, IEventBus eventBus, Effects.EffectManager effectManager)
        {
            m_model = model;
            m_eventBus = eventBus;
            m_effectManager = effectManager;

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
            if (m_effectManager == null) return;
            
            Vector3 targetPos = position ?? transform.position;
            m_effectManager.PlayEffect(type, targetPos);
        }
        #endregion

        #region 내부 로직
        /// <summary>
        /// [설명]: 생명력 수치 변화를 감지하여 증가/감소 시 연출을 수행합니다.
        /// </summary>
        private void HandleLifeCountChanged(int current, int max)
        {
            // [추가]: 생명력이 줄어든 경우 (피격 상황)
            if (current < m_lastLifeCount)
            {
                // [설명]: 플레이어 피격 시에도 사운드 발행. (카메라 쉐이크 등은 CombatEffectPresenter를 통해 확장 가능)
                m_eventBus?.Publish(new OnSoundRequested("Hit"));
            }

            // 현재 생명력이 늘어났거나, 최대 생명력이 늘어난 경우 (하트 추가 상황)
            if (current > m_lastLifeCount || max > m_lastMaxLifeCount)
            {
                PlayEffect(EffectType.HeartGain);
            }

            m_lastLifeCount = current;
            m_lastMaxLifeCount = max;
        }
        #endregion
    }
}
