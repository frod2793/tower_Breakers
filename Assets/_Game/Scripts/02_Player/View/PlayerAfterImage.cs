using UnityEngine;
using System.Collections.Generic;
using TowerBreakers.Player.Logic;

namespace TowerBreakers.Player.View
{
    /// <summary>
    /// [설명]: 플레이어의 잔상(Afterimage) 효과를 생성하고 관리하는 클래스입니다.
    /// 오브젝트 풀링을 사용하여 매 프레임 발생하는 할당을 최소화합니다.
    /// </summary>
    public class PlayerAfterImage : MonoBehaviour
    {
        #region 에디터 설정
        [Header("잔상 설정")]
        [SerializeField, Tooltip("잔상 간격 (0.1이면 0.1초마다 생성)")]
        private float m_ghostInterval = 0.05f;

        [SerializeField, Tooltip("잔상 유지 시간")]
        private float m_ghostLifetime = 0.3f;

        [SerializeField, Tooltip("잔상 기본 색상")]
        private Color m_ghostColor = new Color(1f, 1f, 1f, 0.5f);
        #endregion

        #region 내부 필드
        private PlayerView m_view;
        private List<SpriteRenderer> m_sourceRenderers = new List<SpriteRenderer>();
        
        private readonly Queue<AfterImageGhost> m_pool = new Queue<AfterImageGhost>();
        private bool m_isActive = false;
        private float m_timer = 0f;
        #endregion

        #region 초기화
        /// <summary>
        /// [설명]: PlayerView를 통해 복제할 스프라이트 렌더러들을 캐싱합니다.
        /// </summary>
        public void Initialize(PlayerView view)
        {
            m_view = view;
            if (m_view != null)
            {
                // SPUM 캐릭터 내부의 모든 SpriteRenderer를 찾아 캐싱
                m_view.GetComponentsInChildren<SpriteRenderer>(true, m_sourceRenderers);
            }
        }
        #endregion

        #region 공개 API
        /// <summary>
        /// [설명]: 잔상 생성을 시작합니다.
        /// </summary>
        public void StartEffect()
        {
            m_isActive = true;
            m_timer = 0f; // 즉시 첫 잔상 생성
        }

        /// <summary>
        /// [설명]: 잔상 생성을 중단합니다.
        /// </summary>
        public void StopEffect()
        {
            m_isActive = false;
        }
        #endregion

        #region 유니티 생명주기
        private void Update()
        {
            if (!m_isActive) return;

            m_timer += Time.deltaTime;
            if (m_timer >= m_ghostInterval)
            {
                m_timer = 0f;
                SpawnGhost();
            }
        }
        #endregion

        #region 내부 로직
        private void SpawnGhost()
        {
            if (m_view == null) return;

            AfterImageGhost ghost;
            if (m_pool.Count > 0)
            {
                ghost = m_pool.Dequeue();
                ghost.gameObject.SetActive(true);
            }
            else
            {
                GameObject obj = new GameObject("GhostObject");
                ghost = obj.AddComponent<AfterImageGhost>();
            }

            // 위치와 회전값 동기화
            ghost.transform.position = m_view.transform.position;
            ghost.transform.rotation = m_view.transform.rotation;
            ghost.transform.localScale = m_view.transform.localScale;

            // 초기화 및 실행
            ghost.Init(m_sourceRenderers, m_ghostColor, m_ghostLifetime, ReturnToPool);
        }

        private void ReturnToPool(AfterImageGhost ghost)
        {
            ghost.gameObject.SetActive(false);
            m_pool.Enqueue(ghost);
        }
        #endregion
    }
}
