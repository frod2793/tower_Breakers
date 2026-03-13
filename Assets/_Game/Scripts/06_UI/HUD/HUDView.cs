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
        [Header("생명 시스템")]
        [SerializeField, Tooltip("하트 아이콘들을 담을 컨테이너 (Horizontal Layout Group 권장)")]
        private Transform m_heartContainer;

        [SerializeField, Tooltip("하트 아이콘 프리팹 (Image 컴포넌트 포함)")]
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
        #endregion

        #region 내부 필드
        private HUDViewModel m_viewModel;
        private Vector3 m_originalHeartScale = Vector3.one;
        #endregion

        #region 초기화 및 바인딩
        public void Initialize(HUDViewModel viewModel)
        {
            m_viewModel = viewModel;
            m_viewModel.OnDataUpdated += UpdateUI;

            // 프리펩의 원본 스케일 캐싱
            if (m_heartPrefab != null)
            {
                m_originalHeartScale = m_heartPrefab.transform.localScale;
            }

            UpdateUI();
        }

        private void UpdateUI()
        {
            if (m_viewModel == null) return;

            // Debug.Log($"[HUDView] UI 데이터 갱신 요청 수신 (Life: {m_viewModel.CurrentLifeCount})");

            // 1. 하트 생명 표시 업데이트
            UpdateHearts();

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
        /// [설명]: 모델의 생명 수에 맞춰 하트 아이콘을 동적으로 생성/갱신합니다.
        /// </summary>
        private void UpdateHearts()
        {
            if (m_heartContainer == null || m_heartPrefab == null) return;

            // 현재 자식 수와 필요한 하트 수 비교
            int currentChildCount = m_heartContainer.childCount;
            int targetLife = m_viewModel.CurrentLifeCount;

            // 부족하면 생성
            if (currentChildCount < targetLife)
            {
                for (int i = 0; i < targetLife - currentChildCount; i++)
                {
                    Instantiate(m_heartPrefab, m_heartContainer);
                }
            }
            
            // 모든 하트 순회하며 활성화 여부 결정 (또는 단순 개수 맞춤)
            for (int i = 0; i < m_heartContainer.childCount; i++)
            {
                var heartChild = m_heartContainer.GetChild(i).gameObject;
                bool shouldBeActive = i < targetLife;
                
                // [개선]: 활성화되어야 한다면 현재 상태에 관계없이 무조건 강제 적용 및 트윈 중단 (원본 스케일 유지)
                if (shouldBeActive)
                {
                    heartChild.transform.DOKill();
                    heartChild.SetActive(true);
                    heartChild.transform.localScale = m_originalHeartScale; 
                }
                else if (heartChild.activeSelf) // 비활성화되어야 하는데 현재 켜져 있다면 연출 시작
                {
                    heartChild.transform.DOKill();
                    
                    // 파괴 연출: 원본 대비 1.3배 커졌다가 빠르게 사라짐
                    heartChild.transform.DOScale(m_originalHeartScale * 1.3f, 0.1f).SetEase(Ease.OutQuad)
                        .OnComplete(() => {
                            heartChild.transform.DOScale(Vector3.zero, 0.15f).SetEase(Ease.InQuad)
                                .OnComplete(() => {
                                    // 연출 완료 후에도 여전히 비활성 상태여야 할 때만 SetActive(false)
                                    heartChild.SetActive(false);
                                });
                        });
                }
            }
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
