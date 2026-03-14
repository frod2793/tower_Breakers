using UnityEngine;
using DG.Tweening;
using System;

namespace TowerBreakers.Enemy.View
{
    /// <summary>
    /// [설명]: 적 사망 시 산산조각 나는 개별 파편을 관리하는 클래스입니다.
    /// 수동 물리 시뮬레이션과 DOTween 페이드 아웃을 결합하여 연출합니다.
    /// </summary>
    public class DeathFragment : MonoBehaviour
    {
        #region 에디터 설정
        [SerializeField, Tooltip("파편의 스프라이트 렌더러")]
        private SpriteRenderer m_spriteRenderer;
        #endregion

        #region 내부 변수
        private Vector3 m_velocity;
        private float m_angularVelocity;
        private float m_gravity = -20f;
        private Action<DeathFragment> m_onComplete;
        private bool m_isActive = false;
        #endregion

        #region 유니티 생명주기
        private void Update()
        {
            if (!m_isActive) return;

            // 1. 수동 물리 시뮬레이션
            m_velocity.y += m_gravity * Time.deltaTime;
            transform.position += m_velocity * Time.deltaTime;
            transform.Rotate(0, 0, m_angularVelocity * Time.deltaTime);
        }
        #endregion

        #region 공개 API
        /// <summary>
        /// [설명]: 파편의 초기 상태와 물리 속성을 설정하고 연출을 시작합니다.
        /// </summary>
        public void Initialize(
            Sprite sourceSprite, 
            Vector3 position, 
            Vector3 initialVelocity, 
            float torque, 
            float fadeDelay,
            float fadeDuration,
            int sortingOrder,
            int sortingLayerID,
            Color color,
            Vector3 scale,
            Action<DeathFragment> onComplete)
        {
            if (m_spriteRenderer == null) m_spriteRenderer = GetComponent<SpriteRenderer>();

            transform.position = position;
            transform.localScale = scale;
            transform.rotation = Quaternion.identity;

            m_spriteRenderer.sprite = sourceSprite;
            m_spriteRenderer.sortingOrder = sortingOrder;
            m_spriteRenderer.sortingLayerID = sortingLayerID;
            m_spriteRenderer.color = color;

            m_velocity = initialVelocity;
            m_angularVelocity = torque;
            m_onComplete = onComplete;
            m_isActive = true;

            gameObject.SetActive(true);

            // DOTween 기반 연출: 지연 후 페이드 및 스케일 다운
            m_spriteRenderer.DOKill();
            transform.DOKill();

            Sequence seq = DOTween.Sequence();
            seq.AppendInterval(fadeDelay);
            seq.Append(m_spriteRenderer.DOFade(0f, fadeDuration).SetEase(Ease.InQuad));
            seq.Join(transform.DOScale(0f, fadeDuration).SetEase(Ease.InBack));
            seq.OnComplete(Complete);
        }
        #endregion

        #region 내부 로직
        private void Complete()
        {
            m_isActive = false;
            gameObject.SetActive(false);
            m_onComplete?.Invoke(this);
        }
        #endregion
    }
}
