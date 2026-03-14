using UnityEngine;
using DG.Tweening;
using System.Collections.Generic;

namespace TowerBreakers.Player.View
{
    /// <summary>
    /// [설명]: 플레이어의 잔상(Afterimage) 효과를 표현하는 개별 오브젝트 클래스입니다.
    /// 생성 시점의 플레이어 스프라이트 상태를 복사하여 페이드 아웃 연출을 수행합니다.
    /// </summary>
    public class AfterImageGhost : MonoBehaviour
    {
        #region 내부 필드
        private List<SpriteRenderer> m_ghostRenderers = new List<SpriteRenderer>();
        private System.Action<AfterImageGhost> m_onComplete;
        private Tweener m_fadeTweener;
        #endregion

        #region 초기화 및 실행
        /// <summary>
        /// [설명]: 원본 캐릭터의 스프라이트 상태를 잔상으로 복제하고 페이드 연출을 시작합니다.
        /// </summary>
        /// <param name="sourceRenderers">복제할 원본 SpriteRenderer 리스트</param>
        /// <param name="color">잔상의 기본 색상</param>
        /// <param name="duration">페이드 아웃 지속 시간</param>
        /// <param name="onComplete">연출 종료 후 호출될 콜백 (풀 반환용)</param>
        public void Init(IEnumerable<SpriteRenderer> sourceRenderers, Color color, float duration, System.Action<AfterImageGhost> onComplete)
        {
            m_onComplete = onComplete;
            
            // 1. 렌더러 개수 동기화 및 스프라이트 복제
            int idx = 0;
            foreach (var source in sourceRenderers)
            {
                if (source == null || !source.gameObject.activeInHierarchy) continue;

                SpriteRenderer ghost;
                if (idx < m_ghostRenderers.Count)
                {
                    ghost = m_ghostRenderers[idx];
                }
                else
                {
                    // 부족한 경우 새로 생성
                    GameObject obj = new GameObject($"GhostRenderer_{idx}");
                    obj.transform.SetParent(transform);
                    ghost = obj.AddComponent<SpriteRenderer>();
                    m_ghostRenderers.Add(ghost);
                }

                // 원본 상태 복제
                ghost.gameObject.SetActive(true);
                ghost.sprite = source.sprite;
                ghost.flipX = source.flipX;
                ghost.flipY = source.flipY;
                ghost.sortingLayerID = source.sortingLayerID;
                ghost.sortingOrder = source.sortingOrder - 1; // 원본보다 항상 뒤에 표시
                ghost.transform.localPosition = source.transform.localPosition;
                ghost.transform.localScale = source.transform.localScale;
                ghost.transform.localRotation = source.transform.localRotation;
                
                // 잔상 고유 색상 및 투명도 초기화
                ghost.color = color;
                
                idx++;
            }

            // 사용하지 않는 남은 렌더러 비활성화
            for (int i = idx; i < m_ghostRenderers.Count; i++)
            {
                m_ghostRenderers[i].gameObject.SetActive(false);
            }

            // 2. 페이드 아웃 연출 (가장 첫 번째 렌더러를 기준으로 대표 트위닝)
            if (m_ghostRenderers.Count > 0)
            {
                m_fadeTweener?.Kill();
                
                // 모든 렌더러의 투명도를 동시에 낮춤
                m_fadeTweener = DOTween.To(() => color.a, x => 
                {
                    for (int i = 0; i < idx; i++)
                    {
                        Color c = m_ghostRenderers[i].color;
                        c.a = x;
                        m_ghostRenderers[i].color = c;
                    }
                }, 0f, duration).SetEase(Ease.OutQuad).OnComplete(() => m_onComplete?.Invoke(this));
            }
            else
            {
                m_onComplete?.Invoke(this);
            }
        }
        #endregion

        private void OnDestroy()
        {
            m_fadeTweener?.Kill();
        }
    }
}
