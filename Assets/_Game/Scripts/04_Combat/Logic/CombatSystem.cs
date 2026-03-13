using TowerBreakers.Core.Events;
using TowerBreakers.Player.Data;
using TowerBreakers.Player.Logic;
using TowerBreakers.Tower.Logic;
using System;
using UnityEngine;
using VContainer.Unity;

namespace TowerBreakers.Combat.Logic
{
    /// <summary>
    /// [설명]: 전투 관련 핵심 로직(데미지 판정, 이벤트 처리 등)을 담당하는 시스템 클래스입니다.
    /// VContainer에 의해 관리되며, 게임 시작 시 IInitializable을 통해 초기화됩니다.
    /// </summary>
    public class CombatSystem : IInitializable, IDisposable
    {
        #region 내부 필드
        private readonly IEventBus m_eventBus;
        private readonly PlayerModel m_playerModel;
        private readonly PlayerData m_playerData;
        private readonly Tower.Logic.TowerManager m_towerManager;
        #endregion

        #region 초기화
        public CombatSystem(IEventBus eventBus, PlayerModel playerModel, PlayerData playerData, Tower.Logic.TowerManager towerManager)
        {
            m_eventBus = eventBus;
            m_playerModel = playerModel;
            m_playerData = playerData;
            m_towerManager = towerManager;
        }

        /// <summary>
        /// [설명]: VContainer에 의해 인스턴스가 생성된 후 호출됩니다.
        /// </summary>
        public void Initialize()
        {
            m_eventBus.Subscribe<OnPlayerPushed>(HandlePlayerPushedAtWall);
            // Debug.Log("[CombatSystem] 초기화 완료 및 OnPlayerPushed 구독 기동");
        }
        #endregion

        #region 비즈니스 로직
        /// <summary>
        /// [설명]: 플레이어가 벽에 밀렸을 때의 데미지 처리를 수행합니다.
        /// </summary>
        private void HandlePlayerPushedAtWall(OnPlayerPushed evt)
        {
            // [변경]: 횟수 기반 체력 시스템에 맞춰 벽 압착 시 고정 1 데미지 적용
            int damage = 1;
            
            // Debug.Log($"[CombatSystem] OnPlayerPushed 수신! (Distance: {evt.PushDistance:F4})");
            
            if (damage > 0)
            {
                m_playerModel.TakeDamage(damage);
                m_eventBus.Publish(new OnPlayerDamaged(damage, m_playerModel.CurrentLifeCount));
                
                Debug.Log($"[CombatSystem] 플레이어 데미지 처리 완료. 잔여 생명: {m_playerModel.CurrentLifeCount}");
                
                // [신규]: 플레이어 위치에 데미지 텍스트 요청
                // 플레이어 뷰의 위치를 알기 위해 m_playerModel에 위치 정보가 있거나, 
                // 여기서는 간단히 플레이어 뷰의 위치를 가져올 수 없으므로(CombatSystem은 Logic)
                // 중앙 이벤트 버스를 통해 플레이어 위치를 알고 있는 쪽에서 처리하거나, 
                // 보통은 Position 보다는 "Player"라는 타겟 정보를 넘기기도 합니다.
                // 하지만 현재 GameEvents.cs의 OnDamageTextRequested는 Position을 요구하므로
                // 우선은 Vector3.zero 또는 추정치를 넘기고, 실제 좌표는 Presenter나 View에서 보정할 수도 있습니다.
                // 여기서는 (0, 1.5, 0) 정도로 추정하여 넘깁니다.
                m_eventBus.Publish(new OnDamageTextRequested(new UnityEngine.Vector3(0, 1.5f, 0), damage));

                // [신규] 벽 압착 이벤트 발행 → 해당 층의 적만 동결
                int currentFloor = m_towerManager != null ? m_towerManager.CurrentFloorIndex : 0;
                m_eventBus.Publish(new OnWallCrushOccurred(damage, currentFloor));
                
                if (m_playerModel.IsDead)
                {
                    m_eventBus.Publish(new OnGameOver());
                }
            }
        }
        #endregion

        #region 해제
        public void Dispose()
        {
            m_eventBus.Unsubscribe<OnPlayerPushed>(HandlePlayerPushedAtWall);
        }
        #endregion
    }
}
