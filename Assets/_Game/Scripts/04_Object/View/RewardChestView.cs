using UnityEngine;
using System;
using TowerBreakers.Core.Events;
using VContainer;
using DG.Tweening;

namespace TowerBreakers.Interactions.View
{
    /// <summary>
    /// [설명]: 보상 상자의 시각적 상태와 타격 판정을 관리하는 뷰 클래스입니다.
    /// 플레이어의 공격을 받으면 열리며 보상을 지급하는 이벤트를 발생시킵니다.
    /// </summary>
    public class RewardChestView : MonoBehaviour
    {
        #region 에디터 설정
        [SerializeField, Tooltip("상자 외형 스프라이트 렌더러")]
        private SpriteRenderer m_spriteRenderer;

        [SerializeField, Tooltip("닫힌 상자 스프라이트")]
        private Sprite m_closedSprite;

        [SerializeField, Tooltip("열린 상자 스프라이트")]
        private Sprite m_openedSprite;

        [SerializeField, Tooltip("상자 파티클 시스템 (열릴 때 재생)")]
        private ParticleSystem m_openParticle;

        [SerializeField, Tooltip("상자 체력 (몇 번 공격해야 열리는지)")]
        private int m_health = 1;
        #endregion

        #region 내부 필드
        private IEventBus m_eventBus;
        private int m_currentHealth;
        private bool m_isOpened;
        private int m_floorIndex;
        #endregion

        #region 초기화
        [Inject]
        public void Initialize(IEventBus eventBus)
        {
            m_eventBus = eventBus;
            
            if (m_eventBus != null)
            {
                m_eventBus.Subscribe<OnFloorCleared>(OnFloorCleared);
            }
        }

        private void Start()
        {
            // 선배치 시 초기 상태 설정
            Setup(m_floorIndex);
        }

        public void Setup(int floorIndex)
        {
            m_floorIndex = floorIndex;
            m_currentHealth = m_health;
            m_isOpened = false;
            
            if (m_spriteRenderer != null)
            {
                m_spriteRenderer.sprite = m_closedSprite;
                // 활성화 전까지는 숨겨진 상태 (또는 투명하게 시작)
                m_spriteRenderer.enabled = false;
            }

            // 콜라이더 초기 비활성화
            var col = GetComponent<Collider2D>();
            if (col != null) col.enabled = false;
        }
        #endregion

        #region 유니티 생명주기
        private void OnDestroy()
        {
            if (m_eventBus != null)
            {
                m_eventBus.Unsubscribe<OnFloorCleared>(OnFloorCleared);
            }
        }

        private void OnFloorCleared(OnFloorCleared evt)
        {
            // 자신의 층이 클리어되었을 때만 활성화
            if (evt.FloorIndex == m_floorIndex)
            {
                Activate();
            }
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (m_isOpened) return;

            // 플레이어의 공격(Trigger)에 닿았는지 확인
            if (collision.CompareTag("PlayerAttack"))
            {
                OnHit();
            }
        }
        #endregion

        #region 내부 로직
        /// <summary>
        /// [설명]: 상자를 활성화하여 플레이어가 타격할 수 있도록 합니다.
        /// </summary>
        private void Activate()
        {
            if (m_spriteRenderer != null) m_spriteRenderer.enabled = true;

            var col = GetComponent<Collider2D>();
            if (col != null) col.enabled = true;

            // 등장 연출
            transform.localScale = Vector3.zero;
            transform.DOScale(Vector3.one, 0.5f).SetEase(Ease.OutBack);
            
            Debug.Log($"[RewardChestView] {m_floorIndex}층 보상 상자 활성화");
        }
        /// <summary>
        /// [설명]: 상자가 타격받았을 때 호출됩니다.
        /// </summary>
        private void OnHit()
        {
            if (m_isOpened) return;

            m_currentHealth--;

            // 피격 연출 (좌우 흔들림)
            transform.DOShakePosition(0.2f, 0.1f);

            if (m_currentHealth <= 0)
            {
                Open();
            }
        }

        /// <summary>
        /// [설명]: 상자를 엽니다.
        /// </summary>
        private void Open()
        {
            m_isOpened = true;

            if (m_spriteRenderer != null)
                m_spriteRenderer.sprite = m_openedSprite;

            if (m_openParticle != null)
                m_openParticle.Play();

            // 열림 연출 (약간 커졌다가 작아짐)
            transform.DOPunchScale(Vector3.one * 0.2f, 0.3f);

            // 이벤트 발행
            m_eventBus?.Publish(new OnRewardChestOpened(transform.position, m_floorIndex));

            // 일정 시간 후 제거 또는 비활성화
            DOVirtual.DelayedCall(2f, () => {
                transform.DOScale(Vector3.zero, 0.5f).OnComplete(() => Destroy(gameObject));
            });
        }
        #endregion
    }
}
