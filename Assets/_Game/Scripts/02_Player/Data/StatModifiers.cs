using UnityEngine;

namespace TowerBreakers.Player.Data
{
    /// <summary>
    /// [기능]: 스탯 변조값 데이터 클래스
    /// </summary>
    [System.Serializable]
    public class StatModifiers
    {
        [Header("스탯 설정")]
        [Tooltip("공격력 증가량")]
        [SerializeField] private float m_attack;

        [Tooltip("방어력 증가량")]
        [SerializeField] private float m_defense;

        [Tooltip("체력 증가량")]
        [SerializeField] private float m_health;

        [Tooltip("이동속도 증가량")]
        [SerializeField] private float m_moveSpeed;

        [Tooltip("치명타 확률 (%)")]
        [SerializeField] private float m_critRate;

        [Tooltip("치명타 피해 (%)")]
        [SerializeField] private float m_critDamage;

        public float Attack => m_attack;
        public float Defense => m_defense;
        public float Health => m_health;
        public float MoveSpeed => m_moveSpeed;
        public float CritRate => m_critRate;
        public float CritDamage => m_critDamage;

        public StatModifiers()
        {
            m_attack = 0f;
            m_defense = 0f;
            m_health = 0f;
            m_moveSpeed = 0f;
            m_critRate = 0f;
            m_critDamage = 0f;
        }

        public StatModifiers(float attack, float defense, float health, float moveSpeed, float critRate, float critDamage)
        {
            m_attack = attack;
            m_defense = defense;
            m_health = health;
            m_moveSpeed = moveSpeed;
            m_critRate = critRate;
            m_critDamage = critDamage;
        }

        public static StatModifiers operator +(StatModifiers a, StatModifiers b)
        {
            return new StatModifiers(
                a.m_attack + b.m_attack,
                a.m_defense + b.m_defense,
                a.m_health + b.m_health,
                a.m_moveSpeed + b.m_moveSpeed,
                a.m_critRate + b.m_critRate,
                a.m_critDamage + b.m_critDamage
            );
        }

        public static StatModifiers operator -(StatModifiers a, StatModifiers b)
        {
            return new StatModifiers(
                a.m_attack - b.m_attack,
                a.m_defense - b.m_defense,
                a.m_health - b.m_health,
                a.m_moveSpeed - b.m_moveSpeed,
                a.m_critRate - b.m_critRate,
                a.m_critDamage - b.m_critDamage
            );
        }
    }
}
