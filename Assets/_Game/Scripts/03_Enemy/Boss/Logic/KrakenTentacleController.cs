using UnityEngine;
using DG.Tweening;
using TowerBreakers.Core.Interfaces;
using TowerBreakers.Core.Events;
using Cysharp.Threading.Tasks;
using System.Threading;

namespace TowerBreakers.Enemy.Logic
{
    /// <summary>
    /// [설명]: 소환된 크라켄 촉수를 관리하는 컨트롤러입니다.
    /// 보스의 명령(Event)을 받아 공격을 수행하거나, 파괴 시 보스에게 알립니다.
    /// DOTween 기반 애니메이션으로 레거시 타격감을 복원합니다.
    /// </summary>
    public class KrakenTentacleController : MonoBehaviour
    {
        #region 에디터 설정
        [Header("Sprite Renderers")]
        [SerializeField, Tooltip("경고 스프라이트랜더러 (플레이어에게 떨어질 위치를 알림)")]
        private SpriteRenderer m_warningSpriteRenderer;
        [SerializeField, Tooltip("촉수 스프라이트랜더러 (실제 공격 렌더러)")]
        private SpriteRenderer m_tentacleSpriteRenderer;

        [Header("Falling Attack")]
        [SerializeField, Tooltip("낙하 공격 드롭 높이")]
        private float m_fallingDropHeight = 1.0f;
        [SerializeField, Tooltip("地面까지 하강 거리 (로컬 Y 기준)")]
        private float m_fallingGroundOffset = 0.77f;
        [SerializeField, Tooltip("하강 지속 시간")]
        private float m_fallingDuration = 0.8f;
        [SerializeField, Tooltip("경고 표시 시간")]
        private float m_warningDuration = 0.5f;

        [Header("Strike Attack")]
        [SerializeField, Tooltip("시작 위치 오프셋 (플레이어 오른쪽)")]
        private float m_strikeStartOffset = 2.0f;
        [SerializeField, Tooltip("돌진 시 이동 거리 (왼쪽)")]
        private float m_strikeAttackOffset = 2.5f;
        [SerializeField, Tooltip("Y축 보정 값")]
        private float m_strikeYOffset = 0.0f;
        [SerializeField, Tooltip("돌진 지속 시간")]
        private float m_strikeDuration = 0.5f;
        [SerializeField, Tooltip("복귀 지속 시간")]
        private float m_strikeReturnDuration = 0.3f;

        [Header("Common")]
        [SerializeField, Tooltip("공격 범위 반지름")]
        private float m_attackRadius = 2.5f;
        [SerializeField, Tooltip("낙하 공격 데미지")]
        private int m_fallingDamage = 20;
        [SerializeField, Tooltip("강타 공격 데미지")]
        private int m_strikeDamage = 15;
        #endregion

        public enum ControllerMode { Obstacle, FallingAttack, StrikeAttack }

        #region 내부 필드
        private IEventBus m_eventBus;
        private Effects.EffectManager m_effectManager;
        private int m_floorIndex;
        private bool m_isInitialized = false;
        private float m_health = 100f;
        private ControllerMode m_mode = ControllerMode.Obstacle;
        private Vector3 m_targetPosition;
        private CancellationTokenSource m_cts;
        #endregion

        #region 초기화
        /// <summary>
        /// [설명]: 촉수를 초기화합니다.
        /// </summary>
        public void Initialize(int floorIndex, IEventBus eventBus, Effects.EffectManager effectManager, ControllerMode mode = ControllerMode.Obstacle)
        {
            m_floorIndex = floorIndex;
            m_eventBus = eventBus;
            m_effectManager = effectManager;
            m_mode = mode;
            m_isInitialized = true;
            m_cts = new CancellationTokenSource();

            if (m_mode == ControllerMode.Obstacle)
            {
                if (m_eventBus != null)
                {
                    m_eventBus.Subscribe<OnKrakenTentacleActionRequested>(HandleActionRequested);
                }
            }

            Debug.Log($"[KrakenTentacle] 초기화 완료: 층={m_floorIndex}, 모드={m_mode}, 위치={transform.position}");
        }

        /// <summary>
        /// [설명]: Falling/Strike 공격을 즉시 시작합니다 (이벤트 기반).
        /// </summary>
        public void StartAttack(Vector3 targetPosition)
        {
            if (m_mode == ControllerMode.FallingAttack)
            {
                ExecuteFallingAttack(targetPosition, this.GetCancellationTokenOnDestroy()).Forget();
            }
            else if (m_mode == ControllerMode.StrikeAttack)
            {
                ExecuteStrikeAttack(targetPosition, this.GetCancellationTokenOnDestroy()).Forget();
            }
        }
        #endregion

        #region 비즈니스 로직
        /// <summary>
        /// [설명]: 보스로부터 액션 요청을 받았을 때 실행됩니다.
        /// </summary>
        private void HandleActionRequested(OnKrakenTentacleActionRequested evt)
        {
            if (!m_isInitialized || evt.FloorIndex != m_floorIndex) return;

            m_cts.Cancel();
            m_cts = new CancellationTokenSource();

            switch (evt.Type)
            {
                case OnKrakenTentacleActionRequested.ActionType.Falling:
                    ExecuteFallingAttack(evt.TargetPosition, m_cts.Token).Forget();
                    break;
                case OnKrakenTentacleActionRequested.ActionType.Strike:
                    ExecuteStrikeAttack(evt.TargetPosition, m_cts.Token).Forget();
                    break;
                case OnKrakenTentacleActionRequested.ActionType.Idle:
                    KillAllTweens();
                    break;
            }
        }

        /// <summary>
        /// [설명]: Falling Tentacle (낙하 촉수) 공격을 실행합니다.
        /// 레거시 Kraken.cs의 FallingTentacle() 코루틴 로직을 이식했습니다.
        /// 플레이어 바로 위에서 Y축만 하강합니다. 사전 경고는 플레이어 위치에 표시됩니다.
        /// </summary>
        private async UniTask ExecuteFallingAttack(Vector3 targetPosition, CancellationToken ct)
        {
            Debug.Log($"[KrakenTentacle] Falling Attack 시작: {name}");
            Debug.Log($"[KrakenTentacle] 타겟 위치 (플레이어): {targetPosition}");

            float targetY = targetPosition.y - m_fallingGroundOffset;
            Debug.Log($"[KrakenTentacle] Falling - 타격 Y 위치 (로컬 Y=0.77까지): {targetY}");
            float startX = targetPosition.x;
            float startY = targetY + m_fallingDropHeight;

            Vector3 startPos = new Vector3(startX, startY, targetPosition.z);
            transform.position = startPos;

            Debug.Log($"[KrakenTentacle] Falling - 시작 위치: {startPos}, 플레이어 바로 위 Y축만 하강");

            if (m_warningSpriteRenderer != null)
            {
                m_warningSpriteRenderer.transform.position = new Vector3(targetPosition.x, targetPosition.y, targetPosition.z);
                m_warningSpriteRenderer.gameObject.SetActive(true);
                m_warningSpriteRenderer.color = new Color(1f, 1f, 1f, 0f);

                Debug.Log($"[KrakenTentacle] Falling - 경고 스프라이트 위치 (플레이어 위치): {m_warningSpriteRenderer.transform.position}");

                Sequence warningSeq = DOTween.Sequence()
                    .SetId($"FallingWarning_{GetInstanceID()}")
                    .Append(m_warningSpriteRenderer.DOFade(1f, m_warningDuration * 0.3f).SetEase(Ease.OutQuad))
                    .Append(m_warningSpriteRenderer.DOFade(0.3f, m_warningDuration * 0.3f).SetEase(Ease.InOutQuad))
                    .Append(m_warningSpriteRenderer.DOFade(1f, m_warningDuration * 0.4f).SetEase(Ease.InOutQuad))
                    .OnComplete(() => m_warningSpriteRenderer.gameObject.SetActive(false));

                await warningSeq.WithCancellation(ct);
            }
            else
            {
                Debug.LogWarning($"[KrakenTentacle] Falling - 경고 스프라이트가 할당되지 않았습니다!");
            }

            if (m_tentacleSpriteRenderer != null)
            {
                m_tentacleSpriteRenderer.gameObject.SetActive(true);
                m_tentacleSpriteRenderer.transform.position = startPos;
                Debug.Log($"[KrakenTentacle] Falling - 촉수 스프라이트 활성화, 위치: {startPos}");
            }

            if (m_eventBus != null)
            {
                m_eventBus.Publish(new OnHitEffectRequested(startPos, 0.6f, 0.3f));
            }

            Sequence fallSeq = DOTween.Sequence()
                .SetId($"Falling_{GetInstanceID()}");

            var targetTransform = m_tentacleSpriteRenderer != null ? m_tentacleSpriteRenderer.transform : transform;
            Debug.Log($"[KrakenTentacle] Falling - 하강 시작 (플레이어 위로 떨어짐), Duration={m_fallingDuration}, Ease=InExpo");

            fallSeq.Append(targetTransform.DOMoveY(targetY, m_fallingDuration)
                .SetEase(Ease.InExpo)
                .OnComplete(() =>
                {
                    Vector3 hitPos = new Vector3(startX, targetY, targetPosition.z);
                    Debug.Log($"[KrakenTentacle] Falling - 타격 발생, 위치: {hitPos}");
                    ApplyDamage(m_fallingDamage, m_attackRadius);
                    m_effectManager?.PlayEffect(Effects.EffectType.Hit, hitPos);

                    if (m_eventBus != null)
                    {
                        m_eventBus.Publish(new OnHitEffectRequested(hitPos, 0.3f, 0.15f));
                    }
                }));

            fallSeq.Append(targetTransform.DOPunchScale(new Vector3(0.3f, -0.2f, 0), 0.2f, 1, 0));

            await fallSeq.WithCancellation(ct);

            if (m_mode != ControllerMode.Obstacle)
            {
                await UniTask.Delay(500, cancellationToken: ct);
                if (gameObject != null)
                {
                    Debug.Log($"[KrakenTentacle] Falling - 공격 완료, 오브젝트 소멸 예정");
                    DOTween.To(() => targetTransform.localScale, x => targetTransform.localScale = x, Vector3.zero, 0.3f)
                        .OnComplete(() =>
                        {
                            if (gameObject != null) Destroy(gameObject);
                        }).ToUniTask(cancellationToken: ct).Forget();
                }
            }
            else
            {
                if (m_tentacleSpriteRenderer != null)
                {
                    m_tentacleSpriteRenderer.gameObject.SetActive(false);
                }
                targetTransform.localScale = Vector3.one;
                Debug.Log($"[KrakenTentacle] Falling - Obstacle 모드, 촉수 비활성화 및 스케일 복원");
            }
        }

        /// <summary>
        /// [설명]: Strike Tentacle (강타 촉수) 공격을 실행합니다.
        /// 레거시 Kraken.cs의 StrikeTentacle() 코루틴 로직을 이식했습니다.
        /// 플레이어 오른쪽에서 왼쪽으로 돌진 후 타격, 다시 오른쪽으로 복귀합니다.
        /// </summary>
        private async UniTask ExecuteStrikeAttack(Vector3 targetPosition, CancellationToken ct)
        {
            Debug.Log($"[KrakenTentacle] Strike Attack 시작: {name}");
            Debug.Log($"[KrakenTentacle] Strike - 타겟 위치 (플레이어): {targetPosition}");

            float startX = targetPosition.x + m_strikeStartOffset;
            float attackX = targetPosition.x - m_strikeAttackOffset;
            float startY = targetPosition.y - m_fallingGroundOffset + m_strikeYOffset;
            
            Vector3 startPos = new Vector3(startX, startY, targetPosition.z);
            transform.position = startPos;

            Debug.Log($"[KrakenTentacle] Strike - 시작 위치 (플레이어 오른쪽): {startPos}");
            Debug.Log($"[KrakenTentacle] Strike - 타격 위치 (왼쪽): X={attackX}");

            if (m_eventBus != null)
            {
                m_eventBus.Publish(new OnHitEffectRequested(startPos, 0.6f, 0.3f));
            }

            Sequence seq = DOTween.Sequence()
                .SetId($"Strike_{GetInstanceID()}");

            Debug.Log($"[KrakenTentacle] Strike - 왼쪽으로 돌진 시작, Duration={m_strikeDuration}");

            Vector3 attackPos = new Vector3(attackX, startY, targetPosition.z);
            seq.Append(transform.DOMove(attackPos, m_strikeDuration)
                .SetEase(Ease.InExpo)
                .OnComplete(() =>
                {
                    Debug.Log($"[KrakenTentacle] Strike - 타격 발생, 위치: {attackPos}");
                    ApplyDamage(m_strikeDamage, m_attackRadius);
                    m_effectManager?.PlayEffect(Effects.EffectType.Hit, attackPos);

                    if (m_eventBus != null)
                    {
                        m_eventBus.Publish(new OnHitEffectRequested(attackPos, 0.3f, 0.15f));
                    }
                }));

            seq.Append(transform.DOPunchScale(new Vector3(0.2f, 0.2f, 0), 0.15f, 1, 0));

            Debug.Log($"[KrakenTentacle] Strike - 오른쪽으로 복귀 시작, Duration={m_strikeReturnDuration}");

            seq.Append(transform.DOMove(startPos, m_strikeReturnDuration)
                .SetEase(Ease.OutQuad)
                .OnComplete(() =>
                {
                    Debug.Log($"[KrakenTentacle] Strike - 복귀 완료, 위치: {startPos}");
                }));

            await seq.WithCancellation(ct);

            if (m_mode != ControllerMode.Obstacle)
            {
                await UniTask.Delay(500, cancellationToken: ct);
                if (gameObject != null)
                {
                    Debug.Log($"[KrakenTentacle] Strike - 공격 완료, 오브젝트 소멸 예정");
                    DOTween.To(() => transform.localScale, x => transform.localScale = x, Vector3.zero, 0.3f)
                        .OnComplete(() =>
                        {
                            if (gameObject != null) Destroy(gameObject);
                        }).ToUniTask(cancellationToken: ct).Forget();
                }
            }
            else
            {
                transform.localScale = Vector3.one;
                Debug.Log($"[KrakenTentacle] Strike - Obstacle 모드, 스케일 복원");
            }
        }

        /// <summary>
        /// [설명]: 데미지 판정을 실행합니다. 레거시 Tentacle.cs의 Area 트리거 방식을 참고했습니다.
        /// </summary>
        private void ApplyDamage(int damage, float radius = 2.5f)
        {
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, radius);
            foreach (var hit in hits)
            {
                if (hit.CompareTag("Player"))
                {
                    var damageable = hit.GetComponent<IDamageable>();
                    if (damageable != null)
                    {
                        damageable.TakeDamage(damage);
                        Debug.Log($"[KrakenTentacle] 데미지 적용: 플레이어, 데미지={damage}");
                    }
                }
            }
        }

        /// <summary>
        /// [설명]: 모든 트윈을 안전하게 종료합니다.
        /// </summary>
        private void KillAllTweens()
        {
            DOTween.Kill($"FallingWarning_{GetInstanceID()}");
            DOTween.Kill($"Falling_{GetInstanceID()}");
            DOTween.Kill($"Strike_{GetInstanceID()}");
        }

        public void TakeDamage(float damage)
        {
            m_health -= damage;

            if (m_eventBus != null)
            {
                m_eventBus.Publish(new OnKrakenSummonDamaged(damage));
            }

            if (m_health <= 0)
            {
                Destroy(gameObject);
            }
        }
        #endregion

        #region 유니티 생명주기
        private void Start()
        {
            if (m_mode == ControllerMode.Obstacle)
            {
                AutonomousRoutine(this.GetCancellationTokenOnDestroy()).Forget();
            }
        }

        /// <summary>
        /// [설명]: 자율적으로 주변 플레이어를 감지하고 공격하는 루틴입니다.
        /// </summary>
        private async UniTask AutonomousRoutine(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested && this != null && gameObject != null)
            {
                Collider2D hit = Physics2D.OverlapCircle(transform.position, 5f);
                if (hit != null && hit.CompareTag("Player"))
                {
                    m_targetPosition = hit.transform.position;
                    await ExecuteStrikeAttack(m_targetPosition, ct);
                    await UniTask.Delay(2000, cancellationToken: ct);
                }

                await UniTask.Delay(500, cancellationToken: ct);
            }
        }

        private void OnDestroy()
        {
            m_cts?.Cancel();
            m_cts?.Dispose();
            KillAllTweens();

            if (m_mode == ControllerMode.Obstacle)
            {
                m_eventBus?.Unsubscribe<OnKrakenTentacleActionRequested>(HandleActionRequested);
            }
        }
        #endregion
    }
}
