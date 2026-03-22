using System;
using TowerBreakers.Core.Events;
using TowerBreakers.Core.Service;
using TowerBreakers.Player.Logic;
using TowerBreakers.Player.DTO;
using VContainer.Unity;
using UnityEngine;
using Cysharp.Threading.Tasks;

namespace TowerBreakers.Battle
{
    /// <summary>
    /// [클래스]: 전투 판정 및 데미지 처리를 담당하는 핵심 시스템입니다.
    /// 압착 피해를 압착당 1회로 제한하며, 패링 시 1초간의 보호 쿨타임 후 피해 상태를 초기화합니다.
    /// </summary>
    public class CombatSystem : IInitializable, IDisposable
    {
        #region 내부 필드
        private readonly IEventBus m_eventBus;
        private readonly PlayerLogic m_logic;
        private readonly PlayerConfigDTO m_playerConfig;
        private readonly IEffectService m_effectService; // Added IEffectService
        private readonly TowerBreakers.Tower.Service.FloorTransitionService m_transitionService;
        
        private bool m_isDamageEnabled = true;         // 스테이지 상태에 따른 전역 활성화 여부
        private bool m_hasReceivedCrushDamage = false;    // 현재 압착 중 데미지 수령 여부
        private float m_parryProtectionEndTime = 0f;   // [추가]: 패링 보호막 종료 시간
        #endregion

        #region 초기화
        public CombatSystem(IEventBus eventBus, PlayerLogic playerLogic, PlayerConfigDTO playerConfig, TowerBreakers.Tower.Service.FloorTransitionService transitionService, IEffectService effectService)
        {
            m_eventBus = eventBus;
            m_logic = playerLogic;
            m_playerConfig = playerConfig;
            m_transitionService = transitionService;
            m_effectService = effectService; // Initialized IEffectService
        }

        public void Initialize()
        {
            SubscribeEvents();
        }

        private void SubscribeEvents()
        {
            if (m_eventBus == null) return;
            
            m_eventBus.Subscribe<OnFloorCleared>(HandleFloorCleared);
            m_eventBus.Subscribe<OnFloorStarted>(HandleFloorStarted);
            m_eventBus.Subscribe<OnParryPerformed>(HandleParryPerformed);

            if (m_transitionService != null)
            {
                m_transitionService.OnTransitionStarted += () => SetDamageEnabled(false);
                m_transitionService.OnTransitionComplete += () => SetDamageEnabled(true);
            }
        }

        public void Dispose()
        {
            UnsubscribeEvents();
        }

        private void UnsubscribeEvents()
        {
            if (m_eventBus == null) return;
            m_eventBus.Unsubscribe<OnFloorCleared>(HandleFloorCleared);
            m_eventBus.Unsubscribe<OnFloorStarted>(HandleFloorStarted);
            m_eventBus.Unsubscribe<OnParryPerformed>(HandleParryPerformed);
        }
        #endregion

        #region 이벤트 핸들러
        private void HandleFloorCleared(OnFloorCleared evt)
        {
            m_isDamageEnabled = false;
            ResetCrushDamageState();
        }

        private void HandleFloorStarted(OnFloorStarted evt)
        {
            m_isDamageEnabled = true;
            ResetCrushDamageState();
        }

        private void HandleParryPerformed(OnParryPerformed evt)
        {
            // [핵심]: 패링 성공 시 설정된 시간 동안 압착 피해 면역 부여 및 피해 기록 초기화
            float duration = m_playerConfig != null ? m_playerConfig.ParryImmunityDuration : 1.0f;
            m_parryProtectionEndTime = Time.time + duration;
            m_hasReceivedCrushDamage = false; 
            
            // [연출]: 패링 성공 쉐이크
            m_effectService?.PlayCameraShake(0.2f, 0.8f);
        }

        private void ResetCrushDamageState()
        {
            m_hasReceivedCrushDamage = false;
            m_parryProtectionEndTime = 0f;
        }
        #endregion

        #region 핵심 로직
        /// <summary>
        /// [설명]: 벽 압착 시 데미지 처리를 수행합니다.
        /// </summary>
        public void HandleWallCrush()
        {
            // [규칙 1]: 스테이지 클리어/연출 중에는 데미지 무시
            if (!m_isDamageEnabled) return;

            // [규칙 2]: 패링 보호 쿨타임 중에는 데미지 무시
            if (Time.time < m_parryProtectionEndTime) return;

            // [규칙 3]: 특수 이동(퇴각, 대시) 중에는 데미지 무시 (판정 안정성 확보)
            if (m_logic.State.IsRetreating || m_logic.State.IsDashing) return;

            // [규칙 4]: 이미 이번 압착에서 데미지를 입었다면 무시 (압착당 1회 제한)
            if (m_hasReceivedCrushDamage) return;

            if (m_logic == null || m_playerConfig == null) return;

            // 데미지 적용
            m_logic.TakeDamage(m_playerConfig.DamagePerHit);
            m_hasReceivedCrushDamage = true;
            
            // [연출]: 카메라 쉐이크 및 경직(Hit Stop)
            m_effectService?.PlayCameraShake(0.3f, 1.5f); // 강한 쉐이크
            ApplyHitStop(0.15f).Forget(); // 0.15초간 경직

            m_eventBus.Publish(new OnWallCrushOccurred());
        }

        public void SetDamageEnabled(bool enabled)
        {
            m_isDamageEnabled = enabled;
            if (enabled)
            {
                ResetCrushDamageState();
                // [추가]: 재활성화 시 설정된 면역 시간만큼 추가 유예 시간 부여
                float duration = m_playerConfig != null ? m_playerConfig.ParryImmunityDuration : 1.0f;
                m_parryProtectionEndTime = Time.time + duration;
            }
        }

        /// <summary>
        /// [설명]: 짧은 시간 동안 게임 속도를 늦춰 타격감을 극대화하는 '히트스톱' 연출입니다.
        /// </summary>
        private async UniTaskVoid ApplyHitStop(float duration)
        {
            float originalTimeScale = Time.timeScale;
            Time.timeScale = 0.05f; // 거의 멈춤 (경직 효과)
            
            await UniTask.Delay(TimeSpan.FromSeconds(duration), ignoreTimeScale: true);
            
            Time.timeScale = originalTimeScale;
        }
        #endregion
    }
}
