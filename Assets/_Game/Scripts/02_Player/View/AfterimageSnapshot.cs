using UnityEngine;
using DG.Tweening;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace TowerBreakers.Player.View
{
    /// <summary>
    /// [설명]: 플레이어의 잔상(Afterimage) 효과를 표현하는 스냅샷 오브젝트 클래스입니다.
    /// 생성 시점의 플레이어 스프라이트 상태를 복사하여 비동기로 페이드 아웃 연출을 수행합니다.
    /// </summary>
    public class AfterimageSnapshot : MonoBehaviour
    {
        #region 내부 필드
        private readonly List<SpriteRenderer> m_ghostRenderers = new List<SpriteRenderer>();
        private System.Action<AfterimageSnapshot> m_onComplete;
        #endregion

        #region 공개 메서드
        /// <summary>
        /// [설명]: 원본 캐릭터의 스프라이트 상태를 잔상으로 복제하고 비동기 페이드 연출을 시작합니다.
        /// </summary>
        /// <param name="sourceRenderers">복제할 원본 SpriteRenderer 리스트</param>
        /// <param name="color">잔상의 기본 색상</param>
        /// <param name="duration">페이드 아웃 지속 시간</param>
        /// <param name="overrideSortingOrder">소팅 오더 강제 지정 여부</param>
        /// <param name="sortingOrderValue">강제 지정할 소팅 오더 값</param>
        /// <param name="onComplete">연출 종료 후 호출될 콜백 (풀 반환용)</param>
        /// <param name="token">작업 취소 토큰</param>
        public async UniTaskVoid ActivateAsync(
            IEnumerable<SpriteRenderer> sourceRenderers, 
            Color color, 
            float duration, 
            bool overrideSortingOrder, 
            int sortingOrderValue,
            System.Action<AfterimageSnapshot> onComplete,
            CancellationToken token)
        {
            m_onComplete = onComplete;
            
            // 1. 렌더러 상태 복제
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
                    GameObject obj = new GameObject($"GhostSnapshot_{idx}");
                    obj.transform.SetParent(transform);
                    ghost = obj.AddComponent<SpriteRenderer>();
                    m_ghostRenderers.Add(ghost);
                }

                // 월드 상태 및 스프라이트 복제
                ghost.gameObject.SetActive(true);
                ghost.sprite = source.sprite;
                ghost.flipX = source.flipX;
                ghost.flipY = source.flipY;
                ghost.sortingLayerID = source.sortingLayerID;
                
                // 소팅 오더 결정
                if (overrideSortingOrder)
                {
                    ghost.sortingOrder = sortingOrderValue + idx;
                }
                else
                {
                    ghost.sortingOrder = source.sortingOrder - 1;
                }

                // [버그 수정]: 이중 반전(Double-flip) 방지를 위해 lossyScale을 정규화합니다.
                // 잔상은 SpriteRenderer.flipX를 사용하므로, Scale은 항상 양수여야 합니다.
                ghost.transform.SetPositionAndRotation(source.transform.position, source.transform.rotation);
                Vector3 sourceScale = source.transform.lossyScale;
                ghost.transform.localScale = new Vector3(Mathf.Abs(sourceScale.x), sourceScale.y, sourceScale.z);

                // [최적화]: 레이어 겹침 방지를 위해 미세한 Z 오프셋 적용
                Vector3 pos = ghost.transform.position;
                pos.z += 0.01f; // 플레이어보다 약간 뒤에 배치하여 Z-Buffer 경합 방지
                ghost.transform.position = pos;
                
                ghost.color = color;
                idx++;
            }

            // 사용하지 않는 렌더러 비활성화
            for (int i = idx; i < m_ghostRenderers.Count; i++)
            {
                m_ghostRenderers[i].gameObject.SetActive(false);
            }

            if (idx == 0)
            {
                m_onComplete?.Invoke(this);
                return;
            }

            // 2. 비동기 페이드 아웃 연출
            try
            {
                await DOTween.To(() => color.a, x => 
                {
                    for (int i = 0; i < idx; i++)
                    {
                        if (m_ghostRenderers[i] == null) continue;
                        Color c = m_ghostRenderers[i].color;
                        c.a = x;
                        m_ghostRenderers[i].color = c;
                    }
                }, 0f, duration).SetEase(Ease.OutQuad).ToUniTask(cancellationToken: token);
            }
            catch (System.OperationCanceledException) { /* 취소 시 정적으로 종료 */ }
            finally
            {
                m_onComplete?.Invoke(this);
            }
        }
        #endregion
    }
}
