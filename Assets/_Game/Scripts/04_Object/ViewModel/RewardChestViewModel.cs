using System;
using TowerBreakers.Core.Events;
using TowerBreakers.Player.Data.SO;
using UnityEngine;

namespace TowerBreakers.Interactions.ViewModel
{
    /// <summary>
    /// [설명]: 보상 상자의 상태와 로직을 담당하는 POCO 클래스입니다.
    /// 체력 관리 및 개방 로직을 수행하며 View에 전파합니다.
    /// </summary>
    public class RewardChestViewModel : IDisposable
    {
        #region 내부 필드
        private readonly IEventBus m_eventBus;
        private int m_currentHealth;
        private bool m_isOpened;
        private bool m_isActivated;
        private int m_floorIndex;
        private Vector3 m_position;
        private RewardTableData m_rewardTable;
        #endregion

        #region 프로퍼티
        /// <summary>
        /// [설명]: 상자가 이미 열렸는지 여부입니다.
        /// </summary>
        public bool IsOpened => m_isOpened;

        /// <summary>
        /// [설명]: 상자가 활성화되어 상호작용 가능한지 여부입니다.
        /// </summary>
        public bool IsActivated => m_isActivated;

        /// <summary>
        /// [설명]: 상자가 속한 층 번호입니다.
        /// </summary>
        public int FloorIndex => m_floorIndex;
        #endregion

        #region 이벤트 (바인딩용)
        /// <summary>
        /// [설명]: 상자가 타격받았을 때 발생합니다.
        /// </summary>
        public event Action OnHit;

        /// <summary>
        /// [설명]: 상자가 열릴 때 발생합니다.
        /// </summary>
        public event Action OnOpened;

        /// <summary>
        /// [설명]: 상자가 활성화될 때 발생합니다.
        /// </summary>
        public event Action OnActivated;
        #endregion

        #region 초기화
    public RewardChestViewModel(IEventBus eventBus)
    {
        m_eventBus = eventBus;
        
        // ViewModel이 이벤트를 구독하여 로직을 처리
        if (m_eventBus != null)
        {
            m_eventBus.Subscribe<OnFloorEnemiesCleared>(OnFloorEnemiesCleared);
        }
    }

        /// <summary>
        /// [설명]: 상자의 초기 데이터를 설정합니다.
        /// </summary>
        /// <param name="floorIndex">층 번호</param>
        /// <param name="initialHealth">초기 체력</param>
        /// <param name="position">위치</param>
        /// <param name="rewardTable">보상 테이블 (선택 사항)</param>
        public void Setup(int floorIndex, int initialHealth, Vector3 position, RewardTableData rewardTable = null)
        {
            m_floorIndex = floorIndex;
            m_currentHealth = initialHealth;
            m_position = position;
            m_rewardTable = rewardTable;
            m_isOpened = false;
            m_isActivated = false;
        }
        #endregion

        #region 공개 API (명령)
        /// <summary>
        /// [설명]: 상자를 활성화하여 타격 가능 상태로 만듭니다.
        /// </summary>
        public void Activate()
        {
            if (m_isActivated) return;
            
            m_isActivated = true;
            OnActivated?.Invoke();
        }
        
        /// <summary>
        /// [설명]: 플레이어의 타격을 처리합니다.
        /// </summary>
        public void ProcessHit(int damage)
        {
            if (!m_isActivated || m_isOpened) return;
            
            m_currentHealth -= damage;
            OnHit?.Invoke();
            
            // [추가]: 몬스터와 동일하게 데미지 텍스트 출력
            if (m_eventBus != null)
            {
                m_eventBus.Publish(new OnDamageTextRequested(m_position + Vector3.up * 1.5f, damage));
            }
            
            if (m_currentHealth <= 0)
            {
                Open();
            }
        }
        
        /// <summary>
        /// [설명]: 강제로 상자를 엽니다. (치트 또는 특정 트리거용)
        /// </summary>
        public void Open()
        {
            if (m_isOpened) return;
            
            m_isOpened = true;
            Debug.Log($"[CHEST_DIAGNOSTIC] ViewModel.Open(): Floor={m_floorIndex}, Pos={m_position}");
            OnOpened?.Invoke();
            
            // 핵심 비즈니스 로직: 보상 개방 이벤트 발행
            if (m_eventBus != null)
            {
                Debug.Log($"[CHEST_DIAGNOSTIC] 발행: OnRewardChestOpened(Floor={m_floorIndex}, Pos={m_position}, Table={m_rewardTable?.name ?? "NULL"})");
                m_eventBus.Publish(new OnRewardChestOpened(m_position, m_floorIndex, m_rewardTable));
            }
            else
            {
                Debug.LogError("[CHEST_DIAGNOSTIC] EventBus가 null입니다! OnRewardChestOpened 이벤트를 발행할 수 없습니다.");
            }
        }
        #endregion
        
        #region 이벤트 핸들러
        /// <summary>
        /// [설명]: 층의 적들이 모두 제거되었을 때 처리합니다.
        /// </summary>
        /// <param name="evt">층 클리어 이벤트 데이터</param>
        private void OnFloorEnemiesCleared(OnFloorEnemiesCleared evt)
        {
            if (evt.FloorIndex == m_floorIndex && !m_isActivated)
            {
                Activate();
            }
        }
        #endregion

        #region IDisposable 구현
        /// <summary>
        /// [설명]: 리소스 해제 및 이벤트 구독 해제
        /// </summary>
        public void Dispose()
        {
            if (m_eventBus != null)
            {
                m_eventBus.Unsubscribe<OnFloorEnemiesCleared>(OnFloorEnemiesCleared);
            }
        }
        #endregion
    }
}
