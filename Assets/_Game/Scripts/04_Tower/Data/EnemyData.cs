using UnityEngine;
using System.Collections.Generic;

namespace TowerBreakers.Tower.Data
{
    /// <summary>
    /// [기능]: 적 데이터
    /// </summary>
    [CreateAssetMenu(fileName = "Enemy_", menuName = "Data/Enemy")]
    public class EnemyData : ScriptableObject
    {
        [Header("기본 정보")]
        [Tooltip("적 고유 ID")]
        [SerializeField] private string m_id;

        [Tooltip("적 표시 이름")]
        [SerializeField] private string m_enemyName;

        [Tooltip("적 등급 (Normal, Elite, Boss)")]
        [UnityEngine.Serialization.FormerlySerializedAs("m_grade")]
        [SerializeField] private EnemyType m_enemyType = EnemyType.Normal;

        [Header("스탯")]
        [Tooltip("체력")]
        [SerializeField] private float m_health = 100f;

        [Tooltip("공격력")]
        [SerializeField] private float m_attack = 10f;

        [Tooltip("공격 속도 (초당 공격 횟수)")]
        [SerializeField] private float m_attackSpeed = 1f;

        [Tooltip("이동 속도")]
        [SerializeField] private float m_moveSpeed = 2f;

        [Tooltip("넉백 저항성 (0: 전체 밀림, 1: 밀림 없음)")]
        [Range(0f, 1f)]
        [SerializeField] private float m_knockbackResistance = 0f;

        [Header("에디터 설정")]
        [Tooltip("적 프리펩 (SPUM 등 시각적 모델 및 컴포넌트 포함)")]
        [SerializeField] private GameObject m_prefab;

        [Header("보상")]
        [Tooltip("획득 경험치")]
        [SerializeField] private int m_experience;

        [Tooltip("드롭 골드")]
        [SerializeField] private int m_gold;

        public string ID => m_id;
        public string EnemyName => m_enemyName;
        public EnemyType Grade => m_enemyType;
        public float Health => m_health;
        public float Attack => m_attack;
        public float AttackSpeed => m_attackSpeed;
        public float MoveSpeed => m_moveSpeed;
        public float KnockbackResistance => m_knockbackResistance;
        public GameObject Prefab => m_prefab;
        public int Experience => m_experience;
        public int Gold => m_gold;

        private void OnValidate()
        {
            if (string.IsNullOrEmpty(m_id))
            {
                m_id = name;
            }
        }
    }
}
