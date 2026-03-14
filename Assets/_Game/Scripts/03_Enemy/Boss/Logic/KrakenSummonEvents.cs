using UnityEngine;
using TowerBreakers.Core.Events;
using VContainer;

namespace TowerBreakers.Enemy.Boss
{
    /// <summary>
    /// [설명]: 소환된 크라켄의 촉수 생명주기를 관리하고 이벤트를 발행하는 컴포넌트입니다.
    /// 소환물이 파괴될 때 보스에게 알림을 보냅니다.
    /// </summary>
    public class KrakenSummonEvents : MonoBehaviour
    {
        #region 내부 필드
        private IEventBus m_eventBus;
        private int m_floorIndex;
        private OnKrakenSummonRequested.SummonType m_summonType;
        private bool m_isInitialized = false;
        #endregion

        #region 초기화
        [Inject]
        public void Construct(IEventBus eventBus)
        {
            m_eventBus = eventBus;
        }

        /// <summary>
        /// [설명]: 소환물의 타입과 층 정보를 설정합니다.
        /// </summary>
        public void Initialize(OnKrakenSummonRequested.SummonType type, int floorIndex, IEventBus eventBus = null)
        {
            m_summonType = type;
            m_floorIndex = floorIndex;
            
            if (eventBus != null)
            {
                m_eventBus = eventBus;
            }
            
            m_isInitialized = true;
        }
        #endregion

        #region 유니티 생명주기
        private void OnDestroy()
        {
            if (m_isInitialized && m_eventBus != null)
            {
                if (m_summonType == OnKrakenSummonRequested.SummonType.Tentacle)
                {
                    m_eventBus.Publish(new OnKrakenTentacleDestroyed(m_floorIndex));
                }
                
                Debug.Log($"[KrakenSummonEvents] 소환물 파괴됨: 타입={m_summonType}, 층={m_floorIndex}");
            }
        }
        #endregion
    }
}
