using UnityEngine;
using System.Collections.Generic;
using DG.Tweening;
using TowerBreakers.SPUM;

namespace TowerBreakers.Tower.Service
{
    /// <summary>
    /// [설명]: 적 캐릭터의 시각적 효과(피격 플래시, 사망 시 파츠 분출)를 담당하는 컨트롤러입니다.
    /// </summary>
    public class EnemyVFXController : MonoBehaviour
    {
        #region 에디터 설정
        [Header("피격 설정")]
        [SerializeField, Tooltip("피격 시 변할 컬러")]
        private Color m_flashColor = Color.red;

        [SerializeField, Tooltip("컬러 유지 시간")]
        private float m_flashDuration = 0.1f;

        [Header("사망 설정")]
        [SerializeField, Tooltip("파츠 폭발 힘")]
        private float m_explodeForce = 5f;

        [SerializeField, Tooltip("파츠 회전 힘")]
        private float m_rotationForce = 360f;

        [SerializeField, Tooltip("페이드 아웃 시간")]
        private float m_fadeOutDuration = 1.0f;
        #endregion

        #region 내부 변수
        private List<SpriteRenderer> m_renderers = new List<SpriteRenderer>();
        private Animator m_animator;
        private bool m_isDead = false;
        #endregion

        #region 유니티 생명주기
        private void Awake()
        {
            // 하위의 모든 SpriteRenderer 캐싱
            m_renderers.AddRange(GetComponentsInChildren<SpriteRenderer>(true));
            m_animator = GetComponentInChildren<Animator>();
        }
        #endregion

        #region 공개 API
        /// <summary>
        /// [설명]: 피격 시 캐릭터를 특정 색상으로 깜빡이게 합니다.
        /// </summary>
        public void FlashColor()
        {
            if (m_isDead) return;

            foreach (var renderer in m_renderers)
            {
                if (renderer == null) continue;

                // 기존 트윈이 있다면 중단
                renderer.DOKill();
                
                // 컬러 플래시 시퀀스
                renderer.DOColor(m_flashColor, m_flashDuration)
                    .SetLoops(2, LoopType.Yoyo)
                    .OnComplete(() => {
                        if (renderer != null) renderer.color = Color.white;
                    });
            }
        }

        /// <summary>
        /// [설명]: 사망 시 캐릭터의 파츠(스프라이트)들을 분출시키며 연출합니다. 
        /// 파츠들이 위로 튀어 올랐다가 아래로 떨어지는 포물선 궤적(Jump)을 그리며 사라집니다.
        /// </summary>
        public void ExplodeParts()
        {
            if (m_isDead) return;
            m_isDead = true;

            // 애니메이터 정지
            if (m_animator != null)
            {
                m_animator.enabled = false;
            }

            // 각 렌더러(파츠)들을 개별 객체로 독립시켜 날려보냄
            foreach (var renderer in m_renderers)
            {
                if (renderer == null) continue;

                GameObject partObj = renderer.gameObject;
                
                // 계층 구조 분리 (월드 공간에서 자유롭게 움직이도록 함)
                partObj.transform.SetParent(null);

                // 랜덤한 좌우 방향 및 낙하지점 계산
                float randomX = Random.Range(-m_explodeForce, m_explodeForce);
                float randomY = Random.Range(-m_explodeForce * 0.5f, -m_explodeForce); // 최종적으로 아래로 떨어지도록 설정
                Vector3 targetPos = partObj.transform.position + new Vector3(randomX, randomY, 0);

                // 점프 높이와 회전값 설정
                float jumpPower = Random.Range(m_explodeForce * 0.8f, m_explodeForce * 1.5f);
                float rotation = Random.Range(-m_rotationForce, m_rotationForce);

                // DOTween 연출: 포물선 이동(DOJump) + 회전 + 페이드아웃
                // 1. 위로 튀었다 아래로 떨어지는 궤적
                partObj.transform.DOJump(targetPos, jumpPower, 1, m_fadeOutDuration)
                    .SetEase(Ease.Linear); // 점프 자체의 곡선미를 위해 Linear 사용 (DOJump 내부에서 높이 연산 포함)
                
                // 2. 랜덤 회전
                partObj.transform.DORotate(new Vector3(0, 0, rotation), m_fadeOutDuration, RotateMode.FastBeyond360)
                    .SetEase(Ease.OutQuad);

                // 3. 페이드아웃 및 제거
                renderer.DOFade(0, m_fadeOutDuration)
                    .SetEase(Ease.InQuart)
                    .OnComplete(() => {
                        if (partObj != null) Destroy(partObj);
                    });
            }
        }
        #endregion
    }
}
