using UnityEngine;
using TowerBreakers.Core.Interfaces;
using TowerBreakers.Effects;
using TowerBreakers.Enemy.Logic;
using System.Collections.Generic;

namespace TowerBreakers.Player.View
{
    /// <summary>
    /// [설명]: 플레이어의 가이드 미사일 발사체입니다. 사인파 이동하며 가장 가까운 적을 추적합니다.
    /// </summary>
    public class PlayerGuidedMissile : PlayerProjectile
    {
        #region 필드
        [SerializeField, Tooltip("추적 회전 속도")]
        private float m_turnSpeed = 180f;

        [SerializeField, Tooltip("사인파 진폭")]
        private float m_waveAmplitude = 0.5f;

        [SerializeField, Tooltip("사인파 빈도")]
        private float m_waveFrequency = 2.0f;

        [SerializeField, Tooltip("잔상 간격")]
        private float m_afterimageInterval = 0.1f;

        // 발사 페이즈 관련
        private float m_launchHeight;
        private float m_launchDuration;
        private float m_launchTimer;
        private bool m_isLaunching;
        private Vector3 m_launchStartPos;

        private int m_targetFloorIndex;
        private Transform m_targetTransform;
        private Collider2D m_selfCollider;
        private SpriteRenderer m_spriteRenderer;
        private HashSet<GameObject> m_hitEnemies = new HashSet<GameObject>();

        private float m_afterimageTimer;
        private Vector3 m_lastPosition;
        private float m_waveOffset;
        
        // 타격 판정 전용 (GC 방지)
        private static readonly List<Collider2D> s_enemyBuffer = new List<Collider2D>(16);
        private static ContactFilter2D s_enemyFilter;
        private static bool s_isFilterInitialized = false;
        #endregion

        #region 유니티 생명주기
        protected override void Awake()
        {
            base.Awake();
            m_selfCollider = GetComponent<Collider2D>();
            m_spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            if (m_selfCollider != null)
            {
                m_selfCollider.isTrigger = true;
                m_selfCollider.enabled = false;
            }
        }
        #endregion

        #region 공개 메서드
        public void InitializeWithWaveAndAfterimage(
            int damage, float speed, float lifetime, int ownerLayer,
            float turnSpeed, float waveAmplitude, float waveFrequency,
            float afterimageInterval, EffectManager effectManager = null, Core.Events.IEventBus eventBus = null)
        {
            Initialize(damage, speed, lifetime, ownerLayer, effectManager, eventBus);
            m_turnSpeed = turnSpeed;
            m_waveAmplitude = waveAmplitude;
            m_waveFrequency = waveFrequency;
            m_afterimageInterval = afterimageInterval;
        }

        public void SetLaunchParameters(float launchHeight, float launchDuration)
        {
            m_launchHeight = launchHeight;
            m_launchDuration = launchDuration;
        }

        public void SetFloorIndex(int floorIndex)
        {
            m_targetFloorIndex = floorIndex;
        }

        public void SetTarget(Transform target) => m_targetTransform = target;

        public override void Activate()
        {
            base.Activate();
            Debug.Log("[PlayerGuidedMissile] Activate called");
            
            m_hitEnemies.Clear();
            m_afterimageTimer = 0f;
            m_waveOffset = 0f;
            m_lastPosition = transform.position;
            
            // 발사 페이즈 진입
            m_launchStartPos = transform.position;
            m_launchTimer = 0f;
            m_isLaunching = (m_launchDuration > 0f && m_launchHeight > 0f);
            
            Debug.Log($"[PlayerGuidedMissile] IsLaunching: {m_isLaunching}, LaunchDuration: {m_launchDuration}, LaunchHeight: {m_launchHeight}");
            
            // 발사 페이즈 중에는 콜라이더 비활성화 (자기 자신 피격 방지)
            if (m_selfCollider != null)
            {
                m_selfCollider.enabled = !m_isLaunching;
                Debug.Log($"[PlayerGuidedMissile] Collider enabled set to: {!m_isLaunching}");
            }
            
            if (!m_isLaunching)
            {
                FindNewTarget();
            }
        }

        public override void Deactivate()
        {
            base.Deactivate();
            m_targetTransform = null;
            m_hitEnemies.Clear();
            m_isLaunching = false;
            if (m_selfCollider != null) m_selfCollider.enabled = false;
        }
        #endregion

        #region 내부 메서드
        protected override void OnMove()
        {
            if (m_isLaunching)
            {
                ExecuteLaunchPhase();
                return;
            }

            // 상승 완료 후: 타겟 추적 및 하강
            UpdateTargetingAndMove();
            UpdateAfterimage();
        }

        private void ExecuteLaunchPhase()
        {
            m_launchTimer += Time.deltaTime;
            float t = Mathf.Clamp01(m_launchTimer / m_launchDuration);

            // EaseOutQuad로 부드러운 상승
            float easedT = 1f - (1f - t) * (1f - t);

            Vector3 targetPos = m_launchStartPos + Vector3.up * m_launchHeight;
            transform.position = Vector3.Lerp(m_launchStartPos, targetPos, easedT);

            // 상승 중에도 타겟 미리 탐지
            if (m_targetTransform == null)
            {
                FindNewTarget();
            }

            // 상승 완료 → 추적 모드 전환
            if (t >= 1f)
            {
                m_isLaunching = false;
                Debug.Log($"[PlayerGuidedMissile] Launch phase ended. Enabling collider. Collider: {m_selfCollider}");
                if (m_selfCollider != null)
                {
                    m_selfCollider.enabled = true;
                    Debug.Log($"[PlayerGuidedMissile] Collider enabled: {m_selfCollider.enabled}");
                }
                
                // 타겟이 있으면 타겟 방향으로 회전
                if (m_targetTransform != null)
                {
                    Vector2 dir = (m_targetTransform.position - transform.position).normalized;
                    float targetAngle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
                    transform.rotation = Quaternion.Euler(0f, 0f, targetAngle);
                }
            }
        }

        private void UpdateTargetingAndMove()
        {
            Debug.Log($"[PlayerGuidedMissile] UpdateTargetingAndMove: Target={m_targetTransform?.name}, ColliderEnabled={m_selfCollider?.enabled}");
            
            // 타겟이 없거나 죽었으면 새로운 타겟 탐지
            if (m_targetTransform == null || m_targetTransform.GetComponentInParent<IDamageable>()?.IsDead == true)
            {
                FindNewTarget();
            }

            if (m_targetTransform == null)
            {
                // 타겟 없으면 아래로 직선 이동
                transform.Translate(Vector3.down * m_speed * Time.deltaTime);
                return;
            }

            // 타겟을 향해 부드럽게 회전
            Vector2 direction = (m_targetTransform.position - transform.position).normalized;
            float targetAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            float currentAngle = transform.rotation.eulerAngles.z;
            float angleDiff = Mathf.MoveTowardsAngle(currentAngle, targetAngle, m_turnSpeed * Time.deltaTime);
            transform.rotation = Quaternion.Euler(0f, 0f, angleDiff);

            // 사인파 이동 적용
            MoveWithWave();
        }

        private void FindNewTarget()
        {
            if (!s_isFilterInitialized)
            {
                s_enemyFilter.SetLayerMask(LayerMask.GetMask("Enemy"));
                s_enemyFilter.useLayerMask = true;
                s_isFilterInitialized = true;
            }

            int count = Physics2D.OverlapCircle(transform.position, 15f, s_enemyFilter, s_enemyBuffer);
            float closestDist = float.MaxValue;
            Transform newTarget = null;

            for (int i = 0; i < count; i++)
            {
                var col = s_enemyBuffer[i];
                if (col == null || col == m_selfCollider) continue;

                var enemyController = col.GetComponentInParent<TowerBreakers.Enemy.Logic.EnemyController>();
                if (enemyController == null) continue;
                if (enemyController.AssignedFloorIndex != m_targetFloorIndex) continue;

                var damageable = col.GetComponentInParent<IDamageable>();
                if (damageable == null || damageable.IsDead) continue;

                float dist = Vector2.Distance(transform.position, col.transform.position);
                if (dist < closestDist)
                {
                    closestDist = dist;
                    newTarget = col.transform;
                }
            }
            m_targetTransform = newTarget;
        }

        private void RotateTowardsTarget()
        {
            Vector2 direction;
            if (m_targetTransform != null)
            {
                direction = (m_targetTransform.position - transform.position).normalized;
            }
            else
            {
                direction = transform.right;
            }

            float targetAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            float currentAngle = transform.rotation.eulerAngles.z;
            float angleDiff = Mathf.MoveTowardsAngle(currentAngle, targetAngle, m_turnSpeed * Time.deltaTime);
            transform.rotation = Quaternion.Euler(0f, 0f, angleDiff);
        }

        private void MoveWithWave()
        {
            m_waveOffset += m_waveFrequency * Time.deltaTime;
            float waveValue = Mathf.Sin(m_waveOffset * Mathf.PI * 2) * m_waveAmplitude;

            Vector3 forward = transform.right;
            Vector3 perpendicular = new Vector3(-forward.y, forward.x, 0);

            Vector3 moveStep = (forward + perpendicular * waveValue) * (m_speed * Time.deltaTime);
            transform.position += moveStep;
        }

        private void UpdateAfterimage()
        {
            if (m_effectManager == null) return;

            m_afterimageTimer += Time.deltaTime;
            if (m_afterimageTimer >= m_afterimageInterval)
            {
                m_afterimageTimer = 0f;
                // [리팩토링]: EffectManager를 사용하여 스프라이트 잔상 연출 (미구현 시 플레이스홀더 처리 가능)
                m_effectManager.PlayEffect(EffectType.Dust, transform.position, transform.rotation);
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            Debug.Log($"[PlayerGuidedMissile] OnTriggerEnter2D called: {other.gameObject.name}, layer: {other.gameObject.layer}, collider enabled: {other.enabled}");
            
            if (!m_isInitialized) return;
            
            int layer = other.gameObject.layer;
            Debug.Log($"[PlayerGuidedMissile] Layer check: {LayerMask.GetMask("Enemy", "Object") & (1 << layer)}");
            
            if ((LayerMask.GetMask("Enemy", "Object") & (1 << layer)) == 0) return;
            if (layer == LayerMask.NameToLayer("Player")) return;

            GameObject enemyObj = other.gameObject;
            if (m_hitEnemies.Contains(enemyObj)) return;
            
            var damageable = other.GetComponentInParent<IDamageable>();
            if (damageable == null || damageable.IsDead) return;

            m_hitEnemies.Add(enemyObj);
            damageable.TakeDamage(m_damage);
            
            PlayExplosion();
            Deactivate();
        }

        private void PlayExplosion()
        {
            if (m_eventBus != null)
            {
                m_eventBus.Publish(new Core.Events.OnHitEffectRequested(transform.position, effectType: EffectType.Explosion));
            }
            else if (m_effectManager != null)
            {
                m_effectManager.PlayEffect(EffectType.Explosion, transform.position);
            }
        }

        protected override void OnLifetimeExpired()
        {
            PlayExplosion();
            Deactivate();
        }
        #endregion
    }
}
