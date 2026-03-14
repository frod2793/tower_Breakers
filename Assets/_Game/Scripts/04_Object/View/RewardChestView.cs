using UnityEngine;
using System;
using TowerBreakers.Core.Events;
using VContainer;
using DG.Tweening;
using TowerBreakers.Interactions.ViewModel;
using TowerBreakers.Player.Data.SO;
using TowerBreakers.Core.Interfaces;
using TowerBreakers.Effects;

namespace TowerBreakers.Interactions.View
{
    /// <summary>
    /// [설명]: 보상 상자의 시각적 상태와 연출을 관리하는 뷰 클래스입니다.
    /// MVVM 패턴에 따라 데이터와 로직은 ViewModel이 담당하고, View는 시각화에 집중합니다.
    /// </summary>
    public class RewardChestView : MonoBehaviour, IDamageable
    {
        #region 에디터 설정

        [Header("컴포넌트 참조")] [SerializeField, Tooltip("상자 외형 스프라이트 렌더러")]
        private SpriteRenderer m_spriteRenderer;

        [SerializeField, Tooltip("아이템 팝업용 스프라이트 렌더러 (상자 내부 자식)")]
        private SpriteRenderer m_itemIconRenderer;


        [Header("스프라이트 설정")] [SerializeField, Tooltip("닫힌 상자 스프라이트")]
        private Sprite m_closedSprite;

        [SerializeField, Tooltip("열린 상자 스프라이트")]
        private Sprite m_openedSprite;
        [Header("데이터 참조 (아이콘 매핑용)")]
        [Tooltip("보상 아이콘 조회를 위한 테이블 데이터")]
        [SerializeField]
        private RewardTableData m_rewardTable;

        /// <summary>
        /// [설명]: 보상 테이블을 외부에서 설정합니다. (EnvironmentManager에서 사용)
        /// </summary>
        /// <param name="table">설정할 보상 테이블</param>
        public void SetRewardTable(RewardTableData table)
        {
            m_rewardTable = table;
        }

        [Header("애니메이션 설정")] [SerializeField, Tooltip("상자 체력 (ViewModel 초기화용)")]
        private int m_health = 1;



        [Header("개방 애니메이션 설정 (Y축 이동)")] [SerializeField, Tooltip("개방 시작 시 압축 깊이 (Squash)")]
        private float m_squashY = -0.325f;

        [SerializeField, Tooltip("점프 높이 (Jump)")]
        private float m_jumpHeightY = 0.75f;

        [SerializeField, Tooltip("착지 시 반동 깊이 (Impact)")]
        private float m_impactY = -0.2f;

        #endregion

        #region 프로퍼티
        /// <summary>
        /// [설명]: 상자가 이미 열렸는지 여부입니다 (IDamageable.IsDead 구현).
        /// </summary>
        public bool IsDead => m_viewModel?.IsOpened ?? false;
        #endregion

        #region 내부 필드

        private IEventBus m_eventBus;
        private RewardChestViewModel m_viewModel;
        private EffectManager m_effectManager;
        private Collider2D m_collider;
        private bool m_isDestroying = false;
        private Vector3 m_spawnPosition;
        private readonly Vector3 m_baseScale = new Vector3(1.5f, 1.5f, 1f);

        // [최적화]: 연출용 정적 벡터 캐싱
        private static readonly Vector3 s_openSquashScale = new Vector3(1.81f, 0.99f, 1f);
        private static readonly Vector3 s_openJumpScale = new Vector3(1.0f, 2.85f, 1f);
        private static readonly Vector3 s_openImpactScale = new Vector3(1.66f, 1.05f, 1f);
        private static readonly Vector3 s_popItemPunchRotation = new Vector3(0f, 0f, 15f);
        #endregion

        #region 초기화 및 바인딩

        [Inject]
        public void Initialize(IEventBus eventBus, RewardChestViewModel viewModel, EffectManager effectManager)
        {
            if (eventBus == null) return;
            m_eventBus = eventBus;
            m_effectManager = effectManager;

            // DIP 적용: ViewModel을 DI로 주입받음 (직접 new 생성 대신)
            m_viewModel = viewModel;
            Bind();

            m_eventBus.Subscribe<OnRewardSpawned>(OnRewardSpawned);
            // Floor 시작 이벤트 수신 추가
            m_eventBus.Subscribe<OnFloorStarted>(OnFloorStarted);
        }

        private void Bind()
        {
            if (m_viewModel == null) return;

            m_viewModel.OnActivated += PlayActivateAnimation;
            m_viewModel.OnHit += PlayHitAnimation;
            m_viewModel.OnOpened += PlayOpenAnimation;
        }

        private void Start()
        {
            // [수정]: 콜라이더가 자식 오브젝트(스프라이트 렌더러와 동일 레벨)에 있는 경우를 대비해 하위 객체 탐색
            m_collider = GetComponentInChildren<Collider2D>();

            // [수정]: EnvironmentManager에서 Setup을 명시적으로 호출하므로 Start에서의 중복 호출 제거
            // Setup(m_floorIndex);
        }

        public void Setup(int floorIndex)
        {
            if (m_viewModel != null)
            {
                m_viewModel.Setup(floorIndex, m_health, transform.position, m_rewardTable);
            }
            
            if (m_spriteRenderer != null && m_closedSprite != null)
            {
                m_spriteRenderer.sprite = m_closedSprite;
            }
            
            if (m_spriteRenderer != null) m_spriteRenderer.enabled = false;
            if (m_itemIconRenderer != null) m_itemIconRenderer.enabled = false;
            
            if (m_collider != null) m_collider.enabled = false;
            
            transform.localScale = m_baseScale;
            
            // 타워 매니저에 자신을 등록 (명시적 null 체크)
            if (m_eventBus != null)
            {
                m_eventBus.Publish(new OnRewardChestRegistered(floorIndex));
            }

            // [수정]: 맵 스크롤(하강) 대응을 위해 생성 시점의 월드 좌표 저장
            m_spawnPosition = transform.position;
        }
        #endregion

        #region 유니티 생명주기

        private void OnDestroy()
        {
            if (m_eventBus != null)
            {
                m_eventBus.Unsubscribe<OnRewardSpawned>(OnRewardSpawned);
                m_eventBus.Unsubscribe<OnFloorStarted>(OnFloorStarted);
            }

            if (m_viewModel != null)
            {
                m_viewModel.OnActivated -= PlayActivateAnimation;
                m_viewModel.OnHit -= PlayHitAnimation;
                m_viewModel.OnOpened -= PlayOpenAnimation;
                // ViewModel의 IDisposable 구현을 통해 이벤트 구독 해제
                m_viewModel.Dispose();
            }

            DOTween.Kill(this);
        }

        private void OnFloorStarted(OnFloorStarted evt)
        {
            // 현재 상자 층보다 높은 층이 시작되었다면 (다음 층으로 이동했다면)
            // 이미 열린 상자이거나, 이전 층의 남겨진 상자를 정리
            if (m_viewModel != null && evt.FloorIndex > m_viewModel.FloorIndex)
            {
                DestroyChest();
            }
        }

        #region IDamageable 구현
        /// <summary>
        /// [설명]: 데미지를 입힐 때 호출됩니다. (몬스터와 동일 규격)
        /// </summary>
        /// <param name="damage">입힐 데미지 양</param>
        /// <param name="knockbackForce">밀어낼 힘 (보상 상자는 사용하지 않음)</param>
        public void TakeDamage(int damage, float knockbackForce = 0f)
        {
            // ViewModel이 없거나 이미 열린 경우 무시
            if (m_viewModel == null || m_viewModel.IsOpened) return;
            
            // 뷰모델을 통해 타격 처리 (데미지 전달)
            m_viewModel.ProcessHit(damage);
        }
        #endregion

        #endregion

        #region 연출 로직 (애니메이션)

        /// <summary>
        /// [설명]: 상자가 활성화될 때(적이 모두 처치되었을 때) 연출을 실행합니다.
        /// </summary>
        public void PlayActivateAnimation()
        {
            if (m_spriteRenderer == null) return;

            // 중복 실행 방지 및 초기화
            DOTween.Kill(transform);
            DOTween.Kill(m_spriteRenderer);

            m_spriteRenderer.enabled = true;
            if (m_collider != null) m_collider.enabled = true;

            m_spriteRenderer.color = Color.white;
            transform.localScale = Vector3.zero;

            // 나타나기 연출
            Sequence seq = DOTween.Sequence().SetTarget(this);
            seq.Append(transform.DOScale(m_baseScale, 0.6f).SetEase(Ease.OutBack));
            seq.Join(m_spriteRenderer.DOColor(Color.white, 0.3f));
            seq.Append(transform.DOPunchScale(Vector3.one * 0.15f, 0.4f, 8, 0.5f));
        }

        private void PlayHitAnimation()
        {
            transform.DOKill();
            transform.DOShakePosition(0.2f, 0.1f).SetTarget(this);
            transform.DOPunchScale(Vector3.one * 0.1f, 0.15f, 10, 1f).SetTarget(this);

            if (m_spriteRenderer != null)
            {
                m_spriteRenderer.DOKill();
                m_spriteRenderer.DOColor(Color.red, 0.1f)
                    .OnComplete(() => m_spriteRenderer.DOColor(Color.white, 0.1f));
            }
        }

        private void PlayOpenAnimation()
        {
            Debug.Log($"[CHEST_DIAGNOSTIC] PlayOpenAnimation 호출됨. SpriteRenderer={m_spriteRenderer != null}, OpenedSprite={m_openedSprite != null}, EffectManager={m_effectManager != null}");
            if (m_spriteRenderer != null && m_openedSprite != null)
            {
                m_spriteRenderer.sprite = m_openedSprite;
            }
            
            if (m_effectManager != null)
            {
                m_effectManager.PlayEffect(EffectType.ChestOpen, transform.position);
            }
            
            Vector3 startPosition = transform.localPosition;
            
            // [DOTween]: 연출 시퀀스 최적화
            Sequence openSeq = DOTween.Sequence().SetTarget(this);
            
            openSeq.Append(transform.DOLocalMoveY(startPosition.y + m_squashY, 0.16f).SetEase(Ease.InQuad));
            openSeq.Join(transform.DOScale(s_openSquashScale, 0.16f).SetEase(Ease.InQuad));
            
            openSeq.Append(transform.DOLocalMoveY(startPosition.y + m_jumpHeightY, 0.24f).SetEase(Ease.OutQuad));
            openSeq.Join(transform.DOScale(s_openJumpScale, 0.24f).SetEase(Ease.OutQuad));
            
            openSeq.Append(transform.DOLocalMoveY(startPosition.y + m_impactY, 0.18f).SetEase(Ease.InQuad));
            openSeq.Join(transform.DOScale(s_openImpactScale, 0.18f).SetEase(Ease.InQuad));
            
            openSeq.Append(transform.DOLocalMoveY(startPosition.y, 0.1f).SetEase(Ease.OutQuad));
            openSeq.Join(transform.DOScale(m_baseScale, 0.18f).SetEase(Ease.OutQuad));
        }

        private void OnRewardSpawned(OnRewardSpawned evt)
        {
            Debug.Log($"[CHEST_DIAGNOSTIC] OnRewardSpawned 수신: Key={evt.RewardKey}, EvtFloor={evt.FloorIndex}, EvtPos={evt.Position}, MyFloor={m_viewModel?.FloorIndex}, MySpawnPos={m_spawnPosition}");
            if (Application.isPlaying)
            {
                float distance = Vector3.Distance(evt.Position, m_spawnPosition);
                bool isFloorMatch = evt.FloorIndex == m_viewModel.FloorIndex;
                bool isPosMatch = distance <= 0.1f;

                Debug.Log($"[CHEST_DIAGNOSTIC] 필터 결과: FloorMatch={isFloorMatch}, Dist={distance:F4}, PosMatch={isPosMatch}");

                if (!isFloorMatch || !isPosMatch) return;
            }

            Sprite icon = ResolveRewardSprite(evt.RewardKey);
            
            // 유니티 객체 세이프 로그 (?.name 사용 시 예외 발생 가능성 대응)
            string iconName = icon != null ? icon.name : "NULL";
            string tableName = m_rewardTable != null ? m_rewardTable.name : "NULL";

            Debug.Log($"[CHEST_DIAGNOSTIC] 스프라이트 조회 결과: Key={evt.RewardKey}, Icon={iconName}, RewardTable={tableName}");
            
            if (icon == null)
            {
                Debug.LogWarning($"[RewardChestView] 스프라이트를 찾을 수 없음: {evt.RewardKey}");
                return;
            }

            if (m_itemIconRenderer != null)
            {
                Debug.Log($"[CHEST_DIAGNOSTIC] 아이템 팝업 시작: Key={evt.RewardKey}, IconRenderer.enabled={m_itemIconRenderer.enabled}");
                m_itemIconRenderer.sprite = icon;
                m_itemIconRenderer.enabled = true;
                m_itemIconRenderer.transform.localPosition = Vector3.zero;
                m_itemIconRenderer.color = new Color(1, 1, 1, 0);

                Sequence popSeq = DOTween.Sequence().SetTarget(this);
                popSeq.Append(m_itemIconRenderer.transform.DOLocalMoveY(1.5f, 0.5f).SetEase(Ease.OutBack));
                popSeq.Join(m_itemIconRenderer.DOFade(1f, 0.2f));
                popSeq.Append(m_itemIconRenderer.transform.DOPunchRotation(s_popItemPunchRotation, 0.5f, 10, 1f));
                popSeq.Join(m_itemIconRenderer.transform.DOScale(Vector3.one * 1.2f, 0.25f).SetLoops(2, LoopType.Yoyo));
                popSeq.AppendInterval(0.3f);
                popSeq.Append(m_itemIconRenderer.DOFade(0f, 0.4f));
                popSeq.OnComplete(() =>
                {
                    m_itemIconRenderer.enabled = false;
                    DestroyChest();
                });
            }
            else
            {
                Debug.LogWarning("[RewardChestView] m_itemIconRenderer가 할당되지 않았습니다.");
                DOVirtual.DelayedCall(1.0f, DestroyChest).SetTarget(this);
            }
        }

        #endregion

        #region 에디터 디버그 API

        /// <summary>
        /// [설명]: 에디터 테스트용으로 강제 활성화 연출을 실행합니다.
        /// </summary>
        public void Debug_Activate() => PlayActivateAnimation();

        /// <summary>
        /// [설명]: 에디터 테스트용으로 강제 피격 연출을 실행합니다.
        /// </summary>
        public void Debug_Hit() => PlayHitAnimation();

        /// <summary>
        /// [설명]: 에디터 테스트용으로 강제 개방 연출을 실행합니다.
        /// </summary>
        public void Debug_Open() => PlayOpenAnimation();

        /// <summary>
        /// [설명]: 에디터 테스트용으로 아이템 팝업 연출을 실행합니다.
        /// </summary>
        /// <param name="key">팝업할 아이템 키</param>
        public void Debug_PopItem(string key)
        {
            int currentFloor = m_viewModel != null ? m_viewModel.FloorIndex : 0;
            // [수정]: 디버그 호출 시에도 m_spawnPosition을 전달하여 거리 체크 통과 보장
            OnRewardSpawned(new OnRewardSpawned(key, m_spawnPosition, currentFloor));
        }

        #endregion

        #region 데이터 헬퍼

        /// <summary>
        /// [설명]: RewardKey를 받아 실제 스프라이트 리소스로 변환합니다. (View 계층 업무)
        /// </summary>
        private Sprite ResolveRewardSprite(string rewardKey)
        {
            if (m_rewardTable == null) return null;
            
            // 보상 테이블에서 키에 해당하는 아이콘 검색
            return m_rewardTable.GetSprite(rewardKey);
        }

        private void DestroyChest()
        {
            if (m_isDestroying) return;
            m_isDestroying = true;

            // 물리 판정 즉시 제거
            if (m_collider != null) m_collider.enabled = false;

            transform.DOScale(Vector3.zero, 0.4f)
                .SetEase(Ease.InBack)
                .OnComplete(() =>
                {
                    if (gameObject != null) Destroy(gameObject);
                })
                .SetTarget(this);
        }

        #endregion
    }
}
