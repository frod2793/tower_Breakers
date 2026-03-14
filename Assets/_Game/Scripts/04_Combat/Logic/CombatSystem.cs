using TowerBreakers.Core.Events;
using TowerBreakers.Player.Data.Models;
using TowerBreakers.Player.Data.SO;
using TowerBreakers.Player.Logic;
using TowerBreakers.Tower.Logic;
using System;
using UnityEngine;
using VContainer.Unity;

namespace TowerBreakers.Combat.Logic
{
    /// <summary>
    /// [설명]: 전투 관련 핵심 로직(데미지 판정, 이벤트 처리 등)을 담당하는 시스템 클래스입니다.
    /// 순수 C# 로직 클래스로서 외부 의존성을 주입받아 비즈니스 로직을 수행합니다.
    /// </summary>
    public class CombatSystem : IInitializable, IDisposable
    {
        #region 내부 필드
        private readonly IEventBus m_eventBus;
        private readonly PlayerModel m_playerModel;
        private readonly PlayerData m_playerData;
        private readonly TowerManager m_towerManager;
        #endregion

        #region 초기화 및 해제
        /// <summary>
        /// [설명]: 생성자를 통해 필요한 의존성을 주입받습니다.
        /// </summary>
        public CombatSystem(
            IEventBus eventBus, 
            PlayerModel playerModel, 
            PlayerData playerData, 
            TowerManager towerManager)
        {
            m_eventBus = eventBus;
            m_playerModel = playerModel;
            m_playerData = playerData;
            m_towerManager = towerManager;
        }

        /// <summary>
        /// [설명]: 시스템 초기화 시 필요한 이벤트를 구독합니다.
        /// </summary>
        public void Initialize()
        {
            if (m_eventBus != null)
            {
                m_eventBus.Subscribe<OnPlayerPushed>(HandlePlayerPushedAtWall);
            }
        }

        /// <summary>
        /// [설명]: 시스템 종료 시 이벤트 구독을 해제하여 메모리 누수를 방지합니다.
        /// </summary>
        public void Dispose()
        {
            if (m_eventBus != null)
            {
                m_eventBus.Unsubscribe<OnPlayerPushed>(HandlePlayerPushedAtWall);
            }
        }
        #endregion

        #region 비즈니스 로직
        /// <summary>
        /// [설명]: 플레이어가 벽에 밀렸을 때의 데미지 처리를 수행합니다.
        /// </summary>
        /// <param name="evt">플레이어 밀림 이벤트 데이터</param>
        private void HandlePlayerPushedAtWall(OnPlayerPushed evt)
        {
            if (m_playerModel == null || m_eventBus == null) return;

            // 횟수 기반 체력 시스템: 벽 압착 시 고정 1 데미지 소모
            const int DAMAGE_AMOUNT = 1;
            
            m_playerModel.TakeDamage(DAMAGE_AMOUNT);
            
            // 데미지 발생 이벤트 발행
            m_eventBus.Publish(new OnPlayerDamaged(DAMAGE_AMOUNT, m_playerModel.CurrentLifeCount));
            
            // 데미지 텍스트 출력 요청 (플레이어 머리 위 부근 추정 좌표)
            m_eventBus.Publish(new OnDamageTextRequested(new Vector3(0f, 1.5f, 0f), DAMAGE_AMOUNT));

            // 벽 압착에 의한 특수 효과(적 동결 등)를 위해 현재 층 정보를 포함한 이벤트 발행
            int currentFloor = (m_towerManager != null) ? m_towerManager.CurrentFloorIndex : 0;
            m_eventBus.Publish(new OnWallCrushOccurred(DAMAGE_AMOUNT, currentFloor));
            
            // 사망 판정
            if (m_playerModel.IsDead)
            {
                #if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.LogWarning("[CombatSystem] 플레이어 사망 감지: OnGameOver 발행");
                #endif
                m_eventBus.Publish(new OnGameOver());
            }
        }
        #endregion
    }
}
