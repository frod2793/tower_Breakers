using UnityEngine;

namespace TowerBreakers.Player.Data
{
    /// <summary>
    /// [기능]: 플레이어 기본 스탯 데이터
    /// </summary>
    [CreateAssetMenu(fileName = "PlayerStats_", menuName = "Data/Player/Stats")]
    public class PlayerStatsData : ScriptableObject
    {
        [Header("기본 스탯")]
        [Tooltip("기본 공격력")]
        [SerializeField] private float m_baseAttack = 10f;

        [Tooltip("기본 체력 (카운트)")]
        [SerializeField] private int m_baseHealth = 3;

        [Tooltip("공격 속도 (초당 공격 횟수)")]
        [SerializeField] private float m_attackSpeed = 1f;

        [Tooltip("이동 속도")]
        [SerializeField] private float m_moveSpeed = 5f;

        public float BaseAttack => m_baseAttack;
        public int BaseHealth => m_baseHealth;
        public float AttackSpeed => m_attackSpeed;
        public float MoveSpeed => m_moveSpeed;
    }
}
