using UnityEngine;

namespace TowerBreakers.Enemy.Data
{
    public enum EnemyType
    {
        Normal,
        Tank,
        SupportBuffer,
        SupportShooter,
        Elite,
        Boss
    }

    /// <summary>
    /// [설명]: 적의 기본 스탯 및 설정을 저장하는 데이터 에셋입니다.
    /// </summary>
    [CreateAssetMenu(fileName = "NewEnemyData", menuName = "TowerBreakers/Enemy Data")]
    public class EnemyData : ScriptableObject
    {
        #region 에디터 설정
        [SerializeField, Tooltip("적 타입")]
        private EnemyType m_enemyType = EnemyType.Normal;

        [SerializeField, Tooltip("적 프리팹 (EnemyView 컴포넌트 포함)")]
        private GameObject m_enemyPrefab;

        [SerializeField, Tooltip("적 이름")]
        private string m_enemyName = "Skeleton";

        [SerializeField, Tooltip("최대 체력")]
        private int m_hp = 30;

        [SerializeField, Tooltip("밀기 힘 (플레이어를 밀어내는 정도)")]
        private float m_pushForce = 2.0f;

        [SerializeField, Tooltip("이동 속도")]
        private float m_moveSpeed = 1.5f;

        [SerializeField, Tooltip("처치 시 획득 포인트/재화")]
        private int m_rewardPoints = 10;

        [Header("서포터 설정")]
        [SerializeField, Tooltip("특수 능력 쿨다운 주기")]
        private float m_abilityCooldown = 5.0f;

        [SerializeField, Tooltip("버프 HP 회복량")]
        private int m_buffHealAmount = 10;

        [Header("패링 저항 설정")]
        [SerializeField, Tooltip("패링(방어) 시 밀림에 대한 저항 계수 (0 ~ 1). 예: 0.25면 밀림력이 25% 감소합니다.")]
        private float m_parryResistance = 0f;

        /// <summary>
        /// [설명]: 플레이어의 패링에 대한 적의 저항 계수 (0~1).
        /// </summary>
        public float ParryResistance => m_parryResistance;

        [SerializeField, Tooltip("투사체 프리팹")]
        private GameObject m_projectilePrefab;

        [SerializeField, Tooltip("투사체 밀기 거리")]
        private float m_projectilePushDistance = 8.0f;

        [SerializeField, Tooltip("능력 시전 모션 시간")]
        private float m_abilityDuration = 0.5f;

        [Header("보스 전용 설정")]
        [SerializeField, Tooltip("보스 페이즈 전환 HP 비율 (0.0~1.0)")]
        private float[] m_phaseThresholds = new float[] { 0.5f };

        [SerializeField, Tooltip("패턴 간 대기 시간 (초)")]
        private float m_patternDelay = 2.0f;


        [SerializeField, Tooltip("보스 기본 공격 데미지")]
        private int m_attackDamage = 10;
        #endregion

        #region 프로퍼티
        /// <summary>
        /// [설명]: 적의 타입(일반, 탱커, 서포터, 엘리트, 보스)을 반환합니다.
        /// </summary>
        public EnemyType Type => m_enemyType;

        /// <summary>
        /// [설명]: 적 생성 시 사용할 오리지널 프리닥입니다.
        /// </summary>
        public GameObject EnemyPrefab => m_enemyPrefab;

        public string EnemyName => m_enemyName;
        public int Hp => m_hp;
        public float PushForce => m_pushForce;
        public float MoveSpeed => m_moveSpeed;
        public int RewardPoints => m_rewardPoints;

        // 서포터 전용 프로퍼티
        public float AbilityCooldown => m_abilityCooldown;
        public int BuffHealAmount => m_buffHealAmount;
        public GameObject ProjectilePrefab => m_projectilePrefab;
        public float ProjectilePushDistance => m_projectilePushDistance;
        public float AbilityDuration => m_abilityDuration;
        public float[] PhaseThresholds => m_phaseThresholds;
        public float PatternDelay => m_patternDelay;
        public int AttackDamage => m_attackDamage;
        #endregion
    }
}
