using UnityEngine;
using TowerBreakers.Player.Data;
using TowerBreakers.Player.Service;

namespace TowerBreakers.Player.Stat
{
    /// <summary>
    /// [기능]: 플레이어 스탯 서비스 (장비 스탯을 플레이어에 적용)
    /// </summary>
    public interface IPlayerStatService
    {
        float TotalAttack { get; }
        int TotalHealth { get; }
        float AttackSpeed { get; }
        float MoveSpeed { get; }
        void ApplyEquipmentStats();
        EquipmentData GetEquippedWeapon();
        void ResetToBase();
    }

    /// <summary>
    /// [기능]: 플레이어 스탯 서비스 구현체
    /// </summary>
    public class PlayerStatService : IPlayerStatService
    {
        private readonly PlayerStatsData m_baseStats;
        private readonly IEquipmentService m_equipmentService;

        private StatModifiers m_currentModifiers;
        private float m_totalAttack;
        private int m_totalHealth;
        private float m_attackSpeed;
        private float m_moveSpeed;

        public float TotalAttack => m_totalAttack;
        public int TotalHealth => m_totalHealth;
        public float AttackSpeed => m_attackSpeed;
        public float MoveSpeed => m_moveSpeed;

        public PlayerStatService(PlayerStatsData baseStats, IEquipmentService equipmentService)
        {
            m_baseStats = baseStats;
            m_equipmentService = equipmentService;
            m_currentModifiers = new StatModifiers();
            ResetToBase();
        }

        public void ApplyEquipmentStats()
        {
            if (m_equipmentService == null || m_baseStats == null)
            {
                Debug.LogWarning("[PlayerStatService] 장비 서비스 또는 기본 스탯이 null입니다.");
                return;
            }

            m_currentModifiers = m_equipmentService.CalculateTotalStats();

            m_totalAttack = m_baseStats.BaseAttack + m_currentModifiers.Attack;
            m_totalHealth = m_baseStats.BaseHealth + (int)m_currentModifiers.Health;
            m_attackSpeed = m_baseStats.AttackSpeed;
            m_moveSpeed = m_baseStats.MoveSpeed;

            Debug.Log($"[PlayerStatService] 스탯 적용 - 공격력: {m_totalAttack}, 체력: {m_totalHealth}");
        }

        public EquipmentData GetEquippedWeapon()
        {
            return m_equipmentService?.GetEquippedItem(EquipmentType.Weapon);
        }

        public void ResetToBase()
        {
            if (m_baseStats == null)
            {
                return;
            }

            m_currentModifiers = new StatModifiers();
            m_totalAttack = m_baseStats.BaseAttack;
            m_totalHealth = m_baseStats.BaseHealth;
            m_attackSpeed = m_baseStats.AttackSpeed;
            m_moveSpeed = m_baseStats.MoveSpeed;
        }
    }
}
