using System;
using TowerBreakers.Core.Events;
using TowerBreakers.Player.Logic;
using TowerBreakers.Player.DTO;
using VContainer.Unity;
using UnityEngine;

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
        
        private bool m_isDamageEnabled = true;         // 스테이지 상태에 따른 전역 활성화 여부
        private bool m_hasReceivedCrushDamage = false;    // 현재 압착 중 데미지 수령 여부
        private float m_parryProtectionEndTime = 0f;   // [추가]: 패링 보호막 종료 시간
        #endregion

        #region 초기화
        public CombatSystem(IEventBus eventBus, PlayerLogic m_logic, PlayerConfigDTO playerConfig)
        {
            m_eventBus = eventBus;
            this.m_logic = m_logic;
            m_playerConfig = playerConfig;
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
            // [핵심]: 패링 성공 시 1초간 압착 피해 면역 부여 및 피해 기록 초기화
            m_parryProtectionEndTime = Time.time + 1.0f;
            m_hasReceivedCrushDamage = false;
            
            Debug.Log($"[CombatSystem] 패링 성공 - 1초간 압착 피해 면역 활성화 (종료 예상: {m_parryProtectionEndTime:F2}s)");
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

            // [규칙 2]: 패링 보호 쿨타임 중에는 데미지 무시 (1초 보호)
            if (Time.time < m_parryProtectionEndTime) return;

            // [규칙 3]: 이미 이번 압착에서 데미지를 입었다면 무시 (압착당 1회 제한)
            if (m_hasReceivedCrushDamage) return;

            if (m_logic == null || m_playerConfig == null) return;

            // 데미지 적용
            m_logic.TakeDamage(m_playerConfig.DamagePerHit);
            m_hasReceivedCrushDamage = true;
            
            m_eventBus.Publish(new OnWallCrushOccurred());
            
            Debug.Log($"[CombatSystem] 플레이어 압착 피해 발생 - 다음 패링 성공 시까지 추가 압착 피해는 발생하지 않습니다.");
        }

        public void SetDamageEnabled(bool enabled)
        {
            m_isDamageEnabled = enabled;
            if (enabled) ResetCrushDamageState();
        }
        #endregion
    }
}
