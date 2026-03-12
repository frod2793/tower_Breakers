using UnityEngine;

namespace TowerBreakers.Enemy.Data
{
    public enum EnemyType
    {
        Normal,
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
        #endregion

        #region 프로퍼티
        /// <summary>
        /// [설명]: 적의 타입(일반, 엘리트, 보스)을 반환합니다.
        /// </summary>
        public EnemyType Type => m_enemyType;

        /// <summary>
        /// [설명]: 적 생성 시 사용할 오리지널 프리팹입니다.
        /// </summary>
        public GameObject EnemyPrefab => m_enemyPrefab;

        public string EnemyName => m_enemyName;
        public int Hp => m_hp;
        public float PushForce => m_pushForce;
        public float MoveSpeed => m_moveSpeed;
        public int RewardPoints => m_rewardPoints;
        #endregion
    }
}
