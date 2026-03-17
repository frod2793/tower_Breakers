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

        [Tooltip("적 등급 (0: 일반, 1: 엘리트, 2: 보스)")]
        [SerializeField] private int m_grade;

        [Header("스탯")]
        [Tooltip("체력")]
        [SerializeField] private float m_health = 100f;

        [Tooltip("공격력")]
        [SerializeField] private float m_attack = 10f;

        [Tooltip("공격 속도 (초당 공격 횟수)")]
        [SerializeField] private float m_attackSpeed = 1f;

        [Tooltip("이동 속도")]
        [SerializeField] private float m_moveSpeed = 2f;

        [Header("보상")]
        [Tooltip("드롭 아이템 ID 리스트")]
        [SerializeField] private List<string> m_dropItemIds;

        [Tooltip("획득 경험치")]
        [SerializeField] private int m_experience;

        [Tooltip("드롭 골드")]
        [SerializeField] private int m_gold;

        public string ID => m_id;
        public string EnemyName => m_enemyName;
        public int Grade => m_grade;
        public float Health => m_health;
        public float Attack => m_attack;
        public float AttackSpeed => m_attackSpeed;
        public float MoveSpeed => m_moveSpeed;
        public List<string> DropItemIds => m_dropItemIds;
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
