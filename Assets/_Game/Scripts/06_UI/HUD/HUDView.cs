using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using VContainer;
using DG.Tweening;

namespace TowerBreakers.UI.HUD
{
    /// <summary>
    /// [설명]: 실제 HUD UI 요소를 관리하고 뷰모델의 데이터를 시각화하는 뷰 클래스입니다.
    /// </summary>
    public class HUDView : MonoBehaviour
    {
        #region 에디터 설정
        [Header("체력 현황 (하트)")]
        [SerializeField, Tooltip("하트 아이콘 그룹 컴포넌트")]
        private TowerBreakers.UI.Common.StatusIconGroup m_heartIconGroup;

        [SerializeField, Tooltip("하트 아이콘 프리팹 (초기화용)")]
        private GameObject m_heartPrefab;

        [Header("통계 및 진행")]
        [SerializeField, Tooltip("현재 층 텍스트")]
        private Text m_floorText;

        [SerializeField, Tooltip("적 처치 수 텍스트")]
        private Text m_killText;

        [SerializeField, Tooltip("보물상자 수 텍스트")]
        private Text m_chestText;

        [SerializeField, Tooltip("'GO' 메시지 루트 오브젝트")]
        private GameObject m_goRoot;

        [Header("적 현황 (아이콘)")]
        [SerializeField, Tooltip("적 처치 현황 아이콘 뷰")]
        private EnemyStatusView m_enemyStatusView;
        #endregion

        #region 내부 필드
        private HUDViewModel m_viewModel;
        private int m_lastFloor = -1;
        private int m_lastLifeCount = -1;
        #endregion

        #region 초기화 및 바인딩
        public void Initialize(HUDViewModel viewModel)
        {
            m_viewModel = viewModel;
            m_viewModel.OnDataUpdated += UpdateUI;

            // 초기 체력 설정
            m_lastLifeCount = m_viewModel.CurrentLifeCount;
            if (m_heartIconGroup != null)
            {
                m_heartIconGroup.SetIcons(m_heartPrefab, m_lastLifeCount).Forget();
            }

            UpdateUI();
        }

        private void UpdateUI()
        {
            if (m_viewModel == null) return;

            // 1. 하트 생명 표시 업데이트
            UpdateHearts();

            // 1-2. 적 처치 현황 아이콘 업데이트
            if (m_enemyStatusView != null)
            {
                // 층이 바뀌었으면(지면 하강 연출 시점) 전체 초기화
                if (m_lastFloor != m_viewModel.CurrentFloor)
                {
                    m_lastFloor = m_viewModel.CurrentFloor;
                    m_enemyStatusView.InitializeFloor(m_viewModel.CurrentFloorEnemies);
                }
                else
                {
                    // 단순 마릿수/타입 변경(처치 등) 시 부분 업데이트
                    m_enemyStatusView.UpdateStatus(m_viewModel.CurrentFloorEnemies);
                }
            }

            // 2. 통계 텍스트 업데이트
            if (m_floorText != null)
                m_floorText.text = $"Floor {m_viewModel.CurrentFloor}";

            if (m_killText != null)
                m_killText.text = $"Kills: {m_viewModel.KillCount}";

            if (m_chestText != null)
                m_chestText.text = $"Chests: {m_viewModel.ChestCount}";

            if (m_goRoot != null)
            {
                bool isVisible = m_viewModel.IsGoVisible;
                
                // 상태가 바뀔 때만 연출 제어
                if (m_goRoot.activeSelf != isVisible)
                {
                    m_goRoot.SetActive(isVisible);
                    m_goRoot.transform.DOKill();

                    if (isVisible)
                    {
                        // 0.5초 간격으로 커졌다 작아지는 루핑 연출 (점멸 느낌)
                        m_goRoot.transform.localScale = Vector3.one;
                        m_goRoot.transform.DOScale(1.1f, 0.5f)
                            .SetEase(Ease.InOutSine)
                            .SetLoops(-1, LoopType.Yoyo);
                    }
                }
            }
        }

        /// <summary>
        /// [설명]: 모델의 생명 수에 맞춰 하트 아이콘을 공통 그룹 컴포넌트로 갱신합니다.
        /// </summary>
        private void UpdateHearts()
        {
            if (m_heartIconGroup == null) return;

            int currentLife = m_viewModel.CurrentLifeCount;
            
            // 감소 시에만 연출
            if (currentLife < m_lastLifeCount)
            {
                for (int i = 0; i < m_lastLifeCount - currentLife; i++)
                {
                    m_heartIconGroup.RemoveLast();
                }
            }
            else if (currentLife > m_lastLifeCount)
            {
                // 회복 또는 초기화 시
                m_heartIconGroup.SetIcons(m_heartPrefab, currentLife).Forget();
            }

            m_lastLifeCount = currentLife;
        }
        #endregion

        #region 유니티 생명주기
        private void OnDestroy()
        {
            if (m_viewModel != null)
                m_viewModel.OnDataUpdated -= UpdateUI;
        }
        #endregion
    }
}
