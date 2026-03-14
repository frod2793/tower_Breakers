using UnityEngine;
using TowerBreakers.Player.Logic;
using System;

namespace TowerBreakers.Player.View
{
    /// <summary>
    /// [설명]: SPUM 캐릭터 프리팹과 플레이어 로직을 연결하는 뷰 클래스입니다.
    /// MVVM의 View 역할을 수행합니다.
    /// </summary>
    public class PlayerView : MonoBehaviour
    {
        #region 에디터 설정
        [SerializeField, Tooltip("SPUM 프리팹 참조")]
        private SPUM_Prefabs m_spumPrefabs;

        [SerializeField, Tooltip("잔상 효과 관리 컴포넌트")]
        private PlayerAfterImage m_afterImage;
        #endregion

        #region 내부 변수
        private PlayerStateMachine m_stateMachine;
        #endregion

        #region 프로퍼티
        public SPUM_Prefabs SpumPrefabs => m_spumPrefabs;
        #endregion

        #region 초기화
        /// <summary>
        /// [설명]: 의존성을 주입받아 초기화합니다.
        /// </summary>
        public void Initialize(PlayerStateMachine stateMachine)
        {
            m_stateMachine = stateMachine;
            
            if (m_spumPrefabs != null)
            {
                m_spumPrefabs.OverrideControllerInit();
            }
            else
            {
                Debug.LogError("[PlayerView] SPUM_Prefabs가 설정되지 않았습니다.");
            }

            // [추가]: 잔상 컴포넌트 초기화
            if (m_afterImage != null)
            {
                m_afterImage.Initialize(this);
            }
        }
        #endregion

        #region 공개 API
        /// <summary>
        /// [설명]: SPUM 애니메이션을 플레이합니다.
        /// </summary>
        public void PlayAnimation(global::PlayerState state, int index = 0)
        {
            if (m_spumPrefabs != null)
            {
                m_spumPrefabs.PlayAnimation(state, index);
            }
        }

        /// <summary>
        /// [설명]: 잔상 효과를 시작하거나 중지합니다.
        /// </summary>
        public void SetAfterImage(bool active)
        {
            if (m_afterImage == null)
            {
                m_afterImage = GetComponent<PlayerAfterImage>();
            }

            if (m_afterImage != null)
            {
                if (active) m_afterImage.StartEffect();
                else m_afterImage.StopEffect();
            }
        }
        #endregion

        #region 유니티 생명주기
        private void Awake()
        {
            // [추가]: 인스펙터에서 할당되지 않았을 경우 자식에서 자동으로 찾음
            if (m_spumPrefabs == null)
            {
                m_spumPrefabs = GetComponentInChildren<SPUM_Prefabs>();
                if (m_spumPrefabs != null)
                {
                    Debug.Log("[PlayerView] SPUM_Prefabs를 자식 오브젝트에서 자동 할당했습니다.");
                }
            }

            if (m_afterImage == null)
            {
                m_afterImage = GetComponent<PlayerAfterImage>();
            }
        }

        private void Update()
        {
            // 로직 업데이트는 GameController에서 수행되지만, 
            // View와 관련된 애니메이션 동기화 등이 필요할 수 있음
        }
        #endregion
    }
}
