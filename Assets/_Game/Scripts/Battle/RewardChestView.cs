using UnityEngine;
using DG.Tweening;
using System;
using TowerBreakers.Enemy.Service;
using TowerBreakers.Tower.Data;
using TowerBreakers.Tower.Service;

namespace TowerBreakers.Battle
{
    /// <summary>
    /// [기능]: 체력을 가지고 있으며 개봉 시 아이템 연출이 발생하는 보상 상자 클래스입니다.
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public class RewardChestView : MonoBehaviour, IEnemyController
    {
        #region 에디터 설정
        [Header("시각적 설정")]
        [SerializeField] private SpriteRenderer m_spriteRenderer;
        [SerializeField] private Sprite m_closedSprite;
        [SerializeField] private Sprite m_openedSprite;
        
        [Header("체력 설정")]
        [SerializeField] private float m_maxHealth = 3.0f;
        
        [Header("애니메이션 설정")]
        [SerializeField] private float m_shakeStrength = 0.2f;
        [SerializeField] private float m_openScaleTime = 0.3f;
        #endregion

        #region 내부 필드
        private float m_currentHealth;
        private bool m_isOpened = false;
        private Action m_onOpenedCallback;
        private Sprite m_rewardSprite;
        #endregion

        #region 초기화
        /// <summary>
        /// [설명]: 상자를 초기화하고 보상 아이콘 및 개봉 콜백을 설정합니다.
        /// </summary>
        public void Initialize(Sprite rewardSprite, Action onOpened)
        {
            m_rewardSprite = rewardSprite;
            m_onOpenedCallback = onOpened;
            
            m_currentHealth = m_maxHealth;
            m_isOpened = false;
            
            gameObject.tag = "Enemy"; 
            if (m_spriteRenderer != null) 
            {
                m_spriteRenderer.sprite = m_closedSprite;
                m_spriteRenderer.sortingOrder = 15; // 배경 및 다른 적들보다 확실히 위로 노출
            }
            transform.localScale = Vector3.one;

            // [기반 수정]: 스폰 시 위치와 상태를 로그로 남겨 가시성 문제 추적 보강
            Debug.Log($"[RewardChest] 상자 초기화 완료 - 위치: {transform.position}, Sprite: {m_spriteRenderer?.sprite?.name}");
        }

        // IEnemyController 인터페이스 구현
        public void Initialize(EnemyData data) { }
        public void SyncAnimation() { }
        public void PlayAnimation(PlayerState state) { }
        public event Action<GameObject> OnDeath
        {
            add { }
            remove { }
        }
        #endregion

        #region 공개 API
        /// <summary>
        /// [설명]: 플레이어의 공격을 받았을 때 호출됩니다. 체력이 다하면 상자가 열립니다.
        /// </summary>
        public void TakeDamage(float damage)
        {
            if (m_isOpened) return;

            m_currentHealth -= damage;
            
            // 피격 흔들림 연출
            transform.DOComplete();
            transform.DOShakePosition(0.1f, m_shakeStrength);
            
            if (m_currentHealth <= 0)
            {
                OpenChest();
            }
        }
        #endregion

        #region 내부 로직
        private void OpenChest()
        {
            m_isOpened = true;

            // 1. 개봉 애니메이션 (커졌다가 작아짐)
            transform.DOScale(1.2f, m_openScaleTime / 2).SetLoops(2, LoopType.Yoyo).OnComplete(() =>
            {
                // 2. 스프라이트 교체 (열린 상태)
                if (m_spriteRenderer != null) m_spriteRenderer.sprite = m_openedSprite;
                
                // 3. 아이템 팝업 연출
                SpawnRewardVisual();
                
                // 4. 지연 후 콜백 호출 (보상 획득 처리)
                DOVirtual.DelayedCall(0.5f, () => m_onOpenedCallback?.Invoke());
            });

            Debug.Log("[RewardChest] 보상 상자 파괴 및 개봉!");
        }

        private void SpawnRewardVisual()
        {
            if (m_rewardSprite == null) return;

            // 1. 아이템 연출용 오브젝트 생성
            GameObject rewardObj = new GameObject("Reward_Visual_Popup");
            rewardObj.transform.position = transform.position + Vector3.up * 0.5f;
            rewardObj.transform.localScale = Vector3.zero; // 작게 시작
            
            var sr = rewardObj.AddComponent<SpriteRenderer>();
            sr.sprite = m_rewardSprite;
            sr.sortingOrder = 20; // 상자보다 앞에 표시

            // 2. 팝업 애니메이션 (포물선 점프 + 바운스)
            Sequence popupSeq = DOTween.Sequence();
            
            // 점프 및 크기 확대
            popupSeq.Append(rewardObj.transform.DOMoveY(transform.position.y + 2.0f, 0.6f).SetEase(Ease.OutQuad));
            popupSeq.Join(rewardObj.transform.DOScale(1.5f, 0.6f).SetEase(Ease.OutBack));
            
            // 공중 부유 (둥둥 떠있기)
            popupSeq.Append(rewardObj.transform.DOMoveY(transform.position.y + 2.2f, 1.0f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine));

            // 3. 지연 후 페이드 아웃 및 소멸
            DOVirtual.DelayedCall(2.0f, () =>
            {
                sr.DOFade(0f, 0.5f).OnComplete(() =>
                {
                    popupSeq.Kill();
                    Destroy(rewardObj);
                });
            });
        }
        #endregion
    }
}
